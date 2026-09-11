using HarmonyLib;
using SmartCraftStorage.ItemMarking;
using UnityEngine;

namespace SmartCraftStorage.ItemMarking
{
    [HarmonyPatch(typeof(InventoryGrid), "OnLeftDown")]
    internal static class AltClickPatch
    {
        private static void Postfix(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            bool altHeld = ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);
            bool ctrlHeld = ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);

            if (!altHeld)
            {
                return;
            }

            var buttonPos = __instance.GetButtonPos(clickHandler.gameObject);
            var item = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);

            if (item == null)
            {
                return;
            }

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
        }
    }
}
