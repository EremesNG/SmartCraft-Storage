using SmartCraftStorage.Storage.Integration;

namespace SmartCraftStorage.Restock
{
    internal static class RestockService
    {
        public static void Execute(Player player) => DirectPlayerTransfers.Start(player, true);
    }
}
