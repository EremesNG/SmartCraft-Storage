using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using SmartCraftStorage.ItemMarking;
using UnityEngine;

namespace SmartCraftStorage.Integrations
{
    internal static class EpicLootSacrificeProtection
    {
        private static FieldInfo _availableItems;
        private static MethodInfo _getSelectedItems;
        private static PropertyInfo _selectedElement;
        private static MethodInfo _getItem;
        private static MethodInfo _cancel;
        private static MethodInfo _refresh;

        public static void Setup(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey("randyknapp.mods.epicloot"))
            {
                return;
            }

            try
            {
                var api = Type.GetType("EpicLoot.API, EpicLoot");
                var register = api?.GetMethod("RegisterSacrificeFilter", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string), typeof(Func<ItemDrop.ItemData, bool>) }, null);
                var unregister = api?.GetMethod("UnregisterSacrificeFilter", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                var ui = Type.GetType("EpicLoot_UnityLib.SacrificeUI, EpicLoot");
                var element = Type.GetType("EpicLoot_UnityLib.IListElement, EpicLoot");
                var sacrifice = ui?.GetMethod("SacrificeItems", BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                var availableItems = ui?.GetField("AvailableItems", BindingFlags.Public | BindingFlags.Instance);
                var getSelectedItems = availableItems?.FieldType.GetMethod("GetSelectedItems", BindingFlags.Public | BindingFlags.Instance);
                var getItem = element?.GetMethod("GetItem", Type.EmptyTypes);
                var cancel = ui?.GetMethod("Cancel", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                var refresh = ui?.GetMethod("RefreshAvailableItems", BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);

                if (register?.ReturnType != typeof(bool) || unregister?.ReturnType != typeof(bool)
                    || sacrifice?.ReturnType != typeof(void) || getItem?.ReturnType != typeof(ItemDrop.ItemData)
                    || cancel?.ReturnType != typeof(void) || refresh?.ReturnType != typeof(void)
                    || getSelectedItems == null || !getSelectedItems.IsGenericMethodDefinition
                    || getSelectedItems.GetGenericArguments().Length != 1 || getSelectedItems.GetParameters().Length != 0)
                {
                    Debug.LogWarning("[SmartCraftStorage] Epic Loot sacrifice protection is unavailable: unsupported filter or UI API.");
                    return;
                }

                var selectedTuple = typeof(Tuple<,>).MakeGenericType(element, typeof(int));
                getSelectedItems = getSelectedItems.MakeGenericMethod(element);
                if (getSelectedItems.ReturnType != typeof(List<>).MakeGenericType(selectedTuple))
                {
                    Debug.LogWarning("[SmartCraftStorage] Epic Loot sacrifice protection is unavailable: unsupported selection type.");
                    return;
                }

                _availableItems = availableItems;
                _getSelectedItems = getSelectedItems;
                _selectedElement = selectedTuple.GetProperty("Item1");
                _getItem = getItem;
                _cancel = cancel;
                _refresh = refresh;

                Func<ItemDrop.ItemData, bool> canSacrifice = item => item == null || !ItemFlags.IsLocked(item);
                if (!(bool)register.Invoke(null, new object[] { Plugin.PluginGuid, canSacrifice }))
                {
                    Debug.LogWarning("[SmartCraftStorage] Epic Loot rejected the sacrifice filter registration.");
                    return;
                }

                try
                {
                    harmony.Patch(sacrifice, prefix: new HarmonyMethod(typeof(EpicLootSacrificeProtection), nameof(BeforeSacrifice)));
                }
                catch
                {
                    // This call registered our ID; leave other mods' filters intact.
                    unregister.Invoke(null, new object[] { Plugin.PluginGuid });
                    throw;
                }

                Debug.Log("[SmartCraftStorage] Registered Epic Loot sacrifice protection for ALT-locked items.");
            }
            catch (Exception error)
            {
                Debug.LogWarning("[SmartCraftStorage] Could not initialize Epic Loot sacrifice protection.");
                Debug.LogException(error);
            }
        }

        private static bool BeforeSacrifice(object __instance)
        {
            try
            {
                var selected = (IEnumerable)_getSelectedItems.Invoke(_availableItems.GetValue(__instance), null);
                bool hasLockedItem = false;
                foreach (var selection in selected)
                {
                    var item = (ItemDrop.ItemData)_getItem.Invoke(_selectedElement.GetValue(selection, null), null);
                    if (item != null && ItemFlags.IsLocked(item))
                    {
                        hasLockedItem = true;
                        break;
                    }
                }

                if (!hasLockedItem)
                {
                    return true;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[SmartCraftStorage] Canceled Epic Loot sacrifice because the selection could not be checked.");
                Debug.LogException(error);
            }

            // The API filter protects the list, but Epic Loot can retain an old
            // selection. Stop before socket reclaim, item removal or rewards.
            try
            {
                _cancel.Invoke(__instance, null);
                _refresh.Invoke(__instance, null);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            return false;
        }
    }
}
