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
using BaseJumperAPI;
using BaseJumperAPI.DependencyManager;
using System.Runtime.CompilerServices;

public class BaseJumper : BaseJumper_LaunchPad {
	public override string ModuleName => "BaseJumper";
	protected virtual bool AutoInitXmls => true;
	public override bool IsCancelExecution => GetType() == typeof(BaseJumper);
	protected Queue<(System.IO.FileInfo bundleFile, IEnumerable<string> skinNames, string internalPath)> charAssetBundleQueue;
	protected Queue<(string skinName, IEnumerable<string> dependencies)> charAssetBundleDependencyQueue;
	private HarmonyHelper harmonyHelper;
	public HarmonyHelper HarmonyHelper {
		get {
			if (harmonyHelper == null) {
				harmonyHelper = new HarmonyHelper(this);
			}
			return harmonyHelper;
		}
	}

	~BaseJumper() {
		BaseJumperModule.KillAllModule(this);
	}

	#if UnitTest
	public void UnitTestInitialize() {
		try {
			if (IsCancelExecution) {return;}
			mainThreadId = Environment.CurrentManagedThreadId;
			var depInstance = DependencyManager.Register(PackageId, DependencyManagedInitialize, DepId, Version, Priority);
			var deps = Dependencies;
			if (deps != null) {
				foreach (var dep in deps) {
					depInstance.AddDependency(dep.modId,
						dep.depId,
						dep.minVersion,
						dep.maxVersion,
						dep.isPostDependency,
						dep.optional,
						dep.steamId);
				}
			}
		} catch (Exception ex) {
			Console.WriteLine(ex);
		}
	}
	#endif

	sealed protected override void AutoInit() {
		var module = BaseJumperModule.GetModule<BaseJumperCore>(this) ??
			BaseJumperModule.NewModule(this, new BaseJumperCore(PackageId, ModContent, DirInfo));
		if (AutoInitXmls || charAssetBundleQueue != null) {
			if (AutoInitXmls) {
				if (dataDir == null) {
					throw new FileNotFoundException("Data directory does not exist");
				}
				module.InitCustomXmls(dataDir);
			}
			if (charAssetBundleQueue != null) {
				var count = charAssetBundleQueue.Count;
				for (int i = 0; i < count; i++) {
					var (bundleFile, skinNames, internalPath) = charAssetBundleQueue.Dequeue();
					module.AttachCharacterAssetBundle(bundleFile, skinNames, internalPath);
				}
				if (charAssetBundleDependencyQueue != null) {
					var count2 = charAssetBundleDependencyQueue.Count;
					for (int i = 0; i < count2; i++) {
						var (skinName, dependencies) = charAssetBundleDependencyQueue.Dequeue();
						module.AttachCharacterAssetBundleDependency(skinName, dependencies);
					}
				}
			}
		}
	}

	public void AttachCharacterAssetBundle(DirectoryInfo resourceDir, string bundleName, IEnumerable<string> skinNames, string internalPath) =>
		AttachCharacterAssetBundle(new System.IO.FileInfo($"{resourceDir.FullName}/AssetBundle/{bundleName}"), skinNames, internalPath);
	public void AttachCharacterAssetBundle(string bundlePath, IEnumerable<string> skinNames, string internalPath) =>
		AttachCharacterAssetBundle(new System.IO.FileInfo(bundlePath), skinNames, internalPath);
	public void AttachCharacterAssetBundle(System.IO.FileInfo bundleFile, IEnumerable<string> skinNames, string internalPath) {
		if (charAssetBundleQueue == null) {
			charAssetBundleQueue = new Queue<(System.IO.FileInfo bundleFile, IEnumerable<string> skinNames, string internalPath)>();
		}
		charAssetBundleQueue.Enqueue((bundleFile, skinNames, internalPath));
	}

	public void AttachCharacterAssetBundleDependencies(string skinName, params string[] dependencies) =>
		AttachCharacterAssetBundleDependencies(skinName, (IEnumerable<string>)dependencies);
	public void AttachCharacterAssetBundleDependencies(string skinName, IEnumerable<string> dependencies) {
		if (charAssetBundleDependencyQueue == null) {
			charAssetBundleDependencyQueue = new Queue<(string skinNames, IEnumerable<string> dependencies)>();
		}
		charAssetBundleDependencyQueue.Enqueue((skinName, dependencies));
	}

