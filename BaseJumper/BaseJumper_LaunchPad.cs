using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using Workshop;
using Mod;
using BaseJumperAPI;
using BaseJumperAPI.DependencyManager;

// I'd handle this as abstract except Ruina doesn't like that
public class BaseJumper_LaunchPad : ModInitializer {
	public const string launchpadVersion = BaseJumperAPI.Globals.Version;
	public virtual string PackageId {get; protected set;}
	private ModContent modContent;
	public ModContent ModContent { get => modContent; protected set => modContent = value; }
	private DirectoryInfo _dirInfo;
	public DirectoryInfo DirInfo { get => _dirInfo; protected set => _dirInfo = value; }
	public virtual bool IsCancelExecution => GetType() == typeof(BaseJumper_LaunchPad);
	public virtual Version Version => new Version(BaseJumperAPI.Globals.Version);
	public virtual Dependency[] Dependencies => null;

	public virtual string DepId => null;
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

	public static void LoadAssemblies() {
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		if (Array.Exists(assemblies, a => a.GetName().Name == "BaseJumperCore")) {return;}
		var assembly = assemblies
			.Where(a => a.GetName().Name == Assembly.GetExecutingAssembly().GetName().Name)
			.OrderByDescending(v => v.GetName().Version)
			.First();
		UnityEngine.Debug.Log($"BaseJumper: Loading assemblies using Version {assembly.GetName().Version}");
		assembly.GetType("BaseJumper").GetMethod("LoadAssemblies_VersionSafe").Invoke(null, null);
	}

	#region ErrorHandler
	private readonly ConcurrentQueue<(string msg, Exception e, bool warning)> queuedErrorLogs
		= new ConcurrentQueue<(string msg, Exception e, bool warning)>();
	public virtual string ModuleName => "BaseJumper_LaunchPad";

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
	static internal void HandleLog((string msg, Exception e, bool warning) tuple) {
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
	public DirectoryInfo dataDir;
	public DirectoryInfo resourceDir;
	private void AutoInitDataMethod() {
		ModContent = Singleton<ModContentManager>.Instance.GetModContent(PackageId);
		DirInfo = ModContent._dirInfo;
		dataDir = DirInfo.EnumerateDirectories("Data").FirstOrDefault();
		resourceDir = DirInfo.EnumerateDirectories("Resource").FirstOrDefault();
	}
	public virtual void BaseJumper_OnInitialize() {}
	protected virtual void AutoInit() {}
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