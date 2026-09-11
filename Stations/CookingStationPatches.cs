using System.Collections.Generic;
using HarmonyLib;
using SmartCraftStorage.Shared;

namespace SmartCraftStorage.Stations
{
    internal static class CookingStationPatches
    {
        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        private static class RefuelPatch
        {
            private static void Postfix(CookingStation __instance)
            {
                try
                {
                    if (!StationConfig.CookingStationAutoRefuel.Value || !__instance.m_nview.IsOwner())
                    {
                        return;
                    }

                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        return;
                    }

                    var containers = new List<Container>(
                        NearbyContainers.Find(__instance.transform.position, StationConfig.CookingStationRadius.Value, player));

                    RefuelFood(__instance, containers);
                    RefuelFuel(__instance, containers);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            private static void RefuelFood(CookingStation station, List<Container> containers)
            {
                while (station.GetFreeSlot() != -1)
                {
                    bool pulled = false;

                    foreach (var container in containers)
                    {
                        var chestInventory = container.GetInventory();
                        var item = station.FindCookableItem(chestInventory);
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
                        station.m_nview.InvokeRPC("RPC_AddItem", prefabName, cheated);
                        pulled = true;
                        break;
                    }

                    if (!pulled)
                    {
                        break;
                    }
                }
            }

            private static void RefuelFuel(CookingStation station, List<Container> containers)
            {
                if (!station.m_useFuel || station.m_fuelItem == null)
                {
                    return;
                }

                string fuelName = station.m_fuelItem.m_itemData.m_shared.m_name;

                while (station.GetFuel() < station.m_maxFuel)
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
                        station.m_nview.InvokeRPC("RPC_AddFuel");
                        pulled = true;
                        break;
                    }

                    if (!pulled)
                    {
                        break;
                    }
                }
            }
        }
    }
}
