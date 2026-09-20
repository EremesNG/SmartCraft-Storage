using System;
using System.Collections.Generic;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;

internal static class Program
{
    private static readonly List<(string Name, Action Body)> Cases = new();

    private static int Main()
    {
        Register("query deduplicates physical inventories and exact identities", QueryDeduplicates);
        Register("local naming confirmation survives inline RPC reply", NameConfirmsInline);
        Register("remote naming waits for confirmation and preserves a final rejection", NameWaitsForOwner);
        Register("retrying a confirmed owner name cannot overwrite a later name", NameOwnerDoesNotReplay);
        Register("deposit reports finite partial acceptance and preserves payload", DepositIsFinite);
        Register("withdraw preserves metadata and destination capacity", WithdrawIsFinite);
        Register("directed withdrawal uses only the clicked compatible player slot", WithdrawTargetsSlot);
        Register("directed withdrawal intent rejects a changed slot after reconnect", WithdrawSlotIntent);
        Register("four ten-slot members compact to three and repeat is unchanged", OrganizeIsStable);
        Register("durable transaction resumes after lost acknowledgement without duplicate effect", TransactionRecovers);
        Register("unknown prepare remains resumable without effects", PrepareRecovers);
        Register("competing transaction cannot reserve the same participant", TransactionConflicts);
        Register("transaction preserves exact quantities through delayed completion", TransactionPreservesQuantities);
        Register("transaction retries a lost release before final confirmation", TransactionRecoversLostRelease);
        Register("aborted preparation retries release without applying layouts", AbortRecoversLostRelease);
        Register("duplicate participant identities are rejected before prepare", DuplicateParticipantsAreRejected);
        Register("compacted completed identifiers remain replay-rejected", CompactedIdentifiersRemainRejected);
        Register("scene-free authority rejects spoofed radius members and anchors", SceneFreeAuthorityRejectsSpoofing);
        Register("persisted public status reconstructs exact partial quantities", PersistedStatusPreservesQuantities);
        Register("combined direct and overlapping networks enforce one deduplicated limit", CombinedProjectionIsBounded);
        Register("production capture persists intent before mutation and retains uncertain custody", CaptureFlowRetainsUnknown);
        Register("persisted request intent survives reconnect without freezing inventory snapshots", RequestIntentSurvivesReconnect);
        Register("public status keeps native payment pending and ignores late downgrades", PublicStatusPreservesFinality);
        Register("effect recovery receipts lost acknowledgements without duplicate output", EffectRecoversLostAcknowledgements);
        Register("rejected captured delivery remains recoverable and is never completed", RejectedEffectIsNotSuccess);
        Register("captured require-all output survives restart until full capacity exists", RequireAllOutputWaits);
        Register("prepared cost effect waits for durable payment commit across restart", CostEffectWaitsForCommit);
        Register("discovery policy excludes wrong network range access and reservations", DiscoveryFiltersInvalidMembers);
        Register("discovery policy deduplicates overlap and preserves direct mobile scope", DiscoveryPreservesDirectBoundary);
        TryRegisterStationScenarios();

        var failed = 0;
        foreach (var test in Cases)
        {
            try { test.Body(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + test.Name + ": " + error.Message); }
        }
        Console.WriteLine($"{Cases.Count - failed}/{Cases.Count} passed");
        return failed == 0 ? 0 : 1;
    }

    private static void Register(string name, Action body) => Cases.Add((name, body));

    private static void NameConfirmsInline()
    {
        StorageOperation current = null;
        StorageOperation Remember(StorageOperation value) => current = value;
        StorageNameFlow.Dispatch("name:local", Remember,
            () => Remember(new StorageOperation("name:local", StorageOperationStatus.Confirmed)));
        Equal(StorageOperationStatus.Confirmed, current.Status);
    }

