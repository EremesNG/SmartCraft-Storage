using System;
using System.Collections.Generic;
using SmartCraftStorage.ItemMarking;

namespace SmartCraftStorage.Shared
{
    internal static class UnlockedInventory
    {
        public static int CountItems(Inventory inventory, string name, int quality = -1, bool matchWorldLevel = true)
        {
            int total = 0;
            foreach (var item in inventory.GetAllItems())
            {
                if (!ItemFlags.IsLocked(item) && MatchesCount(item, name, quality, matchWorldLevel))
                {
                    total += item.m_stack;
                }
            }
            return total;
        }

        public static bool HaveItem(Inventory inventory, string name, bool matchWorldLevel = true)
        {
            return FindItem(inventory, name, matchWorldLevel) != null;
        }

        public static ItemDrop.ItemData FindItem(Inventory inventory, string name, bool matchWorldLevel = true)
        {
            foreach (var item in inventory.GetAllItems())
            {
                if (!ItemFlags.IsLocked(item) && MatchesNamed(item, name, -1, matchWorldLevel))
                {
                    return item;
                }
            }
            return null;
        }

        public static int RemoveItems(Inventory inventory, string name, int amount, int quality = -1, bool matchWorldLevel = true)
        {
            int removed = 0;
            var candidates = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            foreach (var item in candidates)
            {
                if (removed >= amount)
                {
                    break;
                }
                if (!ItemFlags.IsLocked(item) && MatchesNamed(item, name, quality, matchWorldLevel))
                {
                    removed += RemoveItem(inventory, item, amount - removed);
                }
            }
            return removed;
        }

        public static int RemoveItem(Inventory inventory, ItemDrop.ItemData item, int amount)
        {
            if (item == null || amount <= 0 || ItemFlags.IsLocked(item) || !inventory.ContainsItem(item))
            {
                return 0;
            }

            int before = item.m_stack;
            int requested = Math.Min(amount, before);
            if (!inventory.RemoveItem(item, requested))
            {
                return 0;
            }

            int after = inventory.ContainsItem(item) ? item.m_stack : 0;
            return Math.Min(requested, Math.Max(0, before - after));
        }

        private static bool MatchesCount(ItemDrop.ItemData item, string name, int quality, bool matchWorldLevel)
        {
            return (name == null || item.m_shared.m_name == name)
                && (quality < 0 || item.m_quality == quality)
                && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel);
        }

        private static bool MatchesNamed(ItemDrop.ItemData item, string name, int quality, bool matchWorldLevel)
        {
            return item.m_shared.m_name == name
                && (quality < 0 || item.m_quality == quality)
                && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel);
        }
    }
}
