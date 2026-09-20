using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Integrations
{
    /// <summary>
    /// Registers this mod as an Epic Loot "inventory provider" (EpicLoot.API.RegisterInventoryProvider),
    /// so the enchanting table sees nearby chests the same way our own craft/build-from-chest already
    /// does. Called via reflection, never referencing an Epic Loot type directly, so the project needs no
    /// build-time reference to EpicLoot.dll and nothing breaks if it isn't installed. See Epic Loot's own
    /// docs/API.md: "Nothing but primitives, string, vanilla/Unity types... ever crosses this boundary."
    /// </summary>
    internal static class EpicLootProvider
    {
        private const string EpicLootGuid = "randyknapp.mods.epicloot";

        public static void Setup()
        {
            try
            {
                if (!Chainloader.PluginInfos.ContainsKey(EpicLootGuid))
                {
                    return;
                }

                var apiType = Type.GetType("EpicLoot.API, EpicLoot");
                var registerMethod = apiType?.GetMethod("RegisterInventoryProvider", BindingFlags.Public | BindingFlags.Static);
                if (registerMethod == null)
                {
                    Debug.LogWarning("[SmartCraftStorage] Epic Loot is installed but its inventory provider API "
                        + "wasn't found; skipping the integration for this version.");
                    return;
                }

                Func<List<ItemDrop.ItemData>> getItems = GetItems;
                Func<string, int> countItem = CountItem;
                Func<string, int, int> removeItem = RemoveItem;
                Func<ItemDrop.ItemData, int, int> removeExactItem = RemoveExactItem;

                registerMethod.Invoke(null, new object[] { Plugin.PluginGuid, getItems, countItem, removeItem, removeExactItem });
                Debug.Log("[SmartCraftStorage] Registered with Epic Loot: the enchanting table can now see nearby chests.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static List<ItemDrop.ItemData> GetItems()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return null;
            }

            var items = new List<ItemDrop.ItemData>();
            foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
            {
                if (container.m_nview == null || !container.m_nview.IsOwner()) continue;
                items.AddRange(container.GetInventory().GetAllItems());
            }

            return items;
        }

        private static int CountItem(string itemName)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }

            int total = 0;
            foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
            {
                if (container.m_nview == null || !container.m_nview.IsOwner()) continue;
                total += container.GetInventory().CountItems(itemName);
            }

            return total;
        }

        private static int RemoveItem(string itemName, int amount)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }

            int remaining = amount;
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
                int before = chestInventory.CountItems(itemName);
                chestInventory.RemoveItem(itemName, remaining);
                int removed = before - chestInventory.CountItems(itemName);
                remaining -= removed;
            }

            return amount - remaining;
        }

        private static int RemoveExactItem(ItemDrop.ItemData item, int amount)
        {
            var player = Player.m_localPlayer;
            if (player == null || item == null)
            {
                return 0;
            }

            // Matched by reference, like Epic Loot's own RemoveExactItem: the same-named item in a
            // different chest could be a different (or non-magic) instance, and consuming that one
            // instead would destroy the wrong item's magic data.
            foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value, player))
            {
                var chestInventory = container.GetInventory();
                if (!chestInventory.ContainsItem(item))
                {
                    continue;
                }

                if (!NearbyContainers.TryClaimWriteAccess(container))
                {
                    continue;
                }

                int before = item.m_stack;
                chestInventory.RemoveItem(item, amount);
                return before - (chestInventory.ContainsItem(item) ? item.m_stack : 0);
            }

            return 0;
        }
    }
}
