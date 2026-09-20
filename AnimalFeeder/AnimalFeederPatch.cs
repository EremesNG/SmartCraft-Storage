using HarmonyLib;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.AnimalFeeder
{
    [HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
    internal static class AnimalFeederPatch
    {
        private static void Postfix(MonsterAI __instance)
        {
            try
            {
                if (!AnimalFeederConfig.AnimalAutoFeed.Value)
                {
                    return;
                }

                if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
                {
                    return;
                }

                if (__instance.m_consumeSearchTimer != 0f)
                {
                    return;
                }

                if (__instance.m_consumeItems == null || __instance.m_consumeItems.Count == 0)
                {
                    return;
                }

                if (__instance.m_consumeTarget != null)
                {
                    return;
                }

                var tameable = __instance.m_tamable;
                if (tameable == null || !tameable.IsHungry())
                {
                    return;
                }

                var player = Player.m_localPlayer;
                if (player == null)
                {
                    return;
                }

                var creaturePosition = __instance.transform.position;

                foreach (var container in NearbyContainers.Find(creaturePosition, AnimalFeederConfig.AnimalFeederRadius.Value, player))
                {
                    var chestInventory = container.GetInventory();
                    ItemDrop.ItemData match = null;

                    foreach (var candidate in chestInventory.GetAllItems())
                    {
                        if (candidate.m_dropPrefab != null && __instance.CanConsume(candidate))
                        {
                            match = candidate;
                            break;
                        }
                    }

                    if (match == null)
                    {
                        continue;
                    }

                    if (!NearbyContainers.TryClaimWriteAccess(container))
                    {
                        continue;
                    }

                    if (!chestInventory.RemoveItem(match, 1)) continue;

                    var spawnPosition = creaturePosition + Vector3.up * 0.5f;
                    var spawned = ItemDrop.DropItem(match, 1, spawnPosition, Quaternion.identity);
                    spawned.OnPlayerDrop();
                    __instance.m_consumeTarget = spawned;
                    return;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
