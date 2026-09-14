namespace SmartCraftStorage.Stations
{
    internal static class KilnDetection
    {
        public static bool IsKiln(Smelter smelter)
        {
            return GetCoalItemName(smelter) != null;
        }

        public static string GetCoalItemName(Smelter smelter)
        {
            foreach (var conversion in smelter.m_conversion)
            {
                if (conversion.m_to != null && conversion.m_to.gameObject.name == "Coal")
                {
                    return conversion.m_to.m_itemData.m_shared.m_name;
                }
            }

            return null;
        }

        /// <summary>
        /// The kiln's own "regular Wood" input, read straight from its actual conversion
        /// list (not a guessed item name), so a differently configured kiln just falls
        /// back to accepting any wood type instead of never pulling anything.
        /// </summary>
        public static ItemDrop GetRegularWoodItem(Smelter smelter)
        {
            foreach (var conversion in smelter.m_conversion)
            {
                if (conversion.m_from != null && conversion.m_from.gameObject.name == "Wood")
                {
                    return conversion.m_from;
                }
            }

            return null;
        }
    }
}
