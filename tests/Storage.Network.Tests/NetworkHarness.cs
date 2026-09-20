using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

[BepInPlugin("scs.tests.network", "Storage network regression", "1.0.0")]
public sealed class NetworkHarness : BaseUnityPlugin
{
    private string _report;
    private int _failures;
    private void Awake()
    {
        _report = Path.Combine(Paths.GameRootPath, "network-results.txt");
        File.WriteAllText(_report, "START\n");
        var harmony = new Harmony("scs.tests.network");
        foreach (var method in new[] { "PlatformInitializer:InitializePlatform", "EntryPointSceneLoader:Start", "FejdStartup:Awake" })
        {
            var target = AccessTools.Method(method);
            if (target != null) harmony.Patch(target, prefix: new HarmonyMethod(typeof(NetworkHarness), nameof(SkipStartup)));
        }
        StartCoroutine(Run());
    }

    private static bool SkipStartup() => false;
    private IEnumerator Run()
    {
        yield return null;
        try
        {
            var routed = new ZRoutedRpc(false);
            routed.SetUID(123);
            new ZDOMan(64);
            ZNet.m_isServer = false;
            ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
            ZNet.m_world = new World { m_uid = 42 };
            var netObject = new GameObject("test-network"); netObject.SetActive(false);
            ZNet.m_instance = netObject.AddComponent<ZNet>();
            var socket = new RecordingSocket();
            var peer = new ZNetPeer(socket, true) { m_uid = 456 };
            ZNet.instance.m_peers.Add(peer); routed.AddPeer(peer);
            var playerObject = new GameObject("test-player"); playerObject.SetActive(false);
            Player.m_localPlayer = playerObject.AddComponent<Player>();
            Player.m_localPlayer.m_nview = playerObject.AddComponent<ZNetView>();
            Player.m_localPlayer.m_customData["scs.storage.pending.requests.v1"] = PendingRequests();
            double now = 0;
            var service = new StorageService(() => new StorageSettings(true, 32f, 64, 128, 4), () => now);
            service.Tick();
            var initial = socket.Sent.Count;
            Check(initial == 2, "two restored processor requests each send an initial recovery probe");
            for (var i = 0; i < 100; i++) { service.Tick(); service.Resume("processor:1"); service.Resume("processor:2"); }
            Check(socket.Sent.Count == initial, "dense Tick and explicit Resume calls do not resend pending operations before the retry deadline; sent=" + socket.Sent.Count);
            Check(!service.GetOperation("processor:1").IsFinal && !service.GetOperation("processor:2").IsFinal,
                "delayed replies preserve both operations as pending");
            Check(Player.m_localPlayer.m_customData.ContainsKey("scs.storage.pending.requests.v1"),
                "waiting preserves the durable request record");
            Check(socket.LastRequestKind("processor:1") == "cost", "the initial request carries its complete intent");
            DeliverResult(routed, 789, "processor:1", StorageOperationStatus.Preparing);
            Check(service.GetOperation("processor:1").Status == StorageOperationStatus.Requested,
                "a result from a peer other than the server cannot admit a request");
            DeliverResult(routed, 456, "processor:1", StorageOperationStatus.Preparing);
            now = 0.5;
            service.Tick();
            Check(socket.LastRequestKind("processor:1") == "status", "an admitted request polls status instead of repeating inventory snapshots");
            Check(socket.LastRequestKind("processor:2") == "cost", "an unacknowledged operation can still retry its intent independently");
            DeliverResult(routed, 456, "processor:1", StorageOperationStatus.Requested);
            now = 10;
            service.Tick();
            Check(socket.LastRequestKind("processor:1") == "cost", "a server needing fresh intent receives rebuilt inventory data");
            DeliverResult(routed, 456, "processor:1", StorageOperationStatus.Confirmed);
            DeliverResult(routed, 456, "processor:1", StorageOperationStatus.Preparing);
            Check(service.GetOperation("processor:1").Status == StorageOperationStatus.Confirmed,
                "a late pending reply cannot regress a confirmed operation");
            DeliverResult(routed, 456, "output:recovery", StorageOperationStatus.RecoveryPending, true, 10);
            DeliverResult(routed, 456, "output:recovery", StorageOperationStatus.Requested);
            var captured = service.GetOperation("output:recovery");
            Check(captured.Captured && captured.Requested == 10 && captured.Remaining == 10,
                "a fresh-intent probe preserves the client's captured output and quantities");
            DeliverResult(routed, 456, "processor:2", StorageOperationStatus.RecoveryPending);
            var recoveryStart = socket.Sent.Count;
            for (var frame = 0; frame < 3600; frame++)
            {
                now = 10 + (double)frame / 60;
                var sent = socket.Sent.Count;
                service.Tick();
                if (socket.Sent.Count != sent)
                    DeliverResult(routed, 456, "processor:2", socket.LastRequestKind("processor:2") == "status"
                        ? StorageOperationStatus.Requested : StorageOperationStatus.RecoveryPending);
            }
            Check(socket.Sent.Count - recoveryStart == 11,
                "alternating fresh-intent and pending replies retain backoff instead of restarting a per-frame loop; sent=" + (socket.Sent.Count - recoveryStart));
            socket.QueuedBytes = 256 * 1024;
            var rpc = new StorageRpc();
            var request = new ZPackage(); request.Write("status"); request.Write("backpressure");
            var before = socket.Sent.Count;
            rpc.Submit(request);
            Check(socket.Sent.Count == before, "a congested routed connection defers retryable payloads");
            socket.QueuedBytes = 0;
            rpc.Submit(request);
            Check(socket.Sent.Count == before + 1, "retryable traffic resumes when the routed connection drains");
            CheckRetryCadence();
            DeliverResult(routed, 456, "processor:2", StorageOperationStatus.Preparing);
            var reconnected = new ZRoutedRpc(false); reconnected.SetUID(123); reconnected.AddPeer(peer);
            new ZDOMan(64);
            before = socket.Sent.Count;
            service.Tick();
            Check(socket.Sent.Count == before + 1 && socket.LastRequestKind("processor:2") == "cost",
                "reconnect restores only unfinished work and requests fresh admission without the old delay");
            CheckServerProgress(socket, peer);
            CheckServerStatus(socket, peer);
            LocalRecoveryScenario.Run(Check);
            ParticipantRecoveryScenario.Run(Check);
            DurableOrphanRecoveryScenario.Run(Check);
        }
        catch (Exception error) { _failures++; File.AppendAllText(_report, "ERROR " + error + "\n"); }
        File.AppendAllText(_report, "DONE failures=" + _failures + "\n");
        Application.Quit(_failures == 0 ? 0 : 1);
    }

