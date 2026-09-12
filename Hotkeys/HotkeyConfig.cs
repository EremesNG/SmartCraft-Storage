using BepInEx.Configuration;
using UnityEngine;

namespace SmartCraftStorage.Hotkeys
{
    internal static class HotkeyConfig
    {
        public static ConfigEntry<KeyboardShortcut> QuickStackShortcut;
        public static ConfigEntry<KeyboardShortcut> RestockShortcut;
        public static ConfigEntry<KeyboardShortcut> LockClickShortcut;
        public static ConfigEntry<KeyboardShortcut> RestockMarkClickShortcut;

        public static void Bind(ConfigFile config)
        {
            QuickStackShortcut = config.Bind(
                "Hotkeys",
                "QuickStackShortcut",
                new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift),
                "Shortcut to stash inventory items into nearby chests (quick-stack). Click the value in Configuration Manager and press the key combo you want.");

            RestockShortcut = config.Bind(
                "Hotkeys",
                "RestockShortcut",
                new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl),
                "Shortcut to refill items marked for restock from nearby chests. Click the value in Configuration Manager and press the key combo you want.");

            LockClickShortcut = config.Bind(
                "Hotkeys",
                "LockClickShortcut",
                new KeyboardShortcut(KeyCode.LeftAlt),
                "Key(s) held while left-clicking an inventory item to toggle its lock. Independent of RestockMarkClickShortcut.");

            RestockMarkClickShortcut = config.Bind(
                "Hotkeys",
                "RestockMarkClickShortcut",
                new KeyboardShortcut(KeyCode.LeftAlt, KeyCode.LeftControl),
                "Key(s) held while left-clicking an item to mark it for restock instead of toggling lock. Independent of LockClickShortcut — does not require it to also be held.");
        }
    }
}
