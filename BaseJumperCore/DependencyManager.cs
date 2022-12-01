using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using HarmonyLib;

namespace BaseJumperAPI.DependencyManager {
	public static class DependencyManager {
		static bool initialized = false;
		public class DependencyManager_Patcher : GlobalInitializer {
			public override void Initialize() {
				Harmony.Globals.harmony.Patch(
				original: new Action<List<Mod.ModContentInfo>, List<Mod.ModContentInfo>>(
					Singleton<Mod.ModContentManager>.Instance.SaveSelectionData).Method,
				prefix: new Action(AddByMode_PreFix));
				base.Initialize();
			}
		}
		public static void AddByMode_PreFix() => FinalizeDependencySystem();
		// Maybe finish DependencyStorage someday
		// static readonly DependencyStorage modList = new DependencyStorage();
		static readonly List<ModEntry> modList = new List<ModEntry>();

		public static ModEntry Register(
			string modId, Action initializer, string depId = null, Version version = null, int priority = 400)
		{
			var result = new ModEntry(modId, depId, initializer, version, priority);
			modList.Add(result);
			return result;
		}

		static bool CheckAddToLoadOrder(ModEntry entry, List<DependencyBase> orderedList, bool checkOptional) {
			int earliestPostDepIndex = -1;
			foreach (var dep in entry.dependencies) {
				var isPostDependency = dep.isPostDependency;
				bool checkIfContains = orderedList.Contains(dep);
				if (!isPostDependency && !checkIfContains) {
					if (!dep.optional || !checkOptional) {
						return false;
					}
				}
				if (isPostDependency && checkIfContains) {
					int depIndex = orderedList.IndexOf(dep);
					if (earliestPostDepIndex <= 0 || earliestPostDepIndex > depIndex) {
						earliestPostDepIndex = depIndex;
					}
				}
			}
			if (earliestPostDepIndex >= 0) {
				orderedList.Insert(earliestPostDepIndex, entry);
			} else {
				orderedList.Add(entry);
			}
			return true;
		}
		static int ComparePriority(ModEntry x, ModEntry y) => x.priority.CompareTo(y.priority);
		public static void FinalizeDependencySystem() {
			if (initialized) {return;}
			// TODO Optimize the hell out of this thing and improve the entire system.
			// Why don't you just handle everything in the sorting mechanism? Because it doesn't work!
			modList.Sort(ComparePriority);
			var orderedList = new List<DependencyBase>();
			var recheckList = new List<ModEntry>();
			var recheckRemoveList = new List<ModEntry>();
			foreach (var entry in modList) {
				if (!CheckAddToLoadOrder(entry, orderedList, true)) {
					recheckList.Add(entry);
				}
				foreach (var recheck in recheckList) {
					if (CheckAddToLoadOrder(recheck, orderedList, true)) {
						recheckRemoveList.Add(recheck);
					}
				}
				recheckList.RemoveAll(recheckRemoveList.Contains);
				recheckRemoveList.Clear();
			}
			while (recheckList.Count > 0) {
				int lastCount = recheckList.Count;
				foreach (var recheck in recheckList) {
					if (CheckAddToLoadOrder(recheck, orderedList, false)) {
						recheckRemoveList.Add(recheck);
					}
				}
				recheckList.RemoveAll(recheckRemoveList.Contains);
				recheckRemoveList.Clear();
				if (recheckList.Count == lastCount) {
					break;
				}
			}
			var orderedModList = orderedList.Cast<ModEntry>();
			int lastCount2;
			do {
				lastCount2 = orderedList.Count;
				foreach (var mod in orderedModList) {
					foreach (var dep in mod.dependencies) {
						if (dep.optional) {
							continue;
						}
						if (!orderedList.Contains(dep)) {
							orderedList.Remove(mod);
						}
					}
				}
				orderedModList = orderedList.Cast<ModEntry>();
			} while (orderedList.Count != lastCount2);
			if (recheckList.Count > 0) {
				var sb = new System.Text.StringBuilder("BaseJumper: The following mods are missing dependencies: {");
				sb.AppendLine();
				foreach (var entry in recheckList) {
					sb.Append("	");
					sb.Append(entry.modId);
					sb.AppendLine(": {");
					foreach (var dep in entry.dependencies) {
						if (dep.optional) {continue;}
						sb.Append("		");
						sb.AppendLine(dep.modId);
					}
					sb.AppendLine("	}");
				}
				sb.AppendLine("}");
				#if !UnitTest
				Singleton<Mod.ModContentManager>.Instance.AddErrorLog(sb.ToString());
				#else
				Console.WriteLine(sb.ToString());
				#endif
			}
			#if UnitTest
			foreach (var entry in orderedModList) {
				Console.WriteLine(entry.modId);
			}
			#endif
			foreach (var entry in orderedModList) {
				#if DEBUG && !UnitTest
				UnityEngine.Debug.Log($"BaseJumper: Running Initializer for {entry.modId}");
				#endif
				try {
					entry.initializer.Invoke();
				}
				#if UnitTest
				catch (System.Security.SecurityException) {
					Console.WriteLine("Unity Logging Error");
				}
				#endif
				catch (Exception ex) {
					#if !UnitTest
					Singleton<Mod.ModContentManager>.Instance.AddErrorLog(
						$"BaseJumper Initializer for {entry.modId} has catastrophically failed with exception: ", ex);
					#else
					Console.WriteLine(
						$"BaseJumper Initializer for {entry.modId} has catastrophically failed with exception: {ex}");
					#endif
				}
				#if DEBUG && !UnitTest
				UnityEngine.Debug.Log($"BaseJumper: Finished Initializer");
				#endif
			}
			initialized = true;
		}
	}
}