    private static void NameWaitsForOwner()
    {
        StorageOperation current = null;
        StorageOperation Remember(StorageOperation value) => current = StorageEffects.AcceptStatus(current, value);
        Action reply = null;
        StorageNameFlow.Dispatch("name:remote", Remember,
            () => reply = () => Remember(new StorageOperation("name:remote", StorageOperationStatus.Rejected, message: "No access")));
        Equal(StorageOperationStatus.RecoveryPending, current.Status);
        reply(); reply();
        Remember(new StorageOperation("name:remote", StorageOperationStatus.Requested));
        Equal(StorageOperationStatus.Rejected, current.Status); Equal("No access", current.Message);
    }

    private static void NameOwnerDoesNotReplay()
    {
        var saved = "original"; var receipt = false;
        bool Apply() { saved = "first"; return true; }
        True(StorageNameFlow.ApplyOnce(() => receipt, Apply, () => receipt = true));
        Equal("first", saved);
        saved = "later";
        True(StorageNameFlow.ApplyOnce(() => receipt, Apply, () => receipt = true));
        Equal("later", saved);
    }

    private static void WithdrawTargetsSlot()
    {
        var source = new[] { Inv("chest", 2, Stack("wood", 10), Stack("wood", 10, slot: 1)) };
        var player = Inv("player", 3, Stack("wood", 2));
        var intoEmpty = StoragePlanner.Withdraw(source, player, "wood", 15, 2);
        Equal(10, intoEmpty.Accepted);
        Equal(2, intoEmpty.Inventories.Single(x => x.Id == "player").Items.Single(x => x.Slot == 0).Amount);
        Equal(10, intoEmpty.Inventories.Single(x => x.Id == "player").Items.Single(x => x.Slot == 2).Amount);
        var compatible = StoragePlanner.Withdraw(source, player, "wood", 15, 0);
        Equal(8, compatible.Accepted);
        var changed = Inv("player", 3, Stack("stone", 1, slot: 2));
        var rejected = StoragePlanner.Withdraw(source, changed, "wood", 10, 2);
        Equal(0, rejected.Accepted); Equal(1, rejected.Inventories.Single(x => x.Id == "player").Items.Count);
        Equal(0, StoragePlanner.Withdraw(source, player, "wood", 10, 3).Accepted);
        Equal(0, StoragePlanner.Withdraw(source, player, "wood", 10, -2).Accepted);
        Equal(15, StoragePlanner.Withdraw(source, player, "wood", 15).Accepted);
    }

    private static void WithdrawSlotIntent()
    {
        var before = Inv("player", 3, Stack("wood", 2));
        var expected = StorageSlotExpectation.Capture(before, 0);
        True(StorageSlotExpectation.Matches(before, 0, expected));
        True(!StorageSlotExpectation.Matches(Inv("player", 3, Stack("wood", 4)), 0, expected));
        var empty = StorageSlotExpectation.Capture(before, 2);
        True(!StorageSlotExpectation.Matches(Inv("player", 3, Stack("wood", 1, slot: 2)), 2, empty));
        True(!StorageSlotExpectation.Matches(before, 3, "invalid"));
    }

    private static void PersistedStatusPreservesQuantities()
    {
        var store = new MemoryTransactionStore();
        store.Save(new StorageTransactionRecord("restored:1", "deposit", StorageOperationStatus.Confirmed,
            Array.Empty<StorageParticipantPlan>(), requested: 10, accepted: 8));
        var restored = StorageTransactions.GetStatus(store, "restored:1");
        Equal(StorageOperationStatus.Confirmed, restored.Status);
        Equal(10, restored.Requested); Equal(8, restored.Accepted); Equal(2, restored.Remaining);
        store.Save(new StorageTransactionRecord("restored:2", "deposit", StorageOperationStatus.RecoveryPending,
            Array.Empty<StorageParticipantPlan>(), requested: 10, accepted: 8));
        var pending = StorageTransactions.GetStatus(store, "restored:2");
        Equal(0, pending.Accepted); Equal(10, pending.Remaining);
    }

    private static void CombinedProjectionIsBounded()
    {
        var overlap = StorageDiscoveryPolicy.CompleteProjection(new[] { "direct", "a", "a", "b", "direct" }, 3);
        True(!overlap.LimitExceeded); Equal(3, overlap.Ids.Count);
        var tooLarge = StorageDiscoveryPolicy.CompleteProjection(new[] { "direct", "a", "b", "c", "a" }, 3);
        True(tooLarge.LimitExceeded); Equal(0, tooLarge.Ids.Count);
    }

