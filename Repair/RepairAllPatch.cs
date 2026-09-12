using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmartCraftStorage.Repair
{
    [HarmonyPatch(typeof(InventoryGui), "RepairOneItem")]
    internal static class RepairAllPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            try
            {
                if (Player.m_localPlayer == null)
                {
                    return true;
                }

                var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                if ((currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat())
                    || (currentCraftingStation != null && !currentCraftingStation.CheckUsable(Player.m_localPlayer, false)))
                {
                    return true;
                }

                var wornItems = new List<ItemDrop.ItemData>();
                Player.m_localPlayer.GetInventory().GetWornItems(wornItems);

                foreach (var item in wornItems)
                {
                    if (!__instance.CanRepair(item))
                    {
                        continue;
                    }

                    Player.m_localPlayer.RaiseSkill(Skills.SkillType.Crafting, 1f - item.m_durability / item.GetMaxDurability());
                    item.m_durability = item.GetMaxDurability();

                    if (currentCraftingStation != null)
                    {
                        currentCraftingStation.m_repairItemDoneEffects.Create(currentCraftingStation.transform.position, Quaternion.identity);
                    }
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return true;
            }
        }
    }
}
