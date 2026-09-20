using System;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

internal static class LocalRecoveryScenario
{
    private static uint _nextId = 100000;
    internal static void SetupWorld()
    {
        new ZDOMan(512);
        var gameObject = new GameObject("recovery-game"); gameObject.SetActive(false);
        Game.instance = gameObject.AddComponent<Game>();
        var sceneObject = new GameObject("recovery-scene"); sceneObject.SetActive(false);
        ZNetScene.s_instance = sceneObject.AddComponent<ZNetScene>();
        var journal = new GameObject("SCS_StorageJournal"); journal.SetActive(false);
        ZNetScene.instance.m_namedPrefabs.Add(journal.name.GetStableHashCode(), journal);
    }
    internal static void Run(Action<bool, string> check)
    {
        ZNet.m_isServer = true;
        ZNet.instance.m_peers.Clear();
        SetupWorld();
        var routed = new ZRoutedRpc(true); routed.SetUID(ZDOMan.GetSessionID());
        var player = Player.m_localPlayer;
        player.m_customData.Clear();
        player.m_nview.m_zdo = ObjectData();
        player.m_nview.m_zdo.Set(ZDOVars.s_playerID, 77L);
        var first = Chest("recovery-first", 5);
        var second = Chest("recovery-second", 2);
        var station = View("recovery-cooking-station");
        const string operation = "fixture:output";
        const string delivery = "fixture:delivery";
        station.GetZDO().Set("scs.storage.effect.active.v1", operation);
        var layouts = new[] { Layout(first, 4), Layout(second, 3) };
        var plans = layouts.Select(layout => new StorageParticipantPlan(layout.Id, layout.Revision, StorageRpc.Encode(layout)))
            .Concat(new[] { new StorageParticipantPlan(station.GetZDO().m_uid.ToString(), station.GetZDO().DataRevision.ToString(), "anchor") });
        double now = 0;
        var service = new StorageService(() => new StorageSettings(true, 32, 64, 128, 4), () => now);
        if (!service.Ready) throw new InvalidOperationException("Synthetic native service is not ready");
        service._journal.Save(new StorageTransactionRecord(delivery, "output", StorageOperationStatus.Preparing,
            plans, requested: 1, accepted: 1, actorId: 77));
        var capture = new StorageEffectDescriptor("processor.capture", station.GetZDO().m_uid.ToString(), 77);
        service._journal.SaveEffect(new StorageEffectRecord(1, "42", operation, 77, capture.TargetId, "", capture,
            null, null, "", delivery, 1, 1, 0, StorageEffectStage.DeliveryPending));
        check(service.IsBusy(station), "a captured output reserves its station before recovery");
        for (var step = 0; step < 40; step++) { now += 8; service.Resume(operation); }
        var result = service.GetOperation(operation);
        check(result.Status == StorageOperationStatus.Confirmed,
            "local output recovery finishes across real service rebinds; status=" + result.Status + "; transaction=" + service._journal.Load(delivery).Message);
        check(first.GetInventory().GetAllItems().Single().m_stack == 4 && second.GetInventory().GetAllItems().Single().m_stack == 3,
            "recovered native inventory layouts move one item and conserve all seven items");
        check(!service.IsBusy(first.m_nview) && !service.IsBusy(second.m_nview) && !service.IsBusy(station),
            "completed output recovery releases both chest reservations and the cooking station");
        now += 8; service.Resume(operation);
        check(service.GetOperation(operation).Status == StorageOperationStatus.Confirmed && first.GetInventory().GetAllItems().Single().m_stack == 4 &&
            second.GetInventory().GetAllItems().Single().m_stack == 3,
            "replaying completed recovery does not apply the item movement twice");
    }

    internal static StorageInventory Layout(Container chest, int amount)
    {
        var view = chest.m_nview;
        var before = GameInventoryAdapter.Snapshot(view.GetZDO().m_uid.ToString(), view.GetZDO().DataRevision.ToString(), chest.GetInventory());
        return new StorageInventory(before.Id, before.Revision, before.Width, before.Height,
            before.Items.Select(item => item.At(item.Slot, amount)));
    }

    internal static Container Chest(string name, int amount)
    {
        var view = View(name);
        var chest = view.gameObject.AddComponent<Container>();
        chest.m_nview = view; chest.m_checkGuardStone = false;
        chest.m_inventory = new Inventory(name, null, 2, 1);
        var prefab = new GameObject("RawMeat"); prefab.SetActive(false);
        chest.m_inventory.GetAllItems().Add(new ItemDrop.ItemData
        {
            m_dropPrefab = prefab, m_shared = new ItemDrop.ItemData.SharedData { m_name = "$item_rawmeat", m_maxStackSize = 20 },
            m_stack = amount, m_quality = 1, m_gridPos = new Vector2i(0, 0)
        });
        return chest;
    }

    internal static ZNetView View(string name)
    {
        var obj = new GameObject(name); obj.SetActive(false);
        var view = obj.AddComponent<ZNetView>();
        view.m_zdo = ObjectData();
        ZNetScene.instance.AddInstance(view.m_zdo, view);
        return view;
    }

    private static ZDO ObjectData()
    {
        var zdo = ZDOPool.Create(new ZDOID(ZDOMan.GetSessionID(), ++_nextId), Vector3.zero);
        ZDOMan.instance.m_objectsByID.Add(zdo.m_uid, zdo);
        zdo.SetOwner(ZDOMan.GetSessionID());
        return zdo;
    }
}