    private static void CaptureFlowRetainsUnknown()
    {
        var steps = new List<string>();
        var result = StorageCaptureFlow.Execute("output:1", 10,
            () => { steps.Add("persist"); return new StorageOperation("output:1", StorageOperationStatus.Requested); },
            () => { steps.Add("native"); throw new InvalidOperationException("after source transition"); },
            () => steps.Add("forget"),
            () => { steps.Add("submit"); return new StorageOperation("output:1", StorageOperationStatus.RecoveryPending); });
        Equal(new[] { "persist", "native", "submit" }, steps.ToArray());
        True(result.Captured); True(!result.IsFinal); Equal(10, result.Remaining);
        var completed = StorageCaptureFlow.Execute("output:2", 10,
            () => new StorageOperation("output:2", StorageOperationStatus.Requested),
            () => StorageEffectStepResult.Applied(), () => { },
            () => new StorageOperation("output:2", StorageOperationStatus.Confirmed, 10, 8, 2));
        True(completed.Captured); Equal(8, completed.Accepted); Equal(2, completed.Remaining);
    }

    private static void RequestIntentSurvivesReconnect()
    {
        var original = new StorageRequestIntent("output", "hive:42", "world-A", StorageScope.Processor,
            1, 2, 3, 20, "machine:1", new byte[] { 9, 8, 7 });
        var restored = StorageRequestIntent.Decode(original.Encode());
        Equal("hive:42", restored.Id); Equal("world-A", restored.WorldId); Equal("machine:1", restored.AnchorId);
        True(restored.MatchesTarget("world-A", "machine:1", "output"));
        True(!restored.MatchesTarget("world-B", "machine:1", "output"));
        True(!restored.MatchesTarget("world-A", "another-chest", "output"));
        True(!restored.MatchesTarget("world-A", "machine:1", "name"));
        Equal(new byte[] { 9, 8, 7 }, restored.Body); Equal(20f, restored.Radius);
        // Intent carries the immutable production only. The live projection is
        // supplied anew after reconnect and capacity changes.
        var first = restored.Rebuild(() => new byte[] { 0 });
        var later = restored.Rebuild(() => new byte[] { 8 });
        Equal(new byte[] { 0, 9, 8, 7 }, first); Equal(new byte[] { 8, 9, 8, 7 }, later);
    }

    private static void PublicStatusPreservesFinality()
    {
        var record = new StorageEffectRecord(1, "world", "craft:1", 1, "player:1", "context", null,
            new StorageEffectDescriptor("craft", "player:1", 1), null, "escrow", "craft:1", 10, 10, 0, StorageEffectStage.CostPrepared);
        var paid = StorageEffects.PublicStatus(record);
        Equal(StorageOperationStatus.AwaitingEffect, paid.Status); Equal(0, paid.Accepted);
        var done = StorageEffects.PublicStatus(record.At(StorageEffectStage.Completed));
        Equal(StorageOperationStatus.Confirmed, done.Status); Equal(10, done.Accepted);
        Equal(done, StorageEffects.AcceptStatus(done, paid));
    }

    private static StorageStack Stack(string id, int amount, int max = 10, int slot = 0, string payload = null) =>
        new(id, id.Split('|')[0], "$" + id.Split('|')[0], 1, 0, amount, max, slot, payload ?? "payload:" + id);

    private static StorageInventory Inv(string id, int slots, params StorageStack[] items) =>
        new(id, "r1", slots, 1, items);

    private static void QueryDeduplicates()
    {
        var a = Inv("a", 2, Stack("wood|plain", 3), Stack("wood|fine", 2, slot: 1));
        var view = StoragePlanner.Query(new[] { a, a, Inv("b", 1, Stack("stone|plain", 4)) });
        Equal(2, view.Inventories.Count);
        Equal(3, view.Rows.Count);
        Equal(3, view.Rows.Single(x => x.Identity == "wood|plain").Amount);
        Equal(3, view.UsedSlots);
        Equal(3, view.TotalSlots);
    }

