using System;
using System.Globalization;
using System.Linq;

namespace SmartCraftStorage.Storage.Core
{
    public static class StorageSlotExpectation
    {
        public static string Capture(StorageInventory inventory, int slot)
        {
            if (slot < 0 || slot >= inventory.Capacity) return "invalid";
            var item = inventory.Items.FirstOrDefault(x => x.Slot == slot);
            return item == null ? "empty" : item.Identity.Length.ToString(CultureInfo.InvariantCulture) + ":" + item.Identity + ":" + item.Amount.ToString(CultureInfo.InvariantCulture);
        }
        public static bool Matches(StorageInventory inventory, int slot, string expected) =>
            slot >= 0 && slot < inventory.Capacity && !string.IsNullOrEmpty(expected) &&
            string.Equals(Capture(inventory, slot), expected, StringComparison.Ordinal);
    }
}