    private void CheckRetryCadence()
    {
        foreach (var rate in new[] { 30, 60, 144 })
        {
            var schedule = new StorageRetrySchedule(); var first = 0; var second = 0;
            for (var frame = 0; frame < rate * 60; frame++)
            {
                var now = (double)frame / rate;
                if (schedule.TryBegin("first", now)) first++;
                if (schedule.TryBegin("second", now)) second++;
            }
            Check(first == 11 && second == 11, "two independent operations retry 11 times each over 60 seconds at " + rate + " FPS");
        }
        var timing = new StorageRetrySchedule();
        Check(timing.TryBegin("a", 0) && !timing.TryBegin("a", 0.499) && timing.TryBegin("a", 0.5) &&
            !timing.TryBegin("a", 1.499) && timing.TryBegin("a", 1.5), "retry deadlines honor the initial half-second and increasing delay");
        timing.TryBegin("b", 1.5); timing.Progress("a");
        Check(timing.TryBegin("a", 1.5) && !timing.TryBegin("b", 1.5), "new progress unblocks only its own operation");
        timing.Clear();
        Check(timing.TryBegin("b", 1.5), "a new session clears transient retry timing");
    }

    private void CheckServerProgress(RecordingSocket socket, ZNetPeer peer)
    {
        ZNet.m_isServer = true;
        var routed = new ZRoutedRpc(true); routed.SetUID(ZDOMan.GetSessionID()); routed.AddPeer(peer);
        peer.m_playerID = 77;
        var rpc = new StorageRpc(); var progress = 0;
        rpc.ProgressReceived = id => progress++;
        var participant = rpc.BindRemotePlayer(77, 456, "revision", "");
        socket.Sent.Clear(); socket.QueuedBytes = 256 * 1024;
        participant.Prepare("server-op", "revision");
        Check(socket.Sent.Count == 0, "server participant retries also respect connection backpressure");
        socket.QueuedBytes = 0;
        participant.Prepare("server-op", "revision");
        Check(socket.Sent.Count == 1, "server participant commands resume after the queue drains");
        DeliverPlayerAck(routed, 789, 77, 0);
        Check(progress == 0, "an unauthenticated participant reply cannot reset retry timing");
        DeliverPlayerAck(routed, 456, 77, 0);
        Check(progress == 1 && participant.Prepare("server-op", "revision").IsAccepted,
            "a new authenticated preparation receipt advances the participant and reports progress");
        for (var i = 0; i < 100; i++) DeliverPlayerAck(routed, 456, 77, 0);
        Check(progress == 1, "duplicate participant receipts do not repeatedly bypass the retry deadline");
        DeliverPlayerAck(routed, 456, 77, 1);
        Check(progress == 1 && !participant.Receipt("server-op").IsAccepted, "an unsolicited phase acknowledgement cannot advance a transaction");
        participant.Apply("server-op", "");
        DeliverPlayerAck(routed, 456, 77, 1);
        Check(progress == 2 && participant.Receipt("server-op").IsAccepted, "the next acknowledged phase can advance immediately");
        socket.QueuedBytes = 256 * 1024;
        var before = socket.Sent.Count;
        rpc.Reply(456, new StorageOperation("server-op", StorageOperationStatus.Confirmed));
        rpc.ReleaseEffect(456, "test", "server-op");
        Check(socket.Sent.Count == before + 2, "results and effect releases are not stranded behind the retryable-payload gate");
        var inline = false; rpc.NameAcknowledged = (sender, id, accepted, message) => inline = true;
        rpc.ApplyName(ZDOMan.GetSessionID(), ZDOID.None, "inline", 77, "test");
        Check(inline, "local RPC completion remains synchronous even while a remote connection is congested");
    }