    private static void DepositIsFinite()
    {
        var source = Stack("wood|plain", 17, payload: "opaque-A");
        var result = StoragePlanner.Deposit(new[] {
            Inv("a", 2, Stack("wood|plain", 8, payload: "opaque-A"), Stack("stone|plain", 10, slot: 1)),
            Inv("b", 1)
        }, source, 17);
        Equal(12, result.Accepted);
        Equal(5, result.Remaining);
        Equal(new[] { 10, 10, 10 }, result.Inventories.SelectMany(x => x.Items).Select(x => x.Amount).ToArray());
        True(result.Inventories.SelectMany(x => x.Items).Where(x => x.Identity == "wood|plain").All(x => x.Payload == "opaque-A"));
    }

    private static void WithdrawIsFinite()
    {
        var result = StoragePlanner.Withdraw(
            new[] { Inv("a", 2, Stack("ore|q1", 6, payload: "ore-payload")), Inv("b", 1, Stack("ore|q2", 9)) },
            Inv("player", 1, Stack("ore|q1", 7, payload: "ore-payload")), "ore|q1", 8);
        Equal(3, result.Accepted);
        Equal(5, result.Remaining);
        Equal(3, result.Inventories.Single(x => x.Id == "a").Items.Single().Amount);
        Equal(10, result.Inventories.Single(x => x.Id == "player").Items.Single().Amount);
        Equal("ore-payload", result.Inventories.Single(x => x.Id == "player").Items.Single().Payload);
    }

    private static void OrganizeIsStable()
    {
        var members = new[] {
            Inv("a", 10, Enumerable.Range(0, 7).Select(i => Stack("wood|plain", 10, slot: i)).ToArray()),
            Inv("b", 10, Enumerable.Range(0, 7).Select(i => Stack("stone|plain", 10, slot: i)).ToArray()),
            Inv("c", 10, Enumerable.Range(0, 7).Select(i => Stack("resin|plain", 10, slot: i)).ToArray()),
            Inv("d", 10, Stack("wood|plain", 5), Stack("stone|plain", 5, slot: 1), Stack("resin|plain", 5, slot: 2))
        };
        var first = StoragePlanner.Organize(members);
        Equal(3, first.Inventories.Count(x => x.Items.Count > 0));
        True(first.Moves > 0);
        Equal(225, first.Inventories.SelectMany(x => x.Items).Sum(x => x.Amount));
        var second = StoragePlanner.Organize(first.Inventories);
        Equal(0, second.Moves);
    }

    private static void TransactionRecovers()
    {
        var store = new MemoryTransactionStore();
        var participant = new MemoryParticipant("chest", "r1");
        var coordinator = new StorageTransactions(store, new[] { participant });
        var request = new StorageTransactionRequest("op-1", "deposit", new[] { new StorageParticipantPlan("chest", "r1", "layout-2") });
        participant.LoseNextAcknowledgement = true;
        var pending = coordinator.Execute(request);
        Equal(StorageOperationStatus.RecoveryPending, pending.Status);
        Equal("layout-2", participant.Layout);
        var recovered = new StorageTransactions(store, new[] { participant }).Resume("op-1");
        Equal(StorageOperationStatus.Confirmed, recovered.Status);
        Equal(1, participant.AppliedEffects);
    }

    private static void TransactionConflicts()
    {
        var store = new MemoryTransactionStore();
        var participant = new MemoryParticipant("chest", "r1") { HoldAcknowledgement = true };
        var coordinator = new StorageTransactions(store, new[] { participant });
        Equal(StorageOperationStatus.RecoveryPending, coordinator.Execute(new StorageTransactionRequest("first", "withdraw",
            new[] { new StorageParticipantPlan("chest", "r1", "one") })).Status);
        Equal(StorageOperationStatus.Rejected, coordinator.Execute(new StorageTransactionRequest("second", "withdraw",
            new[] { new StorageParticipantPlan("chest", "r1", "two") })).Status);
    }

