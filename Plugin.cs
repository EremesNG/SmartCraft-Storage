using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace SmartCraftStorage
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.zellds.smartcraftstorage";
        public const string PluginName = "SmartCraft-Storage";
        public const string PluginVersion = "0.1.0";

        internal static Harmony HarmonyInstance;

        private void Awake()
        {
            SmartCraftStorage.Config.ModConfig.Bind(Config);

            InputManager.Instance.AddButton(PluginGuid, new ButtonConfig
            {
                Name = "SmartCraft_QuickStack",
                Shortcut = new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift),
                HintToken = "$smartcraft_quickstack_hint"
            });

            InputManager.Instance.AddButton(PluginGuid, new ButtonConfig
            {
                Name = "SmartCraft_Restock",
                Shortcut = new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl),
                HintToken = "$smartcraft_restock_hint"
            });

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