    private static void DeliverPlayerAck(ZRoutedRpc routed, long sender, long actor, int phase)
    {
        var response = new ZPackage(); response.Write("server-op"); response.Write(actor); response.Write(phase); response.Write(true);
        Deliver(routed, sender, "SCS_PlayerAck_v1", response);
    }

    private void CheckServerStatus(RecordingSocket socket, ZNetPeer peer)
    {
        LocalRecoveryScenario.SetupWorld();
        var routed = new ZRoutedRpc(true); routed.SetUID(ZDOMan.GetSessionID()); routed.AddPeer(peer);
        var service = new StorageService(() => new StorageSettings(true, 32f, 64, 128, 4), () => 0);
        socket.Sent.Clear(); socket.QueuedBytes = 0;
        DeliverStatus(routed, "missing");
        Check(socket.LastResult("missing")?.Status == StorageOperationStatus.Requested && service._journal.Load("missing") == null,
            "a server missing an operation requests fresh intent without recording a rejection");
        service._journal.Save(new StorageTransactionRecord("finished", "cost", StorageOperationStatus.Confirmed,
            Array.Empty<StorageParticipantPlan>(), requested: 10, accepted: 10, actorId: 77));
        DeliverStatus(routed, "finished");
        Check(socket.LastResult("finished")?.Status == StorageOperationStatus.Confirmed && socket.LastResult("finished")?.Accepted == 10,
            "status polling returns a durable final outcome without repeating the operation");
        service._journal.Save(new StorageTransactionRecord("other-player", "cost", StorageOperationStatus.Preparing,
            Array.Empty<StorageParticipantPlan>(), actorId: 88));
        DeliverStatus(routed, "other-player");
        Check(socket.LastResult("other-player")?.Status == StorageOperationStatus.Rejected &&
            service._journal.Load("other-player").Status == StorageOperationStatus.Preparing,
            "status polling cannot resume or alter another player's operation");
        var capture = new StorageEffectDescriptor("processor.capture", "player:77", 77);
        service._journal.SaveEffect(new StorageEffectRecord(1, "42", "captured", 77, "player:77", "", capture, null, null,
            "", "delivery", 10, 0, 10, StorageEffectStage.Captured));
        DeliverStatus(routed, "captured");
        Check(socket.LastResult("captured")?.Status == StorageOperationStatus.Requested &&
            service._journal.LoadEffect("captured").Stage == StorageEffectStage.Captured,
            "captured output requests current discovery without losing its durable custody record");
    }

