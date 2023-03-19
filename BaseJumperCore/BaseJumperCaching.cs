using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Mod;
using Mod.XmlExtended;
using LOR_XML;
using XmlLoaders;
using HarmonyLib;
using UnityEngine;
using BaseJumperAPI;
using System.Threading;
#pragma warning restore CS0618

namespace BaseJumperAPI.Caching {
	public class AssetBundleCache : MonoBehaviour {
		static AssetBundleCache instance;
		public static AssetBundleCache Instance {
			get {
				if (instance == null) {
					var @object = new GameObject("BaseJumper_AssetBundleCache", typeof(AssetBundleCache));
					DontDestroyOnLoad(@object);
					instance = @object.GetComponent<AssetBundleCache>();
				}
				return instance;
			}
		}
		// The Unity API isn't thread safe, so this isn't either anymore.
		public readonly Dictionary<string, (AssetBundle bundle, int refCount)> characterCache =
			new Dictionary<string, (AssetBundle bundle, int refCount)>(StringComparer.Ordinal);
		public readonly Dictionary<AssetBundle, HashSet<string>> characterPrereqCache =
			new Dictionary<AssetBundle, HashSet<string>>();

		public static void ReleaseAllCharacters(Dictionary<string, AssetBundleManagerRemake.AssetResourceCacheData> vanillaCache) {
			var cache = Instance.characterCache;
			var prereqCache = Instance.characterPrereqCache;
			var hashTable = cache.Values.Select(x => x.bundle).ToHashSet();
			var toRemove = new List<string>(vanillaCache.Count);
			foreach (var pair in vanillaCache) {
				if (hashTable.Contains(pair.Value.asset)) {
					toRemove.Add(pair.Key);
				}
			}
			foreach (var key in toRemove) {
				vanillaCache.Remove(key);
			}
			foreach (var tuple in cache.Values) {
				tuple.bundle?.Unload(true);
			}
			foreach (var bundle in prereqCache.Keys) {
				bundle?.Unload(true);
			}
			cache.Clear();
			prereqCache.Clear();
		}
		public static AssetBundle GetCachedCharacter(FileInfo file) => GetCachedCharacter(file.FullName);
		public static AssetBundle GetCachedCharacter(string file) {
			var cache = Instance.characterCache;
			bool success = cache.TryGetValue(file, out var entry);
			(AssetBundle bundle, int refCount) = entry;
			if (success) {
				if (bundle == null) {
					refCount = 0;
					bundle = AssetBundle.LoadFromFile(file);
				}
				refCount++;
				cache[file] = (bundle, refCount);
				return bundle;
			}
			bundle = AssetBundle.LoadFromFile(file);
			refCount = 1;
			cache[file] = (bundle, refCount);
			return bundle;
		}
		public static void AddPrereq(string file, IEnumerable<string> prereqs) {
			var cache = Instance.characterCache;
			var prereqCache = Instance.characterPrereqCache;
			bool success = cache.TryGetValue(file, out var entry);
			if (success) {
				var bundle = entry.bundle;
				if (bundle == null) {
					Debug.LogError("BaseJumperCore: Main AssetBundle is not loaded (this shouldn't ever happen)");
				}
				if (!prereqCache.TryGetValue(bundle, out var deps)) {
					deps = new HashSet<string>(prereqs);
					prereqCache[bundle] = deps;
				} else {
					deps.UnionWith(prereqs);
				}
			}
		}
		public static bool ReleaseCachedCharacter(FileInfo file, out int remainingRefs) => ReleaseCachedCharacter(file.FullName, out remainingRefs);
		public static bool ReleaseCachedCharacter(string file, out int remainingRefs) {
			var cache = Instance.characterCache;
			var prereqCache = Instance.characterPrereqCache;
			if (cache.TryGetValue(file, out var entry)) {
				var bundle = entry.bundle;
				remainingRefs = entry.refCount;
				bool hasDeps = prereqCache.TryGetValue(bundle, out var deps);
				if (bundle == null) {
					foreach (var dep in deps) {
						for (int i = 0; i < remainingRefs; i++) {
							Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdObject(dep);
						}
					}
					remainingRefs = 0;
					return true;
				}
				remainingRefs--;
				if (hasDeps) {
					foreach (var dep in deps) {
						Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdObject(dep);
					}
				}
				if (remainingRefs > 0) {
					cache[file] = (bundle, remainingRefs);
				} else {
					int vanillaRefCount = 0;
					foreach (var vanillaCacheData in Singleton<AssetBundleManagerRemake>.Instance._characterResourceCache.Values) {
						if (vanillaCacheData.asset == bundle) {
							vanillaCacheData.refCount = ushort.MaxValue;
							vanillaRefCount++;
						}
					}
					if (vanillaRefCount > 1) {
						remainingRefs = vanillaRefCount - 1;
						cache[file] = (bundle, remainingRefs);
					} else {
						cache.Remove(file);
						prereqCache.Remove(bundle);
						bundle.Unload(true);
					}
				}
				return true;
			} else {
				remainingRefs = 0;
				return false;
			}
		}
	}
	public interface IAssetBundleContainer : IDisposable {
		string Name {get;}
		ConcurrentDictionary<AssetBundleContainer_RefToken, byte> RefTokens {get;}
		AssetBundle Bundle {get;}
		AssetBundleContainer_RefToken Load();
		void Unload(AssetBundleContainer_RefToken token);
	}
	public sealed class AssetBundleContainer_RefToken : IDisposable {
		internal AssetBundleContainer_RefToken(IAssetBundleContainer container) {
			Container = container;
		}
		public IAssetBundleContainer Container {get;}
		bool disposedValue;

