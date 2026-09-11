using HarmonyLib;
using UnityEngine;

namespace SmartCraftStorage.ItemMarking
{
    [HarmonyPatch(typeof(InventoryGrid), "OnLeftDown")]
    internal static class AltClickPatch
    {
        private static void Postfix(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            bool altHeld = ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);
            if (!altHeld)
            {
                return;
            }

            var buttonPos = __instance.GetButtonPos(clickHandler.gameObject);
            var item = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);

            if (item != null)
            {
                ItemFlags.ToggleLocked(item);
            }
        }
    }
}
