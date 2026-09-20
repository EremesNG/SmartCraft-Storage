using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartCraftStorage.Storage.Core;

namespace SmartCraftStorage.Storage.Runtime
{
    internal sealed class StorageRpc
    {
        private const string PrepareRpc = "SCS_StoragePrepare_v1";
        private const string ApplyRpc = "SCS_StorageApply_v1";
        private const string ReleaseRpc = "SCS_StorageRelease_v1";
        private const string ReleaseAckRpc = "SCS_StorageReleaseAck_v1";
        private const string ParticipantAckRpc = "SCS_StorageParticipantAck_v1";
        private const string RequestRpc = "SCS_StorageRequest_v1";
        private const string ResultRpc = "SCS_StorageResult_v1";
        private const string PlayerPrepareRpc = "SCS_PlayerPrepare_v1";
        private const string PlayerApplyRpc = "SCS_PlayerApply_v1";
        private const string PlayerReleaseRpc = "SCS_PlayerRelease_v1";
        private const string PlayerAckRpc = "SCS_PlayerAck_v1";
        private const string EffectApplyRpc = "SCS_EffectApply_v1";
        private const string EffectAckRpc = "SCS_EffectAck_v1";
        private const string NameApplyRpc = "SCS_NameApply_v1";
        private const string NameAckRpc = "SCS_NameAck_v1";
        private const string EffectReleaseRpc = "SCS_EffectRelease_v1";
        private const string ActiveKey = "scs.storage.active.v1";
        private const string ReceiptsKey = "scs.storage.receipts.v1";
        private const string ReceiptWatermarksKey = "scs.storage.receipt.watermarks.v1";
        private const string PlayerEffectActiveKey = "scs.storage.effect.active.v1";
        private readonly Dictionary<string, IStorageTransactionParticipant> _participants = new Dictionary<string, IStorageTransactionParticipant>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, ParticipantProgress>> _progress = new Dictionary<string, Dictionary<string, ParticipantProgress>>(StringComparer.Ordinal);
        private ZRoutedRpc _registeredRpc;
        internal Action<long, ZPackage> RequestReceived;
        internal Action<StorageOperation> ResultReceived;
        internal Func<string, StorageEffectStage, StorageEffectDescriptor, string, StorageEffectStepResult> EffectReceived;
        internal Action<long, string, StorageEffectStage, StorageEffectStepResult> EffectAcknowledged;
        internal Action<long, string, bool, string> NameAcknowledged;
        internal Action<string, string> EffectReleased;
        internal Action<string> ProgressReceived;

        internal StorageRpc()
        {
            EnsureRegistered();
        }

        internal void ResetParticipants() { _participants.Clear(); _progress.Clear(); }
        internal void ForgetProgress(string operationId) => _progress.Remove(operationId);

        internal void EnsureRegistered()
        {
            if (ZRoutedRpc.instance == null || ReferenceEquals(_registeredRpc, ZRoutedRpc.instance)) return;
            ResetParticipants();
            TryRegister(PrepareRpc, OnPrepare);
            TryRegister(ApplyRpc, OnApply);
            TryRegister(ReleaseRpc, OnRelease);
            TryRegister(ReleaseAckRpc, OnReleaseAck);
            TryRegister(ParticipantAckRpc, OnParticipantAck);
            TryRegister(RequestRpc, OnRequest);
            TryRegister(ResultRpc, OnResult);
            TryRegister(PlayerPrepareRpc, OnPlayerPrepare);
            TryRegister(PlayerApplyRpc, OnPlayerApply);
            TryRegister(PlayerReleaseRpc, OnPlayerRelease);
            TryRegister(PlayerAckRpc, OnPlayerAck);
            TryRegister(EffectApplyRpc, OnEffectApply);
            TryRegister(EffectAckRpc, OnEffectAck);
            TryRegister(NameApplyRpc, OnNameApply);
            TryRegister(NameAckRpc, OnNameAck);
            TryRegister(EffectReleaseRpc, OnEffectRelease);
            _registeredRpc = ZRoutedRpc.instance;
        }

