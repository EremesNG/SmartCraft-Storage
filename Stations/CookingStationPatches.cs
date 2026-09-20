using HarmonyLib;
using SmartCraftStorage.Storage.Integration;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class CookingStationPatches
    {
        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        private static class UpdatePatch
        {
            private static void Postfix(CookingStation __instance)
            {
                try
                {
                    ProcessorStorage.Refuel(__instance);
                    if (StationConfig.CookingStationAutoCollect.Value && Player.m_localPlayer != null &&
                        __instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner() &&
                        __instance.HaveDoneItem())
                        __instance.OnInteract(Player.m_localPlayer);
                }
                catch (System.Exception ex) { Debug.LogException(ex); }
            }
        }

        [HarmonyPatch(typeof(CookingStation), "RPC_RemoveDoneItem")]
        private static class CollectPatch
        {
            private static bool Prefix(CookingStation __instance, Vector3 userPoint, int amount) =>
                !ProcessorStorage.Collect(__instance, userPoint, amount);
        }
    }
}
