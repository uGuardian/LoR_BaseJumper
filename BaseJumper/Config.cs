using System;
using System.IO;
using UnityEngine;
using GameSave;
using Mod;

public partial class BaseJumper : ModInitializer {
	public static T InitConfig<T>() where T : Singleton<T>, IConfig, new() {
		T instance = Singleton<T>.Instance;
		InitConfig(instance);
		return instance;
	}
	public static void InitConfig<T>(T instance) where T : IConfig {
		// This is for ConfigAPI implementation.
		if (Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name.Contains("ConfigAPI"))) {
			// If ConfigAPI is loaded, use it.
			Init_ConfigAPI.Init(instance);
		} else {
			// If ConfigAPI isn't loaded, use the normal loader.
			StandaloneConfig.Load(instance);
			StandaloneConfig.EchoAll(instance);
		}
	}

	// This entire class is for ConfigAPI hooking.
	internal static class Init_ConfigAPI {
		internal static void Init<T>(T instance) where T : IConfig {
			var configName = instance.Name;
			var subDirectory = instance.SubDirectory;
			if (string.IsNullOrEmpty(subDirectory)) {
				ConfigAPI.Init(configName, instance);
			} else {
				ConfigAPI.Init($"{subDirectory}/{configName}", instance);
			}
		}
	}
	
	public interface IConfig {
		int Version {get;}
		string Name {get;}
		string SubDirectory {get;}
	}

	// This class is used to process all configs, even if you have multiple ones.
	internal static class StandaloneConfig {
		internal static void Load<T>(T instance) where T : IConfig {
			var configName = instance.Name;
			var subDirectory = instance.SubDirectory;
			DirectoryInfo directory;
			if (string.IsNullOrEmpty(subDirectory)) {
				directory = Directory.CreateDirectory(SaveManager.GetFullPath("ModConfigs"));
			} else {
				directory = Directory.CreateDirectory(SaveManager.GetFullPath($"ModConfigs/{subDirectory}"));
			}
			var configFile = new FileInfo($"{directory.FullName}/{configName}.ini");
			var exists = configFile.Exists;
			string config;
			using (var stream = configFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
				if (exists) {
					try {
						var reader = new StreamReader(stream);
						config = reader.ReadToEnd();
						JsonUtility.FromJsonOverwrite(config, instance);
						reader.DiscardBufferedData();
					} catch (Exception ex) {
						Debug.LogError($"Error reading config file {configName}");
						Debug.LogException(ex);
						Singleton<ModContentManager>.Instance.AddErrorLog($"{configName}: ini file invalid, resetting it");
					}
				}
				stream.Seek(0, SeekOrigin.Begin);
				config = JsonUtility.ToJson(instance, true);
				var writer = new StreamWriter(stream);
				writer.Write(config);
				writer.Flush();
				stream.SetLength(stream.Position);
			}
		}
		internal static void EchoAll<T>(T instance) where T : IConfig =>
			Debug.Log($"{instance.Name}.ini: {JsonUtility.ToJson(instance, true)}");
	}
}