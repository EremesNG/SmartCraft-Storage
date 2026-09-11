using HarmonyLib;
using SmartCraftStorage.ItemMarking;
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
                bool altHeld = ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);
                if (!altHeld)
                {
                    return true;
                }

                var buttonPos = __instance.GetButtonPos(clickHandler.gameObject);
                var item = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);

                if (item == null)
                {
                    return true;
                }

                bool ctrlHeld = ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);

                if (ctrlHeld)
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
    }
}
