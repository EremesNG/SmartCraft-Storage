using HarmonyLib;
using SmartCraftStorage.Storage.Integration;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class FermenterPatches
    {
        [HarmonyPatch(typeof(Fermenter), "SlowUpdate")]
        private static class AutoProcessPatch
        {
            private static void Postfix(Fermenter __instance)
            {
                try
                {
                    if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return;
                    if (StationConfig.FermenterDurationOverride.Value > 0f)
                        __instance.m_fermentationDuration = StationConfig.FermenterDurationOverride.Value;
                    if (!StationConfig.FermenterAutoProcess.Value || Player.m_localPlayer == null) return;
                    if (__instance.GetStatus() == Fermenter.Status.Ready)
                        __instance.RPC_Tap(ZNet.GetUID());
                    else ProcessorStorage.Refuel(__instance);
                }
                catch (System.Exception ex) { Debug.LogException(ex); }
            }
        }

        // Capture before vanilla clears the content and schedules a volatile
        // DelayedTap callback; a disconnect cannot leave the only output in RAM.
        [HarmonyPatch(typeof(Fermenter), "RPC_Tap")]
        private static class CollectPatch
        {
            private static bool Prefix(Fermenter __instance) => !ProcessorStorage.Collect(__instance);
        }
    }
}
