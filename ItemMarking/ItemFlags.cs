namespace SmartCraftStorage.ItemMarking
{
    internal static class ItemFlags
    {
        private const string LockedKey = "SmartCraft_Locked";

        public static bool IsLocked(ItemDrop.ItemData item)
        {
            return item.m_customData.TryGetValue(LockedKey, out var value) && value == "1";
        }

        public static void SetLocked(ItemDrop.ItemData item, bool locked)
        {
            if (locked)
            {
                item.m_customData[LockedKey] = "1";
            }
            else
            {
                item.m_customData.Remove(LockedKey);
            }
        }

        public static void ToggleLocked(ItemDrop.ItemData item)
        {
            SetLocked(item, !IsLocked(item));
        }
    }
}
