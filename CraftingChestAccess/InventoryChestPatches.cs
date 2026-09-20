using System;
using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;
using SmartCraftStorage.Storage.Integration;

namespace SmartCraftStorage.CraftingChestAccess
{
    internal static class InventoryChestPatches
    {
        private static bool IsCraftingOrBuildingContext(Inventory instance, out Player player)
        {
            player = Player.m_localPlayer;
            if (player == null || instance != player.GetInventory())
            {
                return false;
            }

            return player.GetCurrentCraftingStation() != null || player.InPlaceMode();
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), new[] { typeof(string), typeof(int), typeof(bool) })]
        private static class CountItemsPatch
        {
            private static void Postfix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
            {
                try
                {
                    if (!IsCraftingOrBuildingContext(__instance, out var player))
                    {
                        return;
                    }

                    if (!player.InPlaceMode())
                    {
                        if (CraftingStoragePatch.IsReplaying(player))
                        {
                            __result = CraftingStoragePatch.PreparedInventory.CountItems(name, quality, matchWorldLevel);
                            return;
                        }
                        foreach (var item in CraftingStoragePatch.Available(player, false))
                            if ((name == null || item.m_shared.m_name == name) && (quality < 0 || item.m_quality == quality) &&
                                (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel)) __result += item.m_stack;
                        return;
                    }

                    // Only the chest half is cached; __result already holds the
                    // player's own live count, so what you carry is never stale.
                    if (ChestCountCache.TryGet(name, quality, matchWorldLevel, out int cached))
                    {
                        __result += cached;
                        return;
                    }

                    int chestTotal = 0;
                    foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
                    {
                        if (container.m_nview == null || !container.m_nview.IsOwner()) continue;
                        chestTotal += container.GetInventory().CountItems(name, quality, matchWorldLevel);
                    }

                    ChestCountCache.Store(name, quality, matchWorldLevel, chestTotal);
                    __result += chestTotal;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), new[] { typeof(string), typeof(bool) })]
        private static class HaveItemPatch
        {
            private static void Postfix(Inventory __instance, string name, bool matchWorldLevel, ref bool __result)
            {
                try
                {
                    if (!IsCraftingOrBuildingContext(__instance, out var player))
                    {
                        return;
                    }

                    if (!player.InPlaceMode())
                    {
                        if (CraftingStoragePatch.IsReplaying(player))
                        {
                            __result = CraftingStoragePatch.PreparedInventory.HaveItem(name, matchWorldLevel);
                            return;
                        }
                        if (__result) return;
                        foreach (var item in CraftingStoragePatch.Available(player, false))
                            if (item.m_shared.m_name == name && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel))
                            { __result = true; break; }
                        return;
                    }

                    if (__result) return;
                    foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
                    {
                        if (container.m_nview == null || !container.m_nview.IsOwner()) continue;
                        if (container.GetInventory().HaveItem(name, matchWorldLevel))
                        {
                            __result = true;
                            return;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        private static class RemoveItemPatch
        {
            private static bool Prefix(Inventory __instance, string name, ref int amount, int itemQuality, bool worldLevelBased)
            {
                try
                {
                    if (!IsCraftingOrBuildingContext(__instance, out var player))
                    {
                        return true;
                    }

                    if (!player.InPlaceMode())
                    {
                        if (!CraftingStoragePatch.IsReplaying(player)) return true;
                        var escrow = CraftingStoragePatch.PreparedInventory;
                        if (escrow.CountItems(name, itemQuality, worldLevelBased) < amount)
                            throw new InvalidOperationException("Native craft requested more than its prepared cost.");
                        escrow.RemoveItem(name, amount, itemQuality, worldLevelBased);
                        return false;
                    }

                    int haveInInventory = SumMatchingStack(__instance, name, itemQuality);
                    if (amount <= haveInInventory)
                    {
                        return true;
                    }

                    int remaining = amount - haveInInventory;
                    amount = haveInInventory;

                    foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
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
                        int haveInChest = SumMatchingStack(chestInventory, name, itemQuality);
                        int takeFromChest = Math.Min(remaining, haveInChest);

                        if (takeFromChest > 0)
                        {
                            chestInventory.RemoveItem(name, takeFromChest, itemQuality, worldLevelBased);
                            ChestCountCache.Invalidate();
                            remaining -= takeFromChest;
                            amount += takeFromChest;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    if (CraftingStoragePatch.IsReplaying(Player.m_localPlayer)) throw;
                }
                return true;
            }

            private static int SumMatchingStack(Inventory inventory, string name, int quality)
            {
                int total = 0;
                foreach (var item in inventory.GetAllItems())
                {
                    if (item.m_shared.m_name == name && (quality < 0 || item.m_quality == quality))
                    {
                        total += item.m_stack;
                    }
                }
                return total;
            }
        }
    }
}