	[Obsolete("Don't call this directly, call LoadAssemblies() instead", true)]
	public static void LoadAssemblies_VersionSafe() {
		new AssemblyLoader().LoadAssemblies();
	}
	private sealed class AssemblyLoader {
		public void LoadAssemblies() {
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (Array.Exists(assemblies, a => a.GetName().Name == "BaseJumperCore")) {return;}

			// var dirs = Singleton<ModContentManager>.Instance._loadedContents.Select(c => c._dirInfo).AsParallel();
			/*
			var dirs = Enumerable.Empty<DirectoryInfo>().AsParallel();
			var localMods = new DirectoryInfo (Path.Combine(UnityEngine.Application.dataPath, "Mods"));
			if (localMods.Exists) {
				dirs = dirs.Concat(localMods.EnumerateDirectories().AsParallel());
			}
			var workshopMods = new DirectoryInfo(PlatformManager.Instance.GetWorkshopDirPath());
			if (workshopMods.Exists) {
				dirs = dirs.Concat(workshopMods.EnumerateDirectories().AsParallel());
			}
			*/
			Singleton<ModContentManager>.Instance._loadedContents.AsParallel()
				.Select(c => c._dirInfo)
				.SelectMany(d => d.EnumerateDirectories("BaseJumper_Modules"))
				.SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories))
				.Select(info => (AssemblyName.GetAssemblyName(info.FullName), info))
				.GroupBy(t => (t.Item1.Name, t.Item1.GetPublicKeyToken().Any()))
				.ForAll(ForAllModules);

