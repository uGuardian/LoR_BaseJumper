using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using BaseJumperAPI.Harmony;
#pragma warning restore CS0618

namespace BaseJumperAPI {
	public static class Globals {
		public const string Version = "0.0.6.0";
	}
	public static class Extensions {
		public static string TrimEnd(this string source, string value) {
			if (!source.EndsWith(value))
				return source;
			return source.Remove(source.LastIndexOf(value));
		}
	}

	public abstract class GlobalInitializer {
		public virtual void Initialize() {} 
	}
	public interface IInstanceInitializer {
		void Initialize();
	}

	public abstract class BaseJumperModule {
		public static T NewModule<T>(object instance, T module) where T : BaseJumperModule {
			if (!dic.TryGetValue(instance, out var list)) {
				list = new List<BaseJumperModule>();
				dic.Add(instance, list);
			}
			list.Add(module);
			if (module is IInstanceInitializer moduleInit) {
				moduleInit.Initialize();
			}
			return module;
		}
		public static BaseJumperModule GetModule(object instance) {
			if (dic.TryGetValue(instance, out var list)) {
				var result = list.First();
				if (list.Count > 1) {
					result.AddWarningLog($"{instance} has more than one module");
				}
				return result;
			}
			return null;
		}
		public static T GetModule<T>(object instance) where T : BaseJumperModule {
			if (dic.TryGetValue(instance, out var list)) {
				var results = list.FindAll(e => e.GetType() == typeof(T));
				var result = results.First();
				if (results.Count > 1) {
					result.AddWarningLog($"{instance} has more than one module of type {nameof(T)}");
				}
				return (T)result;
			}
			return null;
		}
		[Obsolete("Call the generic version or KillAllModule", true)]
		public static void KillModule(object instance) => KillAllModule(instance);
		public static void KillModule<T>(object instance) where T : BaseJumperModule {
			if (dic.TryGetValue(instance, out var list)) {
				var results = list.FindAll(e => e.GetType() == typeof(T));
				var result = results.First();
				if (results.Count > 1) {
					result.AddWarningLog($"{instance} has more than one module of type {nameof(T)}");
				}
				list.Remove(result);
			}
		}
		public static void KillAllModule(object instance) {
			dic.Remove(instance);
		}
		public readonly static Dictionary<object, List<BaseJumperModule>> dic = new Dictionary<object, List<BaseJumperModule>>();
		protected internal sealed class SanityCheck : ModInitializer {
			public sealed override void OnInitializeMod() {
				try {
					var assembly = Assembly.GetExecutingAssembly();
					DirectoryInfo dirInfo = new System.IO.FileInfo(assembly.Location).Directory;
					var files = dirInfo.EnumerateFiles("StageModInfo.xml");
					while (files.FirstOrDefault() == null) {
						dirInfo = dirInfo.Parent;
						files = dirInfo.EnumerateFiles("StageModInfo.xml");
					}
					string uniqueId;
					using (var streamReader = files.First().OpenRead()) {
						NormalInvitation invInfo = (NormalInvitation) new XmlSerializer(typeof(NormalInvitation)).Deserialize(streamReader);
						if (string.IsNullOrEmpty(invInfo.workshopInfo.uniqueId) || invInfo.workshopInfo.uniqueId == "-1") {
							invInfo.workshopInfo.uniqueId = dirInfo.Name;
						}
						uniqueId = invInfo.workshopInfo.uniqueId;
						}
					Singleton<ModContentManager>.Instance.GetErrorLogs().Add(
						$"pid: ({uniqueId}) BaseJumperModule:{Environment.NewLine}BaseJumper Modules CANNOT be in Assemblies{Environment.NewLine}");
				} catch {
					Singleton<ModContentManager>.Instance.GetErrorLogs().Add(
						$"pid: (NULL) BaseJumperModule:{Environment.NewLine}BaseJumper Modules CANNOT be in Assemblies{Environment.NewLine}");
				}
			}
		}
		public readonly string packageId;
		public readonly ModContent modContent;
		public readonly DirectoryInfo _dirInfo;

