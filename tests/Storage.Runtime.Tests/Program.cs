using System;
using System.Collections.Generic;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;
using SmartCraftStorage.Storage.Runtime;

internal static class Program
{
    private static readonly List<(string Name, Action Body)> Cases = new();
    private static int Main()
    {
        Cases.Add(("journal reloads durable operations after same-world manager replacement", JournalRejoinsSameWorld));
        Cases.Add(("pooled ZDO objects cannot redirect a journal write into a chest", JournalIgnoresReusedObjects));
        Cases.Add(("world changes isolate records and returning reloads them", JournalSeparatesWorlds));
        Cases.Add(("display and installed layout retain complete item metadata", ItemRoundTrip));
        var failed = 0;
        foreach (var test in Cases) { try { test.Body(); Console.WriteLine("PASS " + test.Name); } catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + test.Name + ": " + error.Message); } }
        Console.WriteLine($"{Cases.Count - failed}/{Cases.Count} passed");
        return failed == 0 ? 0 : 1;
    }
    private static void JournalRejoinsSameWorld()
    {
        new ZDOMan();
        var journal = new StorageJournal();
        journal.Save(new StorageTransactionRecord("name:1", "name", StorageOperationStatus.Confirmed, Array.Empty<StorageParticipantPlan>(), "saved", actorId: 7));
        journal.SaveEffect(new StorageEffectRecord(1, "42", "output:1", 7, "chest", "context", null, null, null, "custody", "delivery:1", 10, 4, 6, StorageEffectStage.DeliveryPending));
        new ZDOMan(ZDOMan.instance.Objects);
        Equal("saved", journal.Load("name:1").Message);
        Equal("custody", journal.LoadEffect("output:1").Escrow);
        Equal(1, journal.PendingEffects.Count);
    }
    private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void JournalIgnoresReusedObjects()
    {
        var manager = new ZDOMan(); var journal = new StorageJournal();
        journal.Save(new StorageTransactionRecord("name:pool", "name", StorageOperationStatus.Confirmed, Array.Empty<StorageParticipantPlan>(), "first"));
        var oldRecord = manager.Objects.Single(x => x.GetString("scs.storage.journal.record.v1") == "name:pool");
        oldRecord.Data.Clear(); oldRecord.SetPrefab("chest".GetStableHashCode()); oldRecord.Set("foreign-data", "keep");
        journal.Save(new StorageTransactionRecord("name:pool", "name", StorageOperationStatus.Confirmed, Array.Empty<StorageParticipantPlan>(), "updated"));
        Equal("", oldRecord.GetString("scs.storage.journal.record.data.v1")); Equal("keep", oldRecord.GetString("foreign-data"));
        var oldRoot = manager.Objects.Single(x => x.GetBool("scs.storage.journal.root.v1"));
        oldRoot.Data.Clear(); oldRoot.SetPrefab("chest".GetStableHashCode());
        Equal("updated", journal.Load("name:pool").Message);
        Equal(false, oldRoot.GetBool("scs.storage.journal.root.v1"));
    }
    private static void JournalSeparatesWorlds()
    {
        new ZDOMan(); var journal = new StorageJournal();
        journal.Save(new StorageTransactionRecord("name:world", "name", StorageOperationStatus.Confirmed, Array.Empty<StorageParticipantPlan>(), "world-42"));
        ZNet.instance.World = 43;
        Equal<StorageTransactionRecord>(null, journal.Load("name:world"));
        ZNet.instance.World = 42;
        Equal("world-42", journal.Load("name:world").Message);
    }
    private static void ItemRoundTrip()
    {
        var prefab = new UnityEngine.GameObject { name = "Wood", Item = new ItemDrop() };
        prefab.Item.m_itemData = new ItemDrop.ItemData { m_shared = new ItemDrop.SharedData { m_name = "$item_wood", m_maxStackSize = 50, m_icons = new[] { "wood-icon" } }, m_dropPrefab = prefab };
        ObjectDB.instance.Items[prefab.name] = prefab;
        var item = prefab.Item.m_itemData.Clone();
        item.m_stack = 17; item.m_durability = 12.34567f; item.m_quality = 2; item.m_variant = 3;
        item.m_worldLevel = 4; item.m_crafterName = "custom maker"; item.m_crafterID = 0;
        item.m_cheated = true; item.m_pickedUp = true; item.m_customData["mod:data"] = "rare";
        var stack = GameInventoryAdapter.ToStack(item, 0);
        var displayed = GameInventoryAdapter.DisplayItem(stack);
        Equal("$item_wood", displayed.m_shared?.m_name);
        Equal("wood-icon", displayed.m_shared.m_icons.Single());
        Equal(stack.Identity, GameInventoryAdapter.Identity(displayed));
        Equal(17, displayed.m_stack); Equal(true, displayed.m_cheated); Equal(true, displayed.m_pickedUp);
        var inventory = new Inventory("player", null, 2, 1);
        GameInventoryAdapter.ApplyLayout(inventory, new StorageInventory("player", "r", 2, 1, new[] { stack.At(1, 17) }));
        Equal(stack.Identity, GameInventoryAdapter.Identity(inventory.GetAllItems().Single()));
        Equal(1, inventory.GetAllItems().Single().m_gridPos.x);
        var retained = inventory.GetAllItems().Single(); retained.m_equipped = true;
        GameInventoryAdapter.ApplyLayout(inventory, new StorageInventory("player", "r", 2, 1, new[] { stack.At(1, 17) }));
        Equal(true, ReferenceEquals(retained, inventory.GetAllItems().Single())); Equal(true, retained.m_equipped);
        var threw = false;
        try { GameInventoryAdapter.ApplyLayout(inventory, new StorageInventory("player", "r", 2, 1, new[] { stack.At(0, 17) })); }
        catch (InvalidOperationException) { threw = true; }
        Equal(true, threw); Equal(1, retained.m_gridPos.x); Equal(true, retained.m_equipped);
        displayed.m_customData["mod:data"] = "changed";
        Equal("rare", item.m_customData["mod:data"]);
    }
}
