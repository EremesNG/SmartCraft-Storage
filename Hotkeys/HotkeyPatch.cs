using HarmonyLib;

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

                if (ZInput.GetButtonDown("SmartCraft_QuickStack"))
                {
                    QuickStack.QuickStackService.Execute(__instance);
                }

                if (ZInput.GetButtonDown("SmartCraft_Restock"))
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