    private static void DeliverStatus(ZRoutedRpc routed, string id)
    {
        var request = new ZPackage(); request.Write("status"); request.Write(id);
        Deliver(routed, 456, "SCS_StorageRequest_v1", request);
    }

    private static string PendingRequests()
    {
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
        {
            writer.Write(2);
            for (var i = 1; i <= 2; i++)
            {
                var id = "processor:" + i;
                var intent = new StorageRequestIntent("cost", id, "42", StorageScope.Processor, 0, 0, 0, 10, "none", Array.Empty<byte>()).Encode();
                writer.Write(id); writer.Write(intent.Length); writer.Write(intent);
            }
            return Convert.ToBase64String(stream.ToArray());
        }
    }

    private static void DeliverResult(ZRoutedRpc routed, long sender, string id, StorageOperationStatus status, bool captured = false, int amount = 1)
    {
        var response = new ZPackage(); response.Write(id); response.Write((int)status);
        response.Write(amount); response.Write(0); response.Write(amount); response.Write("test reply"); response.Write(captured);
        Deliver(routed, sender, "SCS_StorageResult_v1", response);
    }

    internal static void Deliver(ZRoutedRpc routed, long sender, string method, ZPackage response)
    {
        var parameters = new ZPackage(); parameters.Write(response); parameters.SetPos(0);
        routed.HandleRoutedRPC(new ZRoutedRpc.RoutedRPCData
        {
            m_senderPeerID = sender, m_targetPeerID = 123, m_targetZDO = ZDOID.None,
            m_methodHash = method.GetStableHashCode(), m_parameters = parameters
        });
    }

    private void Check(bool condition, string message)
    {
        if (!condition) _failures++;
        File.AppendAllText(_report, (condition ? "PASS " : "FAIL ") + message + "\n");
    }

    internal sealed class RecordingSocket : ISocket
    {
        public readonly List<byte[]> Sent = new List<byte[]>();
        public int QueuedBytes;
        public StorageOperation LastResult(string id)
        {
            for (var i = Sent.Count - 1; i >= 0; i--)
            {
                var wire = new ZPackage(Sent[i]);
                if (wire.ReadInt() != "RoutedRPC".GetStableHashCode()) continue;
                var routed = new ZRoutedRpc.RoutedRPCData(); routed.Deserialize(wire.ReadPackage());
                if (routed.m_methodHash != "SCS_StorageResult_v1".GetStableHashCode()) continue;
                var response = routed.m_parameters.ReadPackage();
                if (response.ReadString() != id) continue;
                return new StorageOperation(id, (StorageOperationStatus)response.ReadInt(), response.ReadInt(),
                    response.ReadInt(), response.ReadInt(), response.ReadString(), response.ReadBool());
            }
            return null;
        }
        public string LastRequestKind(string id)
        {
            for (var i = Sent.Count - 1; i >= 0; i--)
            {
                var wire = new ZPackage(Sent[i]);
                if (wire.ReadInt() != "RoutedRPC".GetStableHashCode()) continue;
                var routed = new ZRoutedRpc.RoutedRPCData(); routed.Deserialize(wire.ReadPackage());
                if (routed.m_methodHash != "SCS_StorageRequest_v1".GetStableHashCode()) continue;
                var request = routed.m_parameters.ReadPackage(); var kind = request.ReadString();
                if (request.ReadString() == id) return kind;
            }
            return null;
        }
        public bool IsConnected() => true;
        public void Send(ZPackage package) => Sent.Add(package.GetArray());
        public ZPackage Recv() => null;
        public int GetSendQueueSize() => QueuedBytes;
        public int GetCurrentSendRate() => 1024 * 1024;
        public bool IsHost() => false;
        public void Dispose() { }
        public bool GotNewData() => false;
        public void Close() { }
        public string GetEndPointString() => "isolated-test";
        public void GetAndResetStats(out int sent, out int received) { sent = 0; received = 0; }
        public void GetConnectionQuality(out float local, out float remote, out int ping, out float outgoing, out float incoming)
        { local = 1; remote = 1; ping = 100; outgoing = 0; incoming = 0; }
        public ISocket Accept() => null;
        public int GetHostPort() => 0;
        public bool Flush() => true;
        public string GetHostName() => "isolated-test";
        public void VersionMatch() { }
    }
}
