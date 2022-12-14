using System.Collections.Generic;
using System.Linq;
using Mod;

namespace ErrorRemover {
	public class AutoRemover : ModInitializer {
		public override void OnInitializeMod() => ErrorRemover.RemoveErrors();
	}
	public static class ErrorRemover {
		static readonly string exists = "The same assembly name already exists. : "; 
		public static void RemoveErrors() {
			var dllList = new string[] {
				$"{exists}0Harmony",
				$"{exists}Mono.Cecil",
				$"{exists}Mono.Cecil.Mdb",
				$"{exists}Mono.Cecil.Pdb",
				$"{exists}Mono.Cecil.Rocks",
				$"{exists}MonoMod.RuntimeDetour",
				$"{exists}MonoMod.Utils",
				$"{exists}NAudio",
				$"{exists}BaseJumper",
			};
			Singleton<ModContentManager>.Instance.GetErrorLogs().RemoveAll(x => dllList.Any(x.Contains));
		}
		public static void RemoveErrors(string[] dllList)
			=> Singleton<ModContentManager>.Instance.GetErrorLogs().RemoveAll(x => dllList.Any(x.Contains));
		public static void RemoveErrors(List<string> dllList)
			=> Singleton<ModContentManager>.Instance.GetErrorLogs().RemoveAll(x => dllList.Any(x.Contains));
	}
}