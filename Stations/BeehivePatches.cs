using HarmonyLib;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    internal static class BeehivePatches
    {
        [HarmonyPatch(typeof(Beehive), "UpdateBees")]
        private static class AutoCollectTriggerPatch
        {
            private static void Postfix(Beehive __instance)
            {
                try
                {
                    if (!StationConfig.BeehiveAutoCollect.Value
                        || __instance.m_nview == null || !__instance.m_nview.IsValid()
                        || !__instance.m_nview.IsOwner())
                    {
                        return;
                    }

                    int honeyLevel = __instance.GetHoneyLevel();
                    if (honeyLevel > 0)
                    {
                        __instance.Extract();
                        // Mirrors what a manual interaction does in Beehive.Interact(),
                        // so the "bees harvested" stat keeps tracking correctly.
                        Game.instance.IncrementPlayerStat(PlayerStatType.BeesHarvested, honeyLevel);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Beehive), "RPC_Extract")]
        private static class CollectRedirectPatch
        {
            private static bool Prefix(Beehive __instance, long caller)
            {
                try
                {
                    if (!StationConfig.BeehiveAutoCollect.Value)
                    {
                        return true;
                    }

                    int honeyLevel = __instance.GetHoneyLevel();
                    if (honeyLevel <= 0)
                    {
                        return true;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return true;
                    }

                    // Same per-unit scaling the vanilla method applies before instantiating
                    // each item, just summed into one stack instead of spawned individually.
                    int totalHoney = 0;
                    for (int i = 0; i < honeyLevel; i++)
                    {
                        totalHoney += Game.instance.ScaleDrops(__instance.m_honeyItem.m_itemData, 1);
                    }

                    int remaining = totalHoney;
                    string itemName = __instance.m_honeyItem.m_itemData.m_shared.m_name;

                    foreach (var container in NearbyContainers.Find(__instance.transform.position, StationConfig.BeehiveRadius.Value, player))
                    {
                        if (remaining <= 0)
                        {
                            break;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        var chestInventory = container.GetInventory();
                        int before = chestInventory.CountItems(itemName);
                        chestInventory.AddItem(__instance.m_honeyItem.gameObject, remaining);
                        int added = chestInventory.CountItems(itemName) - before;
                        if (added > 0)
                        {
                            remaining -= added;
                        }
                    }

                    if (remaining <= 0)
                    {
                        __instance.m_spawnEffect.Create(__instance.m_spawnPoint.position, Quaternion.identity);
                        __instance.ResetLevel();
                        return false;
                    }

                    // No chest had room: fall back to vanilla, which drops everything on the ground.
                    return true;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    return true;
                }
            }
        }
    }
}
