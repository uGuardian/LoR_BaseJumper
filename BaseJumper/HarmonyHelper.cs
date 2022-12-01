using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BaseJumperAPI {
	public class HarmonyHelper {
		internal HarmonyHelper(BaseJumper parent) {
			this.parent = parent;
			BaseJumper.LoadAssemblies();
		}
		public HarmonyHelper(HarmonyLib.Harmony harmony) {
			this.harmony = harmony;
			BaseJumper.LoadAssemblies();
		}

		protected readonly BaseJumper parent;
		protected HarmonyLib.Harmony harmony;

		public HarmonyLib.Harmony Harmony {
			get {
				if (harmony == null) {
					harmony = new HarmonyLib.Harmony(parent.PackageId);
				}
				return harmony;
			}
			set {
				if (harmony != null) {
					if (parent != null) {
						parent.AddErrorLog("Harmony instance for BaseJumper instance already set");
					} else {
						parent.AddErrorLog("Harmony instance for HarmonyHelper instance already set");
					}
				} else {
					harmony = value;
				}
			}
		}

		public Task<MethodInfo> PatchAsync(
			MethodBase original,
			HarmonyMethod prefix = null,
			HarmonyMethod postfix = null,
			HarmonyMethod transpiler = null,
			HarmonyMethod finalizer = null,
			HarmonyMethod ilmanipulator = null)
			=> Task.Run(()
				=> Harmony.PatchAsync(original, prefix, postfix, transpiler, finalizer, ilmanipulator));
		public Task<MethodInfo> PatchAsync(
			MethodBase original,
			MethodInfo prefix = null,
			MethodInfo postfix = null,
			MethodInfo transpiler = null,
			MethodInfo finalizer = null,
			MethodInfo ilmanipulator = null)
			=> Task.Run(()
				=> Harmony.PatchAsync(original, prefix, postfix, transpiler, finalizer, ilmanipulator));
		public Task<MethodInfo> PatchAsync(
			MethodBase original,
			Delegate prefix = null,
			Delegate postfix = null,
			Delegate transpiler = null,
			Delegate finalizer = null,
			Delegate ilmanipulator = null)
			=> Task.Run(()
				=> Harmony.PatchAsync(original, prefix, postfix, transpiler, finalizer, ilmanipulator));
		public MethodInfo[] WaitForPatches(IEnumerable<Task<MethodInfo>> list)
			=> Harmony.WaitForPatches(list);
		public MethodInfo[] WaitForPatches(params Task<MethodInfo>[] list)
			=> Harmony.WaitForPatches(list);
		public MethodInfo Patch(
			MethodBase original,
			MethodInfo prefix = null,
			MethodInfo postfix = null,
			MethodInfo transpiler = null,
			MethodInfo finalizer = null,
			MethodInfo ilmanipulator = null)
			=> Harmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator);
		public MethodInfo Patch(
			MethodBase original,
			Delegate prefix = null,
			Delegate postfix = null,
			Delegate transpiler = null,
			Delegate finalizer = null,
			Delegate ilmanipulator = null)
			=> Harmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator);
	}
}