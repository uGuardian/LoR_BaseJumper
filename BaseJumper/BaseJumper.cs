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

public partial class BaseJumper : ModInitializer {
	public virtual string PackageId {get; protected set;}
	private ModContent modContent;
	public ModContent ModContent { get => modContent; protected set => modContent = value; }
	private DirectoryInfo _dirInfo;
	public DirectoryInfo DirInfo { get => _dirInfo; protected set => _dirInfo = value; }
	#pragma warning disable CS0436
	protected virtual XMLTypes AutoInitXmls => XMLTypes.ALL;
	#pragma warning restore CS0436
	public virtual bool IsCancelExecution => GetType() == typeof(BaseJumper);
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
	public virtual string DepId => null;
	public virtual Version Version => new Version(1, 0);
	public virtual int Priority => BaseJumperAPI.Priority.Normal;
	public struct Dependency {
		public string modId;
		public string depId;
		public Version minVersion;
		public Version maxVersion;
		public bool isPostDependency;
		public bool optional;
		public ulong steamId;

		public Dependency(
			string modId,
			string depId = null,
			Version minVersion = null,
			Version maxVersion = null,
			bool isPostDependency = false,
			bool optional = false,
			ulong steamId = 0)
		{
			this.modId = modId;
			this.depId = depId;
			this.minVersion = minVersion;
			this.maxVersion = maxVersion;
			this.isPostDependency = isPostDependency;
			this.optional = optional;
			this.steamId = steamId;
		}
	}
	public virtual Dependency[] Dependencies => null;

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
	
