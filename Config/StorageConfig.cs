using BepInEx.Configuration;
using SmartCraftStorage.Storage.Runtime;

namespace SmartCraftStorage.Config
{
    internal static class StorageConfig
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> TerminalRadius;
        public static ConfigEntry<int> MaxMembers;
        public static ConfigEntry<int> MaxPendingOperations;
        public static ConfigEntry<int> OperationsPerTick;

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("Storage network", "Enabled", true,
                Description("Enable storage terminals and station access through their linked chests."));
            TerminalRadius = config.Bind("Storage network", "TerminalRadius", 32f,
                Description("Radius in meters from a terminal to its named backing chests. Station discovery uses the station's own radius.",
                    new AcceptableValueRange<float>(1f, 100f)));
            MaxMembers = config.Bind("Storage network", "MaxMembers", 64,
                Description("Maximum physical chests in one operation. Networks above this limit report unavailable instead of silently hiding contents.",
                    new AcceptableValueRange<int>(1, 256)));
            MaxPendingOperations = config.Bind("Storage network", "MaxPendingOperations", 128,
                Description("Maximum concurrent queued operations. Pending recovery is retained; new work waits when the limit is reached.",
                    new AcceptableValueRange<int>(8, 1024)));
            OperationsPerTick = config.Bind("Storage network", "OperationsPerTick", 4,
                Description("Maximum operation transitions per update, limiting storage work per frame.",
                    new AcceptableValueRange<int>(1, 32)));
        }

        public static StorageSettings Snapshot() => new StorageSettings(Enabled.Value,
            TerminalRadius.Value, MaxMembers.Value, MaxPendingOperations.Value, OperationsPerTick.Value);

        private static ConfigDescription Description(string text, AcceptableValueBase range = null) =>
            new ConfigDescription(text, range, new ConfigurationManagerAttributes { IsAdminOnly = true });
    }
}
