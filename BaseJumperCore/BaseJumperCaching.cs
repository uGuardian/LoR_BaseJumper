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
}