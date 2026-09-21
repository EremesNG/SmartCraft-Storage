using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using EpicLoot;
using EpicLoot_UnityLib;
using HarmonyLib;
using SmartCraftStorage.ItemMarking;

internal static class Program
{
    private const string EpicLootGuid = "randyknapp.mods.epicloot";
    private static readonly Harmony Patches = new Harmony("test.smartcraft.sacrifice");

    private static int Main()
    {
        var cases = new List<(string Name, Action Run)>
        {
#if MissingFilter || MissingSelection || WrongUnregister
            ("unsupported external interface is skipped without partial registration or patches", UnsupportedInterfaceIsSkipped)
#else
            ("marked carried shield and sword are excluded without changing unlocked candidates", MarkedCandidatesAreExcluded),
            ("a stale mixed selection preserves all items and grants no materials or sockets", StaleSelectionIsCanceled),
            ("unlocked bulk sacrifice retains exact amounts and socket rewards", UnlockedSelectionWorks),
            ("an empty selection retains normal harmless behavior", EmptySelectionWorks),
            ("an absent Epic Loot plugin installs nothing", AbsentPluginIsSkipped),
            ("other mods sacrifice filters continue to apply", OtherFiltersArePreserved),
            ("rejected filter registration installs no execution patch", RejectedRegistrationIsSkipped),
            ("a duplicate ID is preserved without adding an execution patch", DuplicateFilterIsPreserved),
            ("external registration exceptions do not escape initialization", RegistrationExceptionIsContained),
            ("patch installation failure rolls back only the new filter", FailedPatchRollsBack),
            ("an unreadable execution selection is canceled before side effects", UnreadableSelectionIsCanceled)
#endif
        };
        int failures = 0;
        foreach (var test in cases)
        {
            Reset();
            try { test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception error) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + error.GetBaseException().Message); }
        }
        Patches.UnpatchSelf();
        Console.WriteLine($"{cases.Count - failures}/{cases.Count} passed");
        return failures == 0 ? 0 : 1;
    }

    private static void Reset()
    {
        Patches.UnpatchSelf();
        Chainloader.PluginInfos.Clear();
        Chainloader.PluginInfos.Add(EpicLootGuid, new object());
        API.Filters.Clear();
        API.RejectRegistration = false;
        API.ThrowRegistration = false;
        UnityEngine.Debug.Messages.Clear();
    }

    private static void Setup(bool failPatch = false)
    {
        // Optional lookup lets the original pre-fix behavior run for the RED
        // case before the new production module exists. No policy is mocked.
        var type = typeof(Program).Assembly.GetType("SmartCraftStorage.Integrations.EpicLootSacrificeProtection");
        type?.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { failPatch ? null : Patches });
    }

    private static ItemDrop.ItemData Item(string name, bool locked = false)
    {
        var item = new ItemDrop.ItemData { Name = name };
        ItemFlags.SetLocked(item, locked);
        return item;
    }

    private static void MarkedCandidatesAreExcluded()
    {
        var shield = Item("shield", true);
        var sword = Item("sword", true);
        var freeShield = Item("shield");
        freeShield.m_customData["magic"] = "distinct shield";
        var freeAxe = Item("axe");
        var ineligible = Item("stone");
        ineligible.Eligible = false;
        var ui = new SacrificeUI();
        ui.Inventory.AddRange(new[] { shield, freeShield, sword, ineligible, freeAxe });
        Setup();
        ui.Open();
        Equal(2, ui.Visible.Count);
        Equal(freeShield, ui.Visible[0]);
        Equal(freeAxe, ui.Visible[1]);
        ItemFlags.SetLocked(shield, false);
        ui.Open();
        Equal(3, ui.Visible.Count);
        Equal(shield, ui.Visible[0]);
        Equal("distinct shield", freeShield.m_customData["magic"]);
        Equal(true, API.Filters[SmartCraftStorage.Plugin.PluginGuid](null));
    }

    private static void StaleSelectionIsCanceled()
    {
        var shield = Item("shield");
        shield.SocketCount = 2;
        shield.m_customData["magic"] = "reserved enchantment";
        var sword = Item("sword");
        var ui = new SacrificeUI();
        ui.Inventory.AddRange(new[] { shield, sword });
        Setup();
        ui.Open();
        Equal(2, ui.Visible.Count);
        ui.Select(shield);
        ui.Select(sword);
        ItemFlags.SetLocked(shield, true);
        ui.ClickSacrifice();
        Equal(1, shield.m_stack);
        Equal(1, sword.m_stack);
        Equal("reserved enchantment", shield.m_customData["magic"]);
        Equal(0, ui.Rewards);
        Equal(0, ui.ReclaimedSockets);
        Equal(1, ui.CancelCount);
        Equal(1, ui.RefreshCount);
        Equal(1, ui.Visible.Count);
        Equal(sword, ui.Visible[0]);
        Equal(0, ui.AvailableItems.Selected.Count);
    }

    private static void UnlockedSelectionWorks()
    {
        var marked = Item("shield", true);
        var free = Item("shield");
        free.SocketCount = 2;
        var stack = Item("rune");
        stack.m_stack = 4;
        var ui = new SacrificeUI();
        ui.Inventory.AddRange(new[] { marked, free, stack });
        Setup();
        ui.Select(free);
        ui.Select(stack, 2);
        ui.ClickSacrifice();
        Equal(1, marked.m_stack);
        Equal(0, free.m_stack);
        Equal(2, stack.m_stack);
        Equal(3, ui.Rewards);
        Equal(2, ui.ReclaimedSockets);
        Equal(true, ui.Inventory.Contains(marked));
        Equal(false, ui.Inventory.Contains(free));
    }

    private static void EmptySelectionWorks()
    {
        var ui = new SacrificeUI();
        Setup();
        ui.ClickSacrifice();
        Equal(0, ui.Rewards);
        Equal(0, ui.ReclaimedSockets);
        Equal(1, ui.RefreshCount);
    }

    private static void AbsentPluginIsSkipped()
    {
        Chainloader.PluginInfos.Clear();
        Setup();
        Equal(0, API.Filters.Count);
        NoExecutionPatch();
        Equal(0, UnityEngine.Debug.Messages.Count);
    }

    private static void OtherFiltersArePreserved()
    {
        var forbidden = Item("axe");
        var reserved = Item("sword", true);
        var free = Item("shield");
        API.Filters.Add("other-mod", item => item != forbidden);
        Setup();
        var list = SacrificeController.GetCandidates(new[] { forbidden, reserved, free });
        Equal(1, list.Count);
        Equal(free, list[0]);
        Equal(2, API.Filters.Count);
    }

    private static void RejectedRegistrationIsSkipped()
    {
        API.RejectRegistration = true;
        Setup();
        Equal(0, API.Filters.Count);
        NoExecutionPatch();
        WarningWasLogged();
    }

    private static void DuplicateFilterIsPreserved()
    {
        Func<ItemDrop.ItemData, bool> existing = item => false;
        API.Filters.Add(SmartCraftStorage.Plugin.PluginGuid, existing);
        Setup();
        Equal(existing, API.Filters[SmartCraftStorage.Plugin.PluginGuid]);
        NoExecutionPatch();
        WarningWasLogged();
    }

    private static void RegistrationExceptionIsContained()
    {
        API.ThrowRegistration = true;
        Setup();
        Equal(0, API.Filters.Count);
        NoExecutionPatch();
        WarningWasLogged();
    }

    private static void FailedPatchRollsBack()
    {
        API.Filters.Add("other-mod", item => true);
        // A missing external patcher forces the same post-registration failure
        // boundary without mocking any SmartCraft policy or installed patch.
        Setup(failPatch: true);
        Equal(false, API.Filters.ContainsKey(SmartCraftStorage.Plugin.PluginGuid));
        Equal(true, API.Filters.ContainsKey("other-mod"));
        NoExecutionPatch();
        WarningWasLogged();
    }

    private static void UnreadableSelectionIsCanceled()
    {
        var shield = Item("shield");
        var ui = new SacrificeUI();
        ui.Inventory.Add(shield);
        Setup();
        ui.AvailableItems = null;
        ui.ClickSacrifice();
        Equal(1, shield.m_stack);
        Equal(0, ui.Rewards);
        Equal(0, ui.ReclaimedSockets);
        Equal(1, ui.CancelCount);
        Equal(1, ui.RefreshCount);
        Equal(true, UnityEngine.Debug.Messages.Exists(message => message.Contains("selection could not be checked")));
    }

    private static void UnsupportedInterfaceIsSkipped()
    {
        Setup();
        Equal(0, API.Filters.Count);
        NoExecutionPatch();
        WarningWasLogged();
    }

    private static void NoExecutionPatch()
    {
        var target = typeof(SacrificeUI).GetMethod("SacrificeItems", BindingFlags.NonPublic | BindingFlags.Instance);
        var patchInfo = Harmony.GetPatchInfo(target);
        Equal(false, patchInfo != null && patchInfo.Owners.Contains(Patches.Id));
    }

    private static void WarningWasLogged()
    {
        Equal(true, UnityEngine.Debug.Messages.Exists(message => message.Contains("[SmartCraftStorage]")));
        Equal(false, UnityEngine.Debug.Messages.Exists(message => message.Contains("Registered Epic Loot sacrifice protection")));
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"expected {expected}, got {actual}");
    }
}
