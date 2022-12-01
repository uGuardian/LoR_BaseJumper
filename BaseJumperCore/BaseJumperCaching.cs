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
		public readonly ConcurrentDictionary<string, (AssetBundle bundle, int refCount)> characterCache =
			new ConcurrentDictionary<string, (AssetBundle bundle, int refCount)>(StringComparer.Ordinal);
		public readonly ConcurrentDictionary<AssetBundle, HashSet<string>> characterPrereqCache =
			new ConcurrentDictionary<AssetBundle, HashSet<string>>();
			
		public static void ReleaseAllCharacters(Dictionary<string, AssetBundleManagerRemake.AssetResourceCacheData> vanillaCache) {
			var cache = Instance.characterCache;
			var hashTable = cache.Values.Select(x => x.bundle).ToHashSet();
			var dicCopy = new Dictionary<string, AssetBundleManagerRemake.AssetResourceCacheData>(vanillaCache);
			foreach (var pair in dicCopy) {
				if (hashTable.Contains(pair.Value.asset)) {
					vanillaCache.Remove(pair.Key);
				}
			}
			foreach (var bundle in hashTable) {
				bundle.Unload(true);
			}
			cache.Clear();
			Instance.characterPrereqCache.Clear();
		}
		// TODO Improve thread safety
		public static AssetBundle GetCachedCharacter(FileInfo file) => GetCachedCharacter(file.FullName);
		public static AssetBundle GetCachedCharacter(string file) {
			var cache = Instance.characterCache;
			bool success = cache.TryRemove(file, out var entry);
			(AssetBundle bundle, int refCount) = entry;
			if (success) {
				if (bundle == null) {
					refCount = 0;
					bundle = AssetBundle.LoadFromFile(file);
				}
				refCount++;
				cache.TryAdd(file, (bundle, refCount));
				return bundle;
			}
			bundle = AssetBundle.LoadFromFile(file);
			refCount = 1;
			cache.TryAdd(file, (bundle, refCount));
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
			if (cache.TryRemove(file, out var entry)) {
				(AssetBundle bundle, int refCount) = entry;
				bool hasDeps = prereqCache.TryRemove(bundle, out var deps);
				if (bundle == null) {
					foreach (var dep in deps) {
						for (int i = 0; i < refCount; i++) {
							Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdObject(dep);
						}
					}
					remainingRefs = 0;
					return true;
				}
				refCount--;
				if (hasDeps) {
					foreach (var dep in deps) {
						Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdObject(dep);
					}
				}
				if (refCount > 0) {
					cache.TryAdd(file, (bundle, refCount));
					if (hasDeps) {
						prereqCache.TryAdd(bundle, deps);
					}
				} else {
					bundle.Unload(true);
				}
				remainingRefs = refCount;
				return true;
			} else {
				remainingRefs = 0;
				return false;
			}
		}
	}
}