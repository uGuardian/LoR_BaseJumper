using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using HarmonyLib;

namespace BaseJumperAPI.DependencyManager {
		/*
		public class DependencyComparer : IComparer<ModEntry> {
			public int Compare(ModEntry x, ModEntry y) {
				Dependency output;
				x.GetHashCodes(out int xHash, out int? xBoth);
				x.GetHashCodes(out int yHash, out int? yBoth);
				if (xBoth != null && y.dependencies.TryGetValue(xBoth.GetValueOrDefault(), out output) && x == output) {
					return output.isPostDependency ? 1 : -1;
				}
				if (yBoth != null && x.dependencies.TryGetValue(yBoth.GetValueOrDefault(), out output) && y == output) {
					return output.isPostDependency ? -1 : 1;
				}
				if (y.dependencies.TryGetValue(xHash, out output) && x == output) {
					return output.isPostDependency ? 1 : -1;
				}
				if (x.dependencies.TryGetValue(yHash, out output) && y == output) {
					return output.isPostDependency ? -1 : 1;
				}
				return y.priority - x.priority;
			}
		}
		*/

		public class DependencyBase : IEquatable<DependencyBase> {
			public readonly string modId;
			public readonly string depId;
			public readonly int modHash;
			public readonly int depHash;

			public DependencyBase(string modId, string depId = null) {
				this.modId = modId;
				this.depId = depId;

				GenerateHashCodes(modId, depId, out int modHash, out int depHash);
				this.modHash = modHash;
				this.depHash = depHash;
			}

			public bool Equals(DependencyBase other) {
				if (string.Equals(modId, other.modId, StringComparison.Ordinal)) {
					if (string.IsNullOrEmpty(depId) || string.IsNullOrEmpty(other.depId)) {
						return true;
					} else {
						if (string.Equals(depId, other.depId, StringComparison.Ordinal)) {
							return true;
						}
					}
				}
				return false;
			}
			public override bool Equals(object obj)
			{
				if (obj != null && obj is DependencyBase target) {
					return Equals(target);
				}
				return false;
			}

			public static bool operator ==(DependencyBase a, DependencyBase b) => a.Equals(b);
			public static bool operator !=(DependencyBase a, DependencyBase b) => !a.Equals(b);
			
			public override int GetHashCode() {
				return modHash;
			}
			public void GetHashCodes(out int modHash, out int depHash) {
				modHash = this.modHash;
				depHash = this.depHash;
			}
			public static void GenerateHashCodes(string modId, string depId, out int modHash, out int depHash) {
				modHash = StringComparer.Ordinal.GetHashCode(modId);
				depHash = StringComparer.Ordinal.GetHashCode(depId ?? string.Empty);
			}
		}

		public class ModEntry : DependencyBase {
			public readonly Action initializer;
			public readonly Version version;
			public readonly int priority;
			public readonly HashSet<Dependency> dependencies;

			public ModEntry(string modId,
				string depId,
				Action initializer,
				Version version,
				int priority) : base(modId, depId)
			{
				this.initializer = initializer;
				this.version = version;
				this.priority = priority;

				dependencies = new HashSet<Dependency>();
			}

			public void AddDependency(
				string modId,
				string depId = null,
				Version minVersion = null,
				Version maxVersion = null,
				bool isPostDependency = false,
				bool optional = false,
				ulong steamId = 0)
			{
			dependencies.Add(new Dependency(modId,
				depId,
				minVersion,
				maxVersion,
				isPostDependency,
				optional,
				steamId));
			}
		}
		public class Dependency : DependencyBase {
			public readonly Version minVersion;
			public readonly Version maxVersion;
			public readonly bool isPostDependency;
			public readonly bool optional;
			public readonly ulong steamId;

			public Dependency(string modId,
				string depId = null,
				Version minVersion = null,
				Version maxVersion = null,
				bool isPostDependency = false,
				bool optional = false,
				ulong steamId = 0) : base(modId, depId)
			{
				this.minVersion = minVersion;
				this.maxVersion = maxVersion;
				this.isPostDependency = isPostDependency;
				this.optional = optional;
				this.steamId = steamId;
			}
		}
}