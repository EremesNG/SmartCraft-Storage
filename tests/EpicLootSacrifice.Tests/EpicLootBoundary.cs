// External boundary modeled on installed Epic Loot 0.13.0.0. This assembly is
// named EpicLoot so production reflection and actual Harmony dispatch are used.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public class ItemDrop
{
    public class ItemData
    {
        public readonly Dictionary<string, string> m_customData = new Dictionary<string, string>();
        public int m_stack = 1;
        public string Name;
        public bool Eligible = true;
        public int SocketCount;
    }
}

namespace UnityEngine
{
    public static class Debug
    {
        public static readonly List<string> Messages = new List<string>();
        public static void Log(object value) => Messages.Add(value.ToString());
        public static void LogWarning(object value) => Messages.Add(value.ToString());
        public static void LogException(Exception error) => Messages.Add(error.ToString());
    }
}

namespace BepInEx.Bootstrap
{
    public static class Chainloader
    {
        public static readonly Dictionary<string, object> PluginInfos = new Dictionary<string, object>();
    }
}

namespace SmartCraftStorage
{
    internal static class Plugin { public const string PluginGuid = "com.zellds.smartcraftstorage"; }
}

namespace EpicLoot
{
    public static class API
    {
        public static readonly Dictionary<string, Func<ItemDrop.ItemData, bool>> Filters = new Dictionary<string, Func<ItemDrop.ItemData, bool>>();
        public static bool RejectRegistration;
        public static bool ThrowRegistration;

#if !MissingFilter
        public static bool RegisterSacrificeFilter(string id, Func<ItemDrop.ItemData, bool> canSacrifice)
        {
            if (ThrowRegistration) throw new InvalidOperationException("external registration failure");
            if (RejectRegistration || Filters.ContainsKey(id)) return false;
            Filters.Add(id, canSacrifice);
            return true;
        }
#endif
#if WrongUnregister
        public static int UnregisterSacrificeFilter(string id) => Filters.Remove(id) ? 1 : 0;
#else
        public static bool UnregisterSacrificeFilter(string id) => Filters.Remove(id);
#endif

        public static bool SacrificeAllowed(ItemDrop.ItemData item)
        {
            if (item == null) return true;
            foreach (var filter in Filters.Values)
            {
                try { if (!filter(item)) return false; }
                catch (Exception error) { UnityEngine.Debug.LogException(error); }
            }
            return true;
        }
    }

    public static class SacrificeController
    {
        public static List<ItemDrop.ItemData> GetCandidates(IEnumerable<ItemDrop.ItemData> items)
            => items.Where(item => item != null && item.Eligible && API.SacrificeAllowed(item)).ToList();

        public static int GetProducts(IEnumerable<Tuple<EpicLoot_UnityLib.IListElement, int>> selected)
            => selected.Where(row => API.SacrificeAllowed(row.Item1.GetItem())).Sum(row => row.Item2);
    }
}

namespace EpicLoot_UnityLib
{
    public interface IListElement { ItemDrop.ItemData GetItem(); }

    public class ItemElement : IListElement
    {
        public ItemDrop.ItemData Item;
        public ItemDrop.ItemData GetItem() => Item;
    }

    public class MultiSelectItemList
    {
        public readonly List<Tuple<IListElement, int>> Selected = new List<Tuple<IListElement, int>>();
#if !MissingSelection
        public List<Tuple<T, int>> GetSelectedItems<T>()
            => Selected.Select(row => Tuple.Create((T)row.Item1, row.Item2)).ToList();
#endif
    }

    public class EnchantingTableUIPanelBase
    {
        public MultiSelectItemList AvailableItems = new MultiSelectItemList();
    }

    public class SacrificeUI : EnchantingTableUIPanelBase
    {
        public int Rewards;
        public int ReclaimedSockets;
        public int CancelCount;
        public int RefreshCount;
        public readonly List<ItemDrop.ItemData> Inventory = new List<ItemDrop.ItemData>();
        public List<ItemDrop.ItemData> Visible = new List<ItemDrop.ItemData>();

        public void Open() => Visible = EpicLoot.SacrificeController.GetCandidates(Inventory);
        public void Select(ItemDrop.ItemData item, int amount = 1)
            => AvailableItems.Selected.Add(Tuple.Create<IListElement, int>(new ItemElement { Item = item }, amount));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClickSacrifice() => SacrificeItems();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SacrificeItems()
        {
            // Mirrors the external ordering: products honor filters but the
            // removal/socket loop trusts the selected snapshot unconditionally.
            var selected = AvailableItems.Selected.ToList();
            int products = EpicLoot.SacrificeController.GetProducts(selected);
            Cancel();
            foreach (var row in selected)
            {
                var item = row.Item1.GetItem();
                ReclaimedSockets += item.SocketCount;
                item.m_stack -= row.Item2;
                if (item.m_stack == 0) Inventory.Remove(item);
            }
            Rewards += products;
            RefreshAvailableItems();
        }

        public virtual void Cancel() => CancelCount++;

        private void RefreshAvailableItems()
        {
            RefreshCount++;
            Open();
            AvailableItems?.Selected.Clear();
        }
    }
}
