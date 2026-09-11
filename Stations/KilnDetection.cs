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
    }
}
