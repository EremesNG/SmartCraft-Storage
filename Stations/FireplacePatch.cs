using HarmonyLib;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    [HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
    internal static class FireplacePatch
    {
        private static void Postfix(Fireplace __instance)
        {
            try
            {
                if (!StationConfig.FireplaceAutoRefuel.Value
                    || __instance.m_nview == null || !__instance.m_nview.IsValid()
                    || !__instance.m_nview.IsOwner())
                {
                    return;
                }

                if (__instance.m_fuelItem == null)
                {
                    return;
                }

                var player = Player.m_localPlayer;
                if (player == null)
                {
                    return;
                }

                float currentFuel = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
                if (Mathf.CeilToInt(currentFuel) >= __instance.m_maxFuel)
                {
                    return;
                }

                string fuelName = __instance.m_fuelItem.m_itemData.m_shared.m_name;

                foreach (var container in NearbyContainers.Find(__instance.transform.position, StationConfig.FireplaceRadius.Value, player))
                {
                    if (Mathf.CeilToInt(currentFuel) >= __instance.m_maxFuel)
                    {
                        break;
                    }

                    var chestInventory = container.GetInventory();
                    var fuel = UnlockedInventory.FindItem(chestInventory, fuelName, true);
                    if (fuel == null)
                    {
                        continue;
                    }

                    if (!NearbyContainers.TryClaimWriteAccess(container))
                    {
                        continue;
                    }

                    if (UnlockedInventory.RemoveItem(chestInventory, fuel, 1) != 1)
                    {
                        continue;
                    }
                    __instance.m_nview.InvokeRPC("RPC_AddFuel");
                    currentFuel += 1f;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
