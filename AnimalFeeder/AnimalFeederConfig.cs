using BepInEx.Configuration;

namespace SmartCraftStorage.AnimalFeeder
{
    internal static class AnimalFeederConfig
    {
        public static ConfigEntry<float> AnimalFeederRadius;
        public static ConfigEntry<bool> AnimalAutoFeed;

        public static void Bind(ConfigFile config)
        {
            AnimalFeederRadius = config.Bind(
                "Animais",
                "AnimalFeederRadius",
                20f,
                new ConfigDescription(
                    "Raio (em metros) em que animais domesticáveis famintos procuram comida em baús próximos.",
                    new AcceptableValueRange<float>(0f, 100f)));

            AnimalAutoFeed = config.Bind("Animais", "AnimalAutoFeed", true,
                "Animais domesticáveis (selvagens sendo domados, ou já domados) puxam comida compatível de baús próximos automaticamente.");
        }
    }
}
