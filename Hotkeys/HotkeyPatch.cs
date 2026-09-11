using HarmonyLib;

namespace SmartCraftStorage.Hotkeys
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class HotkeyPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
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
    }
}
