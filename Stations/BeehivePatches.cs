using HarmonyLib;
using SmartCraftStorage.Storage.Integration;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class BeehivePatches
    {
        [HarmonyPatch(typeof(Beehive), "UpdateBees")]
        private static class AutoCollectPatch
        {
            private static void Postfix(Beehive __instance)
            {
                try
                {
                    int level = __instance.GetHoneyLevel();
                    if (level > 0 && ProcessorStorage.Collect(__instance, true))
                        Game.instance.IncrementPlayerStat(PlayerStatType.BeesHarvested, level);
                }
                catch (System.Exception ex) { Debug.LogException(ex); }
            }
        }

        [HarmonyPatch(typeof(Beehive), "RPC_Extract")]
        private static class ManualCollectPatch
        {
            private static bool Prefix(Beehive __instance) => !ProcessorStorage.Collect(__instance, false);
        }
    }
}
