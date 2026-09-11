using System.Collections.Generic;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.QuickStack
{
    internal static class QuickStackService
    {
        public static void Execute(Player player)
        {
            var inventory = player.GetInventory();
            var containers = new List<Container>(NearbyContainers.Find(player.transform.position, ModConfig.QuickStackRadius.Value, player));

            if (containers.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "Nenhum baú próximo.");
                return;
            }

            int itemsMoved = 0;
            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());

            foreach (var item in items)
            {
                if (item.m_equipped || SmartCraftStorage.ItemMarking.ItemFlags.IsLocked(item))
                {
                    continue;
                }

                itemsMoved += MoveItemIntoMatchingStacks(inventory, item, containers);
            }

            player.Message(MessageHud.MessageType.Center, itemsMoved > 0
                ? $"Guardado(s) {itemsMoved} item(ns) nos baús próximos."
                : "Nada pra guardar nos baús próximos.");
        }

        private static int MoveItemIntoMatchingStacks(Inventory playerInventory, ItemDrop.ItemData item, List<Container> containers)
        {
            int originalStack = item.m_stack;

            foreach (var container in containers)
            {
                if (item.m_stack <= 0)
                {
                    break;
                }

                var chestInventory = container.GetInventory();
                var matchingStacks = chestInventory.GetAllItems().FindAll(i =>
                    i.m_shared.m_name == item.m_shared.m_name &&
                    i.m_quality == item.m_quality &&
                    i.m_stack < i.m_shared.m_maxStackSize);

                if (matchingStacks.Count == 0)
                {
                    continue;
                }

                if (!NearbyContainers.TryClaimWriteAccess(container))
                {
                    continue;
                }

                foreach (var existingStack in matchingStacks)
                {
                    if (item.m_stack <= 0)
                    {
                        break;
                    }

                    int freeSpace = existingStack.m_shared.m_maxStackSize - existingStack.m_stack;
                    if (freeSpace <= 0)
                    {
                        continue;
                    }

                    int amountToMove = Mathf.Min(freeSpace, item.m_stack);
                    chestInventory.MoveItemToThis(playerInventory, item, amountToMove, existingStack.m_gridPos.x, existingStack.m_gridPos.y);
                }
            }

            return originalStack - item.m_stack;
        }
    }
}