    private static void TransactionPreservesQuantities()
    {
        var store = new MemoryTransactionStore();
        var participant = new MemoryParticipant("chest", "r1") { HoldAcknowledgement = true };
        var coordinator = new StorageTransactions(store, new[] { participant });
        var pending = coordinator.Execute(new StorageTransactionRequest("partial-10", "output",
            new[] { new StorageParticipantPlan("chest", "r1", "eight") }, 10, 8));
        Equal(10, pending.Requested); Equal(0, pending.Accepted); Equal(10, pending.Remaining);
        participant.HoldAcknowledgement = false;
        var complete = coordinator.Resume("partial-10");
        Equal(StorageOperationStatus.Confirmed, complete.Status);
        Equal(10, complete.Requested); Equal(8, complete.Accepted); Equal(2, complete.Remaining);
    }

    private static void TransactionRecoversLostRelease()
    {
        var store = new MemoryTransactionStore();
        var participant = new MemoryParticipant("chest", "r1") { LoseNextReleaseAcknowledgement = true };
        var first = new StorageTransactions(store, new[] { participant }).Execute(new StorageTransactionRequest(
            "release-lost", "deposit", new[] { new StorageParticipantPlan("chest", "r1", "two") }, 2, 2));
        Equal(StorageOperationStatus.RecoveryPending, first.Status);
        Equal(1, participant.AppliedEffects);
        var recovered = new StorageTransactions(store, new[] { participant }).Resume("release-lost");
        Equal(StorageOperationStatus.Confirmed, recovered.Status);
        Equal(1, participant.AppliedEffects);
    }

    private static void AbortRecoversLostRelease()
    {
        var store = new MemoryTransactionStore();
        var first = new MemoryParticipant("a", "r1") { LoseNextReleaseAcknowledgement = true };
        var second = new MemoryParticipant("b", "r1") { RejectPrepare = true };
        var coordinator = new StorageTransactions(store, new[] { first, second });
        var pending = coordinator.Execute(new StorageTransactionRequest("abort-release", "deposit", new[] {
            new StorageParticipantPlan("a", "r1", "x"), new StorageParticipantPlan("b", "r1", "y") }, 1, 1));
        Equal(StorageOperationStatus.RecoveryPending, pending.Status);
        var rejected = coordinator.Resume("abort-release");
        Equal(StorageOperationStatus.Rejected, rejected.Status);
        Equal(0, first.AppliedEffects); Equal(0, second.AppliedEffects);
    }

    private static void DuplicateParticipantsAreRejected()
    {
        var store = new MemoryTransactionStore();
        var participant = new MemoryParticipant("same", "r1");
        var result = new StorageTransactions(store, new[] { participant, participant }).Execute(
            new StorageTransactionRequest("duplicate", "deposit", new[] { new StorageParticipantPlan("same", "r1", "x") }, 1, 1));
        Equal(StorageOperationStatus.Rejected, result.Status);
    }

    private static void CompactedIdentifiersRemainRejected()
    {
        var filter = new StorageReplayFilter(2048);
        for (var sequence = 1; sequence <= 10000; sequence++) filter.Remember("world:issuer:" + sequence);
        var restored = new StorageReplayFilter(filter.Export());
        True(restored.Contains("world:issuer:1"));
        True(restored.Contains("world:issuer:9999"));
        True(!restored.Contains("world:issuer:10001"));
        True(filter.Export().Length < 256);
    }

    private static void SceneFreeAuthorityRejectsSpoofing()
    {
        var terminal = new StorageAuthorityNode("terminal", 1, 0, 0, true, "forge");
        var member = new StorageAuthorityNode("chest", 10, 0, 0, false, "forge");
        True(StorageAuthorityPolicy.Validate(StorageScope.Terminal, true, 20, 8, 0, 0, 0, 0, 0, 0, 20,
            terminal, new[] { member }, new[] { terminal }));
        True(!StorageAuthorityPolicy.Validate(StorageScope.Terminal, true, 20, 8, 0, 0, 0, 0, 0, 0, 128,
            terminal, new[] { member }, new[] { terminal }));
        True(!StorageAuthorityPolicy.Validate(StorageScope.Terminal, true, 20, 8, 50, 0, 0, 0, 0, 0, 20,
            terminal, new[] { member }, new[] { terminal }));
        True(!StorageAuthorityPolicy.Validate(StorageScope.Terminal, true, 20, 8, 0, 0, 0, 0, 0, 0, 20,
            terminal, new[] { new StorageAuthorityNode("spoof", 10, 0, 0, false, "other") }, new[] { terminal }));
    }

