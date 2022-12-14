using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Workshop;
using Mod;
using Mod.XmlExtended;
using LOR_XML;
using XmlLoaders;
using HarmonyLib;
using UnityEngine;
using BaseJumperAPI;
using BaseJumperAPI.Caching;
#pragma warning restore CS0618

namespace BaseJumperAPI.Harmony {
	public static class Globals {
		public readonly static HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("LoR.uGuardian.BaseJumperCore");
	}
	public static class AssetBundlePatches {
		public static class SdResourceObjectPatch {
			internal static bool isUsingCharacterBundles = false;
			// internal static Shader defaultSpriteShader;
			public static void SetUsingCharacterBundles(BaseJumperModule caller) {
				if (!isUsingCharacterBundles) {
					Debug.Log($"{caller.ModuleName}: Adding character bundle functions");
					// defaultSpriteShader = Resources.Load<GameObject>("Prefabs/Characters/[Prefab]Appearance_Custom")
					//	.GetComponentInChildren<SpriteRenderer>().sharedMaterial.shader;
					try {
						isUsingCharacterBundles = true;
						Apply();
						#if DEBUG
						Debug.Log($"{caller.ModuleName}: Harmony patch applied");
						#endif
					} catch (AggregateException ae) {
						ae.Handle(ex => {
							if (caller != null) {
								caller.AddErrorLog(ex);
							} else {
								Debug.LogError(ex);
							}
							return true;
						});
					} catch (Exception ex) {
						if (caller != null) {
							caller.AddErrorLog(ex);
						} else {
							Debug.LogError(ex);
						}
					}
				}
			}
			internal static void Apply() {
				var harmony = Globals.harmony;
				harmony.WaitForPatches(
					harmony.PatchAsync(
						original: typeof(AssetBundleManagerRemake)
							.GetMethod(nameof(AssetBundleManagerRemake.GetSdResourceObject), AccessTools.all),
						prefix: new Action<string>(GetPrefix)
					),
					harmony.PatchAsync(
						original: typeof(AssetBundleManagerRemake)
							.GetMethod(nameof(AssetBundleManagerRemake.ReleaseSdObject), AccessTools.all),
						prefix: new Action<string>(ReleasePrefix)
					),
					harmony.PatchAsync(
						original: typeof(AssetBundleManagerRemake)
							.GetMethod(nameof(AssetBundleManagerRemake.UnloadAllCharacterCache), AccessTools.all),
						prefix: new Action(ClearPrefix)
					)
				);
			}

