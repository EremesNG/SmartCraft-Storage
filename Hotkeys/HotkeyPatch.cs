using BepInEx.Configuration;
using HarmonyLib;
using SmartCraftStorage.Storage.UI;
using UnityEngine;

namespace SmartCraftStorage.Hotkeys
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class HotkeyPatch
    {
        private static void Postfix(Player __instance)
        {
            try
            {
                if (__instance != Player.m_localPlayer || !__instance.TakeInput() || StorageTerminalUi.IsOpen)
                {
                    return;
                }

                if (IsShortcutDown(HotkeyConfig.QuickStackShortcut.Value))
                {
                    QuickStack.QuickStackService.Execute(__instance);
                }
                else if (IsShortcutDown(HotkeyConfig.RestockShortcut.Value))
                {
                    Restock.RestockService.Execute(__instance);
                }
                else if (IsShortcutDown(HotkeyConfig.NetworkNameShortcut.Value) && !Hud.InRadial())
                {
                    StorageChestNaming.OpenHovered(__instance);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        // KeyboardShortcut.IsDown() also requires that no key outside the combo is held at
        // all (BepInEx's own ModifierKeyTest), which would block the shortcut whenever the
        // player is holding a movement key (W/A/S/D) at the same time — extremely common in
        // this game. So we only check the main key plus the configured modifiers here,
        // ignoring any other key the player might be holding.
        private static bool IsShortcutDown(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None || !ZInput.GetKeyDown(shortcut.MainKey))
            {
                return false;
            }

            foreach (var modifier in shortcut.Modifiers)
            {
                if (!ZInput.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
