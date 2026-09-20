using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartCraftStorage.Storage.Core;

namespace SmartCraftStorage.Storage.Runtime
{
    internal sealed class StorageService : IStorageService
    {
        private const string EscrowPrefix = "scs.storage.escrow.v1.";
        private const string EffectReceiptsKey = "scs.storage.effect.receipts.v1";
        private const string EffectReceiptWatermarksKey = "scs.storage.effect.receipt.watermarks.v1";
        private const string EffectActiveKey = "scs.storage.effect.active.v1";
        private const string PendingRequestsKey = "scs.storage.pending.requests.v1";
        private const string PlayerEffectActiveKey = "scs.storage.effect.active.v1";
        private const string OperationSequenceKey = "scs.storage.operation.sequence.v1";
        private const string ProfileProofMarker = "scs.storage.profile-proof.v1";
        private const string CaptureAuthorityKey = "scs.storage.capture.authority.v1";
        internal const string CaptureIntentKey = "scs.storage.capture.intent.v1";
        private readonly Func<StorageSettings> _settings;
        private readonly StorageDiscovery _discovery = new StorageDiscovery();
        private readonly StorageRpc _rpc = new StorageRpc();
        private readonly StorageJournal _journal;
        private readonly StorageEffects _effectCoordinator;
        private readonly Dictionary<string, IStorageEffectHandler> _effects = new Dictionary<string, IStorageEffectHandler>(StringComparer.Ordinal);
        private readonly Dictionary<string, StorageOperation> _operations = new Dictionary<string, StorageOperation>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _requestPeers = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _nameOwners = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, byte[]> _pendingRequests = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly StorageRetrySchedule _retries = new StorageRetrySchedule();
        private readonly HashSet<string> _admittedRequests = new HashSet<string>(StringComparer.Ordinal);
        private readonly Func<double> _clock;
        private readonly Dictionary<string, CachedInventory> _snapshotCache = new Dictionary<string, CachedInventory>(StringComparer.Ordinal);
        private readonly Dictionary<string, StorageView> _viewCache = new Dictionary<string, StorageView>(StringComparer.Ordinal);
        private int _viewCacheFrame = -1;
        private int _tickCursor;
        private ZDOMan _manager;
        private Player _localActor;
        private long _session;
        private string _world;
        private static int _executingEffects;
        internal static bool IsExecutingEffect => _executingEffects > 0;

        internal StorageService(Func<StorageSettings> settings)
            : this(settings, () => UnityEngine.Time.realtimeSinceStartupAsDouble) { }

