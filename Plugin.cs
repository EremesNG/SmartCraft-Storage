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
        public const string PluginVersion = "0.1.2";

        // Jotunn's InputManager mutates ButtonConfig.Name to "<name>!<modGuid>" when it
        // registers the button into ZInput, and it only ever wires the Shortcut's MainKey
        // into the game's input system (the modifier keys in KeyboardShortcut are not
        // enforced by Jotunn 2.30.0) — so we register one plain-E button and disambiguate
        // Shift vs Ctrl ourselves in HotkeyPatch, matching the exact registered name here.
        public const string ActionButtonName = "SmartCraft_Action!" + PluginGuid;

        internal static Harmony HarmonyInstance;

        private void Awake()
        {
            SmartCraftStorage.Config.ModConfig.Bind(Config);
            SmartCraftStorage.Stations.StationConfig.Bind(Config);
            SmartCraftStorage.AnimalFeeder.AnimalFeederConfig.Bind(Config);

            InputManager.Instance.AddButton(PluginGuid, new ButtonConfig
            {
                Name = "SmartCraft_Action",
                Key = KeyCode.E,
                HintToken = "$smartcraft_action_hint"
            });

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
