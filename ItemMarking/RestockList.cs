using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace SmartCraftStorage.ItemMarking
{
    internal static class RestockList
    {
        private const string ZdoKey = "SmartCraft_RestockList";
        private const char Separator = '|';

        public static bool Contains(Player player, string itemName)
        {
            return GetAll(player).Contains(itemName);
        }

        public static void Toggle(Player player, string itemName)
        {
            var current = new List<string>(GetAll(player));

            if (current.Contains(itemName))
            {
                current.Remove(itemName);
            }
            else
            {
                current.Add(itemName);
            }

            Save(player, current);
        }

        public static IReadOnlyList<string> GetAll(Player player)
        {
            var zdo = GetZdo(player);
            if (zdo == null)
            {
                return Array.Empty<string>();
            }

            var raw = zdo.GetString(ZdoKey, string.Empty);
            return string.IsNullOrEmpty(raw)
                ? Array.Empty<string>()
                : raw.Split(Separator);
        }

        private static void Save(Player player, List<string> items)
        {
            var zdo = GetZdo(player);
            if (zdo == null)
            {
                return;
            }

            zdo.Set(ZdoKey, string.Join(Separator.ToString(), items));
        }

        private static ZDO GetZdo(Player player)
        {
            var nview = Traverse.Create(player).Field("m_nview").GetValue<ZNetView>();
            return nview != null && nview.IsValid() ? nview.GetZDO() : null;
        }
    }
}
