using HarmonyLib;
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
                if (__instance != Player.m_localPlayer || !__instance.TakeInput())
                {
                    return;
                }

                if (!ZInput.GetButtonDown(Plugin.ActionButtonName))
                {
                    return;
                }

                bool shiftHeld = ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);
                bool ctrlHeld = ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);

                if (shiftHeld)
                {
                    QuickStack.QuickStackService.Execute(__instance);
                }
                else if (ctrlHeld)
                {
                    Restock.RestockService.Execute(__instance);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
