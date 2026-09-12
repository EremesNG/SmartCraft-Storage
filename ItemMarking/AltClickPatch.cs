using BepInEx.Configuration;
using HarmonyLib;
using SmartCraftStorage.Hotkeys;
using UnityEngine;

namespace SmartCraftStorage.ItemMarking
{
    [HarmonyPatch(typeof(InventoryGrid), "OnLeftDown")]
    internal static class AltClickPatch
    {
        private static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            try
            {
                bool restockMarkHeld = AreAllKeysHeld(HotkeyConfig.RestockMarkClickShortcut.Value);
                bool lockHeld = AreAllKeysHeld(HotkeyConfig.LockClickShortcut.Value);

                if (!restockMarkHeld && !lockHeld)
                {
                    return true;
                }

                var buttonPos = __instance.GetButtonPos(clickHandler.gameObject);
                var item = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);

                if (item == null)
                {
                    return true;
                }

                if (restockMarkHeld)
                {
                    if (Player.m_localPlayer != null)
                    {
                        RestockList.Toggle(Player.m_localPlayer, item.m_shared.m_name);
                    }
                }
                else
                {
                    ItemFlags.ToggleLocked(item);
                }

                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return true;
            }
        }

        // These two shortcuts are checked as independent "all keys currently held" sets,
        // not as press-triggered KeyboardShortcut.IsDown() combos — the actual trigger here
        // is the mouse click itself (OnLeftDown), the shortcut only describes which keys
        // must be held at click time. RestockMarkClickShortcut never depends on
        // LockClickShortcut also being satisfied.
        private static bool AreAllKeysHeld(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None || !ZInput.GetKey(shortcut.MainKey))
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
