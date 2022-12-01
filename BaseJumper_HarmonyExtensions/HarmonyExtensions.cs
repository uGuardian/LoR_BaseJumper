using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HarmonyLib
{
	public static class BaseJumper_HarmonyExtensions {
		public static Task<MethodInfo> PatchAsync(this Harmony harmony,
			MethodBase original,
			HarmonyMethod prefix = null,
			HarmonyMethod postfix = null,
			HarmonyMethod transpiler = null,
			HarmonyMethod finalizer = null,
			HarmonyMethod ilmanipulator = null)
			=> Task.Run(() => harmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator));
		public static Task<MethodInfo> PatchAsync(this Harmony harmony,
			MethodBase original,
			MethodInfo prefix = null,
			MethodInfo postfix = null,
			MethodInfo transpiler = null,
			MethodInfo finalizer = null,
			MethodInfo ilmanipulator = null)
			=> Task.Run(() => harmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator));
		public static Task<MethodInfo> PatchAsync(this Harmony harmony,
			MethodBase original,
			Delegate prefix = null,
			Delegate postfix = null,
			Delegate transpiler = null,
			Delegate finalizer = null,
			Delegate ilmanipulator = null)
			=> Task.Run(() => harmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator));
		public static MethodInfo[] WaitForPatches(this Harmony _, IEnumerable<Task<MethodInfo>> list) => Task.WhenAll(list).Result;
		public static MethodInfo[] WaitForPatches(this Harmony _, params Task<MethodInfo>[] list) => Task.WhenAll(list).Result;
		public static MethodInfo Patch(this Harmony harmony,
			MethodBase original,
			MethodInfo prefix = null,
			MethodInfo postfix = null,
			MethodInfo transpiler = null,
			MethodInfo finalizer = null,
			MethodInfo ilmanipulator = null)
		{
			HarmonyMethod prefixHarmony = null;
			HarmonyMethod postfixHarmony = null;
			HarmonyMethod transpilerHarmony = null;
			HarmonyMethod finalizerHarmony = null;
			HarmonyMethod ilmanipulatorHarmony = null;

			if (prefix != null) {
				prefixHarmony = new HarmonyMethod(prefix);
			}
			if (postfix != null) {
				postfixHarmony = new HarmonyMethod(prefix);
			}
			if (transpiler != null) {
				transpilerHarmony = new HarmonyMethod(prefix);
			}
			if (finalizer != null) {
				finalizerHarmony = new HarmonyMethod(prefix);
			}
			if (ilmanipulator != null) {
				ilmanipulatorHarmony = new HarmonyMethod(prefix);
			}

			return harmony.Patch(original,
				prefixHarmony,
				postfixHarmony,
				transpilerHarmony,
				finalizerHarmony,
				ilmanipulatorHarmony);
		}
		public static MethodInfo Patch(this Harmony harmony,
			MethodBase original,
			Delegate prefix = null,
			Delegate postfix = null,
			Delegate transpiler = null,
			Delegate finalizer = null,
			Delegate ilmanipulator = null)
		{
			HarmonyMethod prefixHarmony = null;
			HarmonyMethod postfixHarmony = null;
			HarmonyMethod transpilerHarmony = null;
			HarmonyMethod finalizerHarmony = null;
			HarmonyMethod ilmanipulatorHarmony = null;

			if (prefix != null) {
				prefixHarmony = new HarmonyMethod(prefix.Method);
			}
			if (postfix != null) {
				postfixHarmony = new HarmonyMethod(prefix.Method);
			}
			if (transpiler != null) {
				transpilerHarmony = new HarmonyMethod(prefix.Method);
			}
			if (finalizer != null) {
				finalizerHarmony = new HarmonyMethod(prefix.Method);
			}
			if (ilmanipulator != null) {
				ilmanipulatorHarmony = new HarmonyMethod(prefix.Method);
			}

			return harmony.Patch(original,
				prefixHarmony,
				postfixHarmony,
				transpilerHarmony,
				finalizerHarmony,
				ilmanipulatorHarmony);
		}
	}
}