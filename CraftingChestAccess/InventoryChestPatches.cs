using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.CraftingChestAccess
{
    internal static class InventoryChestPatches
    {
        private static bool IsCraftingOrBuildingContext(Inventory instance, out Player player)
        {
            player = Player.m_localPlayer;
            if (player == null || instance != player.GetInventory())
            {
                return false;
            }

            return player.GetCurrentCraftingStation() != null || player.InPlaceMode();
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), new[] { typeof(string), typeof(int), typeof(bool) })]
        private static class CountItemsPatch
        {
            private static void Postfix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
            {
                if (!IsCraftingOrBuildingContext(__instance, out var player))
                {
                    return;
                }

                foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value))
                {
                    __result += container.GetInventory().CountItems(name, quality, matchWorldLevel);
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), new[] { typeof(string), typeof(bool) })]
        private static class HaveItemPatch
        {
            private static void Postfix(Inventory __instance, string name, bool matchWorldLevel, ref bool __result)
            {
                if (__result || !IsCraftingOrBuildingContext(__instance, out var player))
                {
                    return;
                }

                foreach (var container in NearbyContainers.Find(player.transform.position, ModConfig.CraftingChestRadius.Value))
                {
                    if (container.GetInventory().HaveItem(name, matchWorldLevel))
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }
}
