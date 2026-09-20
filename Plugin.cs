using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using SmartCraftStorage.Storage.Runtime;
using SmartCraftStorage.Storage.UI;

namespace SmartCraftStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    // Optional: if Epic Loot is present, we register as one of its inventory providers
    // (see Integrations/EpicLootProvider.cs). Soft so the mod still loads fine without
    // it; declaring it still orders our Awake() after Epic Loot's when both are present.
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    // Network inventory coordination requires matching clients and server.
    // Gameplay settings are administered by the server.
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.zellds.smartcraftstorage";
        public const string PluginName = "SmartCraft-Storage";
        public const string PluginVersion = "0.7.7";

        internal static Harmony HarmonyInstance;

        private void Awake()
        {
            SmartCraftStorage.Config.ModConfig.Bind(Config);
            SmartCraftStorage.Config.StorageConfig.Bind(Config);
            SmartCraftStorage.Stations.StationConfig.Bind(Config);
            SmartCraftStorage.AnimalFeeder.AnimalFeederConfig.Bind(Config);
            SmartCraftStorage.Hotkeys.HotkeyConfig.Bind(Config);
            SmartCraftStorage.PlantHarvest.PlantHarvestConfig.Bind(Config);

            SmartCraftStorage.Translations.ModTranslations.Setup();
            TerminalTranslations.Setup();
            TerminalRegistration.Setup();
            StorageFacade.Service = new StorageService(SmartCraftStorage.Config.StorageConfig.Snapshot);
            SmartCraftStorage.Storage.Integration.CraftingStoragePatch.Setup();
            SmartCraftStorage.Storage.Integration.ProcessorStorage.Setup();
            SmartCraftStorage.Integrations.EpicLootProvider.Setup();

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void Update()
        {
            StorageFacade.Service.Tick();
            SmartCraftStorage.Storage.Integration.DirectPlayerTransfers.Tick();
        }

        private void OnDestroy() => StorageFacade.Service.Dispose();
    }
}
