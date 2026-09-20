using System;
using System.Linq;
using SmartCraftStorage.Storage.Runtime;

internal static class ParticipantRecoveryScenario
{
    internal static void Run(Action<bool, string> check)
    {
        var routed = new ZRoutedRpc(true); routed.SetUID(ZDOMan.GetSessionID());
        var socket = new NetworkHarness.RecordingSocket();
        var peer = new ZNetPeer(socket, true) { m_uid = 456, m_playerID = 77 };
        ZNet.instance.m_peers.Add(peer); routed.AddPeer(peer);
        var rpc = new StorageRpc();
        var chest = LocalRecoveryScenario.Chest("receipt-recovery", 3);
        var zdo = chest.m_nview.GetZDO();
        const string completed = "fixture:applied-before-reload";
        zdo.Set("scs.storage.receipts.v1", completed);
        var applied = rpc.BindRemote(zdo, 77, zdo.DataRevision.ToString(), "", "");
        check(applied.Apply(completed, StorageRpc.Encode(LocalRecoveryScenario.Layout(chest, 1))).IsAccepted,
            "an owner acknowledges a durable apply receipt after the old reservation was released");
        check(chest.GetInventory().GetAllItems().Single().m_stack == 3,
            "receipt recovery never reapplies an already completed inventory layout");

        zdo.SetOwner(456);
        var revision = zdo.DataRevision.ToString();
        const string operation = "fixture:async-rebind";
        var participant = rpc.BindRemote(zdo, 77, revision, "", "");
        check(!participant.Prepare(operation, revision).IsAccepted, "a foreign owner preparation waits for its acknowledgement");
        Ack(routed, 456, zdo, operation, 0);
        participant = rpc.BindRemote(zdo, 77, revision, "", "");
        var sent = socket.Sent.Count;
        check(participant.Prepare(operation, revision).IsAccepted && socket.Sent.Count == sent,
            "a delayed preparation receipt survives rebuilding the participant binding");
        var other = rpc.BindRemote(zdo, 77, revision, "", "");
        check(!other.Prepare("fixture:other-operation", revision).IsAccepted,
            "interleaved operations on one chest never share preparation receipts");
        participant.Apply(operation, "layout");
        participant = rpc.BindRemote(zdo, 77, revision, "", "");
        Ack(routed, 456, zdo, operation, 1);
        check(participant.Receipt(operation).IsAccepted,
            "an apply acknowledgement reaches a rebound participant while another operation is bound");
        participant.Release(operation);
        participant = rpc.BindRemote(zdo, 77, revision, "", "");
        ReleaseAck(routed, 456, zdo, operation);
        check(participant.Release(operation).IsAccepted, "delayed release receipts survive rebinds and complete cleanup");

        zdo.SetOwner(789);
        participant = rpc.BindRemote(zdo, 77, revision, "", "");
        Ack(routed, 456, zdo, operation, 1);
        check(!participant.Receipt(operation).IsAccepted, "an owner change invalidates cached receipts and rejects late old-owner replies");
        participant.Apply(operation, "layout");
        Ack(routed, 789, zdo, operation, 1);
        check(participant.Receipt(operation).IsAccepted, "the new owner can independently confirm the durable apply receipt");
        rpc.ForgetProgress(operation);
        Ack(routed, 789, zdo, operation, 1);
        check(!rpc.BindRemote(zdo, 77, revision, "", "").Receipt(operation).IsAccepted,
            "late replies cannot recreate retired operation progress");

        zdo.SetOwner(456);
        var runtime = rpc.Bind(new[] { chest }, Player.m_localPlayer).Single();
        runtime.Prepare("fixture:runtime", runtime.Revision);
        Ack(routed, 456, zdo, "fixture:runtime", 0);
        check(runtime.Prepare("fixture:runtime", runtime.Revision).IsAccepted,
            "a locally discovered chest with a foreign owner receives its preparation acknowledgement");
        var anchorView = LocalRecoveryScenario.View("remote-anchor"); anchorView.GetZDO().SetOwner(456);
        var anchor = rpc.BindAnchor(anchorView, Player.m_localPlayer);
        anchor.Prepare("fixture:anchor", anchor.Revision);
        Ack(routed, 456, anchorView.GetZDO(), "fixture:anchor", 0);
        check(anchor.Prepare("fixture:anchor", anchor.Revision).IsAccepted,
            "a foreign-owned anchor receives its preparation acknowledgement");

        var player = Player.m_localPlayer;
        player.GetInventory().GetAllItems().Clear();
        player.m_customData.Clear();
        var playerRevision = GameInventoryAdapter.Revision(player.GetInventory());
        var playerLayout = StorageRpc.Encode(GameInventoryAdapter.Snapshot("player:77", playerRevision, player.GetInventory()));
        var localPlayer = rpc.BindRemotePlayer(77, ZDOMan.GetSessionID(), playerRevision, playerLayout);
        check(localPlayer.Prepare("fixture:self-player", playerRevision).IsAccepted &&
            localPlayer.Apply("fixture:self-player", playerLayout).IsAccepted && localPlayer.Release("fixture:self-player").IsAccepted,
            "host player prepare, apply and release accept synchronous self acknowledgements");
        check(!player.m_customData.ContainsKey("scs.storage.active.v1"), "a completed host-player operation leaves no reservation");

        var replicated = LocalRecoveryScenario.Chest("identity-replication", 1).m_nview.GetZDO();
        replicated.Persistent = true; replicated.SetOwner(456);
        replicated.Set("owner.snapshot", "before-server-identity");
        var ownerSnapshot = new ZPackage(); replicated.Serialize(ownerSnapshot);
        StorageJournal.EnsureStableIdentity(replicated);
        var stableIdentity = replicated.GetString("scs.storage.identity.v1", "");
        ownerSnapshot.SetPos(0); replicated.Deserialize(ownerSnapshot);
        check(stableIdentity.Length != 0 && replicated.GetString("scs.storage.identity.v1", "") == stableIdentity,
            "native owner snapshot deserialization preserves a newer server-assigned durable identity");
    }

    private static void Ack(ZRoutedRpc routed, long owner, ZDO zdo, string operation, int phase)
    {
        var reply = new ZPackage(); reply.Write(operation); reply.Write(zdo.m_uid.ToString());
        reply.Write(phase); reply.Write(true); reply.Write("");
        NetworkHarness.Deliver(routed, owner, "SCS_StorageParticipantAck_v1", reply);
    }

    private static void ReleaseAck(ZRoutedRpc routed, long owner, ZDO zdo, string operation)
    {
        var reply = new ZPackage(); reply.Write(zdo.m_uid); reply.Write(operation); reply.Write(true);
        NetworkHarness.Deliver(routed, owner, "SCS_StorageReleaseAck_v1", reply);
    }
}