		private BaseJumperModule() {}
		public BaseJumperModule(string packageId) {
			this.modContent = Singleton<ModContentManager>.Instance.GetModContent(packageId);
			this.packageId = modContent._itemUniqueId;
			this._dirInfo = modContent._dirInfo;
		}
		public BaseJumperModule(ModContent modContent) {
			this.modContent = modContent;
			this.packageId = modContent._itemUniqueId;
			this._dirInfo = modContent._dirInfo;
		}
		public BaseJumperModule(string packageId, ModContent modContent, DirectoryInfo dirInfo) {
			this.packageId = packageId;
			this.modContent = modContent;
			_dirInfo = dirInfo;
		}

		#region ErrorHandler
		private readonly ConcurrentQueue<(string msg, Exception e, bool warning)> queuedErrorLogs
			= new ConcurrentQueue<(string msg, Exception e, bool warning)>();
		public abstract string ModuleName {get;}
		public void AddErrorLog(string msg) {
			queuedErrorLogs.Enqueue(($"{ModuleName}:{Environment.NewLine}{msg}", null, false));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		public void AddErrorLog(Exception ex) {
			queuedErrorLogs.Enqueue(($"{ModuleName}:{Environment.NewLine}", ex, false));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		public void AddWarningLog(string msg) {
			queuedErrorLogs.Enqueue(($"{ModuleName}:{Environment.NewLine}{msg}", null, true));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		public void AddWarningLog(Exception ex) {
			queuedErrorLogs.Enqueue(($"{ModuleName}:{Environment.NewLine}", ex, true));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		public ConcurrentQueue<(string msg, Exception e, bool warning)> GetErrorLogs() {
			return queuedErrorLogs;
		}

		/*
		public int mainThreadId;
		public void PushErrorLogs() {
			if (Environment.CurrentManagedThreadId != mainThreadId) {return;}
			int count = queuedErrorLogs.Count;
			for (int i = 0; i < count; i++) {
				var (msg, e, warning) = queuedErrorLogs.Dequeue();
				switch (warning) {
					case false when e != null:
						AddErrorLog(Singleton<ModContentManager>.Instance, msg, e);
						break;
					case true when e != null:
						AddWarningLog(Singleton<ModContentManager>.Instance, msg, e);
						break;
					case false when e == null:
						AddErrorLog(Singleton<ModContentManager>.Instance, msg);
						break;
					case true when e == null:
						AddWarningLog(Singleton<ModContentManager>.Instance, msg);
						break;
				}
			}
		}
		private static void AddErrorLog(ModContentManager manager, string msg)
			=> manager.GetErrorLogs().Add($"{msg}{Environment.NewLine}");
		private static void AddErrorLog(ModContentManager manager, string msg, Exception e) {
			UnityEngine.Debug.LogException(e);
			manager.GetErrorLogs().Add($"{msg}{e.Message}{Environment.NewLine}");
		}
		private static void AddWarningLog(ModContentManager manager, string msg)
			=> manager.GetErrorLogs().Add($"<color=yellow>{msg}</color>{Environment.NewLine}");
		private static void AddWarningLog(ModContentManager manager, string msg, Exception e) {
			UnityEngine.Debug.LogException(e);
			manager.GetErrorLogs().Add($"<color=yellow>{msg}{e.Message}</color>{Environment.NewLine}");
		}
		*/
		#endregion
	}
	public class BaseJumperCore : BaseJumperModule {
		public BaseJumperCore(string packageId) : base(packageId) {}
		public BaseJumperCore(ModContent modContent) : base(modContent) {}
		public BaseJumperCore(string packageId, ModContent modContent, DirectoryInfo dirInfo) : base(packageId, modContent, dirInfo) {}
		private Dictionary<Type, (XmlSerializer serializer, (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions)> xmlTypes;
		readonly XmlLoaders.DefaultLoader xmlLoader = new XmlLoaders.DefaultLoader();
		readonly XmlLoaders.CustomLoader xmlLoaderCustom = new XmlLoaders.CustomLoader();
		protected virtual Dictionary<Type, (XmlSerializer serializer, (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions)> XmlTypes {
			get {
				if (xmlTypes == null) {
					(XmlSerializer serializer, (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions) entry;
					xmlTypes = new Dictionary<Type, (XmlSerializer serializer, (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions)>{
						// Obsolete All-In-One Deserializer
					//	{XmlSerializerEntry<AllRoots>>
					//		((xmlLoader.Loads, null), out entry), entry},

						// Default Serializers
					//	{XmlSerializerEntry<StageXmlRoot, List<PassiveXmlInfo>>
					//		((xmlLoader.LoadStage, null), out entry), entry},
					//	{XmlSerializerEntry<EnemyUnitClassRoot>
					//		((xmlLoader.LoadEnemyUnit, null), out entry), entry},
						{XmlSerializerEntry<BookXmlRoot_Extended, List<BookXmlInfo_Extended>>
							((xmlLoader.LoadEquipPage, xmlLoader.AddEquipPageByMod), out entry), entry},
					//	{XmlSerializerEntry<BookXmlRoot>
					//		((xmlLoader.LoadEquipEnemyPage, out entry), entry},
					//	{XmlSerializerEntry<CardDropTableXmlRoot>
					//		((xmlLoader.LoadCardDropTable, null), out entry), entry},
					//	{XmlSerializerEntry<BookUseXmlRoot>
					//		((xmlLoader.LoadDropBook, null), out entry), entry},
					//	{XmlSerializerEntry<DiceCardXmlRoot_Extended>
					//		((xmlLoader.LoadCardInfo, null), out entry), entry},
					//	{XmlSerializerEntry<DeckXmlRoot>
					//		((xmlLoader.LoadDeck, null), out entry), entry},
					//	{XmlSerializerEntry<BattleDialogRoot>
					//		((xmlLoader.LoadDialog, null), out entry), entry},
					//	{XmlSerializerEntry<BookDescRoot>
					//		((xmlLoader.LoadBookStory, null), out entry), entry},
					//	{XmlSerializerEntry<PassiveXmlInfo>
					//		((xmlLoader.LoadPassive, null), out entry), entry},

						// Custom Serializers
					//	{XmlSerializerEntry<EmotionCardXmlRoot_Extended, List<EmotionCardXmlInfo_Extended>>
					//		((xmlLoaderCustom.LoadEmotionCard, xmlLoaderCustom.AddEmotionCardByMod), out entry), entry},
					};
				}
				return xmlTypes;
			}
		}

		public class DelegateContainer<T, O> {
			public readonly Func<BaseJumperModule, object, object> loadAction;
			public readonly Action<BaseJumperModule, object> addAction;
			public (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) Actions => (loadAction, addAction);
			public DelegateContainer((Func<BaseJumperModule, T, O> loadAction, Action<BaseJumperModule, O> addAction) actions) {
				var (loadAction, addAction) = actions;
				this.loadAction = new Func<BaseJumperModule, object, object>((module, input) => loadAction(module, (T)input));
				this.addAction = new Action<BaseJumperModule, object>((module, input) => addAction(module, (O)input));
			}
			public DelegateContainer(Func<BaseJumperModule, T, O> loadAction, Action<BaseJumperModule, O> addAction) {
				this.loadAction = new Func<BaseJumperModule, object, object>((module, input) => loadAction(module, (T)input));
				this.addAction = new Action<BaseJumperModule, object>((module, input) => addAction(module, (O)input));
			}
		}
		public static Type XmlSerializerEntry<T, O>((Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions,
			out (XmlSerializer serializer, (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions) entry) {
				var type = typeof(T);
				if (actions.loadAction == null) {
					actions.loadAction = DummyFunctions.DummyFunc;
				}
				if (typeof(IXmlExtended).IsAssignableFrom(type)) {
					entry = (new XmlSerializer(type,
						(XmlAttributeOverrides)type.GetMethod("GetAttributeOverrides").Invoke(null, new object[0])), actions);
					return type;
				} else {
					entry = (new XmlSerializer(type), actions);
					return type;
				}
		}
		public static Type XmlSerializerEntry<T, O>((Func<BaseJumperModule, T, O> loadAction, Action<BaseJumperModule, O> addAction) actions,
			out (XmlSerializer serializer, (Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions) entry) {
				var type = typeof(T);
				var container = new DelegateContainer<T, O>(actions).Actions;
				if (typeof(IXmlExtended).IsAssignableFrom(type)) {
					entry = (new XmlSerializer(type,
						(XmlAttributeOverrides)type.GetMethod("GetAttributeOverrides").Invoke(null, new object[0])), container);
					return type;
				} else {
					entry = (new XmlSerializer(type), container);
					return type;
				}
		}
		public static class DummyFunctions {
			public static object DummyFunc(BaseJumperModule _, object input) {return input;}
			public static T DummyFunc<T>(BaseJumperModule _, T input) {return input;}
			public static O DummyFunc<T, O>(BaseJumperModule _, T input) where T : O {return input;}
		}
		public void AddXmlSerializer<T, O>((Func<BaseJumperModule, object, object> loadAction, Action<BaseJumperModule, object> addAction) actions) {
			XmlTypes.Add(XmlSerializerEntry<T, O>(actions, out var entry), entry);
		}
		public void AddXmlSerializer<T, O>((Func<BaseJumperModule, T, O>, Action<BaseJumperModule, O> addAction) actions) {
			XmlTypes.Add(XmlSerializerEntry<T, O>(actions, out var entry), entry);
		}
		public void InitCustomXmls(DirectoryInfo dataDir) {
			Task.Run(async () => await InitCustomXmlsAsync(dataDir)).Wait();
			foreach (var action in Finalizers) {
				try {
					action(this);
				} catch (Exception ex) {
					AddErrorLog(ex);
				}
			}
		}

		readonly public static ConcurrentDictionary<string, (System.IO.FileInfo bundle, string internalPath, List<string> prerequisites)> bundleDic =
			new ConcurrentDictionary<string, (System.IO.FileInfo bundle, string internalPath, List<string> prerequisites)>(StringComparer.Ordinal);
		public void AttachCharacterAssetBundle(DirectoryInfo resourceDir, string bundleName, IEnumerable<string> skinNames, string internalPath) =>
			AttachCharacterAssetBundle(new System.IO.FileInfo($"{resourceDir.FullName}/AssetBundle/{bundleName}"), skinNames, internalPath);
		public void AttachCharacterAssetBundle(string bundlePath, IEnumerable<string> skinNames, string internalPath) =>
			AttachCharacterAssetBundle(new System.IO.FileInfo(bundlePath), skinNames, internalPath);
		public void AttachCharacterAssetBundle(System.IO.FileInfo bundleFile, IEnumerable<string> skinNames, string internalPath) {
			AssetBundlePatches.SdResourceObjectPatch.SetUsingCharacterBundles(this);
			internalPath = internalPath.TrimEnd('/');
			var bundleDic = BaseJumperCore.bundleDic;
			// NOTE Double check to ensure this is atomic
			// TODO Figure out LorId support
			string debugOutput = $"{ModuleName}: Attaching assetbundle {bundleFile.Name} to skins: {{{Environment.NewLine}";
			if (bundleFile.Exists) {
				foreach (var entry in skinNames) {
					var skin = entry.TrimEnd(".prefab");
					bool foundFile = false;
					if (bundleDic.TryAdd($"{skin}", (bundleFile, internalPath, null))) {
						foundFile |= true;
					}
					foreach (var gender in Enum.GetNames(typeof(Gender))) {
						if (bundleDic.TryAdd($"{skin}_{gender}", (bundleFile, internalPath, null))) {
							foundFile |= true;
						}
					}
					if (foundFile) {
						debugOutput += $"	{skin}{Environment.NewLine}";
					}
				}
			} else {
				AddErrorLog($"Assetbundle {bundleFile.Name} does not exist");
			}
			UnityEngine.Debug.Log(debugOutput += "}");
		}
		public void AttachCharacterAssetBundleDependency(string skin, params string[] dependencies) =>
			AttachCharacterAssetBundleDependency(skin, (IEnumerable<string>)dependencies);
		public void AttachCharacterAssetBundleDependency(string skin, IEnumerable<string> dependencies) {
			// NOTE Double check to ensure this is atomic
			var debugOutput = new System.Text.StringBuilder();
			debugOutput.Append($"{ModuleName}: Attaching dependencies to skins: {{{Environment.NewLine}");
			bool foundSkin = false;
			(System.IO.FileInfo bundle, string internalPath, List<string> prerequisites) tuple;
			var bundleDic = BaseJumperCore.bundleDic;
			if (bundleDic.TryRemove(skin, out tuple)) {
				AddPrereqs();
				bundleDic.TryAdd(skin, tuple);
				foundSkin |= true;
			}
			foreach (var gender in Enum.GetNames(typeof(Gender))) {
				if (bundleDic.TryRemove($"{skin}_{gender}", out tuple)) {
					AddPrereqs();
					bundleDic.TryAdd($"{skin}_{gender}", tuple);
					foundSkin |= true;
				}
			}
			if (foundSkin) {
				debugOutput.Append($"	{skin}{Environment.NewLine}");
				UnityEngine.Debug.Log(debugOutput.Append("}"));
			} else {
				AddErrorLog($"Skin {skin} does not exist");
			}

			void AddPrereqs() {
				List<string> list;
				if (tuple.prerequisites == null) {
					list = new List<string>();
					tuple.prerequisites = list;
				} else {
					list = tuple.prerequisites;
				}
				list.AddRange(dependencies);
			}
		}

		private List<Action<BaseJumperModule>> finalizers;
		public virtual List<Action<BaseJumperModule>> Finalizers {
			get {
				if (finalizers == null) {
					finalizers = new List<Action<BaseJumperModule>> {
						xmlLoader.AddEquipPageByModFinalizer,
						// xmlLoaderCustom.AddEmotionCardByModFinalizer,
					};
				}
				return finalizers;
			}
		}

		public override string ModuleName => nameof(BaseJumperCore);

		protected async virtual Task InitCustomXmlsAsync(DirectoryInfo dataDir) {
			var allFiles = dataDir.EnumerateFiles("*", SearchOption.AllDirectories);
			var loadTasks = new List<Task<List<(object xml, Action<BaseJumperModule, object> addAction)>>>();
			var exceptions = new List<Exception>();
			foreach (var file in allFiles) {
				loadTasks.Add(Task.Run(() => DeserializeFile(file)));
			}
			foreach (var task in loadTasks) {
				try {
					var resultList = await task;
					foreach (var (xml, addAction) in resultList) {
						addAction(this, xml);
					}
				} catch (AggregateException a) {
					a.Handle((ex) => {
						if (ex is ArgumentException ar) {
							ex = new DuplicateKeyException(ar);
						}
						AddErrorLog(ex);
						return true;
						}
					);
				} catch (NotImplementedException ex) {
					AddWarningLog(ex);
					// exceptions.Add(ex);
				} catch (Exception ex) {
					AddErrorLog(ex);
					// exceptions.Add(ex);
				}
			}
		}
		protected class DuplicateKeyException : ArgumentException {
			const string DefaultMessage = "Duplicate Keys exist within the same mod!";
			public DuplicateKeyException(Exception innerException) : base(DefaultMessage, innerException) {}
			public DuplicateKeyException(string message, Exception innerException) : base(message, innerException) {}
		}
		List<(object result, Action<BaseJumperModule, object> addAction)> DeserializeFile(System.IO.FileInfo file) {
			var list = new List<(object result, Action<BaseJumperModule, object> addAction)>();
			using (var reader = XmlReader.Create(file.OpenRead())) {
				foreach (var variant in XmlTypes) {
					var (serializer, actions) = variant.Value;
					if (serializer.CanDeserialize(reader)) {
						var (loadAction, addAction) = actions;
						var result = loadAction(this, serializer.Deserialize(reader));
						list.Add((result, addAction));
					}
				}
			}
			return list;
		}
	}
}