using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.Stations
{
    internal static class SmelterPatches
    {
        [HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
        private static class RefuelPatch
        {
            private static void Postfix(Smelter __instance)
            {
                try
                {
                    bool isKiln = KilnDetection.IsKiln(__instance);
                    bool enabled = isKiln ? StationConfig.KilnAutoRefuel.Value : StationConfig.SmelterAutoRefuel.Value;
                    if (!enabled || !__instance.m_nview.IsOwner())
                    {
                        return;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return;
                    }

                    var containers = new List<Container>(
                        NearbyContainers.Find(__instance.transform.position, StationConfig.SmelterKilnRadius.Value, player));

                    RefuelOre(__instance, isKiln, containers);
                    RefuelFuel(__instance, containers);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static void RefuelOre(Smelter smelter, bool isKiln, List<Container> containers)
            {
                int targetQueue = isKiln ? StationConfig.KilnWoodBuffer.Value : smelter.m_maxOre;
                string coalName = isKiln ? KilnDetection.GetCoalItemName(smelter) : null;

                while (smelter.GetQueueSize() < targetQueue)
                {
                    if (isKiln && KilnCoalCapReached(coalName, containers))
                    {
                        break;
                    }

                    if (!TryPullOneOre(smelter, containers))
                    {
                        break;
                    }
                }
            }

            private static bool TryPullOneOre(Smelter smelter, List<Container> containers)
            {
                foreach (var container in containers)
                {
                    var chestInventory = container.GetInventory();
                    var item = smelter.FindCookableItem(chestInventory);
                    if (item == null)
                    {
                        continue;
                    }

                    if (!NearbyContainers.TryClaimWriteAccess(container))
                    {
                        continue;
                    }

                    string prefabName = item.m_dropPrefab.name;
                    bool cheated = item.m_cheated;
                    chestInventory.RemoveItem(item, 1);
                    smelter.m_nview.InvokeRPC("RPC_AddOre", prefabName, cheated);
                    return true;
                }

                return false;
            }

            private static void RefuelFuel(Smelter smelter, List<Container> containers)
            {
                if (smelter.m_maxFuel <= 0 || smelter.m_fuelItem == null)
                {
                    return;
                }

                string fuelName = smelter.m_fuelItem.m_itemData.m_shared.m_name;

                while (smelter.GetFuel() < smelter.m_maxFuel)
                {
                    bool pulled = false;

                    foreach (var container in containers)
                    {
                        var chestInventory = container.GetInventory();
                        if (!chestInventory.HaveItem(fuelName))
                        {
                            continue;
                        }

                        if (!NearbyContainers.TryClaimWriteAccess(container))
                        {
                            continue;
                        }

                        chestInventory.RemoveItem(fuelName, 1);
                        smelter.m_nview.InvokeRPC("RPC_AddFuel");
                        pulled = true;
                        break;
                    }

                    if (!pulled)
                    {
                        break;
                    }
                }
            }

            private static bool KilnCoalCapReached(string coalName, List<Container> containers)
            {
                if (coalName == null)
                {
                    return false;
                }

                int totalCoal = 0;
                foreach (var container in containers)
                {
                    totalCoal += container.GetInventory().CountItems(coalName);
                    if (totalCoal >= StationConfig.KilnMaxCoalInChest.Value)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