		void Dispose(bool disposing) {
			if (!disposedValue) {
				disposedValue = true;
				Container.Unload(this);
			}
		}

		~AssetBundleContainer_RefToken() {
		    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		    Dispose(disposing: false);
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
	public abstract class CharacterBundleContainer : IAssetBundleContainer {
		public string Name {get; protected set;}
		public ConcurrentDictionary<AssetBundleContainer_RefToken, byte> RefTokens {get;}
		public AssetBundle Bundle {get; set;}

		public CharacterBundleContainer() {
			RefTokens = new ConcurrentDictionary<AssetBundleContainer_RefToken, byte>();
		}

		public abstract AssetBundleContainer_RefToken Load();
		public abstract void Unload(AssetBundleContainer_RefToken token);

		internal bool disposedValue;

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				DisposeAllTokens();
				disposedValue = true;
			}
		}

		~CharacterBundleContainer() {
		    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		    Dispose(disposing: false);
		}

		void IDisposable.Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected void DisposeAllTokens() {
			var queue = new Queue<AssetBundleContainer_RefToken>(RefTokens.Keys);
			while (queue.Count != 0) {
				queue.Dequeue().Dispose();
			}
		}
	}
	public class VanillaCharacterBundleContainer : CharacterBundleContainer {
		public static readonly ConcurrentDictionary<string, byte> overriddenVanillaCharacters =
			new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<ResourceXmlInfo, VanillaCharacterBundleContainer> containers =
			new Dictionary<ResourceXmlInfo, VanillaCharacterBundleContainer>();
		static readonly object creationLocker = new object();
		static ResourceInfoManager Manager => ResourceInfoManager.Instance;
		readonly ResourceXmlInfo info;
		public int ID => info.ID;
		private VanillaCharacterBundleContainer(ResourceXmlInfo info) : base() {
			this.info = info;
			Name = info.fileName;
			containers.Add(info, this);
		}
		internal static VanillaCharacterBundleContainer GetInstance(ResourceXmlInfo info) => GetInstance(info, false);
		private static VanillaCharacterBundleContainer GetInstance(ResourceXmlInfo info, bool isLoop) {
			if (containers.TryGetValue(info, out var container)) {
				if ((!container?.disposedValue) ?? false) {
					return container;
				} else {
					containers.Remove(info);
				}
			}
			if (isLoop) {
				return new VanillaCharacterBundleContainer(info);
			} else {
				lock (creationLocker) {
					return GetInstance(info, true);
				}
			}
		}
		internal static VanillaCharacterBundleContainer GetInstance(string name) {
			if (TryGetVanillaBundleId(name, out int vanillaId)) {
				return GetInstance(vanillaId);
			}
			if (Manager._characterResourceTableByName.TryGetValue(name, out var info)) {
				return GetInstance(info);
			}
			return null;
		}
		internal static VanillaCharacterBundleContainer GetInstance(int id) {
			if (Manager._characterResourceTableById.TryGetValue(id, out var info)) {
				return GetInstance(info);
			}
			return null;
		}
		private static bool TryGetVanillaBundleId(string name, out int vanillaId) {
			var subName = Path.GetFileNameWithoutExtension(name);
			if (subName.StartsWith("char_", StringComparison.OrdinalIgnoreCase)) {
				subName = name.Substring(5);
			}
			return int.TryParse(subName, out vanillaId);
		}
		public override AssetBundleContainer_RefToken Load() {
			if (overriddenVanillaCharacters.ContainsKey(Name)) {
				if (Bundle == null) {
					Bundle = AssetBundle.LoadFromFile(AssetBundleManagerRemake.GetCharacterResourcePath(ID));
				}
			} else {
				// REVIEW
				AssetBundleManagerRemake.Instance.GetSdResourceObject(Name);
			}
			var token = new AssetBundleContainer_RefToken(this);
			if (!RefTokens.TryAdd(token, default)) {
				throw new InvalidOperationException("Somehow a duplicate RefToken got produced");
			}
			return token;
		}
		public override void Unload(AssetBundleContainer_RefToken token) {
			token.Dispose();
			if (overriddenVanillaCharacters.ContainsKey(Name)) {
				if (RefTokens.Count <= 0) {
					Bundle?.Unload(true);
				}
			}
		}
	}
	public class CustomCharacterBundleContainer : CharacterBundleContainer {
		public override void Unload(AssetBundleContainer_RefToken token) {
			token.Dispose();
			if (RefTokens.Count <= 0) {
				Bundle.Unload(true);
			}
		}
	}
}