        internal StorageService(Func<StorageSettings> settings, Func<double> clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings)); _journal = new StorageJournal();
            _rpc.RequestReceived = HandleServerRequest; _rpc.ResultReceived = operation =>
            {
                EnsureRuntimeContext();
                _operations.TryGetValue(operation.Id, out var previous);
                var needsFreshIntent = operation.Status == StorageOperationStatus.Requested;
                // Requested is also the server's request for fresh discovery.
                // It neither relinquishes captured output nor counts as progress.
                if (needsFreshIntent && previous != null)
                    operation = new StorageOperation(operation.Id, previous.Status, previous.Requested,
                        previous.Accepted, previous.Remaining, operation.Message, previous.Captured);
                operation = StorageEffects.AcceptStatus(previous, operation);
                _operations[operation.Id] = operation;
                if (operation.IsFinal || needsFreshIntent) _admittedRequests.Remove(operation.Id);
                else if (_pendingRequests.ContainsKey(operation.Id)) _admittedRequests.Add(operation.Id);
                if (!needsFreshIntent && (previous == null || previous.Status != operation.Status)) _retries.Progress(operation.Id);
                if (operation.IsFinal) { _pendingRequests.Remove(operation.Id); _retries.Forget(operation.Id); PersistPendingRequests(); }
                if (operation.IsFinal && Player.m_localPlayer != null) RemoveEscrow(Player.m_localPlayer, operation.Id);
            };
            _rpc.EffectReceived = ApplyLocalEffect;
            _rpc.EffectAcknowledged = AcknowledgeEffect;
            _rpc.NameAcknowledged = AcknowledgeName;
            _rpc.EffectReleased = RemoveLocalEffectMarker;
            _rpc.ProgressReceived = OnParticipantProgress;
            _effectCoordinator = new StorageEffects(_journal, new RuntimeEffectPort(this));
        }
        public bool Ready { get { EnsureRuntimeContext(); return ZNet.instance != null && ZDOMan.instance != null; } }

        private void EnsureRuntimeContext()
        {
            var manager = ZDOMan.instance;
            var session = manager != null ? ZDOMan.GetSessionID() : 0L;
            var world = WorldIdentity();
            if (ReferenceEquals(_manager, manager) && _session == session && _world == world && ReferenceEquals(_localActor, Player.m_localPlayer)) return;
            _manager = manager; _session = session; _world = world; _localActor = Player.m_localPlayer;
            _operations.Clear(); _requestPeers.Clear(); _nameOwners.Clear(); _pendingRequests.Clear();
            _retries.Clear();
            _admittedRequests.Clear();
            _snapshotCache.Clear(); _viewCache.Clear(); _viewCacheFrame = -1; _tickCursor = 0;
            _rpc.ResetParticipants();
            // Durable world records, profile intents and effect handlers survive this reset.
        }

        public StorageView Query(StorageContext context)
        {
            if (!Ready || context?.Actor == null) return StorageView.Unavailable();
            if (context.Scope == StorageScope.Terminal && !_settings().Enabled) return StorageView.Unavailable("Storage terminals disabled");
            var members = _discovery.Find(context, _settings());
            if (_discovery.LastUnavailableReason.Length != 0) return StorageView.Unavailable(_discovery.LastUnavailableReason);
            if (_viewCacheFrame != UnityEngine.Time.frameCount) { _viewCache.Clear(); _viewCacheFrame = UnityEngine.Time.frameCount; }
            var snapshots = members.Select(CachedSnapshot).ToList();
            var cacheKey = context.Actor.GetPlayerID().ToString(CultureInfo.InvariantCulture) + "|" + (int)context.Scope + "|" +
                (context.Anchor != null && context.Anchor.IsValid() ? context.Anchor.GetZDO().m_uid.ToString() : "none") + "|" +
                string.Join(";", snapshots.Select(x => x.Id + "@" + x.Revision));
            if (_viewCache.TryGetValue(cacheKey, out var cachedView)) return cachedView;
            var query = StoragePlanner.Query(snapshots);
            var rows = query.Rows.Select(x => new StorageRow(x.Identity, GameInventoryAdapter.DisplayItem(x.Sample), x.Amount)).ToList();
            var view = new StorageView(rows, query.Inventories.Select(x => x.Id).ToList(), query.UsedSlots, query.TotalSlots);
            if (_viewCache.Count < 64) _viewCache[cacheKey] = view;
            return view;
        }

        public StorageOperation Deposit(StorageContext context, ItemDrop.ItemData item, int amount, string operationKey = null)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer()) return QueueDeposit(context, item, amount, operationKey);
            if (!Valid(context) || item == null || amount <= 0 || item.m_equipped || !PlayerOwnsItem(context.Actor, item)) return Reject(operationKey, amount, "Invalid deposit");
            var existing = Existing(operationKey); if (existing != null) return existing;
            var members = _discovery.Find(context, _settings()); if (_discovery.LastUnavailableReason.Length != 0) return Reject(operationKey, amount, _discovery.LastUnavailableReason); if (members.Count == 0) return Reject(operationKey, amount, "No available members");
            EnsureStableIdentities(members.Select(x => x.m_nview.GetZDO()).Concat(ContextAnchors(context)));
            var plan = StoragePlanner.Deposit(members.Select(Snapshot), GameInventoryAdapter.ToStack(item, 0), amount);
            if (plan.Accepted == 0) return Confirm(operationKey, amount, 0, "Storage full");
            var playerBefore = PlayerSnapshot(context.Actor);
            var playerAfter = RemoveAtSlot(playerBefore, item.m_gridPos.y * playerBefore.Width + item.m_gridPos.x, plan.Accepted);
            return ExecuteLayouts(operationKey, "deposit", context.Actor, context.Anchor, members, plan.Inventories.Concat(new[] { playerAfter }), amount, plan.Accepted);
        }

        public StorageOperation Withdraw(StorageContext context, string identity, int amount, string operationKey = null, int destinationSlot = -1)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer()) return QueueSimple("withdraw", context, operationKey, package =>
            {
                package.Write(identity ?? ""); package.Write(amount); package.Write(destinationSlot);
                package.Write(destinationSlot >= 0 && context?.Actor != null ? StorageSlotExpectation.Capture(PlayerSnapshot(context.Actor), destinationSlot) : "");
            });
            if (!Valid(context) || string.IsNullOrEmpty(identity) || amount <= 0) return Reject(operationKey, amount, "Invalid withdrawal");
            var existing = Existing(operationKey); if (existing != null) return existing;
            var members = _discovery.Find(context, _settings());
            if (_discovery.LastUnavailableReason.Length != 0) return Reject(operationKey, amount, _discovery.LastUnavailableReason);
            EnsureStableIdentities(members.Select(x => x.m_nview.GetZDO()).Concat(ContextAnchors(context)));
            var plan = StoragePlanner.Withdraw(members.Select(Snapshot), PlayerSnapshot(context.Actor), identity, amount, destinationSlot);
            if (plan.Accepted == 0) return Confirm(operationKey, amount, 0, "No item or player capacity");
            return ExecuteLayouts(operationKey, "withdraw", context.Actor, context.Anchor, members, plan.Inventories, amount, plan.Accepted);
        }

        public StorageOperation Organize(StorageContext context, string operationKey = null)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer()) return QueueSimple("organize", context, operationKey, package => { });
            if (!Valid(context) || context.Scope != StorageScope.Terminal) return Reject(operationKey, 0, "Organize requires a terminal context");
            var existing = Existing(operationKey); if (existing != null) return existing;
            var members = _discovery.Find(context, _settings()); if (_discovery.LastUnavailableReason.Length != 0) return Reject(operationKey, 0, _discovery.LastUnavailableReason);
            EnsureStableIdentities(members.Select(x => x.m_nview.GetZDO()).Concat(ContextAnchors(context)));
            var plan = StoragePlanner.Organize(members.Select(Snapshot));
            if (plan.Moves == 0) return Confirm(operationKey, 0, 0, "Already organized");
            return ExecuteLayouts(operationKey, "organize", context.Actor, context.Anchor, members, plan.Inventories, plan.Moves, plan.Moves);
        }

        public StorageOperation PrepareCost(StorageContext context, IReadOnlyList<StorageRequirement> requirements, bool includePlayer, StorageEffectDescriptor effect, string operationKey)
        {
            if (ZNet.instance != null && !ZNet.instance.IsServer()) return QueueSimple("cost", context, operationKey, package =>
            {
                package.Write(includePlayer); WriteEffect(package, effect); package.Write(requirements?.Count ?? 0);
                foreach (var requirement in requirements ?? Array.Empty<StorageRequirement>()) { package.Write(requirement.SharedName); package.Write(requirement.Amount); package.Write(requirement.Quality); package.Write(requirement.Identity ?? ""); package.Write(requirement.WorldLevel); }
            });
            if (!Valid(context) || string.IsNullOrEmpty(operationKey) || effect == null || !_effects.ContainsKey(effect.Kind)) return Reject(operationKey, 0, "Cost effect unavailable");
            var existing = Existing(operationKey); if (existing != null) return existing;
            var members = _discovery.Find(context, _settings()).ToList();
            if (_discovery.LastUnavailableReason.Length != 0) return Reject(operationKey, 0, _discovery.LastUnavailableReason);
            EnsureStableIdentities(members.Select(x => x.m_nview.GetZDO()).Concat(ContextAnchors(context)));
            var sources = members.Select(Snapshot).ToList(); if (includePlayer) sources.Add(PlayerSnapshot(context.Actor));
            var final = sources.ToDictionary(x => x.Id, StringComparer.Ordinal); var escrowItems = new List<StorageStack>(); var requested = 0;
            foreach (var requirement in requirements ?? Array.Empty<StorageRequirement>())
            {
                requested += requirement.Amount; var remaining = requirement.Amount;
                foreach (var inventory in final.Values.ToList())
                {
                    var items = inventory.Items.ToList();
                    for (var i = 0; i < items.Count && remaining > 0; i++)
                    {
                        var item = items[i];
                        if (!Matches(item, requirement) || (inventory.Id.StartsWith("player:", StringComparison.Ordinal) && IsEquippedAt(context.Actor, item.Slot))) continue;
                        var take = Math.Min(remaining, item.Amount); remaining -= take;
                        escrowItems.Add(item.At(escrowItems.Count, take));
                        items[i] = item.At(item.Slot, item.Amount - take);
                    }
                    final[inventory.Id] = new StorageInventory(inventory.Id, inventory.Revision, inventory.Width, inventory.Height, items.Where(x => x.Amount > 0));
                }
                if (remaining != 0) return Reject(operationKey, requested, "Complete cost unavailable");
            }
            var escrow = new StorageInventory("escrow:" + operationKey, "1", Math.Max(1, escrowItems.Count), 1, escrowItems);
            PersistEscrow(context.Actor, operationKey, escrow, effect);
            var effectRecord = new StorageEffectRecord(1, WorldIdentity(), operationKey, context.Actor.GetPlayerID(), effect.TargetId,
                ContextIdentity(context), null, effect, null, StorageRpc.Encode(escrow), operationKey, requested, requested, 0,
                StorageEffectStage.CostPreparing);
            _journal.SaveEffect(effectRecord);
            var op = ExecuteLayouts(operationKey, "cost", context.Actor, context.Anchor, members, final.Values, requested, requested);
            if (op.Status == StorageOperationStatus.Confirmed)
            {
                _journal.SaveEffect(effectRecord.At(StorageEffectStage.CostPrepared, acknowledgement: "cost-prepared"));
                op = Remember(OperationFromEffect(_effectCoordinator.Resume(operationKey)));
            }
            else if (op.Status == StorageOperationStatus.Rejected || op.Status == StorageOperationStatus.Aborted)
            { _journal.SaveEffect(effectRecord.At(StorageEffectStage.Rejected, message: op.Message)); RemoveEscrow(context.Actor, operationKey); }
            return op;
        }

        public StorageOperation CaptureOutput(StorageContext context, ItemDrop.ItemData item, int amount,
            StorageEffectDescriptor captureEffect, StorageEffectDescriptor remainderEffect, string operationKey, bool requireAll = false)
        {
            if (!Ready || context?.Actor == null || item == null || amount <= 0 || string.IsNullOrEmpty(operationKey) ||
                captureEffect == null || !_effects.ContainsKey(captureEffect.Kind) || context.Anchor == null ||
                !context.Anchor.IsValid() || !context.Anchor.IsOwner())
                return Reject(operationKey, amount, "Producer owner unavailable");
            if (ZNet.instance != null && ZNet.instance.IsServer() && !_journal.Available)
                return Reject(operationKey, amount, "Durable storage journal unavailable");
            if (ZNet.instance != null && ZNet.instance.IsServer()) StorageJournal.EnsureStableIdentity(context.Anchor.GetZDO());
            var existing = Existing(operationKey);
            if (existing != null) { Resume(operationKey); return GetOperation(operationKey); }
            var outbox = new StorageInventory("outbox:" + operationKey, "1", 1, 1,
                new[] { GameInventoryAdapter.ToStack(item, 0).At(0, amount) });
            var result = StorageCaptureFlow.Execute(operationKey, amount, () =>
            {
                var pending = QueueSimple("output", context, operationKey, package =>
                {
                    package.Write(StorageRpc.Encode(outbox)); package.Write(amount);
                    WriteEffect(package, captureEffect); WriteEffect(package, remainderEffect); package.Write(requireAll);
                }, false);
                if (pending.IsFinal || pending.Status == StorageOperationStatus.Unavailable) return pending;
                PersistEscrow(context.Actor, operationKey, outbox, captureEffect);
                context.Anchor.GetZDO().Set(OutputOutboxKey(operationKey), StorageRpc.Encode(outbox));
                context.Anchor.GetZDO().Set(CaptureIntentKey, operationKey + "\n" + captureEffect.Data);
                context.Anchor.GetZDO().Set(CaptureAuthorityKey,
                    CaptureAuthority(operationKey, context.Actor.GetPlayerID()));
                AddActiveEffect(context.Anchor.GetZDO(), operationKey);
                return pending;
            }, () => ApplyLocalEffect(operationKey, StorageEffectStage.CapturePending, captureEffect, StorageRpc.Encode(outbox)),
            () =>
            {
                _pendingRequests.Remove(operationKey); PersistPendingRequests();
                RemoveLocalEffectMarker(captureEffect.TargetId, operationKey); RemoveEscrow(context.Actor, operationKey);
            }, () => { Resume(operationKey); return GetOperation(operationKey); });
            return Remember(result);
        }

        public StorageOperation GetOperation(string operationId)
        {
            EnsureRuntimeContext();
            if (_operations.TryGetValue(operationId ?? "", out var operation)) return operation;
            var effect = _journal.LoadEffect(operationId ?? ""); if (effect != null) return OperationFromEffect(effect);
            return StorageTransactions.GetStatus(_journal, operationId ?? "");
        }
        public StorageOperation GetPendingOperation(ZNetView participant, string kind)
        {
            EnsureRuntimeContext();
            if (participant == null || !participant.IsValid()) return null;
            RestorePendingRequests();
            var target = participant.GetZDO().m_uid.ToString();
            foreach (var encoded in _pendingRequests.Values)
            {
                var intent = StorageRequestIntent.Decode(encoded);
                if (intent.WorldId != WorldIdentity() || (!string.IsNullOrEmpty(kind) && intent.Kind != kind) ||
                    (intent.AnchorId != target && !HasOperationSourceMarker(participant.GetZDO(), intent.Id))) continue;
                var pending = GetOperation(intent.Id);
                if (!pending.IsFinal) return pending;
            }
            var id = participant.GetZDO().GetString("scs.storage.active.v1", ""); if (id.Length == 0) return null;
            var op = GetOperation(id);
            return !op.IsFinal && (string.IsNullOrEmpty(kind) || _journal.Load(id)?.Kind == kind) ? op : null;
        }
        public void Resume(string operationId)
        {
            EnsureRuntimeContext();
            if (!_retries.TryBegin(operationId, _clock())) return;
            if (_pendingRequests.ContainsKey(operationId ?? "")) { SubmitPending(operationId); return; }
            ResumeRecordedOperation(operationId);
        }

        private void ResumeRecordedOperation(string operationId)
        {
            var effect = _journal.LoadEffect(operationId);
            if (effect != null)
            {
                var resumed = _effectCoordinator.Resume(operationId);
                if (resumed?.Stage == StorageEffectStage.Completed)
                {
                    var effectActor = Player.GetPlayer(resumed.ActorId); if (effectActor != null) RemoveEscrow(effectActor, resumed.OperationId);
                    ReleaseEffectSource(resumed);
                }
                Remember(OperationFromEffect(resumed));
                return;
            }
            ResumeTransaction(operationId);
        }

        private void OnParticipantProgress(string operationId)
        {
            _retries.Progress(operationId);
            foreach (var effect in _journal.PendingEffects.Where(x => x.DeliveryTransactionId == operationId))
                _retries.Progress(effect.OperationId);
        }

        private void ResumeTransaction(string operationId)
        {
            var record = _journal.Load(operationId);
            var participants = new List<IStorageTransactionParticipant>();
            if (record == null) return;
            var actorId = record.ActorId;
            if (actorId == 0L)
            {
                var playerPlan = record.Participants.FirstOrDefault(x => x.ParticipantId.StartsWith("player:", StringComparison.Ordinal));
                if (playerPlan == null || !long.TryParse(playerPlan.ParticipantId.Substring(7), out actorId)) return;
            }
            var peer = actorId == (Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : long.MinValue)
                ? ZDOMan.GetSessionID() : ZNet.instance?.GetPeers().FirstOrDefault(x => x.m_playerID == actorId && x.IsReady())?.m_uid ?? 0L;
            foreach (var plan in record.Participants)
            {
                if (plan.ParticipantId.StartsWith("player:", StringComparison.Ordinal))
                {
                    if (peer != 0L) participants.Add(_rpc.BindRemotePlayer(actorId, peer, plan.ExpectedRevision, plan.ExpectedPayload));
                    continue;
                }
                if (!TryParseZdoId(plan.ParticipantId, out var zdoId)) continue;
                var zdo = ZDOMan.instance?.GetZDO(zdoId); if (zdo == null) continue;
                var anchor = plan.Payload == "anchor"; var network = anchor ? "" : zdo.GetString(StorageFacade.NetworkNameKey, "");
                participants.Add(_rpc.BindRemote(zdo, actorId, plan.ExpectedRevision, plan.ExpectedPayload, network, anchor));
            }
            if (participants == null || participants.Count == 0) return;
            var normalizedPlans = record.Participants.Select(plan => NormalizePlanIdentity(plan)).ToList();
            if (normalizedPlans.Where((plan, index) => plan.ParticipantId != record.Participants[index].ParticipantId ||
                    plan.Payload != record.Participants[index].Payload || plan.ExpectedPayload != record.Participants[index].ExpectedPayload).Any())
            {
                record = new StorageTransactionRecord(record.Id, record.Kind, record.Status, normalizedPlans, record.Message,
                    record.Requested, record.Accepted, record.ActorId);
                if (!_journal.TrySave(record)) return;
            }
            Remember(new StorageTransactions(_journal, participants).Resume(operationId));
        }

        private static bool TryParseZdoId(string value, out ZDOID id)
        {
            id = ZDOID.None; var parts = (value ?? "").Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var user) || !uint.TryParse(parts[1], out var objectId)) return false;
            id = new ZDOID(user, objectId); return true;
        }
        public void RegisterEffect(IStorageEffectHandler handler) { if (handler != null) _effects[handler.Kind] = handler; }
        public string GetNetworkName(ZNetView target) => target != null && target.IsValid() ? target.GetZDO().GetString(StorageFacade.NetworkNameKey, "") : "";
        public StorageOperation SetNetworkName(ZNetView target, Player actor, string name)
        {
            EnsureRuntimeContext();
            var id = NewId("name");
            if (actor == null || target == null || !target.IsValid()) return Reject(id, 0, "Target unavailable");
            var body = new ZPackage(); body.Write(target.GetZDO().m_uid); body.Write(name ?? "");
            var intent = new StorageRequestIntent("name", id, WorldIdentity(), StorageScope.Direct,
                target.transform.position.x, target.transform.position.y, target.transform.position.z, 0,
                target.GetZDO().m_uid.ToString(), body.GetArray());
            _pendingRequests[id] = intent.Encode(); PersistPendingRequests();
            Remember(new StorageOperation(id, StorageOperationStatus.Requested, message: "Requested"));
            Resume(id); return GetOperation(id);
        }

        private void AcknowledgeName(long sender, string operationId, bool accepted, string message)
        {
            if (!_nameOwners.TryGetValue(operationId, out var owner) || owner != sender) return;
            _nameOwners.Remove(operationId);
            Remember(new StorageOperation(operationId, accepted ? StorageOperationStatus.Confirmed : StorageOperationStatus.Rejected, message: message));
        }
        public bool IsBusy(ZNetView participant)
        {
            EnsureRuntimeContext();
            if (_rpc.Busy(participant)) return true;
            var player = participant != null ? participant.GetComponent<Player>() : null;
            if (player != null && player.m_customData.TryGetValue("scs.storage.active.v1", out var active) && !string.IsNullOrEmpty(active)) return true;
            if (participant != null && participant.IsValid() && !string.IsNullOrEmpty(participant.GetZDO().GetString(EffectActiveKey, ""))) return true;
            var id = participant != null && participant.IsValid() ? StorageDiscovery.Id(participant) : "";
            return _journal.PendingEffects.Any(x => x.TargetId == id ||
                (player != null && x.TargetId == "player:" + player.GetPlayerID()));
        }
        public bool TryClaimDirectWrite(ZNetView participant, Player actor)
        {
            if (participant == null || actor == null || !participant.IsValid() || IsBusy(participant)) return false;
            var container = participant.GetComponent<Container>(); if (container != null && !StorageAccess.CanUse(container, actor)) return false;
            return participant.IsOwner() && !IsBusy(participant);
        }
        public void Tick()
        {
            EnsureRuntimeContext();
            _rpc.EnsureRegistered();
            RestorePendingRequests();
            var budget = Math.Max(1, _settings().OperationsPerTick);
            var effects = _journal.PendingEffects.ToList();
            var effectIds = new HashSet<string>(effects.Select(x => x.OperationId), StringComparer.Ordinal);
            var effectTransactions = new HashSet<string>(effects.Select(x => x.DeliveryTransactionId).Where(x => !string.IsNullOrEmpty(x)), StringComparer.Ordinal);
            var work = _pendingRequests.Keys.Select(id => Tuple.Create(true, id)).Concat(effects.Select(x => Tuple.Create(true, x.OperationId))).Concat(_journal.Pending
                .Where(x => !effectIds.Contains(x.Id) && !effectTransactions.Contains(x.Id)).Select(x => Tuple.Create(false, x.Id)))
                .GroupBy(x => x.Item2, StringComparer.Ordinal).Select(x => x.First()).ToList();
            if (work.Count != 0)
            {
                var start = _tickCursor % work.Count; var count = Math.Min(budget, work.Count);
                for (var index = 0; index < count; index++)
                {
                    var item = work[(start + index) % work.Count];
                    if (item.Item1) Resume(item.Item2);
                    else
                    {
                        var record = _journal.Load(item.Item2);
                        if (record != null && record.Status == StorageOperationStatus.AwaitingEffect)
                        {
                            var requested = LoadEscrowAmount(record.Id);
                            Remember(new StorageOperation(record.Id, StorageOperationStatus.AwaitingEffect, requested, requested, 0, record.Message));
                        }
                        else Resume(item.Item2);
                    }
                }
                _tickCursor = (start + count) % work.Count;
            }
        }
        public void Dispose() { }

        private StorageOperation ExecuteLayouts(string key, string kind, Player actor, ZNetView anchor,
            IReadOnlyList<Container> members, IEnumerable<StorageInventory> layouts, int requested, int accepted)
        {
            var id = string.IsNullOrEmpty(key) ? NewId(kind) : key;
            var materializedLayouts = layouts.ToList();
            var playerLayout = materializedLayouts.FirstOrDefault(x => x.Id == "player:" + actor.GetPlayerID());
            var playerBefore = PlayerSnapshot(actor);
            var playerExpectedPayload = StorageRpc.Encode(playerBefore);
            var before = members.Select(Snapshot).Concat(new[] { playerBefore }).ToDictionary(x => x.Id, StringComparer.Ordinal);
            var participants = _rpc.Bind(members, actor).ToList(); participants.Add(_rpc.BindPlayer(actor, playerLayout?.Revision, playerExpectedPayload));
            var plans = new List<StorageParticipantPlan>();
            foreach (var layout in materializedLayouts)
            {
                var participant = participants.FirstOrDefault(x => x.Id == layout.Id); if (participant == null) continue;
                if (before.TryGetValue(layout.Id, out var originalLayout) && StorageRpc.Encode(originalLayout) == StorageRpc.Encode(layout) &&
                    !(kind == "cost" && layout.Id.StartsWith("player:", StringComparison.Ordinal))) continue;
                plans.Add(new StorageParticipantPlan(layout.Id, participant.Revision, StorageRpc.Encode(layout),
                    before.TryGetValue(layout.Id, out var original) ? StorageRpc.Encode(original) : ""));
            }
            if (anchor != null && anchor.IsValid())
            {
                var anchorParticipant = _rpc.BindAnchor(anchor, actor);
                if (!plans.Any(x => x.ParticipantId == anchorParticipant.Id))
                {
                    participants.Add(anchorParticipant);
                    plans.Add(new StorageParticipantPlan(anchorParticipant.Id, anchorParticipant.Revision, "anchor"));
                }
            }
            var raw = new StorageTransactions(_journal, participants).Execute(new StorageTransactionRequest(id, kind, plans, requested, accepted, actor.GetPlayerID()));
            return Remember(raw);
        }

        private StorageInventory Snapshot(Container container) => GameInventoryAdapter.Snapshot(StorageDiscovery.Id(container.m_nview), container.m_nview.GetZDO().DataRevision.ToString(), container.GetInventory());
        private sealed class CachedInventory
        {
            internal readonly string Revision; internal readonly StorageInventory Snapshot;
            internal CachedInventory(string revision, StorageInventory snapshot) { Revision = revision; Snapshot = snapshot; }
        }
        private StorageInventory CachedSnapshot(Container container)
        {
            var id = StorageDiscovery.Id(container.m_nview); var revision = container.m_nview.GetZDO().DataRevision.ToString();
            if (_snapshotCache.TryGetValue(id, out var cached) && cached.Revision == revision) return cached.Snapshot;
            var snapshot = Snapshot(container); _snapshotCache[id] = new CachedInventory(revision, snapshot);
            if (_snapshotCache.Count > 256) _snapshotCache.Remove(_snapshotCache.Keys.First());
            return snapshot;
        }
        private StorageInventory PlayerSnapshot(Player player)
        {
            return GameInventoryAdapter.Snapshot("player:" + player.GetPlayerID(), GameInventoryAdapter.Revision(player.GetInventory()), player.GetInventory());
        }
        private static StorageInventory RemoveAtSlot(StorageInventory source, int slot, int amount)
        {
            var items = source.Items.ToList(); var index = items.FindIndex(x => x.Slot == slot); if (index < 0 || items[index].Amount < amount) throw new InvalidOperationException("Deposit source changed");
            items[index] = items[index].At(slot, items[index].Amount - amount); return new StorageInventory(source.Id, source.Revision, source.Width, source.Height, items.Where(x => x.Amount > 0));
        }
        private static bool Matches(StorageStack item, StorageRequirement requirement) =>
            (requirement.Identity == null || item.Identity == requirement.Identity) && item.SharedName == requirement.SharedName &&
            (requirement.Quality < 0 || item.Quality == requirement.Quality) && (requirement.WorldLevel < 0 || item.WorldLevel >= requirement.WorldLevel);
        private static bool IsEquippedAt(Player player, int slot) => player.GetInventory().GetAllItems().Any(x => x.m_equipped && x.m_gridPos.y * player.GetInventory().GetWidth() + x.m_gridPos.x == slot);
        private bool Valid(StorageContext context) => Ready && context?.Actor != null && _journal.Available &&
            _journal.Pending.Count + _journal.PendingEffects.Count < _settings().MaxPendingOperations;

        private static IEnumerable<ZDO> ContextAnchors(StorageContext context)
        {
            if (context?.Anchor != null && context.Anchor.IsValid()) yield return context.Anchor.GetZDO();
        }

        private static bool EnsureStableIdentities(IEnumerable<ZDO> zdos)
        {
            var changed = false;
            foreach (var zdo in (zdos ?? Enumerable.Empty<ZDO>()).Where(x => x != null).GroupBy(x => x.m_uid).Select(x => x.First()))
                changed |= StorageJournal.EnsureStableIdentity(zdo);
            return changed;
        }

        private static StorageParticipantPlan NormalizePlanIdentity(StorageParticipantPlan plan)
        {
            if (plan == null || plan.ParticipantId.StartsWith("player:", StringComparison.Ordinal)) return plan;
            return new StorageParticipantPlan(plan.ParticipantId, plan.ExpectedRevision,
                NormalizeInventoryIdentity(plan.Payload, plan.ParticipantId),
                NormalizeInventoryIdentity(plan.ExpectedPayload, plan.ParticipantId));
        }

        private static string NormalizeInventoryIdentity(string encoded, string participantId)
        {
            if (string.IsNullOrEmpty(encoded) || encoded == "anchor") return encoded;
            try
            {
                var layout = StorageRpc.Decode(encoded);
                if (layout.Id == participantId) return encoded;
                return StorageRpc.Encode(new StorageInventory(participantId, layout.Revision, layout.Width, layout.Height, layout.Items));
            }
            catch { return encoded; }
        }
        private StorageOperation Existing(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_operations.TryGetValue(key, out var value)) return value;
            var effect = _journal.LoadEffect(key); if (effect != null) return OperationFromEffect(effect);
            var transaction = _journal.Load(key); return transaction == null ? null : new StorageOperation(transaction.Id, transaction.Status,
                transaction.Requested, transaction.Status == StorageOperationStatus.Confirmed ? transaction.Accepted : 0,
                transaction.Status == StorageOperationStatus.Confirmed ? transaction.Requested - transaction.Accepted : transaction.Requested, transaction.Message);
        }
        private string NewId(string kind)
        {
            var actor = Player.m_localPlayer; var previous = 0L;
            if (actor != null && actor.m_customData.TryGetValue(OperationSequenceKey, out var encoded)) long.TryParse(encoded, out previous);
            var sequence = Math.Max(previous + 1, DateTime.UtcNow.Ticks);
            if (actor != null) actor.m_customData[OperationSequenceKey] = sequence.ToString(CultureInfo.InvariantCulture);
            var issuer = actor != null ? actor.GetPlayerID() : ZDOMan.GetSessionID();
            return "scs:" + WorldIdentity() + ":" + issuer.ToString(CultureInfo.InvariantCulture) + ":" + (kind ?? "operation") + ":" + sequence.ToString(CultureInfo.InvariantCulture);
        }
        private StorageOperation Remember(StorageOperation operation)
        {
            EnsureRuntimeContext();
            if (operation.Status == StorageOperationStatus.Confirmed)
            {
                var outer = _journal.LoadEffect(operation.Id);
                if (outer?.Cost != null && outer.Stage != StorageEffectStage.Completed && outer.Stage != StorageEffectStage.Rejected)
                    operation = OperationFromEffect(outer);
            }
            _operations.TryGetValue(operation.Id, out var previous);
            operation = StorageEffects.AcceptStatus(previous, operation);
            _operations[operation.Id] = operation;
            if (ZNet.instance != null && ZNet.instance.IsServer() && operation.IsFinal && _journal.Available &&
                _journal.Load(operation.Id) == null && _journal.LoadEffect(operation.Id) == null)
                _journal.Save(new StorageTransactionRecord(operation.Id, "outcome", operation.Status, Array.Empty<StorageParticipantPlan>(),
                    operation.Message, operation.Requested, operation.Accepted));
            if (ZNet.instance != null && ZNet.instance.IsServer() && _requestPeers.TryGetValue(operation.Id, out var peer)) _rpc.Reply(peer, operation);
            if (operation.IsFinal) { _requestPeers.Remove(operation.Id); _retries.Forget(operation.Id); _rpc.ForgetProgress(operation.Id); }
            var excess = _operations.Count - (_settings().MaxPendingOperations + 256);
            if (excess > 0)
                foreach (var retired in _operations.Values.Where(x => x.IsFinal).Take(excess).Select(x => x.Id).ToList()) _operations.Remove(retired);
            return operation;
        }
        private StorageOperation Reject(string key, int requested, string message) => Remember(new StorageOperation(key ?? NewId("rejected"), StorageOperationStatus.Rejected, requested, 0, requested, message));
        private StorageOperation Confirm(string key, int requested, int accepted, string message) => Remember(new StorageOperation(key ?? NewId("confirmed"), StorageOperationStatus.Confirmed, requested, accepted, requested - accepted, message));

        private void PersistEscrow(Player actor, string id, StorageInventory escrow, StorageEffectDescriptor effect)
        {
            actor.m_customData[EscrowPrefix + id] = StorageRpc.Encode(escrow); actor.m_customData[EscrowPrefix + id + ".effect"] = EncodeEffect(effect);
            if (!effect.TargetId.StartsWith("player:", StringComparison.Ordinal)) return;
            var active = new HashSet<string>((actor.m_customData.TryGetValue(PlayerEffectActiveKey, out var value) ? value : "")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            active.Add(id); actor.m_customData[PlayerEffectActiveKey] = string.Join("\n", active.OrderBy(x => x, StringComparer.Ordinal));
        }
        private void RemoveEscrow(Player actor, string id)
        {
            actor.m_customData.Remove(EscrowPrefix + id); actor.m_customData.Remove(EscrowPrefix + id + ".effect");
            var active = new HashSet<string>((actor.m_customData.TryGetValue(PlayerEffectActiveKey, out var value) ? value : "")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            active.Remove(id);
            if (active.Count == 0) actor.m_customData.Remove(PlayerEffectActiveKey);
            else actor.m_customData[PlayerEffectActiveKey] = string.Join("\n", active.OrderBy(x => x, StringComparer.Ordinal));
        }
        private static Inventory EscrowInventory(StorageInventory storage) { var inventory = new Inventory("SCS escrow", null, storage.Width, storage.Height); GameInventoryAdapter.ApplyLayout(inventory, storage); return inventory; }
        private static string EncodeEffect(StorageEffectDescriptor effect) => string.Join("\n", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(effect.Kind)), Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(effect.TargetId)), effect.ActorId.ToString(), Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(effect.Data)));
        private int LoadEscrowAmount(string id)
        {
            var actor = Player.m_localPlayer; if (actor == null || !actor.m_customData.TryGetValue(EscrowPrefix + id, out var persisted)) return 0;
            try { return StorageRpc.Decode(persisted).Items.Sum(x => x.Amount); } catch { return 0; }
        }

        private StorageEffectStepResult ApplyLocalEffect(string operationId, StorageEffectStage stage,
            StorageEffectDescriptor descriptor, string encodedEscrow)
        {
            if (descriptor == null || !_effects.TryGetValue(descriptor.Kind, out var handler))
                return StorageEffectStepResult.Unknown("Effect handler unavailable");
            if (!OwnsEffectTarget(descriptor)) return StorageEffectStepResult.Unknown("Effect owner unavailable");
            var receiptTarget = EffectReceiptTarget(descriptor);
            if (receiptTarget == null) return StorageEffectStepResult.Unknown("Effect receipt target unavailable");
            var receipt = operationId + ":" + stage;
            var receipts = ReadEffectReceipts(receiptTarget);
            if ((receipts.TryGetValue(receipt, out var persisted) && persisted.Applied) ||
                ReadEffectReceiptWatermarks(receiptTarget).Contains(ReceiptWatermarkId(operationId, stage)))
                return StorageEffectStepResult.Applied();
            Inventory escrow;
            try { escrow = EscrowInventory(StorageRpc.Decode(encodedEscrow)); }
            catch (Exception error) { return StorageEffectStepResult.Rejected("Invalid effect escrow: " + error.Message); }
            try
            {
                if (persisted != null)
                {
                    var recovery = ReconcileEffect(handler, operationId, descriptor, escrow, persisted.BeforeState);
                    if (recovery == StorageEffectRecovery.Applied)
                    {
                        receipts[receipt] = persisted.AsApplied(); WriteEffectReceipts(receiptTarget, receipts);
                        return StorageEffectStepResult.Applied();
                    }
                    if (recovery != StorageEffectRecovery.NotApplied)
                        return StorageEffectStepResult.Unknown("Effect reconciliation uncertain");
                }
                var validation = handler.Validate(descriptor, escrow, out var reason);
                if (validation == StorageEffectResult.Rejected) return StorageEffectStepResult.Rejected(reason);
                if (validation != StorageEffectResult.Applied) return StorageEffectStepResult.Unknown("Not ready: " + reason);
                var beforeState = handler.CaptureState(operationId, descriptor, escrow) ?? string.Empty;
                persisted = new EffectReceipt(false, beforeState);
                receipts[receipt] = persisted;
                WriteEffectReceipts(receiptTarget, receipts);
                StorageEffectResult result;
                _executingEffects++;
                try { result = handler.Apply(operationId, descriptor, escrow); }
                catch (Exception error)
                {
                    var recovery = ReconcileEffect(handler, operationId, descriptor, escrow, beforeState);
                    if (recovery == StorageEffectRecovery.Applied)
                    {
                        receipts[receipt] = persisted.AsApplied(); WriteEffectReceipts(receiptTarget, receipts);
                        return StorageEffectStepResult.Applied();
                    }
                    return StorageEffectStepResult.Unknown(recovery == StorageEffectRecovery.NotApplied
                        ? "Apply failed before mutation: " + error.Message
                        : "Apply outcome uncertain: " + error.Message);
                }
                finally { _executingEffects--; }
                if (result == StorageEffectResult.Rejected)
                {
                    var recovery = ReconcileEffect(handler, operationId, descriptor, escrow, beforeState);
                    if (recovery == StorageEffectRecovery.Applied)
                    {
                        receipts[receipt] = persisted.AsApplied(); WriteEffectReceipts(receiptTarget, receipts);
                        return StorageEffectStepResult.Applied();
                    }
                    if (recovery != StorageEffectRecovery.NotApplied)
                        return StorageEffectStepResult.Unknown("Rejected effect outcome uncertain");
                    return StorageEffectStepResult.Rejected("Effect rejected before mutation");
                }
                if (result != StorageEffectResult.Applied)
                {
                    var recovery = ReconcileEffect(handler, operationId, descriptor, escrow, beforeState);
                    if (recovery != StorageEffectRecovery.Applied)
                        return StorageEffectStepResult.Unknown("Apply uncertain: " + result);
                }
                receipts[receipt] = persisted.AsApplied(); WriteEffectReceipts(receiptTarget, receipts);
                return StorageEffectStepResult.Applied();
            }
            catch (Exception error) { return StorageEffectStepResult.Unknown(error.Message); }
        }

        private static StorageEffectRecovery ReconcileEffect(IStorageEffectHandler handler, string operationId,
            StorageEffectDescriptor descriptor, Inventory escrow, string beforeState)
        {
            _executingEffects++;
            try { return handler.Reconcile(operationId, descriptor, escrow, beforeState); }
            finally { _executingEffects--; }
        }

        private void AcknowledgeEffect(long sender, string operationId, StorageEffectStage stage, StorageEffectStepResult result)
        {
            var record = _journal.LoadEffect(operationId); if (record == null || record.Stage != stage) return;
            var descriptor = DescriptorFor(record, stage); if (descriptor == null || EffectOwner(descriptor) != sender) return;
            if (result.IsRejected)
            {
                if (record.Stage == StorageEffectStage.CostPrepared)
                {
                    RefundCost(record, result.Message);
                    Remember(OperationFromEffect(_journal.LoadEffect(operationId)));
                    return;
                }
                var rejected = record.Stage == StorageEffectStage.CapturePending || record.Stage == StorageEffectStage.CostPreparing
                    ? record.At(StorageEffectStage.Rejected, message: result.Message)
                    : record.At(record.Stage, message: result.Message);
                _journal.SaveEffect(rejected);
                Remember(OperationFromEffect(_journal.LoadEffect(operationId)));
                return;
            }
            if (!result.IsAccepted) return;
            _retries.Progress(operationId);
            _journal.SaveEffect(record.At(stage, acknowledgement: Acknowledgement(stage)));
            var resumed = _effectCoordinator.Resume(operationId);
            if (resumed.Stage == StorageEffectStage.Completed) ReleaseEffectSource(resumed);
            Remember(OperationFromEffect(resumed));
        }

        private bool OwnsEffectTarget(StorageEffectDescriptor descriptor)
        {
            if (descriptor.TargetId.StartsWith("player:", StringComparison.Ordinal))
                return Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == descriptor.ActorId;
            var view = ResolveTarget(descriptor.TargetId);
            return view != null && view.IsValid() && view.IsOwner();
        }

        private object EffectReceiptTarget(StorageEffectDescriptor descriptor)
        {
            if (descriptor.TargetId.StartsWith("player:", StringComparison.Ordinal)) return Player.m_localPlayer;
            return ResolveTarget(descriptor.TargetId)?.GetZDO();
        }

        private sealed class EffectReceipt
        {
            internal readonly bool Applied;
            internal readonly string BeforeState;
            internal EffectReceipt(bool applied, string beforeState) { Applied = applied; BeforeState = beforeState ?? string.Empty; }
            internal EffectReceipt AsApplied() => new EffectReceipt(true, BeforeState);
        }

        private static Dictionary<string, EffectReceipt> ReadEffectReceipts(object target)
        {
            string encoded = "";
            if (target is Player player) player.m_customData.TryGetValue(EffectReceiptsKey, out encoded);
            else if (target is ZDO zdo) encoded = zdo.GetString(EffectReceiptsKey, "");
            var result = new Dictionary<string, EffectReceipt>(StringComparer.Ordinal);
            foreach (var line in (encoded ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(new[] { '|' }, 3);
                if (parts.Length == 1) result[parts[0]] = new EffectReceipt(true, string.Empty);
                else
                {
                    try { result[parts[0]] = new EffectReceipt(parts[1] == "A", parts.Length == 3 ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])) : string.Empty); }
                    catch { }
                }
            }
            return result;
        }

        private static void WriteEffectReceipts(object target, Dictionary<string, EffectReceipt> receipts)
        {
            var watermarks = ReadEffectReceiptWatermarks(target);
            foreach (var retired in receipts.Where(x => x.Value.Applied && TryReceiptWatermarkId(x.Key, out _))
                         .OrderByDescending(x => x.Key, StringComparer.Ordinal).Skip(128).ToList())
            { TryReceiptWatermarkId(retired.Key, out var id); watermarks.Remember(id); receipts.Remove(retired.Key); }
            var encoded = string.Join("\n", receipts.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + "|" +
                (x.Value.Applied ? "A" : "S") + "|" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(x.Value.BeforeState))));
            if (target is Player player) player.m_customData[EffectReceiptsKey] = encoded;
            else if (target is ZDO zdo) zdo.Set(EffectReceiptsKey, encoded);
            WriteEffectReceiptWatermarks(target, watermarks);
        }

        private static StorageReplayFilter ReadEffectReceiptWatermarks(object target)
        {
            string encoded = "";
            if (target is Player player) player.m_customData.TryGetValue(EffectReceiptWatermarksKey, out encoded);
            else if (target is ZDO zdo) encoded = zdo.GetString(EffectReceiptWatermarksKey, "");
            try { return new StorageReplayFilter(encoded ?? ""); } catch { return new StorageReplayFilter(8192); }
        }

        private static void WriteEffectReceiptWatermarks(object target, StorageReplayFilter watermarks)
        {
            if (target is Player player) player.m_customData[EffectReceiptWatermarksKey] = watermarks.Export();
            else if (target is ZDO zdo) zdo.Set(EffectReceiptWatermarksKey, watermarks.Export());
        }

        private static string ReceiptWatermarkId(string operationId, StorageEffectStage stage) => DerivedOperationId(operationId, "effect-" + (int)stage);
        private static bool TryReceiptWatermarkId(string receipt, out string watermark)
        {
            watermark = ""; var separator = (receipt ?? "").LastIndexOf(':'); if (separator <= 0) return false;
            if (!Enum.TryParse(receipt.Substring(separator + 1), out StorageEffectStage stage)) return false;
            watermark = DerivedOperationId(receipt.Substring(0, separator), "effect-" + (int)stage);
            return new StorageReplayFilter(64).CanRemember(watermark);
        }

        private long EffectOwner(StorageEffectDescriptor descriptor)
        {
            if (descriptor.TargetId.StartsWith("player:", StringComparison.Ordinal))
            {
                if (Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == descriptor.ActorId) return ZDOMan.GetSessionID();
                return ZNet.instance?.GetPeers().FirstOrDefault(x => x.m_playerID == descriptor.ActorId && x.IsReady())?.m_uid ?? 0L;
            }
            if (!TryParseZdoId(descriptor.TargetId, out var id)) return 0L;
            return ZDOMan.instance?.GetZDO(id)?.GetOwner() ?? 0L;
        }

        private static ZNetView ResolveTarget(string id)
        {
            var parts = (id ?? "").Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var user) || !uint.TryParse(parts[1], out var objectId)) return null;
            return Resolve(new ZDOID(user, objectId));
        }

        private static StorageEffectDescriptor DescriptorFor(StorageEffectRecord record, StorageEffectStage stage)
        {
            if (stage == StorageEffectStage.CapturePending) return record.Capture;
            if (stage == StorageEffectStage.CostPrepared) return record.Cost;
            if (stage == StorageEffectStage.RemainderPending) return record.Remainder;
            return null;
        }

        private static string Acknowledgement(StorageEffectStage stage)
        {
            if (stage == StorageEffectStage.CapturePending) return "capture";
            if (stage == StorageEffectStage.CostPrepared) return "cost";
            if (stage == StorageEffectStage.DeliveryPending) return "delivery";
            if (stage == StorageEffectStage.RemainderPending) return "remainder";
            return stage.ToString();
        }

        private static StorageOperation OperationFromEffect(StorageEffectRecord record) => StorageEffects.PublicStatus(record);

        private static string OutputOutboxKey(string operationId) => "scs.output.outbox.v1." + operationId;
        private static string CaptureAuthority(string operationId, long actorId) =>
            operationId + "\n" + WorldIdentity() + "\n" + actorId.ToString(CultureInfo.InvariantCulture);

        private static void AddActiveEffect(ZDO zdo, string operationId)
        {
            var active = new HashSet<string>((zdo.GetString(EffectActiveKey, "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            active.Add(operationId); zdo.Set(EffectActiveKey, string.Join("\n", active.OrderBy(x => x, StringComparer.Ordinal)));
        }

        private void ReleaseEffectSource(StorageEffectRecord record)
        {
            if (record?.Capture == null) return;
            var owner = EffectOwner(record.Capture); if (owner == 0L) return;
            if (owner == ZDOMan.GetSessionID()) RemoveLocalEffectMarker(record.Capture.TargetId, record.OperationId);
            else _rpc.ReleaseEffect(owner, record.Capture.TargetId, record.OperationId);
        }

        private static void RemoveLocalEffectMarker(string targetId, string operationId)
        {
            var view = ResolveTarget(targetId); if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var zdo = view.GetZDO();
            var active = new HashSet<string>((zdo.GetString(EffectActiveKey, "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            active.Remove(operationId); zdo.Set(EffectActiveKey, string.Join("\n", active.OrderBy(x => x, StringComparer.Ordinal)));
            zdo.Set(OutputOutboxKey(operationId), "");
            if (zdo.GetString(CaptureAuthorityKey, "").StartsWith(operationId + "\n", StringComparison.Ordinal))
                zdo.Set(CaptureAuthorityKey, "");
            if (zdo.GetString(CaptureIntentKey, "").StartsWith(operationId + "\n", StringComparison.Ordinal))
                zdo.Set(CaptureIntentKey, "");
        }

        private static string WorldIdentity()
        {
            return ZNet.instance != null && ZNet.instance.GetWorld() != null
                ? ZNet.instance.GetWorldUID().ToString(CultureInfo.InvariantCulture) : "world-unavailable";
        }

        private static string ContextIdentity(StorageContext context) => string.Join("|",
            ((int)context.Scope).ToString(CultureInfo.InvariantCulture),
            context.Origin.x.ToString("R", CultureInfo.InvariantCulture), context.Origin.y.ToString("R", CultureInfo.InvariantCulture),
            context.Origin.z.ToString("R", CultureInfo.InvariantCulture), context.Radius.ToString("R", CultureInfo.InvariantCulture),
            context.Anchor != null && context.Anchor.IsValid() ? context.Anchor.GetZDO().m_uid.ToString() : "none");

        private StorageEffectStepResult RefundCost(StorageEffectRecord effect, string reason)
        {
            var original = _journal.Load(effect.OperationId);
            var actor = Player.GetPlayer(effect.ActorId);
            if (original == null || original.Participants.Where(x => x.Payload != "anchor").Any(x => string.IsNullOrEmpty(x.ExpectedPayload)))
                return StorageEffectStepResult.Unknown("Paid cost retained for recovery: " + reason);
            if (actor == null) return RefundRemoteCost(effect, original, reason);
            var participants = new List<IStorageTransactionParticipant>();
            var refundPlans = new List<StorageParticipantPlan>();
            foreach (var plan in original.Participants.Where(x => x.Payload != "anchor"))
            {
                var finalLayout = StorageRpc.Decode(plan.Payload);
                var expectedFinalPayload = plan.Payload;
                IStorageTransactionParticipant participant;
                if (plan.ParticipantId.StartsWith("player:", StringComparison.Ordinal))
                {
                    var expectedRevision = RevisionFor(finalLayout);
                    expectedFinalPayload = StorageRpc.Encode(new StorageInventory(finalLayout.Id, expectedRevision,
                        finalLayout.Width, finalLayout.Height, finalLayout.Items));
                    participant = _rpc.BindPlayer(actor, expectedRevision, expectedFinalPayload);
                }
                else
                {
                    participant = _rpc.ResolveParticipant(plan.ParticipantId, actor, finalLayout.Revision);
                    var view = ResolveTarget(plan.ParticipantId); var container = view != null ? view.GetComponent<Container>() : null;
                    if (participant == null || container == null) return StorageEffectStepResult.Unknown("Refund participant unavailable");
                    var current = GameInventoryAdapter.Snapshot(finalLayout.Id, finalLayout.Revision, container.GetInventory());
                    if (StorageRpc.Encode(current) != plan.Payload) return StorageEffectStepResult.Unknown("Refund participant changed");
                }
                participants.Add(participant);
                refundPlans.Add(new StorageParticipantPlan(plan.ParticipantId, participant.Revision, plan.ExpectedPayload, expectedFinalPayload));
            }
            var refundId = DerivedOperationId(effect.OperationId, "refund");
            var result = new StorageTransactions(_journal, participants).Execute(new StorageTransactionRequest(refundId, "cost-refund", refundPlans,
                effect.Requested, effect.Requested, effect.ActorId));
            if (result.Status != StorageOperationStatus.Confirmed)
                return StorageEffectStepResult.Unknown("Paid cost refund pending: " + result.Message);
            var refunded = effect.At(StorageEffectStage.Rejected, message: "Cost returned: " + reason);
            _journal.SaveEffect(refunded); RemoveEscrow(actor, effect.OperationId);
            return StorageEffectStepResult.Unknown(refunded.Message);
        }

        private StorageEffectStepResult RefundRemoteCost(StorageEffectRecord effect, StorageTransactionRecord original, string reason)
        {
            var peer = ZNet.instance?.GetPeers().FirstOrDefault(x => x.m_playerID == effect.ActorId && x.IsReady());
            if (peer == null) return StorageEffectStepResult.Unknown("Paid cost retained until player reconnects: " + reason);
            var participants = new List<IStorageTransactionParticipant>(); var plans = new List<StorageParticipantPlan>();
            foreach (var plan in original.Participants.Where(x => x.Payload != "anchor"))
            {
                var finalLayout = StorageRpc.Decode(plan.Payload); IStorageTransactionParticipant participant;
                if (plan.ParticipantId.StartsWith("player:", StringComparison.Ordinal))
                {
                    var expectedRevision = RevisionFor(finalLayout);
                    participant = _rpc.BindRemotePlayer(effect.ActorId, peer.m_uid, expectedRevision,
                        StorageRpc.Encode(new StorageInventory(finalLayout.Id, expectedRevision, finalLayout.Width, finalLayout.Height, finalLayout.Items)));
                }
                else
                {
                    if (!TryParseZdoId(plan.ParticipantId, out var zdoId)) return StorageEffectStepResult.Unknown("Refund participant invalid");
                    var zdo = ZDOMan.instance?.GetZDO(zdoId); if (zdo == null) return StorageEffectStepResult.Unknown("Refund participant unavailable");
                    participant = _rpc.BindRemote(zdo, effect.ActorId, zdo.DataRevision.ToString(), plan.Payload,
                        zdo.GetString(StorageFacade.NetworkNameKey, ""));
                }
                participants.Add(participant);
                plans.Add(new StorageParticipantPlan(plan.ParticipantId, participant.Revision, plan.ExpectedPayload, plan.Payload));
            }
            var result = new StorageTransactions(_journal, participants).Execute(new StorageTransactionRequest(DerivedOperationId(effect.OperationId, "refund"),
                "cost-refund", plans, effect.Requested, effect.Requested, effect.ActorId));
            if (result.Status != StorageOperationStatus.Confirmed) return StorageEffectStepResult.Unknown("Paid cost refund pending: " + result.Message);
            var refunded = effect.At(StorageEffectStage.Rejected, message: "Cost returned: " + reason); _journal.SaveEffect(refunded);
            return StorageEffectStepResult.Unknown(refunded.Message);
        }

        private static string RevisionFor(StorageInventory inventory) => string.Join(";", inventory.Items.OrderBy(x => x.Slot)
            .Select(x => x.Identity + ":" + x.Amount + "@" + (x.Slot % inventory.Width) + "," + (x.Slot / inventory.Width)));

        private sealed class RuntimeEffectPort : IStorageEffectPort
        {
            private readonly StorageService _service;
            internal RuntimeEffectPort(StorageService service) { _service = service; }

            public StorageEffectStepResult Receipt(StorageEffectRecord record, StorageEffectStage stage)
            {
                if (stage == StorageEffectStage.Captured)
                {
                    var preparedTransaction = _service._journal.Load(record.DeliveryTransactionId);
                    return preparedTransaction == null ? StorageEffectStepResult.Unknown("Delivery not prepared")
                        : StorageEffectStepResult.Applied(record.Accepted, record.Remaining);
                }
                if (stage == StorageEffectStage.CostPreparing)
                {
                    var costTransaction = _service._journal.Load(record.DeliveryTransactionId);
                    if (costTransaction == null) return StorageEffectStepResult.Unknown("Cost transaction unavailable");
                    if (costTransaction.Status == StorageOperationStatus.Confirmed) return StorageEffectStepResult.Applied();
                    if (costTransaction.Status == StorageOperationStatus.Rejected || costTransaction.Status == StorageOperationStatus.Aborted)
                        return StorageEffectStepResult.Rejected(costTransaction.Message);
                    return StorageEffectStepResult.Unknown(costTransaction.Message);
                }
                if (record.Acknowledgements.Contains(Acknowledgement(stage)))
                    return stage == StorageEffectStage.DeliveryPending
                        ? StorageEffectStepResult.Applied(record.Accepted, record.Remaining)
                        : StorageEffectStepResult.Applied();
                if (stage != StorageEffectStage.DeliveryPending) return StorageEffectStepResult.Unknown("Effect receipt pending");
                var transaction = _service._journal.Load(record.DeliveryTransactionId);
                if (transaction == null) return StorageEffectStepResult.Unknown("Delivery transaction unavailable");
                if (transaction.Status == StorageOperationStatus.Confirmed) return StorageEffectStepResult.Applied(record.Accepted, record.Remaining);
                if (transaction.Status == StorageOperationStatus.Rejected || transaction.Status == StorageOperationStatus.Aborted)
                    return StorageEffectStepResult.Rejected(transaction.Message);
                return StorageEffectStepResult.Unknown(transaction.Message);
            }

            public StorageEffectStepResult Apply(StorageEffectRecord record, StorageEffectStage stage)
            {
                if (stage == StorageEffectStage.Captured) return StorageEffectStepResult.Unknown("Awaiting current owner discovery for output delivery");
                if (stage == StorageEffectStage.CostPreparing)
                {
                    _service.ResumeTransaction(record.DeliveryTransactionId);
                    return Receipt(record, stage);
                }
                if (stage == StorageEffectStage.DeliveryPending)
                {
                    _service.ResumeTransaction(record.DeliveryTransactionId);
                    return Receipt(record, stage);
                }
                var descriptor = DescriptorFor(record, stage);
                if (descriptor == null) return StorageEffectStepResult.Rejected("Effect descriptor unavailable");
                var escrow = record.Escrow;
                if (stage == StorageEffectStage.RemainderPending)
                {
                    try
                    {
                        var original = StorageRpc.Decode(record.Escrow); var sample = original.Items.Single();
                        escrow = StorageRpc.Encode(new StorageInventory("remainder:" + record.OperationId, "1", 1, 1,
                            new[] { sample.At(0, record.Remaining) }));
                    }
                    catch (Exception error) { return StorageEffectStepResult.Rejected("Invalid remainder escrow: " + error.Message); }
                }
                var owner = _service.EffectOwner(descriptor);
                if (owner == 0L) return StorageEffectStepResult.Unknown("Effect owner unavailable");
                if (owner == ZDOMan.GetSessionID())
                {
                    var local = _service.ApplyLocalEffect(record.OperationId, stage, descriptor, escrow);
                    return stage == StorageEffectStage.CostPrepared && local.IsRejected ? _service.RefundCost(record, local.Message) : local;
                }
                _service._rpc.ApplyEffect(owner, record.OperationId, stage, descriptor, escrow);
                return StorageEffectStepResult.Unknown("Awaiting effect owner receipt");
            }
        }

        private StorageOperation QueueDeposit(StorageContext context, ItemDrop.ItemData item, int amount, string key)
        {
            if (item == null || context?.Actor == null) return Reject(key, amount, "Invalid deposit");
            return QueueSimple("deposit", context, key, package => { package.Write(item.m_gridPos.x); package.Write(item.m_gridPos.y); package.Write(GameInventoryAdapter.Identity(item)); package.Write(amount); });
        }

        private StorageOperation QueueSimple(string kind, StorageContext context, string key, Action<ZPackage> write, bool submit = true)
        {
            EnsureRuntimeContext();
            var id = string.IsNullOrEmpty(key) ? NewId(kind) : key;
            if (_operations.TryGetValue(id, out var existing) && existing.IsFinal) return existing;
            if (context?.Actor == null) return Reject(id, 0, "Actor unavailable");
            if (!_pendingRequests.ContainsKey(id) && _pendingRequests.Count >= _settings().MaxPendingOperations)
                return Reject(id, 0, "Storage request limit reached");
            _discovery.Find(context, _settings());
            if (_discovery.LastUnavailableReason.Length != 0) return Reject(id, 0, _discovery.LastUnavailableReason);
            var body = new ZPackage(); write(body);
            var intent = new StorageRequestIntent(kind, id, WorldIdentity(), context.Scope,
                context.Origin.x, context.Origin.y, context.Origin.z, context.Radius,
                context.Anchor != null && context.Anchor.IsValid() ? context.Anchor.GetZDO().m_uid.ToString() : "none", body.GetArray());
            _pendingRequests[id] = intent.Encode(); PersistPendingRequests();
            Remember(new StorageOperation(id, StorageOperationStatus.Requested, message: "Requested"));
            if (submit) Resume(id);
            return GetOperation(id);
        }

        private void SubmitPending(string id)
        {
            if (!_pendingRequests.TryGetValue(id, out var encoded) || Player.m_localPlayer == null) return;
            StorageRequestIntent intent = null;
            try
            {
                intent = StorageRequestIntent.Decode(encoded);
                if (intent.Id != id || intent.WorldId != WorldIdentity())
                    throw new InvalidOperationException("Return to the operation's original world to resume");
                if (intent.Kind != "name" && _admittedRequests.Contains(id))
                {
                    RequestStatus(id);
                    return;
                }
                var anchor = ResolvePendingAnchor(intent);
                if (intent.AnchorId != "none" && (anchor == null || !anchor.IsValid()))
                    throw new InvalidOperationException("Operation anchor is not loaded");
                var context = new StorageContext(Player.m_localPlayer, new UnityEngine.Vector3(intent.X, intent.Y, intent.Z),
                    intent.Radius, intent.Scope, anchor);
                var request = new ZPackage(intent.Rebuild(() => CurrentRequestHeader(intent, context)));
                if (intent.Kind == "output")
                {
                    request.SetPos(request.Size()); request.Write(ProfileProofMarker); request.Write(intent.WorldId);
                    request.Write(Player.m_localPlayer.GetPlayerID());
                }
                _rpc.Submit(request);
            }
            catch (Exception error)
            {
                // Intent and any source outbox remain durable. This retry must
                // never clear custody merely because fresh discovery is unavailable.
                var old = GetOperation(id);
                Remember(new StorageOperation(id, StorageOperationStatus.RecoveryPending,
                    old.Requested, old.Accepted, old.Remaining, error.Message, old.Captured));
                if (intent != null && intent.WorldId == WorldIdentity())
                    RequestStatus(id);
            }
        }

        private void RequestStatus(string id)
        {
            var status = new ZPackage(); status.Write("status"); status.Write(id);
            _rpc.Submit(status);
        }

        private static ZNetView FindOperationSource(string operationId)
        {
            var matches = StorageJournal.WorldObjects().Where(x => HasOperationSourceMarker(x, operationId))
                .Select(x => ZNetScene.instance != null ? ZNetScene.instance.FindInstance(x) : null)
                .Where(x => x != null && x.IsValid()).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        internal static ZNetView ResolvePendingAnchor(StorageRequestIntent intent) => intent == null ? null :
            intent.Kind == "output" ? FindOperationSource(intent.Id) :
            intent.AnchorId == "none" ? null : ResolveTarget(intent.AnchorId);

        private static bool HasOperationSourceMarker(ZDO zdo, string operationId)
        {
            if (zdo == null || string.IsNullOrEmpty(operationId)) return false;
            var active = (zdo.GetString(EffectActiveKey, "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return active.Contains(operationId, StringComparer.Ordinal) &&
                zdo.GetString(CaptureIntentKey, "").StartsWith(operationId + "\n", StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(zdo.GetString(OutputOutboxKey(operationId), ""));
        }

        private bool NeedsFreshOutputIntent(StorageEffectRecord effect) => effect == null ||
            effect.Stage == StorageEffectStage.Captured || effect.Stage == StorageEffectStage.CapturePending ||
            (effect.Stage == StorageEffectStage.DeliveryPending &&
                _journal.Load(effect.DeliveryTransactionId)?.Status == StorageOperationStatus.Rejected);

        private byte[] CurrentRequestHeader(StorageRequestIntent intent, StorageContext context)
        {
            if (intent.Kind == "name")
            {
                var nameHeader = new ZPackage(); nameHeader.Write(intent.Kind); nameHeader.Write(intent.Id);
                return nameHeader.GetArray();
            }
            var members = _discovery.Find(context, _settings());
            if (_discovery.LastUnavailableReason.Length != 0) throw new InvalidOperationException(_discovery.LastUnavailableReason);
            var terminals = _discovery.FindTerminalProofs(context, _settings(), members);
            var package = new ZPackage(); package.Write(intent.Kind); package.Write(intent.Id); package.Write((int)context.Scope);
            package.Write(context.Origin); package.Write(context.Radius);
            package.Write(context.Anchor != null ? context.Anchor.GetZDO().m_uid : ZDOID.None);
            package.Write(StorageRpc.Encode(PlayerSnapshot(context.Actor)));
            var equippedSlots = context.Actor.GetInventory().GetAllItems().Where(x => x.m_equipped)
                .Select(x => x.m_gridPos.y * context.Actor.GetInventory().GetWidth() + x.m_gridPos.x).ToList();
            package.Write(equippedSlots.Count); foreach (var slot in equippedSlots) package.Write(slot);
            package.Write(members.Count);
            foreach (var member in members)
            {
                package.Write(member.m_nview.GetZDO().m_uid); package.Write(StorageRpc.Encode(Snapshot(member)));
                package.Write(member.m_nview.GetZDO().GetString(StorageFacade.NetworkNameKey, ""));
            }
            package.Write(terminals.Count); foreach (var terminal in terminals) package.Write(terminal.GetZDO().m_uid);
            return package.GetArray();
        }

        private void PersistPendingRequests()
        {
            if (Player.m_localPlayer == null) return;
            using (var stream = new System.IO.MemoryStream()) using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(_pendingRequests.Count);
                foreach (var request in _pendingRequests.OrderBy(x => x.Key, StringComparer.Ordinal))
                { writer.Write(request.Key); writer.Write(request.Value.Length); writer.Write(request.Value); }
                Player.m_localPlayer.m_customData[PendingRequestsKey] = Convert.ToBase64String(stream.ToArray());
            }
        }

        private void RestorePendingRequests()
        {
            if (Player.m_localPlayer == null || _pendingRequests.Count != 0 ||
                !Player.m_localPlayer.m_customData.TryGetValue(PendingRequestsKey, out var encoded) || string.IsNullOrEmpty(encoded)) return;
            try
            {
                using (var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(Convert.FromBase64String(encoded))))
                {
                    var count = reader.ReadInt32(); if (count < 0 || count > 1024) throw new System.IO.InvalidDataException("Invalid pending request count");
                    for (var i = 0; i < count; i++)
                    {
                        var id = reader.ReadString(); var length = reader.ReadInt32();
                        if (length < 0 || length > 8 * 1024 * 1024) throw new System.IO.InvalidDataException("Invalid pending request size");
                        var request = reader.ReadBytes(length); var intent = StorageRequestIntent.Decode(request);
                        if (intent.Id != id) throw new System.IO.InvalidDataException("Pending intent identity changed");
                        _pendingRequests[id] = request;
                        int amount = intent.Kind == "output" ? LoadEscrowAmount(id) : 0;
                        _operations[id] = new StorageOperation(id, StorageOperationStatus.Requested, amount, 0, amount,
                            "Restored pending request", intent.Kind == "output");
                    }
                }
            }
            catch (Exception error) { ZLog.LogWarning("[SmartCraft-Storage] Invalid pending requests: " + error.Message); }
        }

        private void HandleServerRequest(long sender, ZPackage package)
        {
            EnsureRuntimeContext();
            string kind = null, id = null;
            AuthenticatedPeer actor = null;
            try
            {
                kind = package.ReadString(); id = package.ReadString();
                actor = AuthenticatedActor(sender); if (actor == null) { _rpc.Reply(sender, new StorageOperation(id, StorageOperationStatus.Rejected, message: "Unauthenticated actor")); return; }
                _requestPeers[id] = sender;
                var admitted = Existing(id);
                if (kind == "status")
                {
                    var effect = _journal.LoadEffect(id); var transaction = _journal.Load(id);
                    var recordedActor = effect?.ActorId ?? transaction?.ActorId ?? 0L;
                    if (recordedActor != 0 && recordedActor != actor.ActorId)
                    { _rpc.Reply(sender, new StorageOperation(id, StorageOperationStatus.Rejected, message: "Operation belongs to another player")); _requestPeers.Remove(id); return; }
                    if (admitted == null || (!admitted.IsFinal && effect == null && transaction == null) ||
                        (effect?.Capture != null && NeedsFreshOutputIntent(effect)))
                        _rpc.Reply(sender, new StorageOperation(id, StorageOperationStatus.Requested, message: "Fresh operation intent required"));
                    else if (admitted.IsFinal) Remember(admitted);
                    else { Resume(id); Remember(GetOperation(id)); }
                    return;
                }
                if (admitted != null && admitted.IsFinal) { Remember(admitted); return; }
                var recordedEffect = _journal.LoadEffect(id);
                if (admitted != null && kind != "name" &&
                    (kind != "output" || !NeedsFreshOutputIntent(recordedEffect)))
                { Resume(id); Remember(GetOperation(id)); return; }
                if (kind == "name")
                {
                    var target = ZDOMan.instance.GetZDO(package.ReadZDOID()); var name = package.ReadString();
                    if (target == null || (target.GetPosition() - actor.Position).sqrMagnitude > 100f || !string.IsNullOrEmpty(target.GetString("scs.storage.active.v1", "")))
                    { Remember(new StorageOperation(id, StorageOperationStatus.Rejected, message: "Invalid name target")); return; }
                    _nameOwners[id] = target.GetOwner();
                    StorageNameFlow.Dispatch(id, Remember, () => _rpc.ApplyName(target.GetOwner(), target.m_uid, id, actor.ActorId, name));
                    return;
                }
                var scope = (StorageScope)package.ReadInt(); var origin = package.ReadVector3(); var radius = package.ReadSingle(); var anchor = ZDOMan.instance.GetZDO(package.ReadZDOID());
                var playerSnapshot = StorageRpc.Decode(package.ReadString());
                if (playerSnapshot.Id != "player:" + actor.ActorId) { Remember(admitted ?? (kind == "output"
                    ? new StorageOperation(id, StorageOperationStatus.RecoveryPending, message: "Captured output awaits valid player state", captured: true)
                    : new StorageOperation(id, StorageOperationStatus.Rejected, message: "Invalid player snapshot"))); return; }
                var equippedCount = package.ReadInt(); if (equippedCount < 0 || equippedCount > playerSnapshot.Capacity) throw new InvalidOperationException("Invalid equipped slots");
                var equippedSlots = new HashSet<int>(); for (var i = 0; i < equippedCount; i++) equippedSlots.Add(package.ReadInt());
                var candidateCount = package.ReadInt(); if (candidateCount < 0 || candidateCount > _settings().MaxMembers) throw new InvalidOperationException("Invalid member count");
                var candidates = new List<RemoteCandidate>();
                for (var i = 0; i < candidateCount; i++)
                {
                    var zdo = ZDOMan.instance.GetZDO(package.ReadZDOID()); var snapshot = StorageRpc.Decode(package.ReadString()); var network = package.ReadString();
                    candidates.Add(new RemoteCandidate(zdo, snapshot, network));
                }
                var proofCount = package.ReadInt(); if (proofCount < 0 || proofCount > _settings().MaxMembers) throw new InvalidOperationException("Invalid terminal proof count");
                var proofs = new List<ZDO>(); for (var i = 0; i < proofCount; i++) proofs.Add(ZDOMan.instance.GetZDO(package.ReadZDOID()));
                var context = new AuthorityContext(actor, origin, radius, scope, anchor, candidates, proofs);
                if (!ValidateServerContext(context))
                {
                    Remember(admitted ?? new StorageOperation(id, kind == "output" ? StorageOperationStatus.RecoveryPending : StorageOperationStatus.Rejected,
                        message: "Invalid or unavailable operation context", captured: kind == "output")); return;
                }
                if (!_journal.Available)
                {
                    Remember(admitted ?? new StorageOperation(id, kind == "output" ? StorageOperationStatus.RecoveryPending : StorageOperationStatus.Rejected,
                        message: "Durable storage journal unavailable", captured: kind == "output")); return;
                }
                if (EnsureStableIdentities(context.Candidates.Select(x => x.Zdo)
                    .Concat(context.TerminalProofs).Concat(context.Anchor != null ? new[] { context.Anchor } : Array.Empty<ZDO>())))
                {
                    Remember(new StorageOperation(id, StorageOperationStatus.Requested,
                        message: "Durable participant identities initialized; retry with current revisions", captured: kind == "output"));
                    return;
                }
                if (admitted == null && kind != "output" && _journal.Pending.Count + _journal.PendingEffects.Count >= _settings().MaxPendingOperations)
                { Remember(new StorageOperation(id, StorageOperationStatus.Rejected, message: "Storage operation limit reached")); return; }
                if (_journal.PendingEffects.Any(x => x.TargetId == "player:" + actor.ActorId && x.OperationId != id))
                { Remember(admitted ?? new StorageOperation(id, kind == "output" ? StorageOperationStatus.RecoveryPending : StorageOperationStatus.Rejected,
                    message: "Player has an unsettled native effect", captured: kind == "output")); return; }
                if (kind == "deposit")
                {
                    var x = package.ReadInt(); var y = package.ReadInt(); var identity = package.ReadString(); var amount = package.ReadInt();
                    var stack = playerSnapshot.Items.FirstOrDefault(candidate => candidate.Slot == y * playerSnapshot.Width + x && candidate.Identity == identity);
                    if (stack == null || amount <= 0 || stack.Amount < amount) { Remember(new StorageOperation(id, StorageOperationStatus.Rejected, amount, message: "Player item changed")); return; }
                    var plan = StoragePlanner.Deposit(candidates.Select(x2 => x2.Snapshot), stack, amount);
                    var playerAfter = RemoveAtSlot(playerSnapshot, stack.Slot, plan.Accepted);
                    Remember(ExecuteRemote(id, "deposit", context, playerSnapshot, plan.Inventories.Concat(new[] { playerAfter }), amount, plan.Accepted, true));
                }
                else if (kind == "withdraw")
                {
                    var identity = package.ReadString(); var amount = package.ReadInt();
                    // Persisted 0.7.0 requests end after amount and retain automatic placement.
                    var destinationSlot = package.GetPos() == package.Size() ? -1 : package.ReadInt();
                    var expectedSlot = package.GetPos() == package.Size() ? "" : package.ReadString();
                    if (destinationSlot >= 0 && !StorageSlotExpectation.Matches(playerSnapshot, destinationSlot, expectedSlot))
                    { Remember(new StorageOperation(id, StorageOperationStatus.Rejected, amount, message: "Destination slot changed")); return; }
                    var plan = StoragePlanner.Withdraw(candidates.Select(x => x.Snapshot), playerSnapshot, identity, amount, destinationSlot);
                    Remember(ExecuteRemote(id, "withdraw", context, playerSnapshot, plan.Inventories, amount, plan.Accepted, true));
                }
                else if (kind == "organize")
                {
                    var plan = StoragePlanner.Organize(candidates.Select(x => x.Snapshot));
                    Remember(ExecuteRemote(id, "organize", context, playerSnapshot, plan.Inventories, plan.Moves, plan.Moves));
                }
                else if (kind == "cost")
                {
                    var includePlayer = package.ReadBool(); var effect = ReadEffect(package); var count = package.ReadInt(); var requirements = new List<StorageRequirement>();
                    for (var i = 0; i < count; i++) { var sharedName = package.ReadString(); var amount = package.ReadInt(); var quality = package.ReadInt(); var identity = package.ReadString(); var worldLevel = package.ReadInt(); requirements.Add(new StorageRequirement(sharedName, amount, quality, identity.Length == 0 ? null : identity, worldLevel)); }
                    Remember(PrepareRemoteCost(id, context, playerSnapshot, equippedSlots, requirements, includePlayer, effect));
                }
                else if (kind == "output")
                {
                    var output = StorageRpc.Decode(package.ReadString()).Items.Single(); var amount = package.ReadInt(); var capture = ReadEffect(package); var remainder = ReadEffect(package); var requireAll = package.ReadBool();
                    if (package.GetPos() < package.Size() && package.ReadString() == ProfileProofMarker)
                    { package.ReadString(); package.ReadLong(); }
                    Remember(PrepareRemoteOutput(id, context, playerSnapshot, output, amount, capture, remainder, requireAll));
                }
                else Remember(new StorageOperation(id, StorageOperationStatus.Rejected, message: "Unsupported request"));
            }
            catch (Exception error)
            {
                ZLog.LogWarning("[SmartCraft-Storage] Rejected malformed request: " + error.Message);
                if (actor != null && !string.IsNullOrEmpty(id))
                {
                    var effect = _journal.LoadEffect(id);
                    var transaction = StorageTransactions.GetStatus(_journal, id);
                    var recorded = effect != null ? OperationFromEffect(effect) : transaction.Status == StorageOperationStatus.Unavailable ? null : transaction;
                    Remember(recorded ?? new StorageOperation(id, kind == "output" ? StorageOperationStatus.RecoveryPending : StorageOperationStatus.Rejected,
                        message: "Invalid storage request: " + error.Message, captured: kind == "output"));
                }
                else if (!string.IsNullOrEmpty(id)) _rpc.Reply(sender, new StorageOperation(id, StorageOperationStatus.Rejected, message: "Malformed storage request"));
            }
        }

        private sealed class AuthenticatedPeer
        {
            internal readonly long ActorId, PeerId; internal readonly UnityEngine.Vector3 Position;
            internal AuthenticatedPeer(long actorId, long peerId, UnityEngine.Vector3 position) { ActorId = actorId; PeerId = peerId; Position = position; }
        }
        private static AuthenticatedPeer AuthenticatedActor(long sender)
        {
            if (sender == ZDOMan.GetSessionID() && Player.m_localPlayer != null)
                return new AuthenticatedPeer(Player.m_localPlayer.GetPlayerID(), sender, Player.m_localPlayer.transform.position);
            var peer = ZNet.instance.GetPeers().FirstOrDefault(x => x.m_uid == sender && x.IsReady() && x.m_playerID != 0L);
            return peer == null ? null : new AuthenticatedPeer(peer.m_playerID, peer.m_uid, peer.m_refPos);
        }
        private static ZNetView Resolve(ZDOID id) { if (id.IsNone()) return null; var zdo = ZDOMan.instance.GetZDO(id); return zdo != null ? ZNetScene.instance.FindInstance(zdo) : null; }
        private sealed class RemoteCandidate
        {
            internal readonly ZDO Zdo; internal readonly StorageInventory Snapshot; internal readonly string Network;
            internal RemoteCandidate(ZDO zdo, StorageInventory snapshot, string network) { Zdo = zdo; Snapshot = snapshot; Network = StorageDiscovery.Normalize(network); }
        }

        private sealed class AuthorityContext
        {
            internal readonly AuthenticatedPeer Actor; internal readonly UnityEngine.Vector3 Origin; internal readonly float Radius;
            internal readonly StorageScope Scope; internal readonly ZDO Anchor; internal readonly IReadOnlyList<RemoteCandidate> Candidates;
            internal readonly IReadOnlyList<ZDO> TerminalProofs;
            internal AuthorityContext(AuthenticatedPeer actor, UnityEngine.Vector3 origin, float radius, StorageScope scope, ZDO anchor,
                IReadOnlyList<RemoteCandidate> candidates, IReadOnlyList<ZDO> proofs)
            { Actor = actor; Origin = origin; Radius = radius; Scope = scope; Anchor = anchor; Candidates = candidates; TerminalProofs = proofs; }
        }

        private static string AuthorityContextIdentity(AuthorityContext context) => string.Join("|",
            ((int)context.Scope).ToString(CultureInfo.InvariantCulture),
            context.Origin.x.ToString("R", CultureInfo.InvariantCulture), context.Origin.y.ToString("R", CultureInfo.InvariantCulture),
            context.Origin.z.ToString("R", CultureInfo.InvariantCulture), context.Radius.ToString("R", CultureInfo.InvariantCulture),
            context.Anchor?.m_uid.ToString() ?? "none");

        private bool ValidateServerContext(AuthorityContext context)
        {
            var settings = _settings();
            if (context == null) return false;
            Func<ZDO, StorageAuthorityNode> node = zdo => zdo == null ? null : new StorageAuthorityNode(zdo.m_uid.ToString(),
                zdo.GetPosition().x, zdo.GetPosition().y, zdo.GetPosition().z, zdo.GetBool(StorageFacade.TerminalMarkerKey, false),
                StorageDiscovery.Normalize(zdo.GetString(StorageFacade.NetworkNameKey, "")));
            if (!StorageAuthorityPolicy.Validate(context.Scope, settings.Enabled, settings.TerminalRadius, settings.MaxMembers,
                context.Actor.Position.x, context.Actor.Position.y, context.Actor.Position.z, context.Origin.x, context.Origin.y, context.Origin.z,
                context.Radius, node(context.Anchor), context.Candidates.Select(x => node(x.Zdo)).ToList(), context.TerminalProofs.Select(node).ToList())) return false;
            foreach (var candidate in context.Candidates)
            {
                if (candidate.Zdo == null || candidate.Snapshot == null || candidate.Snapshot.Id != candidate.Zdo.m_uid.ToString() ||
                    candidate.Snapshot.Revision != candidate.Zdo.DataRevision.ToString() ||
                    candidate.Network != StorageDiscovery.Normalize(candidate.Zdo.GetString(StorageFacade.NetworkNameKey, ""))) return false;
            }
            return true;
        }

        private StorageOperation ExecuteRemote(string id, string kind, AuthorityContext context, StorageInventory playerBefore,
            IEnumerable<StorageInventory> layouts, int requested, int accepted, bool reservePlayer = false)
        {
            var materialized = layouts.ToList();
            if (reservePlayer && !materialized.Any(x => x.Id == playerBefore.Id)) materialized.Add(playerBefore);
            var participants = new List<IStorageTransactionParticipant>(); var plans = new List<StorageParticipantPlan>();
            foreach (var layout in materialized)
            {
                IStorageTransactionParticipant participant; StorageInventory before;
                if (layout.Id == playerBefore.Id)
                {
                    before = playerBefore; participant = _rpc.BindRemotePlayer(context.Actor.ActorId, context.Actor.PeerId, before.Revision, StorageRpc.Encode(before));
                }
                else
                {
                    var candidate = context.Candidates.FirstOrDefault(x => x.Snapshot.Id == layout.Id);
                    if (candidate == null) return new StorageOperation(id, StorageOperationStatus.Rejected, requested, 0, requested, "Unknown inventory participant");
                    before = candidate.Snapshot;
                    participant = _rpc.BindRemote(candidate.Zdo, context.Actor.ActorId, before.Revision, StorageRpc.Encode(before), candidate.Network);
                }
                if (StorageRpc.Encode(before) == StorageRpc.Encode(layout) && !(reservePlayer && layout.Id == playerBefore.Id)) continue;
                participants.Add(participant); plans.Add(new StorageParticipantPlan(layout.Id, participant.Revision, StorageRpc.Encode(layout), StorageRpc.Encode(before)));
            }
            var anchors = context.TerminalProofs.Concat(context.Anchor != null ? new[] { context.Anchor } : Array.Empty<ZDO>())
                .Where(x => x != null).GroupBy(x => x.m_uid).Select(x => x.First()).ToList();
            foreach (var anchorZdo in anchors)
            {
                if (plans.Any(x => x.ParticipantId == anchorZdo.m_uid.ToString())) continue;
                var anchor = _rpc.BindRemote(anchorZdo, context.Actor.ActorId, anchorZdo.DataRevision.ToString(), "", "", true);
                participants.Add(anchor); plans.Add(new StorageParticipantPlan(anchor.Id, anchor.Revision, "anchor"));
            }
            return new StorageTransactions(_journal, participants).Execute(new StorageTransactionRequest(id, kind, plans, requested, accepted, context.Actor.ActorId));
        }

        private StorageOperation PrepareRemoteCost(string id, AuthorityContext context, StorageInventory player,
            HashSet<int> equippedSlots, IReadOnlyList<StorageRequirement> requirements, bool includePlayer, StorageEffectDescriptor effect)
        {
            var actorTarget = "player:" + context.Actor.ActorId.ToString(CultureInfo.InvariantCulture);
            var anchorTarget = context.Anchor?.m_uid.ToString() ?? "";
            if (effect == null || effect.ActorId != context.Actor.ActorId || (effect.TargetId != actorTarget && effect.TargetId != anchorTarget))
                return new StorageOperation(id, StorageOperationStatus.Rejected, message: "Invalid cost effect");
            var sources = context.Candidates.Select(x => x.Snapshot).ToList(); if (includePlayer) sources.Add(player);
            var final = sources.ToDictionary(x => x.Id, StringComparer.Ordinal); var escrowItems = new List<StorageStack>(); var requested = 0;
            foreach (var requirement in requirements ?? Array.Empty<StorageRequirement>())
            {
                requested += requirement.Amount; var remaining = requirement.Amount;
                foreach (var inventory in final.Values.ToList())
                {
                    var items = inventory.Items.ToList();
                    for (var i = 0; i < items.Count && remaining > 0; i++)
                    {
                        var item = items[i]; if (!Matches(item, requirement) || (inventory.Id == player.Id && equippedSlots.Contains(item.Slot))) continue;
                        var take = Math.Min(remaining, item.Amount); remaining -= take; escrowItems.Add(item.At(escrowItems.Count, take)); items[i] = item.At(item.Slot, item.Amount - take);
                    }
                    final[inventory.Id] = new StorageInventory(inventory.Id, inventory.Revision, inventory.Width, inventory.Height, items.Where(x => x.Amount > 0));
                }
                if (remaining != 0) return new StorageOperation(id, StorageOperationStatus.Rejected, requested, 0, requested, "Complete cost unavailable");
            }
            var escrow = new StorageInventory("escrow:" + id, "1", Math.Max(1, escrowItems.Count), 1, escrowItems);
            var record = new StorageEffectRecord(1, WorldIdentity(), id, context.Actor.ActorId, effect.TargetId, "remote", null, effect, null,
                StorageRpc.Encode(escrow), id, requested, requested, 0, StorageEffectStage.CostPreparing);
            _journal.SaveEffect(record);
            var payment = ExecuteRemote(id, "cost", context, player, final.Values, requested, requested,
                effect.TargetId.StartsWith("player:", StringComparison.Ordinal));
            if (payment.Status == StorageOperationStatus.Confirmed) return Remember(OperationFromEffect(_effectCoordinator.Resume(id)));
            return payment;
        }

        private StorageOperation PrepareRemoteOutput(string id, AuthorityContext context, StorageInventory player, StorageStack output,
            int amount, StorageEffectDescriptor capture, StorageEffectDescriptor remainder, bool requireAll)
        {
            if (capture == null || capture.ActorId != context.Actor.ActorId || context.Anchor == null ||
                (remainder != null && remainder.ActorId != context.Actor.ActorId) || amount <= 0 || output.Amount != amount ||
                !ValidateCapturedOutput(context.Anchor, id, output, amount))
                return new StorageOperation(id, StorageOperationStatus.RecoveryPending, amount, 0, amount, "Awaiting valid source capture state", true);
            var outbox = new StorageInventory("outbox:" + id, "1", 1, 1, new[] { output.At(0, amount) });
            var existing = _journal.LoadEffect(id);
            var recoveredLegacy = false;
            if (existing == null && capture.TargetId != context.Anchor.m_uid.ToString())
            {
                existing = RecoverLegacyCapturedOutput(id, context, output, amount, capture, remainder, requireAll);
                if (existing == null)
                    return new StorageOperation(id, StorageOperationStatus.RecoveryPending, amount, 0, amount,
                        "Legacy captured output lacks exact durable recovery evidence", true);
                recoveredLegacy = true;
            }
            if (existing != null && !LegacyCleanupComplete(existing))
                return OperationFromEffect(existing.At(StorageEffectStage.Captured, message: "Releasing proven legacy reservations"));
            if (recoveredLegacy) return OperationFromEffect(existing);
            if (capture.TargetId != context.Anchor.m_uid.ToString())
            {
                capture = new StorageEffectDescriptor(capture.Kind, context.Anchor.m_uid.ToString(), capture.ActorId, capture.Data);
                if (remainder != null) remainder = new StorageEffectDescriptor(remainder.Kind, capture.TargetId, remainder.ActorId, remainder.Data);
            }
            var captured = existing ?? new StorageEffectRecord(1, WorldIdentity(), id, context.Actor.ActorId, capture.TargetId, AuthorityContextIdentity(context), capture, null, remainder,
                StorageRpc.Encode(outbox), NextDeliveryId(id), amount, 0, amount, StorageEffectStage.CapturePending, requireAll: requireAll);
            if (existing == null) _journal.SaveEffect(captured);
            if (captured.Stage == StorageEffectStage.CapturePending)
            {
                captured = _effectCoordinator.Resume(id);
                if (captured.Stage != StorageEffectStage.Captured) return OperationFromEffect(captured);
            }
            if (captured.Stage == StorageEffectStage.DeliveryPending)
            {
                var deliveryState = _journal.Load(captured.DeliveryTransactionId);
                if (deliveryState != null && deliveryState.Status == StorageOperationStatus.Rejected)
                {
                    captured = ResetCapturedDelivery(captured, NextDeliveryId(id)); _journal.SaveEffect(captured);
                }
            }
            if (captured.Stage != StorageEffectStage.Captured) return OperationFromEffect(_effectCoordinator.Resume(id));
            var plan = StoragePlanner.Deposit(context.Candidates.Select(x => x.Snapshot), output, amount);
            if (requireAll && plan.Accepted != amount) return OperationFromEffect(captured.At(StorageEffectStage.Captured, message: "Full output capacity unavailable"));
            ExecuteRemote(captured.DeliveryTransactionId, "output", context, player, plan.Inventories, amount, plan.Accepted);
            var record = captured.At(StorageEffectStage.DeliveryPending, plan.Accepted, amount - plan.Accepted, "delivery-prepared");
            _journal.SaveEffect(record);
            return Remember(OperationFromEffect(_effectCoordinator.Resume(id)));
        }

        private static bool ValidateCapturedOutput(ZDO anchor, string operationId, StorageStack item, int amount)
        {
            try { var captured = StorageRpc.Decode(anchor.GetString(OutputOutboxKey(operationId), "")).Items.Single(); return captured.Amount == amount && captured.Identity == item.Identity; }
            catch { return false; }
        }

        private StorageEffectRecord RecoverLegacyCapturedOutput(string id, AuthorityContext context, StorageStack output, int amount,
            StorageEffectDescriptor capture, StorageEffectDescriptor remainder, bool requireAll)
        {
            var source = context.Anchor;
            if (source == null || !HasIndependentCaptureAuthority(id, context, output, amount, capture, remainder,
                    requireAll) ||
                source.GetString(CaptureIntentKey, "") != id + "\n" + capture.Data ||
                !(source.GetString(EffectActiveKey, "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Contains(id, StringComparer.Ordinal) || !ValidateCapturedOutput(source, id, output, amount) ||
                !HasAppliedCaptureReceipt(source, id)) return null;

            var oldDelivery = DerivedOperationId(id, "delivery-0");
            if (!EnsureLegacyCleanup(oldDelivery, context.Actor.ActorId, source)) return null;

            var currentTarget = source.m_uid.ToString();
            var currentCapture = new StorageEffectDescriptor(capture.Kind, currentTarget, capture.ActorId, capture.Data);
            var currentRemainder = remainder == null ? null : new StorageEffectDescriptor(remainder.Kind, currentTarget, remainder.ActorId, remainder.Data);
            var outbox = new StorageInventory("outbox:" + id, "1", 1, 1, new[] { output.At(0, amount) });
            var recovered = new StorageEffectRecord(1, WorldIdentity(), id, context.Actor.ActorId, currentTarget,
                AuthorityContextIdentity(context), currentCapture, null, currentRemainder, StorageRpc.Encode(outbox),
                DerivedOperationId(id, "delivery-1"), amount, 0, amount, StorageEffectStage.Captured,
                new[] { "capture" }, "legacy-orphan-recovery", requireAll);
            return _journal.TrySaveEffect(recovered) ? recovered : null;
        }

        private bool LegacyCleanupComplete(StorageEffectRecord record)
        {
            if (record == null || record.Message != "legacy-orphan-recovery") return true;
            var oldDelivery = DerivedOperationId(record.OperationId, "delivery-0");
            var cleanup = _journal.Load(oldDelivery);
            if (cleanup == null)
            {
                var source = ResolveTarget(record.TargetId)?.GetZDO();
                if (record.Capture == null || record.WorldId != WorldIdentity() || record.ActorId == 0L || source == null ||
                    source.m_uid.ToString() != record.TargetId ||
                    source.GetString(CaptureIntentKey, "") != record.OperationId + "\n" + record.Capture.Data ||
                    !(source.GetString(EffectActiveKey, "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Contains(record.OperationId, StringComparer.Ordinal) ||
                    !ValidateCapturedEscrow(source, record) || !HasAppliedCaptureReceipt(source, record.OperationId) ||
                    !EnsureLegacyCleanup(oldDelivery, record.ActorId, source)) return false;
                cleanup = _journal.Load(oldDelivery);
            }
            if (cleanup.Status != StorageOperationStatus.Rejected && cleanup.Status != StorageOperationStatus.Aborted)
            { ResumeTransaction(oldDelivery); cleanup = _journal.Load(oldDelivery); }
            return cleanup != null && (cleanup.Status == StorageOperationStatus.Rejected || cleanup.Status == StorageOperationStatus.Aborted);
        }

        private bool EnsureLegacyCleanup(string oldDelivery, long actorId, ZDO source)
        {
            var existing = _journal.Load(oldDelivery);
            if (existing == null)
            {
                var world = StorageJournal.WorldObjects().ToList();
                if (world.Any(x => HasParticipantReceipt(x, oldDelivery))) return false;
                var reservations = world.Where(x => x.GetString("scs.storage.active.v1", "") == oldDelivery).ToList();
                if (reservations.Count == 0) return false;
                var plans = reservations.Select(zdo => new StorageParticipantPlan(zdo.m_uid.ToString(),
                    zdo.DataRevision.ToString(), zdo == source ? "anchor" : "legacy-reservation")).ToList();
                existing = new StorageTransactionRecord(oldDelivery, "legacy-orphan", StorageOperationStatus.RecoveryPending,
                    plans, "Abort pending: recovered prepare-only reservation", actorId: actorId);
                if (!_journal.TrySave(existing)) return false;
            }
            if (existing.Status != StorageOperationStatus.Rejected && existing.Status != StorageOperationStatus.Aborted)
            { ResumeTransaction(oldDelivery); existing = _journal.Load(oldDelivery); }
            return existing != null && (existing.Status == StorageOperationStatus.Rejected || existing.Status == StorageOperationStatus.Aborted);
        }

        private static bool ValidateCapturedEscrow(ZDO source, StorageEffectRecord record)
        {
            try
            {
                var sourceOutbox = source.GetString(OutputOutboxKey(record.OperationId), "");
                return sourceOutbox == record.Escrow && StorageRpc.Decode(sourceOutbox).Items.Sum(x => x.Amount) == record.Requested;
            }
            catch { return false; }
        }

        private static bool HasIndependentCaptureAuthority(string id, AuthorityContext context, StorageStack output, int amount,
            StorageEffectDescriptor capture, StorageEffectDescriptor remainder, bool requireAll)
        {
            if (context.Anchor.GetString(CaptureAuthorityKey, "") == CaptureAuthority(id, context.Actor.ActorId)) return true;
            if (context.Actor.PeerId != ZDOMan.GetSessionID() || Player.m_localPlayer == null ||
                Player.m_localPlayer.GetPlayerID() != context.Actor.ActorId) return false;

            var player = Player.m_localPlayer;
            if (!TryReadPersistedIntent(player, id, out var intent) || intent.Kind != "output" || intent.Id != id ||
                intent.WorldId != WorldIdentity() || intent.AnchorId != capture.TargetId) return false;
            var encodedOutbox = StorageRpc.Encode(new StorageInventory("outbox:" + id, "1", 1, 1,
                new[] { output.At(0, amount) }));
            var body = new ZPackage(); body.Write(encodedOutbox); body.Write(amount);
            WriteEffect(body, capture); WriteEffect(body, remainder); body.Write(requireAll);
            if (!intent.Body.SequenceEqual(body.GetArray())) return false;
            var escrowKey = EscrowPrefix + id;
            var effectKey = escrowKey + ".effect";
            return player.m_customData.TryGetValue(escrowKey, out var escrow) && escrow == encodedOutbox &&
                   player.m_customData.TryGetValue(effectKey, out var effect) && effect == EncodeEffect(capture);
        }

        private static bool TryReadPersistedIntent(Player player, string id, out StorageRequestIntent intent)
        {
            intent = null;
            if (player == null || !player.m_customData.TryGetValue(PendingRequestsKey, out var persisted) || string.IsNullOrEmpty(persisted)) return false;
            try
            {
                using (var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(Convert.FromBase64String(persisted))))
                {
                    var count = reader.ReadInt32(); if (count < 0 || count > 1024) return false;
                    for (var i = 0; i < count; i++)
                    {
                        var candidateId = reader.ReadString(); var length = reader.ReadInt32();
                        if (length < 0 || length > 8 * 1024 * 1024) return false;
                        var encoded = reader.ReadBytes(length); if (encoded.Length != length) return false;
                        var candidate = StorageRequestIntent.Decode(encoded);
                        if (candidateId == id && candidate.Id == id) { intent = candidate; return true; }
                    }
                }
            }
            catch { return false; }
            return false;
        }

        private static bool HasAppliedCaptureReceipt(ZDO source, string operationId)
        {
            var receipt = operationId + ":" + StorageEffectStage.CapturePending;
            var receipts = ReadEffectReceipts(source);
            return (receipts.TryGetValue(receipt, out var persisted) && persisted.Applied) ||
                ReadEffectReceiptWatermarks(source).Contains(ReceiptWatermarkId(operationId, StorageEffectStage.CapturePending));
        }

        private static bool HasParticipantReceipt(ZDO zdo, string operationId)
        {
            if ((zdo.GetString("scs.storage.receipts.v1", "") ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Contains(operationId, StringComparer.Ordinal)) return true;
            try { return new StorageReplayFilter(zdo.GetString("scs.storage.receipt.watermarks.v1", "")).Contains(operationId); }
            catch { return false; }
        }

        private static string DerivedOperationId(string operationId, string phase)
        {
            var separator = (operationId ?? "").LastIndexOf(':');
            return separator > 0 ? operationId.Substring(0, separator) + ":" + phase + operationId.Substring(separator) : operationId + ":" + phase + ":0";
        }

        private string NextDeliveryId(string operationId)
        {
            for (var attempt = 0; attempt < 100000; attempt++)
            {
                var id = DerivedOperationId(operationId, "delivery-" + attempt.ToString(CultureInfo.InvariantCulture));
                if (_journal.Load(id) == null) return id;
            }
            throw new InvalidOperationException("Delivery retry limit exhausted");
        }

        private static StorageEffectRecord ResetCapturedDelivery(StorageEffectRecord record, string deliveryId) =>
            new StorageEffectRecord(record.Version, record.WorldId, record.OperationId, record.ActorId, record.TargetId, record.Context,
                record.Capture, record.Cost, record.Remainder, record.Escrow, deliveryId, record.Requested, 0, record.Requested,
                StorageEffectStage.Captured, new[] { "capture" }, record.Message, record.RequireAll);
        private bool PlayerOwnsItem(Player actor, ItemDrop.ItemData item)
        {
            return actor.GetInventory().ContainsItem(item);
        }
        private static void WriteEffect(ZPackage package, StorageEffectDescriptor effect)
        {
            package.Write(effect != null); if (effect == null) return; package.Write(effect.Kind); package.Write(effect.TargetId); package.Write(effect.ActorId); package.Write(effect.Data);
        }
        private static StorageEffectDescriptor ReadEffect(ZPackage package) => package.ReadBool() ? new StorageEffectDescriptor(package.ReadString(), package.ReadString(), package.ReadLong(), package.ReadString()) : null;
    }
}
