using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
#if !SILVERLIGHT
using System.Runtime.Serialization;
#endif
using System.Security.Permissions;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security;
#if SILVERLIGHT
using System.Core; // for System.Core.SR
#endif

// This entire system is made out of concentrated lunacy and is currently unused. I might finish it someday.

namespace BaseJumperAPI.DependencyManager {
	public class DependencyStorage :
		ICollection<DependencyBase>,
		IEnumerable<ModEntry>,
		ISet<DependencyBase>,
		IList<DependencyBase>,
		ICollection,
		IEnumerable,
		IList
	{
		// store lower 31 bits of hash code
		private const int Lower31BitMask = 0x7FFFFFFF;
		// cutoff point, above which we won't do stackallocs. This corresponds to 100 integers.
		private const int StackAllocThreshold = 100;
		// when constructing a hashset from an existing collection, it may contain duplicates, 
		// so this is used as the max acceptable excess ratio of capacity to count. Note that
		// this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
		// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
		// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
		private const int ShrinkThreshold = 3;

		private int[] modBuckets;
		private int[] depBuckets;
		private Slot[] m_slots;
		private int m_count;
		private int m_lastIndex;
		private int m_freeList;
		private readonly IEqualityComparer<DependencyBase> m_comparer;
		public IEqualityComparer<DependencyBase> Comparer => m_comparer;
		private int m_version;
		private object _syncRoot;

		#region Constructors
		public DependencyStorage()
			: this(EqualityComparer<DependencyBase>.Default) { }

		public DependencyStorage(int capacity)
			: this(capacity, EqualityComparer<DependencyBase>.Default) { }

		public DependencyStorage(IEqualityComparer<DependencyBase> comparer) {
			if (comparer == null) {
				comparer = EqualityComparer<DependencyBase>.Default;
			}

			m_comparer = comparer;
			m_lastIndex = 0;
			m_count = 0;
			m_freeList = -1;
			m_version = 0;
		}

		public DependencyStorage(IEnumerable<DependencyBase> collection)
			: this(collection, EqualityComparer<DependencyBase>.Default) { }

		/// <summary>
		/// Implementation Notes:
		/// Since resizes are relatively expensive (require rehashing), this attempts to minimize 
		/// the need to resize by setting the initial capacity based on size of collection. 
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="comparer"></param>
		public DependencyStorage(IEnumerable<DependencyBase> collection, IEqualityComparer<DependencyBase> comparer)
			: this(comparer) {
			if (collection == null) {
				throw new ArgumentNullException("collection");
			}
			Contract.EndContractBlock();

			if (collection is DependencyStorage otherAsStorageSet)
			{
				CopyFrom(otherAsStorageSet);
			}
			else
			{
				// to avoid excess resizes, first set size based on collection's count. Collection
				// may contain duplicates, so call TrimExcess if resulting hashset is larger than
				// threshold
				int suggestedCapacity = !(collection is ICollection<DependencyBase> coll) ? 0 : coll.Count;
				Initialize(suggestedCapacity);

				this.UnionWith(collection);

				if (m_count > 0 && m_slots.Length / m_count > ShrinkThreshold)
				{
					TrimExcess();
				}
			}
		}

		// Initializes the HashSet from another HashSet with the same element type and
		// equality comparer.
		private void CopyFrom(DependencyStorage source) {
			int count = source.m_count;
			if (count == 0) {
				// As well as short-circuiting on the rest of the work done,
				// this avoids errors from trying to access otherAsHashSet.m_buckets
				// or otherAsHashSet.m_slots when they aren't initialized.
				return;
			}

			int capacity = source.modBuckets.Length;
			int threshold = HashHelpers.ExpandPrime(count + 1);
	
			if (threshold >= capacity) {
				modBuckets = (int[])source.modBuckets.Clone();
				depBuckets = (int[])source.depBuckets.Clone();
				m_slots = (Slot[])source.m_slots.Clone();
	
				m_lastIndex = source.m_lastIndex;
				m_freeList = source.m_freeList;
			}
			else {
				int lastIndex = source.m_lastIndex;
				Slot[] slots = source.m_slots;
				Initialize(count);
				int index = 0;
				for (int i = 0; i < lastIndex; ++i)
				{
					int modHash = slots[i].modHash;
					if (modHash >= 0)
					{
						AddValue(index, modHash, slots[i].depHash, slots[i].value);
						++index;
					}
				}
				Debug.Assert(index == count);
				m_lastIndex = index;
			}
			m_count = count;
		}

		public DependencyStorage(int capacity, IEqualityComparer<DependencyBase> comparer)
			: this(comparer)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity");
			}
			Contract.EndContractBlock();
	