			string logOutput = $"BaseJumper: Using modules {{{Environment.NewLine}";
			var weakAssembliesSorted = weakAssemblies.Select(x => {
				var value = x.Value;
				var (assembly, info) = value.Item1;
				var count = value.count;
				return ((string key, AssemblyName assembly, System.IO.FileInfo info, int count))
					(x.Key, assembly, info, count);
			}).ToList();
			weakAssembliesSorted.Sort(new SortByDependency());
			foreach (var entry in weakAssembliesSorted) {
				try {
					var (key, assembly, info, count) = entry;
					var loadedAssembly = Assembly.LoadFile(info.FullName);
					if (assemblies.Contains(loadedAssembly)) {
						throw new InvalidOperationException($"BaseJumper module {key} already loaded");
					}
					logOutput += $"    {key}: {assembly.Version}{Environment.NewLine}";
					GlobalInits(loadedAssembly);
				} catch (Exception ex) {
					AddErrorLog(ex);
				}
			}
			if (strongAssemblies.Any()) {
				List<AssemblyName> names = new List<AssemblyName>();
				logOutput += $"{Environment.NewLine}Strongly Named Modules: (These are discouraged but still supported){Environment.NewLine}";
				foreach (var (assembly, info) in strongAssemblies) {
					try {
						var loadedAssembly = Assembly.LoadFile(info.FullName);
						if (assemblies.Contains(loadedAssembly)) {
							throw new InvalidOperationException($"Module {assembly.FullName} already loaded{Environment.NewLine}Is it in the assemblies folder?");
						}
						names.Add(assembly);
						GlobalInits(loadedAssembly);
					} catch (Exception ex) {
						AddErrorLog(ex);
					}
				}
				foreach (var assembly in names.Distinct()) {
					UnityEngine.Debug.Log($"    {assembly.FullName}{Environment.NewLine}");
				}
			}
			logOutput += "}";
			UnityEngine.Debug.Log(logOutput);
			PushErrorLogs();
			weakAssemblies.Clear();
			strongAssemblies.Clear();
		}
		class SortByDependency : IComparer<(string key, AssemblyName assembly, System.IO.FileInfo info, int count)> {
			public int Compare((string key, AssemblyName assembly, System.IO.FileInfo info, int count) x,
				(string key, AssemblyName assembly, System.IO.FileInfo info, int count) y) {
					return -x.count.CompareTo(y.count);
			}
		}
		void GlobalInits(Assembly assembly) {
			foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(GlobalInitializer)))) {
				try {
					(Activator.CreateInstance(type) as GlobalInitializer).Initialize();
				} catch (Exception ex) {
					AddErrorLog(ex);
				}
			}
		}
		void ForAllModules(IGrouping<(string Name, bool isStrong), (AssemblyName assembly, System.IO.FileInfo info)> group) {
			var (Name, isStrong) = group.Key;
			if (isStrong) {
				foreach (var entry in group) {
					strongAssemblies.Add(entry);
				}
			} else {
				var current = GetSeed();
				bool majorVersionMismatch = false;
				bool seeded = false;
				foreach (var next in group) {
					if (!seeded) {
						current = (next, 1);
						seeded = true;
						continue;
					}
					current.count++;
					var nextVersion = next.assembly.Version;
					var currentVersion = current.Item1.assembly.Version;
					if (!majorVersionMismatch && nextVersion.Major != currentVersion.Major) {
						majorVersionMismatch = true;
					}
					if (nextVersion > currentVersion) {
						current = (next, current.count);
					}
				}

				if (!weakAssemblies.TryAdd(Name, current)) {
					AddErrorLog("Tried to add duplicate to assemblies list");
				}

				var targetAssembly = current.Item1.assembly;
				if (majorVersionMismatch) {
					AddWarningLog($"Multiple major versions of assembly {targetAssembly.Name} exist");
				}
				if (baseJumperVersion > targetAssembly.Version && string.Equals(targetAssembly.Name, "BaseJumperCore", StringComparison.Ordinal)) {
					AddWarningLog($"Current Loader version ({baseJumperVersion}) is higher than loaded Core version ({targetAssembly.Version}){Environment.NewLine}Is the modules folder included?");
				}
			}
		}
		readonly Version baseJumperVersion = new Version(Globals.Version);
		static ((AssemblyName assembly, System.IO.FileInfo info), int count) GetSeed() {
			/*
			var assembly = Assembly.GetExecutingAssembly();
			DirectoryInfo dirInfo = new System.IO.FileInfo(assembly.Location).Directory;
			DirectoryInfo dir;
			var dirs = dirInfo.EnumerateDirectories("BaseJumper_Modules");
			while ((dir = dirs.FirstOrDefault()) == null) {
				dirInfo = dirInfo.Parent;
				dirs = dirInfo.EnumerateDirectories("BaseJumper_Modules");
			}
			var file = dir.EnumerateFiles("BaseJumperCore.dll").First();
			*/
			return ((null, null), 0);
		}
		readonly ConcurrentDictionary<string, ((AssemblyName assembly, System.IO.FileInfo info), int count)> weakAssemblies
			= new ConcurrentDictionary<string, ((AssemblyName assembly, System.IO.FileInfo info), int count)>(StringComparer.Ordinal);
		readonly ConcurrentBag<(AssemblyName assembly, System.IO.FileInfo info)> strongAssemblies
			= new ConcurrentBag<(AssemblyName assembly, System.IO.FileInfo info)>();

		#region ErrorHandler_AssemblyLoader
		private readonly ConcurrentQueue<(string msg, Exception e, bool warning)> queuedErrorLogs
			= new ConcurrentQueue<(string msg, Exception e, bool warning)>();
		void AddErrorLog(string msg) {
			queuedErrorLogs.Enqueue(($"BaseJumper Assembly Loader:{Environment.NewLine}{msg}", null, false));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		void AddErrorLog(Exception ex) {
			queuedErrorLogs.Enqueue(($"BaseJumper Assembly Loader:{Environment.NewLine}", ex, false));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		void AddWarningLog(string msg) {
			queuedErrorLogs.Enqueue(($"BaseJumper Assembly Loader:{Environment.NewLine}{msg}", null, true));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		void AddWarningLog(Exception ex) {
			queuedErrorLogs.Enqueue(($"BaseJumper Assembly Loader:{Environment.NewLine}", ex, true));
			// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
		}
		void PushErrorLogs() {
			int count = queuedErrorLogs.Count;
			for (int i = 0; i < count; i++) {
				queuedErrorLogs.TryDequeue(out var entry);
				HandleLog(entry);
			}
		}
		#endregion
	}
}