    private static void PrepareRecovers()
    {
        var store = new MemoryTransactionStore();
        var participant = new MemoryParticipant("chest", "r1") { LoseNextPrepareAcknowledgement = true };
        var coordinator = new StorageTransactions(store, new[] { participant });
        var request = new StorageTransactionRequest("prepare-lost", "deposit", new[] { new StorageParticipantPlan("chest", "r1", "layout-2") });
        Equal(StorageOperationStatus.Preparing, coordinator.Execute(request).Status);
        Equal(0, participant.AppliedEffects);
        Equal(StorageOperationStatus.Confirmed, coordinator.Resume("prepare-lost").Status);
        Equal(1, participant.AppliedEffects);
    }

    private static void EffectRecoversLostAcknowledgements()
    {
        var store = new MemoryEffectStore();
        var port = new MemoryEffectPort { LoseCaptureAck = true, LoseDeliveryAck = true, LoseRemainderAck = true };
        var record = new StorageEffectRecord(1, "world-1", "output-1", 42, "machine-7", "processor",
            new StorageEffectDescriptor("processor.capture", "machine-7", 42), null,
            new StorageEffectDescriptor("processor.remainder", "machine-7", 42), "escrow", "output-1:delivery",
            10, 0, 10, StorageEffectStage.CapturePending);

        var first = new StorageEffects(store, port).Begin(record);
        Equal(StorageEffectStage.CapturePending, first.Stage);
        var second = new StorageEffects(store, port).Resume(record.OperationId);
        Equal(StorageEffectStage.DeliveryPending, second.Stage);
        var third = new StorageEffects(store, port).Resume(record.OperationId);
        Equal(StorageEffectStage.RemainderPending, third.Stage);
        var completed = new StorageEffects(store, port).Resume(record.OperationId);
        Equal(StorageEffectStage.Completed, completed.Stage);
        Equal(8, completed.Accepted);
        Equal(2, completed.Remaining);
        Equal(1, port.Captures);
        Equal(1, port.Deliveries);
        Equal(1, port.Remainders);
    }

    private static void RejectedEffectIsNotSuccess()
    {
        var store = new MemoryEffectStore();
        var port = new MemoryEffectPort { RejectDelivery = true };
        var result = new StorageEffects(store, port).Begin(new StorageEffectRecord(1, "world-1", "output-2", 42,
            "machine-7", "processor", null, null, null, "escrow", "output-2:delivery", 3, 0, 3,
            StorageEffectStage.Captured));
        Equal(StorageEffectStage.DeliveryPending, result.Stage);
        True(result.Stage != StorageEffectStage.Completed);
    }

    private static void RequireAllOutputWaits()
    {
        var store = new MemoryEffectStore();
        var port = new MemoryEffectPort { CapacityReady = false };
        var record = new StorageEffectRecord(1, "world-1", "hive-1", 42, "hive-7", "processor", null, null, null,
            "honey-outbox", "hive-1:delivery", 4, 0, 4, StorageEffectStage.Captured, new[] { "capture" }, requireAll: true);
        var waiting = new StorageEffects(store, port).Begin(record);
        Equal(StorageEffectStage.Captured, waiting.Stage);
        port.CapacityReady = true;
        var completed = new StorageEffects(store, port).Resume(record.OperationId);
        Equal(StorageEffectStage.Completed, completed.Stage);
        Equal(4, completed.Accepted);
        Equal(0, completed.Remaining);
    }