			if (capacity > 0)
			{
				Initialize(capacity);
			}
		}
		#endregion

		public DependencyBase this[int index] {
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public int Count => m_count;
		public bool IsReadOnly => false;
		public object SyncRoot {
			get {
				if (this._syncRoot == null)
				{
					System.Threading.Interlocked.CompareExchange<object>(
						ref this._syncRoot, new object(), null);
				}
				return this._syncRoot;
			}
		}
		public bool IsSynchronized => false;
		public bool IsFixedSize => false;


		public bool Add(ModEntry item)
		{
			if (modBuckets == null || depBuckets == null) {
				Initialize(0);
			}
 
			var (modHash, depHash) = InternalGetHashCodes(item);
			int modBucket = modHash % modBuckets.Length;
			for (int i = modBuckets[modHash % modBuckets.Length] - 1; i >= 0; i = m_slots[i].next) {
				if (m_slots[i].modHash == modHash && m_comparer.Equals(m_slots[i].value, item)) {
					return false;
				}
			}
			int depBucket = depHash % depBuckets.Length;
			for (int i = depBuckets[depHash % depBuckets.Length] - 1; i >= 0; i = m_slots[i].next) {
				if (m_slots[i].depHash == depHash && m_comparer.Equals(m_slots[i].value, item)) {
					return false;
				}
			}
 
			int index;
			if (m_freeList >= 0) {
				index = m_freeList;
				m_freeList = m_slots[index].next;
			}
			else {
				if (m_lastIndex == m_slots.Length) {
					IncreaseCapacity();
					// this will change during resize
					modBucket = modHash % modBuckets.Length;
					depBucket = depHash % depBuckets.Length;
				}
				index = m_lastIndex;
				m_lastIndex++;
			}
			m_slots[index].modHash = modHash;
			m_slots[index].depHash = depHash;
			m_slots[index].value = item;
			m_slots[index].next = m_buckets[bucket] - 1;
			m_buckets[bucket] = index + 1;
			m_count++;
			m_version++;
 
			return true;
		}
		bool ISet<DependencyBase>.Add(DependencyBase item)
		{
			if (item is ModEntry entry) {
				return Add(entry);
			} else {
				throw WrongValueTypeArgumentException(item, typeof(ModEntry), null);
			}
		}
		void ICollection<DependencyBase>.Add(DependencyBase item)
		{
			if (item is ModEntry entry) {
				Add(entry);
			} else {
				throw WrongValueTypeArgumentException(item, typeof(ModEntry), null);
			}
		}

		private (int modHash, int depHash) InternalGetHashCodes(ModEntry item) {
			if (item == null) {
				return (0, 0);
			} 
			item.GetHashCodes(out int modHash, out int depHash);
			return (modHash & Lower31BitMask, depHash & Lower31BitMask);
		}

		public void Clear() {
			if (m_lastIndex > 0) {
				Debug.Assert(modBuckets != null, "modBuckets was null but m_lastIndex > 0");
				Debug.Assert(depBuckets != null, "depBuckets was null but m_lastIndex > 0");
 
				// clear the elements so that the gc can reclaim the references.
				// clear only up to m_lastIndex for m_slots 
				Array.Clear(m_slots, 0, m_lastIndex);
				Array.Clear(modBuckets, 0, modBuckets.Length);
				Array.Clear(depBuckets, 0, depBuckets.Length);
				m_lastIndex = 0;
				m_count = 0;
				m_freeList = -1;
			}
			m_version++;
		}

		public bool Contains(DependencyBase item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(DependencyBase[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}
		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public void ExceptWith(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public int IndexOf(DependencyBase item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, ModEntry item)
		{
			throw new NotImplementedException();
		}
		void IList<DependencyBase>.Insert(int index, DependencyBase item)
		{
			if (item is ModEntry entry) {
				Insert(index, entry);
			} else {
				throw WrongValueTypeArgumentException(item, typeof(ModEntry), null);
			}
		}

		public void IntersectWith(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public bool IsProperSubsetOf(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public bool IsProperSupersetOf(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSubsetOf(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSupersetOf(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public bool Overlaps(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public bool Remove(DependencyBase item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public bool SetEquals(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public void SymmetricExceptWith(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public void UnionWith(IEnumerable<DependencyBase> other)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<ModEntry> GetEnumerator()
		{
			throw new NotImplementedException();
		}
		IEnumerator<DependencyBase> IEnumerable<DependencyBase>.GetEnumerator()
		{
			throw new NotImplementedException();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		// Add value at known index with known hash code. Used only
		// when constructing from another HashSet.
		private void AddValue(int index, int modHash, int depHash, DependencyBase value) {
			int modBucket = modHash % modBuckets.Length;
			int depBucket = depHash % depBuckets.Length;

			Debug.Assert(m_freeList == -1);
			m_slots[index].modHash = modHash;
			m_slots[index].depHash = depHash;
			m_slots[index].value = value;
			modBuckets[modBucket] = index + 1;
			depBuckets[depBucket] = index + 1;
		}
		/// <summary>
		/// Sets the capacity of this list to the size of the list (rounded up to nearest prime),
		/// unless count is 0, in which case we release references.
		/// 
		/// This method can be used to minimize a list's memory overhead once it is known that no
		/// new elements will be added to the list. To completely clear a list and release all 
		/// memory referenced by the list, execute the following statements:
		/// 
		/// list.Clear();
		/// list.TrimExcess(); 
		/// </summary>
		public void TrimExcess() {
			Debug.Assert(m_count >= 0, "m_count is negative");

			if (m_count == 0) {
				// if count is zero, clear references
				modBuckets = null;
				depBuckets = null;
				m_slots = null;
				m_version++;
			}
			else {
				Debug.Assert(modBuckets != null && depBuckets != null, "buckets were null but m_count > 0");

				// similar to IncreaseCapacity but moves down elements in case add/remove/etc
				// caused fragmentation
				int newSize = HashHelpers.GetPrime(m_count);
				Slot[] newSlots = new Slot[newSize];
				int[] newModBuckets = new int[newSize];
				int[] newDepBuckets = new int[newSize];

				// move down slots and rehash at the same time. newIndex keeps track of current 
				// position in newSlots array
				int newIndex = 0;
				for (int i = 0; i < m_lastIndex; i++) {
					if (m_slots[i].modHash >= 0) {
						newSlots[newIndex] = m_slots[i];

						// rehash
						int modBucket = newSlots[newIndex].modHash % newSize;
						newModBuckets[modBucket] = newIndex + 1;
						int depBucket = newSlots[newIndex].depHash % newSize;
						newDepBuckets[depBucket] = newIndex + 1;

						newIndex++;
					}
				}

				Debug.Assert(newSlots.Length <= m_slots.Length, "capacity increased after TrimExcess");

				m_lastIndex = newIndex;
				m_slots = newSlots;
				modBuckets = newModBuckets;
				depBuckets = newDepBuckets;
				m_freeList = -1;
			}
		}

		/// <summary>
		/// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
		/// greater than or equal to capacity.
		/// </summary>
		/// <param name="capacity"></param>
		private void Initialize(int capacity) {
			Debug.Assert(modBuckets == null && depBuckets == null, "Initialize was called but buckets were non-null");

			int size = HashHelpers.GetPrime(capacity);

			modBuckets = new int[size];
			depBuckets = new int[size];
			m_slots = new Slot[size];
		}

		internal struct Slot {
			internal int modHash;		// Lower 31 bits of hash code, -1 if unused
			internal int depHash;		// Lower 31 bits of hash code, -1 if unused
			internal int next;			// Index of next entry, -1 if last
			internal DependencyBase value;
		}

		#region IList implementations
			object IList.this[int index]
			{
				get => this[index];
				set {
					try {
						this[index] = (DependencyBase)value;
					}
					catch (InvalidCastException ex) {
						throw WrongValueTypeArgumentException(value, typeof(DependencyBase), ex);	 
					}
				}
			}

			int IList.Add(object item)
			{
				try {
					Add((ModEntry)item);
				}
				catch (InvalidCastException ex) {
					throw WrongValueTypeArgumentException(item, typeof(ModEntry), ex);	 
				}
	
				return Count - 1;
			}
			bool IList.Contains(object item)
			{
				if(IsCompatibleObject(item)) {
					return Contains((DependencyBase)item);
				}
				return false;
			}
			int IList.IndexOf(object item)
			{
				if(IsCompatibleObject(item)) {
					return IndexOf((DependencyBase)item);
				}
				return -1;
			}
			void IList.Insert(int index, object item)
			{
				try { 
					Insert(index, (ModEntry)item);
				}
				catch (InvalidCastException ex) { 
					throw WrongValueTypeArgumentException(item, typeof(DependencyBase), ex);			
				}
			}
			void IList.Remove(object item)
			{
				if(IsCompatibleObject(item)) {
					Remove((DependencyBase)item);
				}
			}

			static bool IsCompatibleObject(object value) {
				if (value is DependencyBase || value == null) {
					return true;
				}
				return false;
			}
			static ArgumentException WrongValueTypeArgumentException(object value, Type targetType, Exception innerException) =>
				new ArgumentException($"The value \"{value}\" is not of type \"{targetType}\" and cannot be used in this collection.", "value", innerException);
			#endregion
	}
	public enum LoadState {
		NONE,
	}
}