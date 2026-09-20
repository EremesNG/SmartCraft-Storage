using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Shared;
using SmartCraftStorage.Storage.Integration;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class SmelterPatches
    {
        [HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
        private static class RefuelPatch
        {
            private static void Postfix(Smelter __instance)
            {
                try { ProcessorStorage.Refuel(__instance); }
                catch (System.Exception ex) { Debug.LogException(ex); }
            }
        }

        [HarmonyPatch(typeof(Smelter), "Spawn")]
        private static class CollectPatch
        {
            private static bool Prefix(Smelter __instance, string ore, ref int stack)
            {
                bool kiln = KilnDetection.IsKiln(__instance);
                if (!(kiln ? StationConfig.KilnAutoCollect.Value : StationConfig.SmelterAutoCollect.Value) ||
                    Player.m_localPlayer == null || __instance.m_nview == null || !__instance.m_nview.IsValid() ||
                    !__instance.m_nview.IsOwner()) return true;
                // Keep direct coal routing ahead of chest routing. Decrease the
                // native remainder after each confirmed synchronous owner effect.
                if (kiln) FeedNearbySmelters(__instance, ref stack);
                return stack > 0 && !ProcessorStorage.Collect(__instance, ore, stack);
            }
        }

        private static void FeedNearbySmelters(Smelter kiln, ref int amount)
        {
            string coalName = KilnDetection.GetCoalItemName(kiln);
            var candidates = new List<Smelter>();
            var seen = new HashSet<Smelter>();
            var origin = kiln.transform.position;
            int count = NearbyContainers.OverlapNearby(origin, StationConfig.SmelterKilnRadius.Value);
            for (int i = 0; i < count; i++)
            {
                var smelter = NearbyContainers.Hits[i].GetComponentInParent<Smelter>();
                if (smelter == null || smelter == kiln || !seen.Add(smelter) || KilnDetection.IsKiln(smelter) ||
                    smelter.m_fuelItem == null || smelter.m_fuelItem.m_itemData.m_shared.m_name != coalName ||
                    Mathf.CeilToInt(smelter.GetFuel()) >= smelter.m_maxFuel ||
                    !PrivateArea.CheckAccess(smelter.transform.position, 0f, false)) continue;
                candidates.Add(smelter);
            }
            candidates.Sort((a, b) => StationConfig.KilnFeedStrategyConfig.Value == KilnFeedStrategy.LeastFuelFirst
                ? a.GetFuel().CompareTo(b.GetFuel())
                : (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));
            foreach (var smelter in candidates)
            {
                while (amount > 0 && Mathf.CeilToInt(smelter.GetFuel()) < smelter.m_maxFuel)
                {
                    if (!NearbyContainers.TryClaimWriteAccess(smelter.m_nview)) break;
                    float before = smelter.GetFuel();
                    smelter.RPC_AddFuel(ZNet.GetUID());
                    if (smelter.GetFuel() <= before) break;
                    amount--;
                }
                if (amount == 0) break;
            }
        }
    }
}