    private static void CostEffectWaitsForCommit()
    {
        var store = new MemoryEffectStore();
        var port = new MemoryEffectPort { CostCommitted = false };
        var record = new StorageEffectRecord(1, "world-1", "craft-1", 42, "player:42", "craft", null,
            new StorageEffectDescriptor("craft", "player:42", 42), null, "paid-escrow", "craft-1", 6, 6, 0,
            StorageEffectStage.CostPreparing);
        Equal(StorageEffectStage.CostPreparing, new StorageEffects(store, port).Begin(record).Stage);
        port.CostCommitted = true;
        Equal(StorageEffectStage.Completed, new StorageEffects(store, port).Resume(record.OperationId).Stage);
    }

    private static void DiscoveryFiltersInvalidMembers()
    {
        var candidates = new[]
        {
            Candidate("ok", "forge", 4), Candidate("wrong", "kitchen", 4), Candidate("far", "forge", 101),
            Candidate("ward", "forge", 4, accessible: false), Candidate("busy", "forge", 4, busy: true)
        };
        var selected = StorageDiscoveryPolicy.SelectBacking(candidates, "forge", 10, 8);
        Equal(new[] { "ok" }, selected.Ids.ToArray());
        True(StorageDiscoveryPolicy.CanExpandTerminal(true, true, "forge"));
        True(!StorageDiscoveryPolicy.CanExpandTerminal(true, false, "forge"));
    }

    private static void DiscoveryPreservesDirectBoundary()
    {
        var cart = Candidate("cart", "", 9, mobile: true, created: false);
        var direct = StorageDiscoveryPolicy.SelectDirect(new[] { cart, cart, Candidate("far", "", 101) }, 10);
        Equal(new[] { "cart" }, direct.Ids.ToArray());
        Equal(0, StorageDiscoveryPolicy.SelectBacking(new[] { cart }, "forge", 10, 8).Ids.Count);
        var limited = StorageDiscoveryPolicy.SelectBacking(new[] { Candidate("a", "forge", 1), Candidate("b", "forge", 2) }, "forge", 10, 1);
        True(limited.LimitExceeded);
        Equal(0, limited.Ids.Count);
    }

    private static StorageDiscoveryCandidate Candidate(string id, string name, float distance, bool accessible = true,
        bool busy = false, bool mobile = false, bool created = true) =>
        new(id, name, distance, accessible, busy, false, false, mobile, created);

