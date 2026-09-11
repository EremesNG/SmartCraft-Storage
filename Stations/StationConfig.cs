using BepInEx.Configuration;

namespace SmartCraftStorage.Stations
{
    internal enum KilnFeedStrategy
    {
        LeastFuelFirst,
        Nearest
    }

    internal static class StationConfig
    {
        public static ConfigEntry<float> FireplaceRadius;
        public static ConfigEntry<float> SmelterKilnRadius;
        public static ConfigEntry<float> CookingStationRadius;

        public static ConfigEntry<bool> FireplaceAutoRefuel;
        public static ConfigEntry<bool> SmelterAutoRefuel;
        public static ConfigEntry<bool> SmelterAutoCollect;
        public static ConfigEntry<bool> KilnAutoRefuel;
        public static ConfigEntry<bool> KilnAutoCollect;
        public static ConfigEntry<bool> CookingStationAutoRefuel;
        public static ConfigEntry<bool> CookingStationAutoCollect;

        public static ConfigEntry<int> KilnWoodBuffer;
        public static ConfigEntry<int> KilnMaxCoalInChest;
        public static ConfigEntry<KilnFeedStrategy> KilnFeedStrategyConfig;

        public static void Bind(ConfigFile config)
        {
            FireplaceRadius = config.Bind(
                "Estacoes",
                "FireplaceRadius",
                10f,
                new ConfigDescription(
                    "Raio (em metros) em que fogueiras/tochas/lareiras buscam combustível em baús próximos.",
                    new AcceptableValueRange<float>(0f, 100f)));

            SmelterKilnRadius = config.Bind(
                "Estacoes",
                "SmelterKilnRadius",
                10f,
                new ConfigDescription(
                    "Raio (em metros) compartilhado entre fundição e carvoaria pra buscar em baús próximos.",
                    new AcceptableValueRange<float>(0f, 100f)));

            CookingStationRadius = config.Bind(
                "Estacoes",
                "CookingStationRadius",
                10f,
                new ConfigDescription(
                    "Raio (em metros) em que estações de cozinha buscam comida crua em baús próximos.",
                    new AcceptableValueRange<float>(0f, 100f)));

            FireplaceAutoRefuel = config.Bind("Estacoes", "FireplaceAutoRefuel", true,
                "Fogueiras/tochas puxam combustível de baús próximos automaticamente.");
            SmelterAutoRefuel = config.Bind("Estacoes", "SmelterAutoRefuel", true,
                "Fundições puxam minério/combustível de baús próximos automaticamente.");
            SmelterAutoCollect = config.Bind("Estacoes", "SmelterAutoCollect", true,
                "Fundições guardam a barra produzida no baú mais próximo em vez de derrubar no chão.");
            KilnAutoRefuel = config.Bind("Estacoes", "KilnAutoRefuel", true,
                "Carvoarias puxam madeira de baús próximos automaticamente.");
            KilnAutoCollect = config.Bind("Estacoes", "KilnAutoCollect", true,
                "Carvoarias guardam o carvão produzido (ou alimentam fundições próximas primeiro) em vez de derrubar no chão.");
            CookingStationAutoRefuel = config.Bind("Estacoes", "CookingStationAutoRefuel", true,
                "Estações de cozinha puxam comida crua (e combustível próprio, se aplicável) de baús próximos automaticamente.");
            CookingStationAutoCollect = config.Bind("Estacoes", "CookingStationAutoCollect", true,
                "Estações de cozinha coletam o item pronto sozinhas e guardam no baú mais próximo, sem precisar interagir.");

            KilnWoodBuffer = config.Bind(
                "Carvoaria",
                "KilnWoodBuffer",
                3,
                new ConfigDescription(
                    "Nível de madeira que a carvoaria tenta manter na fila interna (não é a capacidade máxima dela).",
                    new AcceptableValueRange<int>(1, 50)));

            KilnMaxCoalInChest = config.Bind(
                "Carvoaria",
                "KilnMaxCoalInChest",
                50,
                new ConfigDescription(
                    "A carvoaria para de puxar madeira nova quando o(s) baú(s) próximo(s) já somam essa quantidade de carvão.",
                    new AcceptableValueRange<int>(1, 9999)));

            KilnFeedStrategyConfig = config.Bind(
                "Carvoaria",
                "KilnFeedStrategy",
                KilnFeedStrategy.LeastFuelFirst,
                "Como a carvoaria escolhe qual fundição próxima alimentar primeiro com o carvão produzido.");
        }
    }
}
