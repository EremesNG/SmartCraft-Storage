using HarmonyLib;
using SmartCraftStorage.Storage.Integration;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    [HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
    internal static class FireplacePatch
    {
        private static void Postfix(Fireplace __instance)
        {
            try { ProcessorStorage.Refuel(__instance); }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }
    }
}
