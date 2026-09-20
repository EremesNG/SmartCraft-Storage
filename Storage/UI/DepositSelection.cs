using System;
using System.Collections.Generic;

namespace SmartCraftStorage.Storage.UI
{
    // A bulk selection describes what the player selected, independently of
    // ItemData instances replaced when an authoritative inventory is installed.
    public static class DepositSelection
    {
        public static StorageStack Resolve(StorageStack selected, IReadOnlyList<StorageStack> current)
        {
            foreach (var item in current)
                if (item.Slot == selected.Slot && item.Identity == selected.Identity && item.Amount > 0)
                    return item.At(item.Slot, Math.Min(selected.Amount, item.Amount));
            return null;
        }
    }
}
