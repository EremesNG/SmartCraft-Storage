using BepInEx.Configuration;

namespace SmartCraftStorage.Config
{
    internal static class ModConfig
    {
        public static ConfigEntry<float> QuickStackRadius;
        public static ConfigEntry<float> CraftingChestRadius;

        public static void Bind(ConfigFile config)
        {
            QuickStackRadius = config.Bind(
                "Raios",
                "QuickStackRadius",
                20f,
                new ConfigDescription(
                    "Raio (em metros) em que o quick-stack e o restock procuram baús próximos.",
                    new AcceptableValueRange<float>(0f, 100f)));

            CraftingChestRadius = config.Bind(
                "Raios",
                "CraftingChestRadius",
                20f,
                new ConfigDescription(
                    "Raio (em metros) em que crafting/construção considera itens de baús próximos.",
                    new AcceptableValueRange<float>(0f, 100f)));
        }
    }
}