	public override void OnInitializeMod() {
		try {
			LoadAssemblies();
			if (IsCancelExecution) {return;}
			mainThreadId = Environment.CurrentManagedThreadId;
			if (PackageId == null) {
				PackageId = GetIdFromXml(GetType().Assembly);
			} else {
				PackageId = PackageId;
			}
			RegisterDependencies();
		} catch (Exception ex) {
			AddErrorLog(ex);
		} finally {
			PushErrorLogs();
		}
	}
	private void RegisterDependencies() {
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
	}
	private void DependencyManagedInitialize() {
		try {
			AutoInitDataMethod();
			BaseJumper_OnInitialize();
			AutoInit();
		} catch (Exception ex) {
			AddErrorLog(ex);
		} finally {
			PushErrorLogs();
		}
	}
	private void AutoInitDataMethod() {
		ModContent = Singleton<ModContentManager>.Instance.GetModContent(PackageId);
		DirInfo = ModContent._dirInfo;
		dataDir = DirInfo.EnumerateDirectories("Data").FirstOrDefault();
		resourceDir = DirInfo.EnumerateDirectories("Resource").FirstOrDefault();
	}
	public virtual void BaseJumper_OnInitialize() {}
	private void AutoInit() {
		var xmlTypes = AutoInitXmls;
		if ((xmlTypes != XMLTypes.NONE) || charAssetBundleQueue != null) {
			var module = BaseJumperModule.GetModule<BaseJumperCore>(this) ??
				BaseJumperModule.NewModule(this, new BaseJumperCore(PackageId, ModContent, DirInfo));
			if (xmlTypes != XMLTypes.NONE) {
				if (dataDir == null) {
					throw new FileNotFoundException("Data directory does not exist");
				}
				module.InitCustomXmls(dataDir, (ulong)xmlTypes);
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

	public DirectoryInfo dataDir;
	public DirectoryInfo resourceDir;
	public static void LoadAssemblies() {
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		if (Array.Exists(assemblies, a => a.GetName().Name == "BaseJumperCore")) {return;}
		var assembly = assemblies
			.Where(a => a.GetName().Name == Assembly.GetExecutingAssembly().GetName().Name)
			.OrderByDescending(v => v.GetName().Version)
			.First();
		UnityEngine.Debug.Log($"BaseJumper: Loading assemblies using Version {assembly.GetName().Version}");
		assembly.GetType(nameof(BaseJumper)).GetMethod(nameof(LoadAssemblies_VersionSafe)).Invoke(null, null);
	}
	[Obsolete("Don't call this directly, call LoadAssemblies() instead", true)]
	public static void LoadAssemblies_VersionSafe() {
		new AssemblyLoader().LoadAssemblies();
	}
	private sealed class AssemblyLoader {
		public void LoadAssemblies() {
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (Array.Exists(assemblies, a => a.GetName().Name == "BaseJumperCore")) {return;}

			ErrorRemover.ErrorRemover.RemoveErrors();

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

	#region ErrorHandler
	private readonly ConcurrentQueue<(string msg, Exception e, bool warning)> queuedErrorLogs
		= new ConcurrentQueue<(string msg, Exception e, bool warning)>();
	public virtual string ModuleName => "BaseJumper";

	protected internal void AddErrorLog(string msg) {
		queuedErrorLogs.Enqueue(($"pid: ({PackageId}) {ModuleName}:{Environment.NewLine}{msg}", null, false));
		// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
	}
	protected internal void AddErrorLog(Exception ex) {
		queuedErrorLogs.Enqueue(($"pid: ({PackageId}) {ModuleName}:{Environment.NewLine}", ex, false));
		// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
	}
	protected internal void AddWarningLog(string msg) {
		queuedErrorLogs.Enqueue(($"pid: ({PackageId}) {ModuleName}:{Environment.NewLine}{msg}", null, true));
		// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
	}
	protected internal void AddWarningLog(Exception ex) {
		queuedErrorLogs.Enqueue(($"pid: ({PackageId}) {ModuleName}:{Environment.NewLine}", ex, true));
		// AddErrorLog(Singleton<ModContentManager>.Instance, $"pid: ({PackageId}) BaseJumper:{Environment.NewLine}", ex);
	}
	public int mainThreadId;
	public void PushErrorLogs() {
		if (Environment.CurrentManagedThreadId != mainThreadId) {return;}
		int count = queuedErrorLogs.Count;
		for (int i = 0; i < count; i++) {
			queuedErrorLogs.TryDequeue(out var entry);
			HandleLog(entry);
		}
		var baseJumper = BaseJumperModule.GetModule(this);
		if (baseJumper != null) {
			var coreLogs = baseJumper.GetErrorLogs();
			int count2 = coreLogs.Count;
			for (int i = 0; i < count2; i++) {
				coreLogs.TryDequeue(out var entry);
				entry.msg = $"pid: ({PackageId}) {entry.msg}";
				HandleLog(entry);
			}
		}
	}
	static void HandleLog((string msg, Exception e, bool warning) tuple) {
		var (msg, e, warning) = tuple;
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
	private static void AddErrorLog(ModContentManager manager, string msg) {
		UnityEngine.Debug.LogError(msg);
		manager.GetErrorLogs().Add($"{msg}{Environment.NewLine}");
	}

	private static void AddErrorLog(ModContentManager manager, string msg, Exception e) {
		UnityEngine.Debug.LogError(msg);
		UnityEngine.Debug.LogException(e);
		manager.GetErrorLogs().Add($"{msg}{e.Message}{Environment.NewLine}");
	}
	private static void AddWarningLog(ModContentManager manager, string msg) {
		UnityEngine.Debug.LogWarning(msg);
		manager.GetErrorLogs().Add($"<color=yellow>{msg}</color>{Environment.NewLine}");
	}

	private static void AddWarningLog(ModContentManager manager, string msg, Exception e) {
		UnityEngine.Debug.LogWarning(msg);
		UnityEngine.Debug.LogException(e);
		manager.GetErrorLogs().Add($"<color=yellow>{msg}{e.Message}</color>{Environment.NewLine}");
	}
	#endregion
	public string GetIdFromXml(Assembly assembly) {
		DirectoryInfo dirInfo = new System.IO.FileInfo(assembly.Location).Directory.Parent;
		var files = dirInfo.EnumerateFiles("StageModInfo.xml");
		while (files.FirstOrDefault() == null) {
			dirInfo = dirInfo.Parent;
			files = dirInfo.EnumerateFiles("StageModInfo.xml");
		}
		string uniqueId;
		using (var streamReader = files.First().OpenRead()) {
			NormalInvitation invInfo = (NormalInvitation) new XmlSerializer(typeof(NormalInvitation)).Deserialize(streamReader);
			if (string.IsNullOrEmpty(invInfo.workshopInfo.uniqueId) || string.Equals(invInfo.workshopInfo.uniqueId, "-1", StringComparison.Ordinal)) {
				invInfo.workshopInfo.uniqueId = dirInfo.Name;
			}
			uniqueId = invInfo.workshopInfo.uniqueId;
		}
		return uniqueId;
	}
}

namespace BaseJumperAPI {
	public static class Priority {
		public const int Last = 0;
		public const int VeryLow = 100;
		public const int Low = 200;
		public const int LowerThanNormal = 300;
		public const int Normal = 400;
		public const int HigherThanNormal = 500;
		public const int High = 600;
		public const int VeryHigh = 700;
		public const int First = 800;
	}
}