			public static void GetPrefix(string resourceName) {
				try {
					var instance = Singleton<AssetBundleManagerRemake>.Instance;
					if (instance._characterResourceCache.ContainsKey(resourceName)) {
						return;
					}
					instance.CacheSdResourceObject(resourceName);
				} catch (Exception ex) {
					Debug.LogException(ex);
				}
			}
			public static void ReleasePrefix(string resourceName) {
				try {
					Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdResourceObject(resourceName);
				} catch (Exception ex) {
					Debug.LogException(ex);
				}
			}
			public static void ClearPrefix() {
				try {
					Singleton<AssetBundleManagerRemake>.Instance.ClearAllCharacterCache();
				} catch (Exception ex) {
					Debug.LogException(ex);
				}
			}
		}
		private static void CacheSdResourceObject(this AssetBundleManagerRemake instance, string resourceName) {
			if (string.IsNullOrEmpty(resourceName)) {return;}
			var parts = resourceName.Split(new []{'_'}, 2);
			if (!BaseJumperCore.bundleDic.TryGetValue(parts[1], out var entry)) {
				#if AssetBundleDebug
				Debug.LogWarning($"BaseJumperCore: Key {resourceName} did not exist in asset bundle dictionary");
				#endif
				return;
			}
			var (bundle, internalPath, prerequisites) = entry;
			AssetBundle assetBundle = AssetBundleCache.GetCachedCharacter(bundle.FullName);
			if (assetBundle == null) {
				Debug.LogError($"BaseJumperCore: AssetBundle for {resourceName} failed to load");
				return;
			}
			List<string> loadedPrereqs = null;
			if (prerequisites != null) {
				loadedPrereqs = new List<string>();
				foreach (var prereq in prerequisites) {
					instance.LoadCharacterPrefab(prereq, "", out var prereqResource);
					if (!string.IsNullOrEmpty(prereqResource)) {
						loadedPrereqs.Add(prereqResource);
					} else {
						Debug.LogError(
							$"BaseJumperCore: CharacterAppearance for Prerequisite {prereqResource} of {resourceName} doesn't exist");
					}
				}
				AssetBundleCache.AddPrereq(bundle.FullName, loadedPrereqs);
			}
			GameObject gameObject = assetBundle.LoadAsset<GameObject>($"{internalPath}/{resourceName}.prefab");
			if (gameObject == null) {
				#if DEBUG
				Debug.LogError("load asset failed : " + resourceName);
				#endif
				return;
			}

			#region SanityChecking
			var appearance = gameObject.GetComponent<CharacterAppearance>();
			if (appearance.atkEffectRoot == null) {
				var newObject = new GameObject("atkEffectRoot").transform;
				newObject.SetParent(appearance.transform, false);
				appearance.atkEffectRoot = newObject;
				Debug.LogError($"BaseJumperCore: AtkEffectRoot for {resourceName} was null, a substitute has been added");
			}
			int motionRemovals = appearance._motionList.RemoveAll(x => x == null);
			if (motionRemovals > 0) {
				Debug.LogError($"BaseJumperCore: MotionList for {resourceName} had {motionRemovals} null motions in it");
			}
			foreach (var motion in appearance._motionList) {
				int spriteSetRemovals = motion.motionSpriteSet.RemoveAll(x => x == null);
				if (spriteSetRemovals > 0) {
					Debug.LogError($"BaseJumperCore: Action {motion.actionDetail} for {resourceName} had {spriteSetRemovals} null sprites in it");
				}
			}
			#endregion

			AssetBundleManagerRemake.AssetResourceCacheData resourceCacheData = new AssetBundleManagerRemake.AssetResourceCacheData {
				asset = assetBundle,
				name = resourceName,
				refCount = 10000,
				resObject = gameObject,
			};
			instance._characterResourceCache.Add(resourceName, resourceCacheData);
		}
		private static void ReleaseSdResourceObject(this AssetBundleManagerRemake instance, string resourceName) {
			if (string.IsNullOrEmpty(resourceName)) {return;}
			var parts = resourceName.Split(new []{'_'}, 2);
			if (parts.Length <= 1) {return;}
			if (!BaseJumperCore.bundleDic.TryGetValue(parts[1], out var entry))
			{
				#if AssetBundleDebug
				Debug.LogWarning($"BaseJumperCore: Key {resourceName} did not exist in asset bundle dictionary");
				#endif
				return;
			}
			var (bundle, internalPath, prerequisites) = entry;
			if (AssetBundleCache.ReleaseCachedCharacter(bundle.FullName, out int refCount)) {
				if (refCount <= 0) {
					instance._characterResourceCache.Remove(resourceName, out var data);
				}
			}
		}
		private static void ClearAllCharacterCache(this AssetBundleManagerRemake instance) {
			AssetBundleCache.ReleaseAllCharacters(instance._characterResourceCache);
		}
	}
	public static class EmotionCardArtPatches {
		
	}
	public static class OnlyPagePatches {
		public static class SetXmlInfoPatch {
			internal static bool isUsingOnlyPages = false;
			public static void SetUsingOnlyPages(BaseJumperModule caller) {
				if (!isUsingOnlyPages) {
					Debug.Log($"{caller.ModuleName}: Adding OnlyPage functions");
					try {
						isUsingOnlyPages = true;
						Globals.harmony.Patch(
							original: typeof(BookModel)
								.GetMethod(nameof(BookModel.SetXmlInfo), AccessTools.all),
							postfix: new HarmonyMethod(typeof(SetXmlInfoPatch).GetMethod(nameof(Postfix)))
						);
						#if DEBUG
						Debug.Log($"{caller.ModuleName}: Harmony patch applied");
						#endif
					} catch (AggregateException ae) {
						ae.Handle(ex => {
							if (caller != null) {
								caller.AddErrorLog(ex);
							} else {
								Debug.LogError(ex);
							}
							return true;
						});
					} catch (Exception ex) {
						if (caller != null) {
							caller.AddErrorLog(ex);
						} else {
							Debug.LogError(ex);
						}
					}
				}
			}
			public static void Postfix(BookModel __instance) {
				if (__instance.equipeffect is BookEquipEffect_Extended equipEffect) {
					var onlyCards = new List<LOR_DiceSystem.DiceCardXmlInfo>();
					__instance._onlyCards = onlyCards;
					foreach (var id in equipEffect.OnlyCard_Serialized) {
						LOR_DiceSystem.DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(id, false);
						if (cardItem != null) {
							onlyCards.Add(cardItem);
						} else {
							Debug.LogError("BaseJumperCore: OnlyPage: Page not found");
						}
					}
				}
			}
		}
	}
	public static class UIAbnormalityCategoryPanelPatches {
		internal static bool isUsingAbnormalityPages;
		public static void SetUsingAbnormalityPages(BaseJumperModule caller) {
			if (!isUsingAbnormalityPages) {
				Debug.Log($"{caller.ModuleName}: Adding abnormality page patches");
				try {
					isUsingAbnormalityPages = true;
					SetDataPatch.Apply();
					#if DEBUG
					Debug.Log($"{caller.ModuleName}: Harmony patch applied");
					#endif
				} catch (AggregateException ae) {
					ae.Handle(ex => {
						if (caller != null) {
							caller.AddErrorLog(ex);
						} else {
							Debug.LogError(ex);
						}
						return true;
					});
				} catch (Exception ex) {
					if (caller != null) {
						caller.AddErrorLog(ex);
					} else {
						Debug.LogError(ex);
					}
				}
			}
		}
		public static class SetDataPatch {
			internal static void Apply() {
				// TODO Make it expand the abno menu
				var harmony = Globals.harmony;
				/* Use this if more than one patch gets added
				harmony.WaitForPatches(
					harmony.PatchAsync(
						original: typeof(UI.UIAbnormalityCategoryPanel)
							.GetMethod(nameof(UI.UIAbnormalityCategoryPanel.SetData)),
						finalizer: new Func<Exception, Exception>(Finalizer)
					)
				);
				*/
				harmony.Patch(
					original: typeof(UI.UIAbnormalityCategoryPanel)
						.GetMethod(nameof(UI.UIAbnormalityCategoryPanel.SetData)),
					finalizer: new Func<Exception, Exception>(Finalizer)
				);
			}
			public static Exception Finalizer(Exception __exception) {
				if (__exception is ArgumentOutOfRangeException) {
					#if DEBUG
					Debug.LogException(__exception);
					#endif
					return null;
				}
				return __exception;
			}
		}
	}
}