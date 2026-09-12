using BepInEx;
using HarmonyLib;

namespace SmartCraftStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.zellds.smartcraftstorage";
        public const string PluginName = "SmartCraft-Storage";
        public const string PluginVersion = "0.1.3";

        internal static Harmony HarmonyInstance;

        private void Awake()
        {
            SmartCraftStorage.Config.ModConfig.Bind(Config);
            SmartCraftStorage.Stations.StationConfig.Bind(Config);
            SmartCraftStorage.AnimalFeeder.AnimalFeederConfig.Bind(Config);
            SmartCraftStorage.Hotkeys.HotkeyConfig.Bind(Config);

            SmartCraftStorage.Translations.ModTranslations.Setup();

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