    private static void TryRegisterStationScenarios()
    {
        foreach (var name in new[] { "StationScenarios", "NativeUiScenarios" })
            Type.GetType(name)?.GetMethod("Run")?.Invoke(null, new object[] { (Action<string, Action>)Register });
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (expected is Array ea && actual is Array aa)
        {
            if (!ea.Cast<object>().SequenceEqual(aa.Cast<object>())) throw new Exception($"expected [{string.Join(',', ea.Cast<object>())}], got [{string.Join(',', aa.Cast<object>())}]");
            return;
        }
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"expected {expected}, got {actual}");
    }
    private static void True(bool value) { if (!value) throw new Exception("expected true"); }

    private sealed class MemoryTransactionStore : IStorageTransactionStore
    {
        private readonly Dictionary<string, StorageTransactionRecord> _records = new();
        public StorageTransactionRecord Load(string id) => _records.TryGetValue(id, out var value) ? value : null;
        public void Save(StorageTransactionRecord record) => _records[record.Id] = record;
    }

    private sealed class MemoryParticipant : IStorageTransactionParticipant
    {
        private string _reservation;
        private readonly HashSet<string> _receipts = new();
        public string Id { get; }
        public string Revision { get; private set; }
        public string Layout { get; private set; } = "layout-1";
        public bool LoseNextAcknowledgement { get; set; }
        public bool HoldAcknowledgement { get; set; }
        public bool LoseNextPrepareAcknowledgement { get; set; }
        public bool LoseNextReleaseAcknowledgement { get; set; }
        public bool RejectPrepare { get; set; }
        public int AppliedEffects { get; private set; }
        public MemoryParticipant(string id, string revision) { Id = id; Revision = revision; }
        public StorageParticipantResult Prepare(string operationId, string expectedRevision)
        {
            if (RejectPrepare) return StorageParticipantResult.Rejected("denied");
            if (_reservation != null && _reservation != operationId) return StorageParticipantResult.Rejected("busy");
            if (Revision != expectedRevision) return StorageParticipantResult.Rejected("stale");
            _reservation = operationId;
            if (LoseNextPrepareAcknowledgement) { LoseNextPrepareAcknowledgement = false; return StorageParticipantResult.Unknown("lost prepare"); }
            return StorageParticipantResult.Accepted();
        }
        public StorageParticipantResult Apply(string operationId, string layout)
        {
            if (_receipts.Add(operationId)) { Layout = layout; AppliedEffects++; Revision = "r2"; }
            if (HoldAcknowledgement) return StorageParticipantResult.Unknown("held");
            if (LoseNextAcknowledgement) { LoseNextAcknowledgement = false; return StorageParticipantResult.Unknown("lost"); }
            return StorageParticipantResult.Accepted();
        }
        public StorageParticipantResult Receipt(string operationId) => _receipts.Contains(operationId)
            ? StorageParticipantResult.Accepted() : StorageParticipantResult.Unknown("missing");
        public StorageParticipantResult Release(string operationId)
        {
            if (LoseNextReleaseAcknowledgement) { LoseNextReleaseAcknowledgement = false; return StorageParticipantResult.Unknown("lost release"); }
            if (_reservation == operationId) _reservation = null;
            return StorageParticipantResult.Accepted();
        }
    }

    private sealed class MemoryEffectStore : IStorageEffectStore
    {
        private readonly Dictionary<string, StorageEffectRecord> _records = new();
        public StorageEffectRecord LoadEffect(string operationId) => _records.TryGetValue(operationId, out var value) ? value : null;
        public void SaveEffect(StorageEffectRecord record) => _records[record.OperationId] = record;
    }

    private sealed class MemoryEffectPort : IStorageEffectPort
    {
        private readonly HashSet<string> _receipts = new();
        public bool LoseCaptureAck, LoseDeliveryAck, LoseRemainderAck, RejectDelivery;
        public bool CapacityReady = true;
        public bool CostCommitted = true;
        public int Captures, Deliveries, Remainders;
        public StorageEffectStepResult Receipt(StorageEffectRecord record, StorageEffectStage stage) =>
            _receipts.Contains(record.OperationId + ":" + stage) ? Result(record, stage) : StorageEffectStepResult.Unknown("missing");
        public StorageEffectStepResult Apply(StorageEffectRecord record, StorageEffectStage stage)
        {
            if (stage == StorageEffectStage.Captured && record.RequireAll && !CapacityReady)
                return StorageEffectStepResult.Unknown("full capacity unavailable");
            if (stage == StorageEffectStage.CostPreparing && !CostCommitted)
                return StorageEffectStepResult.Unknown("payment pending");
            if (stage == StorageEffectStage.DeliveryPending && RejectDelivery) return StorageEffectStepResult.Rejected("denied");
            var key = record.OperationId + ":" + stage;
            if (_receipts.Add(key))
            {
                if (stage == StorageEffectStage.CapturePending) Captures++;
                else if (stage == StorageEffectStage.DeliveryPending) Deliveries++;
                else if (stage == StorageEffectStage.RemainderPending) Remainders++;
            }
            if (stage == StorageEffectStage.CapturePending && LoseCaptureAck) { LoseCaptureAck = false; return StorageEffectStepResult.Unknown("lost"); }
            if (stage == StorageEffectStage.DeliveryPending && LoseDeliveryAck) { LoseDeliveryAck = false; return StorageEffectStepResult.Unknown("lost"); }
            if (stage == StorageEffectStage.RemainderPending && LoseRemainderAck) { LoseRemainderAck = false; return StorageEffectStepResult.Unknown("lost"); }
            return Result(record, stage);
        }
        private static StorageEffectStepResult Result(StorageEffectRecord record, StorageEffectStage stage)
        {
            var accepted = record.RequireAll ? record.Requested : Math.Min(8, record.Requested);
            return stage == StorageEffectStage.Captured || stage == StorageEffectStage.DeliveryPending
                ? StorageEffectStepResult.Applied(accepted, record.Requested - accepted) : StorageEffectStepResult.Applied();
        }
    }
}
