using System;
using System.Collections.Generic;
using SmartCraftStorage.Config;
using SmartCraftStorage.ItemMarking;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.Restock
{
    internal static class RestockService
    {
        public static void Execute(Player player)
        {
            var itemNames = RestockList.GetAll(player);
            if (itemNames.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "Nenhum item marcado pra restock.");
                return;
            }

            var inventory = player.GetInventory();
            var containers = new List<Container>(NearbyContainers.Find(player.transform.position, ModConfig.QuickStackRadius.Value, player));

            int restocked = 0;

            foreach (var itemName in itemNames)
            {
                restocked += RestockOne(inventory, itemName, containers);
            }

            player.Message(MessageHud.MessageType.Center, restocked > 0
                ? $"Restock: {restocked} item(ns) repostos."
                : "Nada pra restockar nos baús próximos.");
        }

        private static int RestockOne(Inventory playerInventory, string itemName, List<Container> containers)
        {
            int maxStack = FindMaxStackSize(itemName, containers);
            if (maxStack <= 0)
            {
                return 0;
            }

            int needed = maxStack - CountPlayerOwned(playerInventory, itemName);
            if (needed <= 0)
            {
                return 0;
            }

            int totalMoved = 0;

            foreach (var container in containers)
            {
                if (needed <= 0)
                {
                    break;
                }

                var chestInventory = container.GetInventory();
                var matchingStacks = chestInventory.GetAllItems().FindAll(i => i.m_shared.m_name == itemName);
                if (matchingStacks.Count == 0)
                {
                    continue;
                }

                if (!NearbyContainers.TryClaimWriteAccess(container))
                {
                    continue;
                }

                foreach (var stackInChest in matchingStacks)
                {
                    if (needed <= 0)
                    {
                        break;
                    }

                    int amountToTake = Math.Min(needed, stackInChest.m_stack);

                    var existingPlayerStack = playerInventory.GetItem(itemName);
                    Vector2i targetPos = existingPlayerStack != null
                        ? existingPlayerStack.m_gridPos
                        : playerInventory.FindEmptySlot(false);

                    if (targetPos.x < 0)
                    {
                        return totalMoved;
                    }

                    if (playerInventory.MoveItemToThis(chestInventory, stackInChest, amountToTake, targetPos.x, targetPos.y))
                    {
                        needed -= amountToTake;
                        totalMoved += amountToTake;
                    }
                }
            }

            return totalMoved;
        }

        private static int CountPlayerOwned(Inventory playerInventory, string itemName)
        {
            int total = 0;
            foreach (var item in playerInventory.GetAllItems())
            {
                if (item.m_shared.m_name == itemName)
                {
                    total += item.m_stack;
                }
            }
            return total;
        }

        private static int FindMaxStackSize(string itemName, List<Container> containers)
        {
            foreach (var container in containers)
            {
                var item = container.GetInventory().GetAllItems().Find(i => i.m_shared.m_name == itemName);
                if (item != null)
                {
                    return item.m_shared.m_maxStackSize;
                }
            }

            return 0;
        }
    }
}
