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
            var containers = new List<Container>(NearbyContainers.Find(player.transform.position, ModConfig.QuickStackRadius.Value));

            if (containers.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "Nenhum baú próximo.");
                return;
            }

            int itemsMoved = 0;
            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());

            foreach (var item in items)
            {
                if (item.m_equipped)
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
            int moved = 0;

            foreach (var container in containers)
            {
                var chestInventory = container.GetInventory();
                var existingStack = chestInventory.GetItem(item.m_shared.m_name, item.m_quality);

                if (existingStack == null)
                {
                    continue;
                }

                int freeSpace = item.m_shared.m_maxStackSize - existingStack.m_stack;
                if (freeSpace <= 0)
                {
                    continue;
                }

                int amountToMove = Mathf.Min(freeSpace, item.m_stack - moved);
                if (amountToMove <= 0)
                {
                    continue;
                }

                if (chestInventory.MoveItemToThis(playerInventory, item, amountToMove, existingStack.m_gridPos.x, existingStack.m_gridPos.y))
                {
                    moved += amountToMove;
                }

                if (moved >= item.m_stack)
                {
                    break;
                }
            }

            return moved;
        }
    }
}
