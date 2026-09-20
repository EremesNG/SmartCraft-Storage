using SmartCraftStorage.Storage.Integration;

namespace SmartCraftStorage.QuickStack
{
    internal static class QuickStackService
    {
        public static void Execute(Player player) => DirectPlayerTransfers.Start(player, false);
    }
}
