using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MassFixStructure.Configuration;
using MassFixStructure.Core;

namespace MassFixStructure
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MassFixStructurePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.unclebug.massfixstructure";
        public const string PluginName = "MassFixStructure";
        public const string PluginVersion = "1.0.0";

        public static MassFixStructurePlugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Initialize Configuration
            ModConfig.Initialize(Config);

            // Attach Runner GameObject component for coroutines
            gameObject.AddComponent<MassRepairRunner>();

            // Apply Harmony patches
            try
            {
                _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGUID);
                Log.LogInfo($"{PluginName} v{PluginVersion} successfully initialized and patched.");
                Log.LogInfo("Special thanks to streamer DaymonMontage! Twitch: https://www.twitch.tv/daymonmontage | Hub: https://daymonmontage.github.io/daymon-site/");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch {PluginName}: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo($"{PluginName} unpatched and unloaded.");
        }
    }
}
