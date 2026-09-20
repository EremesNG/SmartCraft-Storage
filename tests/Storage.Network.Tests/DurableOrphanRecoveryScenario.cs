using System;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

internal static class DurableOrphanRecoveryScenario
{
    private static uint _nextId = 9000;

    internal static void Run(Action<bool, string> check)
    {
        var player = Player.m_localPlayer;
        player.m_customData.Clear();
        player.m_nview.m_zdo = ObjectData();
        player.m_nview.m_zdo.Set(ZDOVars.s_playerID, 77L);
        var first = Chest("durable-first", 3);
        var second = Chest("durable-second", 2);
        var source = View("durable-cooking-source");
        var staleButValidSource = View("durable-stale-id-decoy");
        const string operation = "1:13201:output:1";
        var staleTarget = staleButValidSource.GetZDO().m_uid.ToString();
        var oldDelivery = Derived(operation, "delivery-0");
        const string effectData = "native-capture-proof";
        var output = first.GetInventory().GetAllItems().Single().Clone(); output.m_stack = 1;
        var outbox = new StorageInventory("outbox:" + operation, "1", 1, 1,
            new[] { GameInventoryAdapter.ToStack(output, 0).At(0, 1) });
        var encodedOutbox = StorageRpc.Encode(outbox);
        source.GetZDO().Set("scs.storage.effect.active.v1", operation);
        source.GetZDO().Set(StorageService.CaptureIntentKey, operation + "\n" + effectData);
        source.GetZDO().Set("scs.output.outbox.v1." + operation, encodedOutbox);
        source.GetZDO().Set("scs.storage.effect.receipts.v1",
            operation + ":" + StorageEffectStage.CapturePending + "|A|");
        foreach (var zdo in new[] { first.m_nview.GetZDO(), second.m_nview.GetZDO(), source.GetZDO() })
            zdo.Set("scs.storage.active.v1", oldDelivery);

        var fresh = Chest("durable-fresh", 1);
        var depositItem = first.GetInventory().GetAllItems().Single().Clone(); depositItem.m_stack = 1;
        depositItem.m_gridPos = new Vector2i(0, 0); player.GetInventory().GetAllItems().Add(depositItem);
        var scene = ZNetScene.instance; ZNetScene.s_instance = null;
        var unavailable = new StorageService(() => new StorageSettings(true, 32, 64, 128, 8), () => 100)
            .Deposit(new StorageContext(player, player.transform.position, 10f, StorageScope.Direct, fresh.m_nview),
                depositItem, 1, "unavailable-durable-deposit");
        ZNetScene.s_instance = scene;
        check(unavailable.Status == StorageOperationStatus.Rejected && depositItem.m_stack == 1 &&
              string.IsNullOrEmpty(fresh.m_nview.GetZDO().GetString("scs.storage.active.v1", "")),
            "unavailable journal rejects before participant reservation or inventory mutation");

        var routed = new ZRoutedRpc(true); routed.SetUID(ZDOMan.GetSessionID());
        var service = new StorageService(() => new StorageSettings(true, 32, 64, 128, 8), () => 100);
        var deposit = service.Deposit(new StorageContext(player, player.transform.position, 10f, StorageScope.Direct, fresh.m_nview),
            depositItem, 1, "fresh-durable-deposit");
        check(deposit.Status == StorageOperationStatus.Confirmed &&
              !string.IsNullOrEmpty(fresh.m_nview.GetZDO().GetString("scs.storage.identity.v1", "")),
            "a fresh unstamped participant is identified before its revision snapshot and commits normally");
        var pendingIntent = new StorageRequestIntent("output", operation, ZNet.instance.GetWorldUID().ToString(),
            StorageScope.Processor, 0, 0, 0, 10, staleTarget, Array.Empty<byte>());
        check(StorageService.ResolvePendingAnchor(pendingIntent) == source,
            "profile recovery follows unique custody markers when a stale raw ID resolves to another valid source");
        foreach (var zdo in new[] { source.GetZDO(), first.m_nview.GetZDO(), second.m_nview.GetZDO() })
            StorageJournal.EnsureStableIdentity(zdo);

        var request = Request(operation, staleTarget, effectData, encodedOutbox, player, source, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", request);
        check(service._journal.LoadEffect(operation) == null,
            "legacy recovery rejects self-RPC proof fields without matching persisted host profile custody");
        PersistProfileProof(player, operation, staleTarget, effectData, encodedOutbox, source, actorId: 78L);
        request = Request(operation, staleTarget, effectData, encodedOutbox, player, source, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", request);
        check(service._journal.LoadEffect(operation) == null,
            "legacy recovery rejects persisted profile custody for the wrong actor");
        PersistProfileProof(player, operation, staleTarget, effectData, encodedOutbox, source, actorId: 77L);
        request = Request(operation, staleTarget, effectData, encodedOutbox, player, source, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", request);
        request = Request(operation, staleTarget, effectData, encodedOutbox, player, source, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", request);
        request = Request(operation, staleTarget, effectData, encodedOutbox, player, source, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", request);
        for (var i = 0; i < 8; i++) service.Resume(operation);

        var recovered = service._journal.LoadEffect(operation);
        check(recovered != null && recovered.OperationId == operation && recovered.Capture.TargetId == source.GetZDO().m_uid.ToString(),
            "legacy orphan persists custody under the opaque operation ID and remaps only its source target");
        check(service._journal.Load(oldDelivery)?.Status == StorageOperationStatus.Rejected &&
            recovered.DeliveryTransactionId == Derived(operation, "delivery-1"),
            "legacy delivery-0 is durably tombstoned before recovery uses delivery-1");
        check(first.GetInventory().GetAllItems().Sum(x => x.m_stack) + second.GetInventory().GetAllItems().Sum(x => x.m_stack) == 6,
            "one captured native output is conserved into storage exactly once");
        check(new[] { first.m_nview.GetZDO(), second.m_nview.GetZDO(), source.GetZDO() }
                .All(x => string.IsNullOrEmpty(x.GetString("scs.storage.active.v1", ""))) &&
              string.IsNullOrEmpty(source.GetZDO().GetString("scs.storage.effect.active.v1", "")),
            "only the proven delivery reservations and completed source reservation are released");

        var blockedSource = View("durable-mismatch-source");
        const string blocked = "1:13202:output:1";
        var blockedDelivery = Derived(blocked, "delivery-0");
        blockedSource.GetZDO().Set("scs.storage.effect.active.v1", blocked);
        blockedSource.GetZDO().Set(StorageService.CaptureIntentKey, blocked + "\n" + effectData);
        blockedSource.GetZDO().Set("scs.output.outbox.v1." + blocked, encodedOutbox);
        blockedSource.GetZDO().Set("scs.storage.active.v1", blockedDelivery);
        var blockedRequest = Request(blocked, staleTarget, effectData, encodedOutbox, player, blockedSource, first, second, false);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", blockedRequest);
        blockedRequest = Request(blocked, staleTarget, effectData, encodedOutbox, player, blockedSource, first, second, false);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", blockedRequest);
        check(service._journal.LoadEffect(blocked) == null && blockedSource.GetZDO().GetString("scs.storage.active.v1", "") == blockedDelivery &&
              blockedSource.GetZDO().GetString("scs.storage.effect.active.v1", "") == blocked,
            "missing applied capture receipt remains fail closed and releases no reservation");

        var interruptedSource = View("durable-interrupted-source");
        const string interrupted = "1:13203:output:1";
        var interruptedDelivery = Derived(interrupted, "delivery-0");
        SeedCapturedSource(interruptedSource, interrupted, effectData, encodedOutbox, interruptedDelivery, true);
        first.m_nview.GetZDO().Set("scs.storage.active.v1", interruptedDelivery);
        var interruptedCapture = new StorageEffectDescriptor("processor.capture", interruptedSource.GetZDO().m_uid.ToString(), 77L, effectData);
        service._journal.SaveEffect(new StorageEffectRecord(1, ZNet.instance.GetWorldUID().ToString(), interrupted, 77L,
            interruptedSource.GetZDO().m_uid.ToString(), "legacy", interruptedCapture, null, null, encodedOutbox,
            Derived(interrupted, "delivery-1"), 1, 0, 1, StorageEffectStage.Captured, new[] { "capture" },
            "legacy-orphan-recovery", false));
        PersistProfileProof(player, interrupted, staleTarget, effectData, encodedOutbox, interruptedSource, actorId: 77L);
        var interruptedRequest = Request(interrupted, staleTarget, effectData, encodedOutbox, player, interruptedSource, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", interruptedRequest);
        for (var i = 0; i < 4; i++) service.Resume(interrupted);
        check(service._journal.Load(interruptedDelivery)?.Status == StorageOperationStatus.Rejected &&
              string.IsNullOrEmpty(interruptedSource.GetZDO().GetString("scs.storage.active.v1", "")) &&
              string.IsNullOrEmpty(first.m_nview.GetZDO().GetString("scs.storage.active.v1", "")),
            "restart reconstructs a missing legacy cleanup record and releases only its durable participant set");

        var receiptSource = View("durable-receipt-source");
        var releasedParticipant = View("durable-released-participant");
        const string receiptBlocked = "1:13204:output:1";
        var receiptDelivery = Derived(receiptBlocked, "delivery-0");
        SeedCapturedSource(receiptSource, receiptBlocked, effectData, encodedOutbox, receiptDelivery, true);
        first.m_nview.GetZDO().Set("scs.storage.active.v1", receiptDelivery);
        releasedParticipant.GetZDO().Set("scs.storage.receipts.v1", receiptDelivery);
        PersistProfileProof(player, receiptBlocked, staleTarget, effectData, encodedOutbox, receiptSource, actorId: 77L);
        var receiptRequest = Request(receiptBlocked, staleTarget, effectData, encodedOutbox, player, receiptSource, first, second, true);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.GetSessionID(), "SCS_StorageRequest_v1", receiptRequest);
        check(service._journal.LoadEffect(receiptBlocked) == null &&
              first.m_nview.GetZDO().GetString("scs.storage.active.v1", "") == receiptDelivery,
            "an applied legacy delivery receipt outside the active reservation set blocks fresh delivery");

        var futureSource = View("durable-future-source");
        var futureDrop = output.m_dropPrefab.GetComponent<ItemDrop>() ?? output.m_dropPrefab.AddComponent<ItemDrop>();
        futureDrop.m_itemData = output.Clone(); futureDrop.m_itemData.m_dropPrefab = output.m_dropPrefab;
        RegisterItemPrefab(output.m_dropPrefab);
        var futureService = new StorageService(() => new StorageSettings(true, 32, 64, 128, 8), () => 100);
        var futureHandler = new PendingEffectHandler(futureSource.GetZDO(), ZNet.instance.GetWorldUID().ToString(), 77L);
        futureService.RegisterEffect(futureHandler);
        ZNet.m_isServer = false;
        var futureResult = futureService.CaptureOutput(new StorageContext(player, player.transform.position, 10f, StorageScope.Processor, futureSource),
            output, 1, new StorageEffectDescriptor("test.pending", futureSource.GetZDO().m_uid.ToString(), 77L, "future"),
            null, "future-capture-authority", false);
        ZNet.m_isServer = true;
        check(futureHandler.SawAuthority,
            "future capture custody records server-verifiable world and actor authority at the source; status=" +
            futureResult.Status + "; message=" + futureResult.Message);
    }

    private static void SeedCapturedSource(ZNetView source, string operation, string data, string encodedOutbox,
        string delivery, bool includeCaptureReceipt)
    {
        StorageJournal.EnsureStableIdentity(source.GetZDO());
        source.GetZDO().Set("scs.storage.effect.active.v1", operation);
        source.GetZDO().Set(StorageService.CaptureIntentKey, operation + "\n" + data);
        source.GetZDO().Set("scs.output.outbox.v1." + operation, encodedOutbox);
        source.GetZDO().Set("scs.storage.active.v1", delivery);
        if (includeCaptureReceipt)
            source.GetZDO().Set("scs.storage.effect.receipts.v1", operation + ":" + StorageEffectStage.CapturePending + "|A|");
    }

    private static void PersistProfileProof(Player player, string operation, string staleTarget, string data,
        string encodedOutbox, ZNetView source, long actorId)
    {
        var body = OutputBody(staleTarget, data, encodedOutbox, actorId);
        var intent = new StorageRequestIntent("output", operation, ZNet.instance.GetWorldUID().ToString(),
            StorageScope.Processor, 0, 0, 0, 10, staleTarget, body);
        using (var stream = new System.IO.MemoryStream()) using (var writer = new System.IO.BinaryWriter(stream))
        {
            var encoded = intent.Encode(); writer.Write(1); writer.Write(operation); writer.Write(encoded.Length); writer.Write(encoded);
            player.m_customData["scs.storage.pending.requests.v1"] = Convert.ToBase64String(stream.ToArray());
        }
        player.m_customData["scs.storage.escrow.v1." + operation] = encodedOutbox;
        player.m_customData["scs.storage.escrow.v1." + operation + ".effect"] = EncodeEffect(
            new StorageEffectDescriptor("processor.capture", staleTarget, actorId, data));
    }

    private static byte[] OutputBody(string target, string data, string encodedOutbox, long actorId)
    {
        var body = new ZPackage(); body.Write(encodedOutbox); body.Write(1);
        WriteEffect(body, new StorageEffectDescriptor("processor.capture", target, actorId, data));
        WriteEffect(body, null); body.Write(false); return body.GetArray();
    }

    private static string EncodeEffect(StorageEffectDescriptor effect) => string.Join("\n",
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(effect.Kind)),
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(effect.TargetId)), effect.ActorId.ToString(),
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(effect.Data)));

    private static void RegisterItemPrefab(GameObject prefab)
    {
        var database = ObjectDB.instance;
        if (database == null)
        {
            var holder = new GameObject("durable-object-db"); holder.SetActive(false);
            database = holder.AddComponent<ObjectDB>();
            foreach (var staticField in typeof(ObjectDB).GetFields(System.Reflection.BindingFlags.Static |
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                     .Where(candidate => candidate.FieldType == typeof(ObjectDB)))
                staticField.SetValue(null, database);
        }
        var prefabsField = typeof(ObjectDB).GetField("m_items", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var prefabs = prefabsField?.GetValue(database) as System.Collections.IList;
        if (prefabs == null && prefabsField != null)
        {
            prefabs = (System.Collections.IList)Activator.CreateInstance(prefabsField.FieldType);
            prefabsField.SetValue(database, prefabs);
        }
        if (prefabs != null && !prefabs.Contains(prefab)) prefabs.Add(prefab);
        var field = typeof(ObjectDB).GetField("m_itemByHash", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var items = field?.GetValue(database) as System.Collections.IDictionary;
        if (items == null && field != null)
        {
            items = (System.Collections.IDictionary)Activator.CreateInstance(field.FieldType);
            field.SetValue(database, items);
        }
        items?[prefab.name.GetStableHashCode()] = prefab;
    }

    private static ZPackage Request(string operation, string staleTarget, string data, string encodedOutbox, Player player,
        ZNetView source, Container first, Container second, bool includeCaptureReceipt)
    {
        if (includeCaptureReceipt && string.IsNullOrEmpty(source.GetZDO().GetString("scs.storage.effect.receipts.v1", "")))
            source.GetZDO().Set("scs.storage.effect.receipts.v1", operation + ":" + StorageEffectStage.CapturePending + "|A|");
        var package = new ZPackage(); package.Write("output"); package.Write(operation); package.Write((int)StorageScope.Processor);
        package.Write(Vector3.zero); package.Write(10f); package.Write(source.GetZDO().m_uid);
        package.Write(StorageRpc.Encode(GameInventoryAdapter.Snapshot("player:77", GameInventoryAdapter.Revision(player.GetInventory()), player.GetInventory())));
        package.Write(0); package.Write(2);
        foreach (var chest in new[] { first, second })
        {
            package.Write(chest.m_nview.GetZDO().m_uid);
            package.Write(StorageRpc.Encode(GameInventoryAdapter.Snapshot(chest.m_nview.GetZDO().m_uid.ToString(),
                chest.m_nview.GetZDO().DataRevision.ToString(), chest.GetInventory())));
            package.Write("");
        }
        package.Write(0); package.Write(encodedOutbox); package.Write(1);
        WriteEffect(package, new StorageEffectDescriptor("processor.capture", staleTarget, 77, data));
        WriteEffect(package, null); package.Write(false);
        package.Write("scs.storage.profile-proof.v1"); package.Write(ZNet.instance.GetWorldUID().ToString()); package.Write(77L);
        return package;
    }

    private static void WriteEffect(ZPackage package, StorageEffectDescriptor effect)
    {
        package.Write(effect != null); if (effect == null) return;
        package.Write(effect.Kind); package.Write(effect.TargetId); package.Write(effect.ActorId); package.Write(effect.Data);
    }

    private static string Derived(string operation, string phase)
    {
        var separator = operation.LastIndexOf(':');
        return operation.Substring(0, separator) + ":" + phase + operation.Substring(separator);
    }

    private static Container Chest(string name, int amount)
    {
        var view = View(name); var chest = view.gameObject.AddComponent<Container>(); chest.m_nview = view; chest.m_checkGuardStone = false;
        chest.m_inventory = new Inventory(name, null, 2, 1); var prefab = new GameObject("RawMeat"); prefab.SetActive(false);
        chest.m_inventory.GetAllItems().Add(new ItemDrop.ItemData { m_dropPrefab = prefab,
            m_shared = new ItemDrop.ItemData.SharedData { m_name = "$item_rawmeat", m_maxStackSize = 20 },
            m_stack = amount, m_quality = 1, m_gridPos = new Vector2i(0, 0) });
        return chest;
    }

    private static ZNetView View(string name)
    {
        var obj = new GameObject(name); obj.SetActive(false); var view = obj.AddComponent<ZNetView>(); view.m_zdo = ObjectData();
        ZNetScene.instance.AddInstance(view.m_zdo, view); return view;
    }

    private static ZDO ObjectData()
    {
        var zdo = ZDOPool.Create(new ZDOID(ZDOMan.GetSessionID(), ++_nextId), Vector3.zero);
        ZDOMan.instance.m_objectsByID.Add(zdo.m_uid, zdo); zdo.SetOwner(ZDOMan.GetSessionID()); zdo.Persistent = true; return zdo;
    }

    private sealed class PendingEffectHandler : IStorageEffectHandler
    {
        private readonly ZDO _source; private readonly string _world; private readonly long _actor;
        internal bool SawAuthority { get; private set; }
        internal PendingEffectHandler(ZDO source, string world, long actor) { _source = source; _world = world; _actor = actor; }
        public string Kind => "test.pending";
        public StorageEffectResult Validate(StorageEffectDescriptor effect, Inventory escrow, out string reason)
        {
            SawAuthority = _source.GetString("scs.storage.capture.authority.v1", "") ==
                "future-capture-authority\n" + _world + "\n" + _actor;
            reason = ""; return StorageEffectResult.Applied;
        }
        public string CaptureState(string operationId, StorageEffectDescriptor effect, Inventory escrow) => "before";
        public StorageEffectRecovery Reconcile(string operationId, StorageEffectDescriptor effect, Inventory escrow, string beforeState) =>
            StorageEffectRecovery.NotApplied;
        public StorageEffectResult Apply(string operationId, StorageEffectDescriptor descriptor, Inventory escrow) =>
            StorageEffectResult.NotReady;
    }
}