        internal void Submit(ZPackage package)
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance != null) Send(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, package);
        }
        internal void Reply(long peer, StorageOperation operation)
        {
            EnsureRegistered();
            var package = new ZPackage(); package.Write(operation.Id); package.Write((int)operation.Status); package.Write(operation.Requested); package.Write(operation.Accepted); package.Write(operation.Remaining); package.Write(operation.Message); package.Write(operation.Captured);
            ZRoutedRpc.instance?.InvokeRoutedRPC(peer, ResultRpc, package);
        }
        internal void ApplyEffect(long owner, string operationId, StorageEffectStage stage, StorageEffectDescriptor descriptor, string escrow)
        {
            EnsureRegistered();
            var package = new ZPackage(); package.Write(operationId); package.Write((int)stage); WriteDescriptor(package, descriptor); package.Write(escrow ?? "");
            Send(owner, EffectApplyRpc, package);
        }
        internal void ApplyName(long owner, ZDOID target, string operationId, long actorId, string name)
        {
            EnsureRegistered();
            var package = new ZPackage(); package.Write(target); package.Write(operationId); package.Write(actorId); package.Write(name ?? "");
            Send(owner, NameApplyRpc, package);
        }
        internal void ReleaseEffect(long owner, string targetId, string operationId)
        {
            EnsureRegistered();
            var package = new ZPackage(); package.Write(targetId ?? ""); package.Write(operationId);
            ZRoutedRpc.instance?.InvokeRoutedRPC(owner, EffectReleaseRpc, package);
        }

        private void OnRequest(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (sender != ZDOMan.GetSessionID() && !ZNet.instance.GetPeers().Any(x => x.m_uid == sender && x.IsReady())) return;
            RequestReceived?.Invoke(sender, package);
        }
        private void OnResult(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            ResultReceived?.Invoke(new StorageOperation(package.ReadString(), (StorageOperationStatus)package.ReadInt(), package.ReadInt(), package.ReadInt(), package.ReadInt(), package.ReadString(), package.ReadBool()));
        }

        private static void OnPlayerPrepare(long sender, ZPackage package)
        {
            if (!FromServer(sender) || Player.m_localPlayer == null) return;
            var operationId = package.ReadString(); var expectedRevision = package.ReadString(); var expectedPayload = package.ReadString(); var actorId = package.ReadLong();
            if (Player.m_localPlayer.GetPlayerID() != actorId) return;
            Player.m_localPlayer.m_customData.TryGetValue(PlayerParticipant.PlayerActiveKey, out var active);
            Player.m_localPlayer.m_customData.TryGetValue(PlayerEffectActiveKey, out var activeEffects);
            var actual = GameInventoryAdapter.Snapshot("player:" + actorId, GameInventoryAdapter.Revision(Player.m_localPlayer.GetInventory()), Player.m_localPlayer.GetInventory());
            var effectConflict = (activeEffects ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x != operationId);
            var accepted = !effectConflict && (string.IsNullOrEmpty(active) || active == operationId) && actual.Revision == expectedRevision &&
                (string.IsNullOrEmpty(expectedPayload) || Encode(actual) == expectedPayload);
            if (accepted) Player.m_localPlayer.m_customData[PlayerParticipant.PlayerActiveKey] = operationId;
            SendPlayerAck(operationId, actorId, 0, accepted);
        }
        private static void OnPlayerApply(long sender, ZPackage package)
        {
            if (!FromServer(sender) || Player.m_localPlayer == null) return;
            var operationId = package.ReadString(); var actorId = package.ReadLong(); var payload = package.ReadString();
            if (Player.m_localPlayer.GetPlayerID() != actorId) return;
            var receipts = PlayerParticipant.ReadPlayerReceipts(Player.m_localPlayer);
            var accepted = PlayerParticipant.HasPlayerReceipt(Player.m_localPlayer, operationId);
            Player.m_localPlayer.m_customData.TryGetValue(PlayerParticipant.PlayerActiveKey, out var active);
            if (!accepted && active == operationId)
            {
                try { GameInventoryAdapter.ApplyLayout(Player.m_localPlayer.GetInventory(), Decode(payload)); receipts.Add(operationId); PlayerParticipant.WritePlayerReceipts(Player.m_localPlayer, receipts); accepted = true; }
                catch (Exception error) { ZLog.LogWarning("[SmartCraft-Storage] Player apply remains pending: " + error.Message); }
            }
            SendPlayerAck(operationId, actorId, 1, accepted);
        }
        private static void OnPlayerRelease(long sender, ZPackage package)
        {
            if (!FromServer(sender) || Player.m_localPlayer == null) return;
            var operationId = package.ReadString(); Player.m_localPlayer.m_customData.TryGetValue(PlayerParticipant.PlayerActiveKey, out var active);
            if (active == operationId) Player.m_localPlayer.m_customData.Remove(PlayerParticipant.PlayerActiveKey);
            SendPlayerAck(operationId, Player.m_localPlayer.GetPlayerID(), 2, true);
        }
        private void OnPlayerAck(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            var operationId = package.ReadString(); var actorId = package.ReadLong(); var phase = package.ReadInt(); var accepted = package.ReadBool();
            var local = sender == ZDOMan.GetSessionID() && Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == actorId;
            var peer = ZNet.instance.GetPeers().FirstOrDefault(x => x.m_uid == sender && x.m_playerID == actorId && x.IsReady());
            if (!local && peer == null) return;
            Acknowledge(operationId, "player:" + actorId, sender, phase, accepted, "Player preparation rejected", actorId);
        }
        private void OnEffectApply(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            var operationId = package.ReadString(); var stage = (StorageEffectStage)package.ReadInt(); var descriptor = ReadDescriptor(package); var escrow = package.ReadString();
            var result = EffectReceived?.Invoke(operationId, stage, descriptor, escrow) ?? StorageEffectStepResult.Unknown("Effect handler unavailable");
            var reply = new ZPackage(); reply.Write(operationId); reply.Write((int)stage); WriteStepResult(reply, result);
            ZRoutedRpc.instance?.InvokeRoutedRPC(EffectAckRpc, reply);
        }
        private void OnEffectAck(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (sender != ZDOMan.GetSessionID() && !ZNet.instance.GetPeers().Any(x => x.m_uid == sender && x.IsReady())) return;
            EffectAcknowledged?.Invoke(sender, package.ReadString(), (StorageEffectStage)package.ReadInt(), ReadStepResult(package));
        }
        private static void OnNameApply(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            var view = Resolve(package.ReadZDOID()); var operationId = package.ReadString(); var actorId = package.ReadLong(); var name = package.ReadString();
            var accepted = false; var message = "Target unavailable";
            if (view != null && view.IsValid() && view.IsOwner())
            {
                var zdo = view.GetZDO();
                accepted = StorageNameFlow.ApplyOnce(() => RuntimeParticipant.HasReceipt(zdo, operationId), () =>
                {
                    var container = view.GetComponent<Container>();
                    if (!StorageAccess.HasWardAccess(view.transform.position, actorId) || !string.IsNullOrEmpty(zdo.GetString(ActiveKey, "")) ||
                        (container != null && !StorageAccess.CanUse(container, actorId))) return false;
                    zdo.Set(StorageFacade.NetworkNameKey, StorageDiscovery.Normalize(name)); return true;
                }, () =>
                {
                    var receipts = RuntimeParticipant.ReadReceipts(zdo); receipts.Add(operationId); RuntimeParticipant.WriteReceipts(zdo, receipts);
                });
                message = accepted ? "Network name updated" : "Access denied";
            }
            var reply = new ZPackage(); reply.Write(operationId); reply.Write(accepted); reply.Write(message); ZRoutedRpc.instance?.InvokeRoutedRPC(NameAckRpc, reply);
        }
        private void OnNameAck(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (sender != ZDOMan.GetSessionID() && !ZNet.instance.GetPeers().Any(x => x.m_uid == sender && x.IsReady())) return;
            NameAcknowledged?.Invoke(sender, package.ReadString(), package.ReadBool(), package.ReadString());
        }
        private void OnEffectRelease(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            EffectReleased?.Invoke(package.ReadString(), package.ReadString());
        }
        private static void SendPlayerAck(string operationId, long actorId, int phase, bool accepted)
        {
            var package = new ZPackage(); package.Write(operationId); package.Write(actorId); package.Write(phase); package.Write(accepted); ZRoutedRpc.instance?.InvokeRoutedRPC(PlayerAckRpc, package);
        }

        internal IReadOnlyList<IStorageTransactionParticipant> Bind(IEnumerable<Container> containers, Player actor)
        {
            var bound = new List<IStorageTransactionParticipant>();
            foreach (var container in containers)
            {
                var id = StorageDiscovery.Id(container.m_nview);
                var participant = new RuntimeParticipant(this, container, actor, container.m_nview.GetZDO().GetString(StorageFacade.NetworkNameKey, ""));
                _participants[id] = participant; bound.Add(participant);
            }
            return bound;
        }
        internal IStorageTransactionParticipant BindAnchor(ZNetView anchor, Player actor)
        {
            var participant = new AnchorParticipant(this, anchor, actor);
            _participants[participant.Id] = participant;
            return participant;
        }
        internal IStorageTransactionParticipant BindPlayer(Player actor, string expectedRevision = null, string expectedPayload = "")
        {
            var id = "player:" + actor.GetPlayerID();
            var participant = new PlayerParticipant(this, actor, expectedRevision, expectedPayload);
            _participants[id] = participant;
            return participant;
        }
        internal IStorageTransactionParticipant BindRemote(ZDO zdo, long actorId, string expectedRevision,
            string expectedPayload, string expectedNetworkName, bool anchor = false)
        {
            var participant = new RemoteParticipant(this, zdo, actorId, expectedRevision, expectedPayload, expectedNetworkName, anchor);
            _participants[participant.Id] = participant; return participant;
        }
        internal IStorageTransactionParticipant BindRemotePlayer(long actorId, long peerId, string expectedRevision, string expectedPayload)
        {
            var participant = new RemotePlayerParticipant(this, actorId, peerId, expectedRevision, expectedPayload);
            _participants[participant.Id] = participant; return participant;
        }
        internal IStorageTransactionParticipant Find(string id) { _participants.TryGetValue(id, out var value); return value; }
        internal IStorageTransactionParticipant ResolveParticipant(string id, Player actor, string expectedRevision, string expectedPayload = "")
        {
            if (_participants.TryGetValue(id, out var existing)) return existing;
            if (id.StartsWith("player:", StringComparison.Ordinal))
            {
                if (!long.TryParse(id.Substring(7), out var playerId)) return null;
                var player = Player.GetPlayer(playerId); return player != null ? BindPlayer(player, expectedRevision, expectedPayload) : null;
            }
            var parts = id.Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var userId) || !uint.TryParse(parts[1], out var objectId)) return null;
            var zdo = ZDOMan.instance?.GetZDO(new ZDOID(userId, objectId));
            var view = zdo != null ? ZNetScene.instance?.FindInstance(zdo) : null;
            var container = view != null ? view.GetComponent<Container>() : null;
            if (container == null || actor == null) return null;
            var participant = new RuntimeParticipant(this, container, actor, zdo.GetString(StorageFacade.NetworkNameKey, ""));
            _participants[id] = participant; return participant;
        }
        internal bool Busy(ZNetView view) => view != null && view.IsValid() && !string.IsNullOrEmpty(view.GetZDO().GetString(ActiveKey, ""));

        private static void TryRegister(string name, Action<long, ZPackage> handler)
        {
            try { ZRoutedRpc.instance.Register(name, handler); }
            catch (ArgumentException) { }
        }

        private static void OnPrepare(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            var id = package.ReadZDOID(); var view = Resolve(id); var operationId = package.ReadString(); var expectedRevision = package.ReadString(); var actorId = package.ReadLong(); var expectedName = package.ReadString(); var expectedPayload = package.ReadString();
            var container = view != null ? view.GetComponent<Container>() : null;
            var anchor = expectedName == "@anchor";
            var accepted = view != null && view.IsOwner() &&
                (anchor ? StorageAccess.HasWardAccess(view.transform.position, actorId) : container != null && StorageAccess.CanUse(container, actorId));
            if (!accepted) { SendParticipantAck(operationId, id.ToString(), 0, false, "Access denied"); return; }
            var zdo = view.GetZDO(); var active = zdo.GetString(ActiveKey, "");
            if (active == operationId) { SendParticipantAck(operationId, id.ToString(), 0, true, ""); return; }
            accepted = active.Length == 0 && zdo.DataRevision.ToString() == expectedRevision &&
                (anchor || zdo.GetString(StorageFacade.NetworkNameKey, "") == expectedName) &&
                (anchor || string.IsNullOrEmpty(expectedPayload) || StorageRpc.Encode(GameInventoryAdapter.Snapshot(id.ToString(), expectedRevision, container.GetInventory())) == expectedPayload);
            if (accepted) zdo.Set(ActiveKey, operationId);
            SendParticipantAck(operationId, id.ToString(), 0, accepted, accepted ? "" : "Participant changed");
        }

        private static void OnApply(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            var id = package.ReadZDOID(); var view = Resolve(id); var operationId = package.ReadString(); var actorId = package.ReadLong(); var payload = package.ReadString();
            var container = view != null ? view.GetComponent<Container>() : null;
            var anchor = payload == "anchor";
            if (view == null || !view.IsOwner()) return;
            // A completed receipt remains valid after release or owner migration.
            // Reply without touching the inventory or a newer reservation.
            if (RuntimeParticipant.HasReceipt(view.GetZDO(), operationId))
            { SendParticipantAck(operationId, id.ToString(), 1, true, ""); return; }
            if (view.GetZDO().GetString(ActiveKey, "") != operationId ||
                (anchor ? !StorageAccess.HasWardAccess(view.transform.position, actorId) : container == null || !StorageAccess.CanUse(container, actorId))) return;
            var receipts = RuntimeParticipant.ReadReceipts(view.GetZDO());
            try { if (!anchor) GameInventoryAdapter.ApplyLayout(container.GetInventory(), Decode(payload)); receipts.Add(operationId); RuntimeParticipant.WriteReceipts(view.GetZDO(), receipts); }
            catch (Exception error) { ZLog.LogWarning("[SmartCraft-Storage] Owner apply remains pending: " + error.Message); }
            var applied = RuntimeParticipant.HasReceipt(view.GetZDO(), operationId);
            SendParticipantAck(operationId, id.ToString(), 1, applied, applied ? "" : "Apply pending");
        }

        private static void OnRelease(long sender, ZPackage package)
        {
            if (!FromServer(sender)) return;
            var id = package.ReadZDOID(); var view = Resolve(id); var operationId = package.ReadString();
            if (view != null && view.IsOwner() && view.GetZDO().GetString(ActiveKey, "") == operationId) view.GetZDO().Set(ActiveKey, "");
            var reply = new ZPackage(); reply.Write(id); reply.Write(operationId);
            reply.Write(view != null && view.IsOwner() && view.GetZDO().GetString(ActiveKey, "") != operationId);
            ZRoutedRpc.instance?.InvokeRoutedRPC(ReleaseAckRpc, reply);
        }

        private void OnReleaseAck(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            var id = package.ReadZDOID(); var operationId = package.ReadString(); var accepted = package.ReadBool();
            var zdo = ZDOMan.instance?.GetZDO(id);
            if (zdo == null || zdo.GetOwner() != sender) return;
            Acknowledge(operationId, id.ToString(), sender, 2, accepted, "Release pending");
        }

        private void OnParticipantAck(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            var operationId = package.ReadString(); var participantId = package.ReadString(); var phase = package.ReadInt();
            var accepted = package.ReadBool(); var message = package.ReadString();
            Acknowledge(operationId, participantId, sender, phase, accepted, message);
        }

        // Bindings are rebuilt from the journal on each recovery attempt. Receipts
        // belong to the operation and participant, not to one binding instance.
        private ParticipantProgress Progress(string operationId, string participantId, long actorId, long owner, ZDO zdo = null)
        {
            if (!_progress.TryGetValue(operationId, out var participants))
                _progress[operationId] = participants = new Dictionary<string, ParticipantProgress>(StringComparer.Ordinal);
            if (!participants.TryGetValue(participantId, out var value) || value.Owner != owner ||
                value.ActorId != actorId || !ReferenceEquals(value.Zdo, zdo))
                participants[participantId] = value = new ParticipantProgress(actorId, owner, zdo);
            return value;
        }

        private StorageParticipantResult Exchange(string operationId, string participantId, long actorId, long owner,
            int phase, string method, ZPackage package, ZDO zdo = null)
        {
            var progress = Progress(operationId, participantId, actorId, owner, zdo);
            var result = progress.Result(phase);
            if (result.IsAccepted || result.IsRejected) return result;
            progress.Requested[phase] = true;
            Send(owner, method, package);
            return progress.Result(phase); // Self-routed acknowledgements can arrive inline.
        }

        private void Acknowledge(string operationId, string participantId, long sender, int phase, bool accepted,
            string message, long? actorId = null)
        {
            if (phase < 0 || phase > 2 || !_progress.TryGetValue(operationId, out var participants) ||
                !participants.TryGetValue(participantId, out var progress) || progress.Owner != sender ||
                (actorId.HasValue && progress.ActorId != actorId.Value)) return;
            if (progress.Zdo != null && (ZDOMan.instance?.GetZDO(progress.Zdo.m_uid) != progress.Zdo || progress.Zdo.GetOwner() != sender)) return;
            if (progress.Acknowledge(phase, accepted, message)) ProgressReceived?.Invoke(operationId);
        }

        private sealed class ParticipantProgress
        {
            internal readonly long ActorId, Owner;
            internal readonly ZDO Zdo;
            internal readonly bool[] Requested = new bool[3];
            private readonly bool[] _accepted = new bool[3];
            private bool _prepareRejected;
            private string _rejection = "";
            internal ParticipantProgress(long actorId, long owner, ZDO zdo) { ActorId = actorId; Owner = owner; Zdo = zdo; }
            internal StorageParticipantResult Result(int phase) => _accepted[phase] ? StorageParticipantResult.Accepted()
                : phase == 0 && _prepareRejected ? StorageParticipantResult.Rejected(_rejection)
                : StorageParticipantResult.Unknown(phase == 0 ? "Awaiting participant preparation" : phase == 1 ? "Awaiting participant receipt" : "Awaiting participant release");
            internal bool Acknowledge(int phase, bool accepted, string message)
            {
                if (!Requested[phase] || _accepted[phase] || (phase == 0 && _prepareRejected)) return false;
                if (accepted) { _accepted[phase] = true; return true; }
                // An unsuccessful apply/release is retryable, not permanent proof.
                if (phase != 0) return false;
                _prepareRejected = true; _rejection = message ?? "Participant rejected preparation"; return true;
            }
        }

        private static void SendParticipantAck(string operationId, string participantId, int phase, bool accepted, string message)
        {
            var package = new ZPackage(); package.Write(operationId); package.Write(participantId); package.Write(phase);
            package.Write(accepted); package.Write(message ?? ""); ZRoutedRpc.instance?.InvokeRoutedRPC(ParticipantAckRpc, package);
        }

        private static bool FromServer(long sender)
        {
            if (ZNet.instance == null) return false;
            return ZNet.instance.IsServer() ? sender == ZDOMan.GetSessionID() : ZNet.instance.GetServerPeer()?.m_uid == sender;
        }
        private static ZNetView Resolve(ZDOID id)
        {
            var zdo = ZDOMan.instance?.GetZDO(id); return zdo != null ? ZNetScene.instance?.FindInstance(zdo) : null;
        }
        private static void Send(long owner, string method, ZPackage package)
        {
            if (ZRoutedRpc.instance == null || ZNet.instance == null || owner == 0L) return;
            if (ZDOMan.instance == null || owner != ZDOMan.GetSessionID())
            {
                // Clients route through the server even when a different owner is
                // the final recipient. Leave room for game traffic and receipts.
                var peer = ZNet.instance.IsServer()
                    ? ZNet.instance.GetPeers().FirstOrDefault(x => x.m_uid == owner)
                    : ZNet.instance.GetServerPeer();
                if (peer?.m_socket == null || !peer.m_socket.IsConnected() || peer.m_socket.GetSendQueueSize() >= 64 * 1024) return;
            }
            // Only retryable commands pass here. Results, acknowledgements and
            // effect releases retain their normal send path so custody can settle.
            ZRoutedRpc.instance.InvokeRoutedRPC(owner, method, package);
        }

        private static void WriteDescriptor(ZPackage package, StorageEffectDescriptor descriptor)
        {
            package.Write(descriptor != null); if (descriptor == null) return;
            package.Write(descriptor.Kind); package.Write(descriptor.TargetId); package.Write(descriptor.ActorId); package.Write(descriptor.Data);
        }
        private static StorageEffectDescriptor ReadDescriptor(ZPackage package) => package.ReadBool()
            ? new StorageEffectDescriptor(package.ReadString(), package.ReadString(), package.ReadLong(), package.ReadString()) : null;
        private static void WriteStepResult(ZPackage package, StorageEffectStepResult result)
        {
            package.Write(result.IsAccepted); package.Write(result.IsRejected); package.Write(result.Accepted); package.Write(result.Remaining); package.Write(result.Message);
        }
        private static StorageEffectStepResult ReadStepResult(ZPackage package)
        {
            var accepted = package.ReadBool(); var rejected = package.ReadBool(); var exact = package.ReadInt(); var remaining = package.ReadInt(); var message = package.ReadString();
            return accepted ? StorageEffectStepResult.Applied(exact, remaining) : rejected ? StorageEffectStepResult.Rejected(message) : StorageEffectStepResult.Unknown(message);
        }

        internal static string Encode(StorageInventory inventory)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(inventory.Id); writer.Write(inventory.Revision); writer.Write(inventory.Width); writer.Write(inventory.Height); writer.Write(inventory.Items.Count);
                foreach (var item in inventory.Items) { writer.Write(item.Identity); writer.Write(item.Prefab); writer.Write(item.SharedName); writer.Write(item.Quality); writer.Write(item.WorldLevel); writer.Write(item.Amount); writer.Write(item.MaxStack); writer.Write(item.Slot); writer.Write(item.Payload); }
                return Convert.ToBase64String(stream.ToArray());
            }
        }
        internal static StorageInventory Decode(string encoded)
        {
            using (var reader = new BinaryReader(new MemoryStream(Convert.FromBase64String(encoded))))
            {
                var id = reader.ReadString(); var revision = reader.ReadString(); var width = reader.ReadInt32(); var height = reader.ReadInt32(); var count = reader.ReadInt32(); var items = new List<StorageStack>();
                for (var i = 0; i < count; i++) items.Add(new StorageStack(reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadString()));
                return new StorageInventory(id, revision, width, height, items);
            }
        }

        private sealed class RuntimeParticipant : IStorageTransactionParticipant
        {
            private readonly StorageRpc _rpc;
            private readonly Container _container; private readonly Player _actor; private readonly string _expectedNetworkName;
            internal RuntimeParticipant(StorageRpc rpc, Container container, Player actor, string expectedNetworkName) { _rpc = rpc; _container = container; _actor = actor; _expectedNetworkName = expectedNetworkName; }
            public string Id => StorageDiscovery.Id(_container.m_nview);
            public string Revision => _container.m_nview.GetZDO().DataRevision.ToString();
            public StorageParticipantResult Prepare(string operationId, string expectedRevision)
            {
                if (!StorageAccess.CanUse(_container, _actor)) return StorageParticipantResult.Rejected("Access or availability changed");
                var zdo = _container.m_nview.GetZDO(); var active = zdo.GetString(ActiveKey, "");
                if (active.Length != 0 && active != operationId) return StorageParticipantResult.Rejected("Participant busy");
                if (active == operationId) return StorageParticipantResult.Accepted();
                if (zdo.GetString(StorageFacade.NetworkNameKey, "") != _expectedNetworkName) return StorageParticipantResult.Rejected("Network membership changed");
                if (!_container.m_nview.IsOwner())
                {
                    var package = new ZPackage(); package.Write(_container.m_nview.GetZDO().m_uid); package.Write(operationId); package.Write(expectedRevision); package.Write(_actor.GetPlayerID()); package.Write(_expectedNetworkName);
                    package.Write(Encode(GameInventoryAdapter.Snapshot(Id, expectedRevision, _container.GetInventory())));
                    return _rpc.Exchange(operationId, Id, _actor.GetPlayerID(), zdo.GetOwner(), 0, PrepareRpc, package, zdo);
                }
                if (Revision != expectedRevision) return StorageParticipantResult.Rejected("Participant revision changed");
                zdo.Set(ActiveKey, operationId); return StorageParticipantResult.Accepted();
            }
            public StorageParticipantResult Apply(string operationId, string payload)
            {
                var receipt = Receipt(operationId); if (receipt.IsAccepted) return receipt;
                if (!StorageAccess.CanUse(_container, _actor) || _container.m_nview.GetZDO().GetString(StorageFacade.NetworkNameKey, "") != _expectedNetworkName) return StorageParticipantResult.Rejected("Authorization or membership changed");
                if (!_container.m_nview.IsOwner())
                {
                    var package = new ZPackage(); package.Write(_container.m_nview.GetZDO().m_uid); package.Write(operationId); package.Write(_actor.GetPlayerID()); package.Write(payload);
                    var zdo = _container.m_nview.GetZDO();
                    return _rpc.Exchange(operationId, Id, _actor.GetPlayerID(), zdo.GetOwner(), 1, ApplyRpc, package, zdo);
                }
                if (_container.m_nview.GetZDO().GetString(ActiveKey, "") != operationId) return StorageParticipantResult.Unknown("Reservation missing");
                try
                {
                    GameInventoryAdapter.ApplyLayout(_container.GetInventory(), Decode(payload));
                    var receipts = ReadReceipts(_container.m_nview.GetZDO()); receipts.Add(operationId); WriteReceipts(_container.m_nview.GetZDO(), receipts);
                    return StorageParticipantResult.Accepted();
                }
                catch (Exception error) { return StorageParticipantResult.Unknown(error.Message); }
            }
            public StorageParticipantResult Receipt(string operationId) => HasReceipt(_container.m_nview.GetZDO(), operationId) ? StorageParticipantResult.Accepted()
                : _rpc.Progress(operationId, Id, _actor.GetPlayerID(), _container.m_nview.GetZDO().GetOwner(), _container.m_nview.GetZDO()).Result(1);
            public StorageParticipantResult Release(string operationId)
            {
                if (_container.m_nview.IsOwner())
                {
                    if (_container.m_nview.GetZDO().GetString(ActiveKey, "") == operationId) _container.m_nview.GetZDO().Set(ActiveKey, "");
                    return StorageParticipantResult.Accepted();
                }
                var zdo = _container.m_nview.GetZDO();
                var package = new ZPackage(); package.Write(zdo.m_uid); package.Write(operationId);
                return _rpc.Exchange(operationId, Id, _actor.GetPlayerID(), zdo.GetOwner(), 2, ReleaseRpc, package, zdo);
            }
            internal static HashSet<string> ReadReceipts(ZDO zdo) => new HashSet<string>((zdo.GetString(ReceiptsKey, "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            internal static bool HasReceipt(ZDO zdo, string operationId)
            {
                if (ReadReceipts(zdo).Contains(operationId)) return true;
                try { return new StorageReplayFilter(zdo.GetString(ReceiptWatermarksKey, "")).Contains(operationId); } catch { return false; }
            }
            internal static void WriteReceipts(ZDO zdo, HashSet<string> receipts)
            {
                StorageReplayFilter watermarks; try { watermarks = new StorageReplayFilter(zdo.GetString(ReceiptWatermarksKey, "")); } catch { watermarks = new StorageReplayFilter(8192); }
                foreach (var retired in receipts.Where(watermarks.CanRemember).OrderByDescending(x => x, StringComparer.Ordinal).Skip(128).ToList())
                { watermarks.Remember(retired); receipts.Remove(retired); }
                zdo.Set(ReceiptsKey, string.Join("\n", receipts.OrderBy(x => x, StringComparer.Ordinal)));
                zdo.Set(ReceiptWatermarksKey, watermarks.Export());
            }
        }

        private sealed class AnchorParticipant : IStorageTransactionParticipant
        {
            private readonly StorageRpc _rpc;
            private readonly ZNetView _view;
            private readonly Player _actor;
            internal AnchorParticipant(StorageRpc rpc, ZNetView view, Player actor) { _rpc = rpc; _view = view; _actor = actor; }
            public string Id => StorageDiscovery.Id(_view);
            public string Revision => _view.GetZDO().DataRevision.ToString();
            public StorageParticipantResult Prepare(string operationId, string expectedRevision)
            {
                var zdo = _view.GetZDO(); var active = zdo.GetString(ActiveKey, "");
                if (active.Length != 0 && active != operationId) return StorageParticipantResult.Rejected("Anchor busy");
                if (active == operationId) return StorageParticipantResult.Accepted();
                if (Revision != expectedRevision || !StorageAccess.HasWardAccess(_view.transform.position, _actor.GetPlayerID()))
                    return StorageParticipantResult.Rejected("Anchor changed or access denied");
                if (!_view.IsOwner())
                {
                    var package = new ZPackage(); package.Write(zdo.m_uid); package.Write(operationId); package.Write(expectedRevision);
                    package.Write(_actor.GetPlayerID()); package.Write("@anchor"); package.Write("");
                    return _rpc.Exchange(operationId, Id, _actor.GetPlayerID(), zdo.GetOwner(), 0, PrepareRpc, package, zdo);
                }
                zdo.Set(ActiveKey, operationId); return StorageParticipantResult.Accepted();
            }
            public StorageParticipantResult Apply(string operationId, string payload)
            {
                var receipt = Receipt(operationId); if (receipt.IsAccepted) return receipt;
                var zdo = _view.GetZDO();
                if (!StorageAccess.HasWardAccess(_view.transform.position, _actor.GetPlayerID())) return StorageParticipantResult.Rejected("Anchor access changed");
                if (!_view.IsOwner())
                {
                    var package = new ZPackage(); package.Write(zdo.m_uid); package.Write(operationId); package.Write(_actor.GetPlayerID()); package.Write("anchor");
                    return _rpc.Exchange(operationId, Id, _actor.GetPlayerID(), zdo.GetOwner(), 1, ApplyRpc, package, zdo);
                }
                if (zdo.GetString(ActiveKey, "") != operationId) return StorageParticipantResult.Unknown("Anchor reservation missing");
                var receipts = RuntimeParticipant.ReadReceipts(zdo); receipts.Add(operationId); RuntimeParticipant.WriteReceipts(zdo, receipts);
                return StorageParticipantResult.Accepted();
            }
            public StorageParticipantResult Receipt(string operationId) => RuntimeParticipant.HasReceipt(_view.GetZDO(), operationId)
                ? StorageParticipantResult.Accepted() : _rpc.Progress(operationId, Id, _actor.GetPlayerID(), _view.GetZDO().GetOwner(), _view.GetZDO()).Result(1);
            public StorageParticipantResult Release(string operationId)
            {
                var zdo = _view.GetZDO();
                if (_view.IsOwner()) { if (zdo.GetString(ActiveKey, "") == operationId) zdo.Set(ActiveKey, ""); return StorageParticipantResult.Accepted(); }
                var package = new ZPackage(); package.Write(zdo.m_uid); package.Write(operationId);
                return _rpc.Exchange(operationId, Id, _actor.GetPlayerID(), zdo.GetOwner(), 2, ReleaseRpc, package, zdo);
            }
        }

        private sealed class RemoteParticipant : IStorageTransactionParticipant
        {
            private readonly StorageRpc _rpc;
            private readonly ZDO _zdo;
            private readonly long _actorId;
            private readonly string _expectedRevision, _expectedPayload, _expectedNetwork;
            private readonly bool _anchor;
            internal RemoteParticipant(StorageRpc rpc, ZDO zdo, long actorId, string expectedRevision, string expectedPayload, string expectedNetwork, bool anchor)
            { _rpc = rpc; _zdo = zdo; _actorId = actorId; _expectedRevision = expectedRevision; _expectedPayload = expectedPayload ?? ""; _expectedNetwork = expectedNetwork ?? ""; _anchor = anchor; }
            public string Id => _zdo.m_uid.ToString();
            public string Revision => _expectedRevision;
            internal long Owner => _zdo.GetOwner();
            public StorageParticipantResult Prepare(string operationId, string expectedRevision)
            {
                if (Owner == 0L) return StorageParticipantResult.Unknown("Participant owner unavailable");
                var package = new ZPackage(); package.Write(_zdo.m_uid); package.Write(operationId); package.Write(expectedRevision);
                package.Write(_actorId); package.Write(_anchor ? "@anchor" : _expectedNetwork); package.Write(_anchor ? "" : _expectedPayload);
                return _rpc.Exchange(operationId, Id, _actorId, Owner, 0, PrepareRpc, package, _zdo);
            }
            public StorageParticipantResult Apply(string operationId, string payload)
            {
                if (Owner == 0L) return StorageParticipantResult.Unknown("Participant owner unavailable");
                var package = new ZPackage(); package.Write(_zdo.m_uid); package.Write(operationId); package.Write(_actorId); package.Write(_anchor ? "anchor" : payload);
                return _rpc.Exchange(operationId, Id, _actorId, Owner, 1, ApplyRpc, package, _zdo);
            }
            public StorageParticipantResult Receipt(string operationId) => _rpc.Progress(operationId, Id, _actorId, Owner, _zdo).Result(1);
            public StorageParticipantResult Release(string operationId)
            {
                if (Owner == 0L) return StorageParticipantResult.Unknown("Participant owner unavailable during release");
                var package = new ZPackage(); package.Write(_zdo.m_uid); package.Write(operationId);
                return _rpc.Exchange(operationId, Id, _actorId, Owner, 2, ReleaseRpc, package, _zdo);
            }
        }

        private sealed class RemotePlayerParticipant : IStorageTransactionParticipant
        {
            private readonly StorageRpc _rpc;
            private readonly long _actorId, _peerId;
            private readonly string _revision, _payload;
            internal RemotePlayerParticipant(StorageRpc rpc, long actorId, long peerId, string revision, string payload)
            { _rpc = rpc; _actorId = actorId; _peerId = peerId; _revision = revision; _payload = payload ?? ""; }
            public string Id => "player:" + _actorId;
            public string Revision => _revision;
            public StorageParticipantResult Prepare(string operationId, string expectedRevision)
            {
                var package = new ZPackage(); package.Write(operationId); package.Write(expectedRevision); package.Write(_payload); package.Write(_actorId);
                return _rpc.Exchange(operationId, Id, _actorId, _peerId, 0, PlayerPrepareRpc, package);
            }
            public StorageParticipantResult Apply(string operationId, string payload)
            {
                var package = new ZPackage(); package.Write(operationId); package.Write(_actorId); package.Write(payload);
                return _rpc.Exchange(operationId, Id, _actorId, _peerId, 1, PlayerApplyRpc, package);
            }
            public StorageParticipantResult Receipt(string operationId) => _rpc.Progress(operationId, Id, _actorId, _peerId).Result(1);
            public StorageParticipantResult Release(string operationId)
            {
                var package = new ZPackage(); package.Write(operationId);
                return _rpc.Exchange(operationId, Id, _actorId, _peerId, 2, PlayerReleaseRpc, package);
            }
        }

        private sealed class PlayerParticipant : IStorageTransactionParticipant
        {
            private readonly StorageRpc _rpc;
            internal const string PlayerActiveKey = "scs.storage.active.v1";
            private const string PlayerReceiptsKey = "scs.storage.receipts.v1";
            private readonly Player _player;
            private readonly string _expectedRevision;
            private readonly string _expectedPayload;
            internal PlayerParticipant(StorageRpc rpc, Player player, string expectedRevision, string expectedPayload) { _rpc = rpc; _player = player; _expectedRevision = expectedRevision; _expectedPayload = expectedPayload ?? ""; }
            public string Id => "player:" + _player.GetPlayerID();
            public string Revision => _expectedRevision ?? ComputeRevision();
            public StorageParticipantResult Prepare(string operationId, string expectedRevision)
            {
                if (_player != Player.m_localPlayer)
                {
                    var peer = ZNet.instance.GetPeers().FirstOrDefault(x => x.m_playerID == _player.GetPlayerID() && x.IsReady()); if (peer == null) return StorageParticipantResult.Unknown("Player owner unavailable");
                    var package = new ZPackage(); package.Write(operationId); package.Write(expectedRevision); package.Write(_expectedPayload); package.Write(_player.GetPlayerID());
                    return _rpc.Exchange(operationId, Id, _player.GetPlayerID(), peer.m_uid, 0, PlayerPrepareRpc, package);
                }
                _player.m_customData.TryGetValue(PlayerActiveKey, out var active);
                _player.m_customData.TryGetValue(PlayerEffectActiveKey, out var activeEffects);
                if ((activeEffects ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x != operationId))
                    return StorageParticipantResult.Rejected("Player has an unsettled native effect");
                if (!string.IsNullOrEmpty(active) && active != operationId) return StorageParticipantResult.Rejected("Player inventory busy");
                if (ComputeRevision() != expectedRevision) return StorageParticipantResult.Rejected("Player inventory changed");
                if (_expectedPayload.Length != 0)
                {
                    var actual = GameInventoryAdapter.Snapshot(Id, Revision, _player.GetInventory());
                    if (Encode(actual) != _expectedPayload) return StorageParticipantResult.Rejected("Player inventory payload changed");
                }
                _player.m_customData[PlayerActiveKey] = operationId; return StorageParticipantResult.Accepted();
            }
            public StorageParticipantResult Apply(string operationId, string payload)
            {
                if (Receipt(operationId).IsAccepted) return StorageParticipantResult.Accepted();
                if (_player != Player.m_localPlayer)
                {
                    var peer = ZNet.instance.GetPeers().FirstOrDefault(x => x.m_playerID == _player.GetPlayerID() && x.IsReady()); if (peer == null) return StorageParticipantResult.Unknown("Player owner unavailable");
                    var package = new ZPackage(); package.Write(operationId); package.Write(_player.GetPlayerID()); package.Write(payload);
                    return _rpc.Exchange(operationId, Id, _player.GetPlayerID(), peer.m_uid, 1, PlayerApplyRpc, package);
                }
                _player.m_customData.TryGetValue(PlayerActiveKey, out var active);
                if (active != operationId) return StorageParticipantResult.Unknown("Player reservation missing");
                try
                {
                    GameInventoryAdapter.ApplyLayout(_player.GetInventory(), Decode(payload));
                    var receipts = ReceiptSet(); receipts.Add(operationId); WritePlayerReceipts(_player, receipts);
                    return StorageParticipantResult.Accepted();
                }
                catch (Exception error) { return StorageParticipantResult.Unknown(error.Message); }
            }
            public StorageParticipantResult Receipt(string operationId)
            {
                if (_player == Player.m_localPlayer) return HasPlayerReceipt(_player, operationId) ? StorageParticipantResult.Accepted() : StorageParticipantResult.Unknown("No receipt");
                var peer = ZNet.instance.GetPeers().FirstOrDefault(x => x.m_playerID == _player.GetPlayerID() && x.IsReady());
                return peer == null ? StorageParticipantResult.Unknown("Player owner unavailable") : _rpc.Progress(operationId, Id, _player.GetPlayerID(), peer.m_uid).Result(1);
            }
            public StorageParticipantResult Release(string operationId)
            {
                if (_player == Player.m_localPlayer)
                {
                    _player.m_customData.TryGetValue(PlayerActiveKey, out var active); if (active == operationId) _player.m_customData.Remove(PlayerActiveKey);
                    return StorageParticipantResult.Accepted();
                }
                var peer = ZNet.instance.GetPeers().FirstOrDefault(x => x.m_playerID == _player.GetPlayerID() && x.IsReady());
                if (peer == null) return StorageParticipantResult.Unknown("Player owner unavailable during release");
                var package = new ZPackage(); package.Write(operationId);
                return _rpc.Exchange(operationId, Id, _player.GetPlayerID(), peer.m_uid, 2, PlayerReleaseRpc, package);
            }
            private HashSet<string> ReceiptSet() => ReadPlayerReceipts(_player);
            internal static HashSet<string> ReadPlayerReceipts(Player player) { player.m_customData.TryGetValue(PlayerReceiptsKey, out var value); return new HashSet<string>((value ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal); }
            internal static bool HasPlayerReceipt(Player player, string operationId)
            {
                if (ReadPlayerReceipts(player).Contains(operationId)) return true;
                player.m_customData.TryGetValue(ReceiptWatermarksKey, out var encoded);
                try { return new StorageReplayFilter(encoded ?? "").Contains(operationId); } catch { return false; }
            }
            internal static void WritePlayerReceipts(Player player, HashSet<string> receipts)
            {
                player.m_customData.TryGetValue(ReceiptWatermarksKey, out var encoded); StorageReplayFilter watermarks;
                try { watermarks = new StorageReplayFilter(encoded ?? ""); } catch { watermarks = new StorageReplayFilter(8192); }
                foreach (var retired in receipts.Where(watermarks.CanRemember).OrderByDescending(x => x, StringComparer.Ordinal).Skip(128).ToList())
                { watermarks.Remember(retired); receipts.Remove(retired); }
                player.m_customData[PlayerReceiptsKey] = string.Join("\n", receipts.OrderBy(x => x, StringComparer.Ordinal));
                player.m_customData[ReceiptWatermarksKey] = watermarks.Export();
            }
            private string ComputeRevision() => GameInventoryAdapter.Revision(_player.GetInventory());
        }
    }
}
