using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using SmartCraftStorage.Storage.Core;

namespace SmartCraftStorage.Storage.Runtime
{
    internal sealed class StorageJournal : IStorageTransactionStore, IStorageEffectStore
    {
        private const string JournalKey = "scs.storage.journal.v1";
        private const string JournalPrefab = "SCS_StorageJournal";
        private const string JournalWorldKey = "scs.storage.journal.world.v1";
        private const string JournalRootKey = "scs.storage.journal.root.v1";
        private const string JournalRecordKey = "scs.storage.journal.record.v1";
        private const string JournalRecordTypeKey = "scs.storage.journal.record.type.v1";
        private const string JournalRecordDataKey = "scs.storage.journal.record.data.v1";
        private const string JournalReplayKey = "scs.storage.journal.replay.v1";
        private const int RetainedCompleted = 256;
        private readonly Dictionary<string, StorageTransactionRecord> _records = new Dictionary<string, StorageTransactionRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, StorageEffectRecord> _effects = new Dictionary<string, StorageEffectRecord>(StringComparer.Ordinal);
        private bool _loaded;
        private ZDOID _worldId = ZDOID.None;
        private ZDO _journalZdo;
        private long _journalWorld;
        private ZDOMan _manager;
        private long _session;
        private long _contextWorld;
        private readonly Dictionary<string, ZDO> _recordZdos = new Dictionary<string, ZDO>(StringComparer.Ordinal);
        private readonly List<ZDO> _pendingDeletes = new List<ZDO>();
        private StorageReplayFilter _compacted = new StorageReplayFilter(8192);

        internal StorageJournal() { }
        public StorageTransactionRecord Load(string id)
        {
            EnsureLoaded();
            if (_records.TryGetValue(id, out var value)) return value;
            return _compacted.Contains(id) ? new StorageTransactionRecord(id, "compacted", StorageOperationStatus.Rejected,
                Array.Empty<StorageParticipantPlan>(), "Retired operation ID cannot be replayed") : null;
        }
        public void Save(StorageTransactionRecord record) { EnsureLoaded(); _records[record.Id] = record; PersistTransaction(record); CompactCompleted(); PersistReplay(); }
        public StorageEffectRecord LoadEffect(string operationId)
        {
            EnsureLoaded();
            if (_effects.TryGetValue(operationId, out var value)) return value;
            if (!_compacted.Contains(operationId)) return null;
            var world = ZNet.instance != null && ZNet.instance.GetWorld() != null ? ZNet.instance.GetWorldUID().ToString() : "retired-world";
            return new StorageEffectRecord(1, world, operationId, 0L, "", "", null, null, null, "", "", 0, 0, 0,
                StorageEffectStage.Rejected, message: "Retired effect ID cannot be replayed");
        }
        public void SaveEffect(StorageEffectRecord record) { EnsureLoaded(); _effects[record.OperationId] = record; PersistEffect(record); CompactCompleted(); PersistReplay(); }
        internal IReadOnlyList<StorageTransactionRecord> Pending { get { EnsureLoaded(); return _records.Values.Where(x => x.Status != StorageOperationStatus.Confirmed && x.Status != StorageOperationStatus.Rejected && x.Status != StorageOperationStatus.Aborted).ToList(); } }
        internal IReadOnlyList<StorageEffectRecord> PendingEffects { get { EnsureLoaded(); return _effects.Values.Where(x => x.Stage != StorageEffectStage.Completed && x.Stage != StorageEffectStage.Rejected).ToList(); } }

        private ZDO WorldZdo()
        {
            EnsureContext();
            if (ZDOMan.instance == null || ZNet.instance == null || !ZNet.instance.IsServer()) return null;
            var worldUid = ZNet.instance.GetWorldUID(); var world = worldUid.ToString();
            if (_journalZdo != null && IsRoot(_journalZdo, world) && ZDOMan.instance.GetZDO(_journalZdo.m_uid) == _journalZdo)
            { if (_journalZdo.GetOwner() != ZDOMan.GetSessionID()) _journalZdo.SetOwner(ZDOMan.GetSessionID()); return _journalZdo; }
            if (_journalZdo != null) ClearCache();
            if (ZNetScene.instance == null || !ZNetScene.instance.HasPrefab(JournalPrefab.GetStableHashCode())) return null;
            var candidates = new List<ZDO>(); var index = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(JournalPrefab, candidates, ref index)) { }
            var existing = candidates.FirstOrDefault(x => IsRoot(x, world));
            if (existing != null)
            {
                if (existing.GetOwner() != ZDOMan.GetSessionID()) existing.SetOwner(ZDOMan.GetSessionID());
                _journalZdo = existing; _journalWorld = worldUid; return existing;
            }
            var created = ZDOMan.instance.CreateNewZDO(new UnityEngine.Vector3(0f, -100000f, 0f), JournalPrefab.GetStableHashCode());
            created.SetPrefab(JournalPrefab.GetStableHashCode());
            created.Persistent = true;
            created.Distant = true;
            created.Set(JournalRootKey, true);
            created.Set(JournalWorldKey, world);
            _journalZdo = created; _journalWorld = worldUid; return created;
        }

        private ZDO RecordZdo(string id, string type)
        {
            var root = WorldZdo(); if (root == null) return null;
            var key = type + ":" + id;
            if (_recordZdos.TryGetValue(key, out var cached) && IsJournalObject(cached, _journalWorld.ToString(CultureInfo.InvariantCulture)) &&
                !cached.GetBool(JournalRootKey, false) && cached.GetString(JournalRecordKey, "") == id &&
                cached.GetString(JournalRecordTypeKey, "") == type && ZDOMan.instance.GetZDO(cached.m_uid) == cached) return cached;
            var created = ZDOMan.instance.CreateNewZDO(new UnityEngine.Vector3(0f, -100000f, 0f), JournalPrefab.GetStableHashCode());
            created.SetPrefab(JournalPrefab.GetStableHashCode()); created.Persistent = true;
            created.Set(JournalWorldKey, _journalWorld.ToString(CultureInfo.InvariantCulture)); created.Set(JournalRecordKey, id); created.Set(JournalRecordTypeKey, type);
            _recordZdos[key] = created; return created;
        }

        private void PersistTransaction(StorageTransactionRecord record)
        {
            var zdo = RecordZdo(record.Id, "transaction"); if (zdo == null) return;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(record.Id); writer.Write(record.Kind); writer.Write((int)record.Status); writer.Write(record.Message);
                writer.Write(record.Requested); writer.Write(record.Accepted); writer.Write(record.ActorId); writer.Write(record.Participants.Count);
                foreach (var plan in record.Participants) { writer.Write(plan.ParticipantId); writer.Write(plan.ExpectedRevision); writer.Write(plan.Payload); writer.Write(plan.ExpectedPayload); }
                zdo.Set(JournalRecordDataKey, Convert.ToBase64String(stream.ToArray()));
            }
        }

        private void PersistEffect(StorageEffectRecord record)
        {
            var zdo = RecordZdo(record.OperationId, "effect"); if (zdo == null) return;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            { WriteEffectRecord(writer, record); zdo.Set(JournalRecordDataKey, Convert.ToBase64String(stream.ToArray())); }
        }

        private void PersistReplay()
        {
            var root = WorldZdo(); if (root == null) return;
            root.Set(JournalReplayKey, _compacted.Export());
            foreach (var zdo in _pendingDeletes) if (ZDOMan.instance.GetZDO(zdo.m_uid) == zdo) ZDOMan.instance.DestroyZDO(zdo);
            _pendingDeletes.Clear();
        }

        private void CompactCompleted()
        {
            var protectedTransactions = new HashSet<string>(_effects.Values
                .Where(x => x.Stage != StorageEffectStage.Completed && x.Stage != StorageEffectStage.Rejected)
                .SelectMany(x => new[] { x.OperationId, x.DeliveryTransactionId, DerivedId(x.OperationId, "refund") })
                .Where(x => !string.IsNullOrEmpty(x)), StringComparer.Ordinal);
            var transactions = _records.Values.Where(x => (x.Status == StorageOperationStatus.Confirmed ||
                x.Status == StorageOperationStatus.Rejected || x.Status == StorageOperationStatus.Aborted)
                && !protectedTransactions.Contains(x.Id) && _compacted.CanRemember(x.Id))
                .OrderByDescending(x => x.Id, StringComparer.Ordinal).Skip(RetainedCompleted).ToList();
            foreach (var record in transactions) { _compacted.Remember(record.Id); _records.Remove(record.Id); Retire(record.Id, "transaction"); }
            var effects = _effects.Values.Where(x => (x.Stage == StorageEffectStage.Completed || x.Stage == StorageEffectStage.Rejected)
                && _compacted.CanRemember(x.OperationId))
                .OrderByDescending(x => x.OperationId, StringComparer.Ordinal).Skip(RetainedCompleted).ToList();
            foreach (var record in effects) { _compacted.Remember(record.OperationId); _effects.Remove(record.OperationId); Retire(record.OperationId, "effect"); }
        }

        private static string DerivedId(string operationId, string phase)
        {
            var separator = (operationId ?? "").LastIndexOf(':');
            return separator > 0 ? operationId.Substring(0, separator) + ":" + phase + operationId.Substring(separator) : "";
        }

        private void Retire(string id, string type)
        { var key = type + ":" + id; if (_recordZdos.TryGetValue(key, out var zdo)) { _recordZdos.Remove(key); _pendingDeletes.Add(zdo); } }

        private void LoadAll()
        {
            var root = WorldZdo(); if (root == null) return;
            var encoded = root.GetString(JournalKey, "");
            try
            {
                if (!string.IsNullOrEmpty(encoded)) using (var reader = new BinaryReader(new MemoryStream(Convert.FromBase64String(encoded))))
                {
                    var version = reader.ReadInt32();
                    if (version < 1 || version > 4) return;
                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        var id = reader.ReadString(); var kind = reader.ReadString(); var status = (StorageOperationStatus)reader.ReadInt32(); var message = reader.ReadString();
                        var requested = version >= 4 ? reader.ReadInt32() : 0; var accepted = version >= 4 ? reader.ReadInt32() : 0;
                        var plans = new List<StorageParticipantPlan>(); var planCount = reader.ReadInt32();
                        for (var p = 0; p < planCount; p++)
                        {
                            var participant = reader.ReadString(); var revision = reader.ReadString(); var payload = reader.ReadString();
                            plans.Add(new StorageParticipantPlan(participant, revision, payload, version >= 3 ? reader.ReadString() : ""));
                        }
                        _records[id] = new StorageTransactionRecord(id, kind, status, plans, message, requested, accepted);
                    }
                    if (version >= 2)
                    {
                        var effectCount = reader.ReadInt32();
                        if (effectCount < 0 || effectCount > 100000) throw new InvalidDataException("Invalid effect record count");
                        for (var i = 0; i < effectCount; i++)
                        {
                            var record = ReadEffectRecord(reader, version);
                            _effects[record.OperationId] = record;
                        }
                    }
                    if (version >= 4) _compacted = new StorageReplayFilter(reader.ReadString());
                }
                var replay = root.GetString(JournalReplayKey, "");
                if (!string.IsNullOrEmpty(replay)) _compacted = new StorageReplayFilter(replay);
                LoadRecordZdos();
                if (!string.IsNullOrEmpty(encoded))
                {
                    foreach (var record in _records.Values) PersistTransaction(record);
                    foreach (var record in _effects.Values) PersistEffect(record);
                    root.Set(JournalKey, ""); PersistReplay();
                }
            }
            catch (Exception error) { ZLog.LogWarning("[SmartCraft-Storage] Ignoring invalid storage journal: " + error.Message); }
        }

        private void LoadRecordZdos()
        {
            var candidates = new List<ZDO>(); var index = 0; var world = _journalWorld.ToString(CultureInfo.InvariantCulture);
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(JournalPrefab, candidates, ref index)) { }
            foreach (var zdo in candidates.Where(x => x.Persistent && x.GetString(JournalWorldKey, "") == world))
            {
                var id = zdo.GetString(JournalRecordKey, ""); if (string.IsNullOrEmpty(id)) continue;
                try
                {
                    var data = zdo.GetString(JournalRecordDataKey, ""); if (string.IsNullOrEmpty(data)) continue;
                    using (var reader = new BinaryReader(new MemoryStream(Convert.FromBase64String(data))))
                    {
                        if (zdo.GetString(JournalRecordTypeKey, "") == "effect") _effects[id] = ReadEffectRecord(reader, 4);
                        else
                        {
                            var recordId = reader.ReadString(); var kind = reader.ReadString(); var status = (StorageOperationStatus)reader.ReadInt32(); var message = reader.ReadString();
                            var requested = reader.ReadInt32(); var accepted = reader.ReadInt32(); var actorId = reader.ReadInt64(); var count = reader.ReadInt32();
                            if (count < 0 || count > 1024) throw new InvalidDataException("Invalid participant count");
                            var plans = new List<StorageParticipantPlan>();
                            for (var i = 0; i < count; i++) plans.Add(new StorageParticipantPlan(reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadString()));
                            _records[id] = new StorageTransactionRecord(recordId, kind, status, plans, message, requested, accepted, actorId);
                        }
                    }
                    _recordZdos[zdo.GetString(JournalRecordTypeKey, "transaction") + ":" + id] = zdo;
                }
                catch (Exception error) { ZLog.LogWarning("[SmartCraft-Storage] Ignoring invalid journal record " + id + ": " + error.Message); }
            }
        }

        private static void WriteEffectRecord(BinaryWriter writer, StorageEffectRecord record)
        {
            writer.Write(record.Version); writer.Write(record.WorldId); writer.Write(record.OperationId); writer.Write(record.ActorId);
            writer.Write(record.TargetId); writer.Write(record.Context); WriteDescriptor(writer, record.Capture); WriteDescriptor(writer, record.Cost);
            WriteDescriptor(writer, record.Remainder); writer.Write(record.Escrow); writer.Write(record.DeliveryTransactionId);
            writer.Write(record.Requested); writer.Write(record.Accepted); writer.Write(record.Remaining); writer.Write((int)record.Stage);
            writer.Write(record.Acknowledgements.Count); foreach (var ack in record.Acknowledgements) writer.Write(ack); writer.Write(record.Message); writer.Write(record.RequireAll);
        }

        private static StorageEffectRecord ReadEffectRecord(BinaryReader reader, int journalVersion)
        {
            var version = reader.ReadInt32(); var world = reader.ReadString(); var operation = reader.ReadString(); var actor = reader.ReadInt64();
            var target = reader.ReadString(); var context = reader.ReadString(); var capture = ReadDescriptor(reader); var cost = ReadDescriptor(reader);
            var remainder = ReadDescriptor(reader); var escrow = reader.ReadString(); var delivery = reader.ReadString();
            var requested = reader.ReadInt32(); var accepted = reader.ReadInt32(); var remaining = reader.ReadInt32(); var stage = (StorageEffectStage)reader.ReadInt32();
            var ackCount = reader.ReadInt32(); if (ackCount < 0 || ackCount > 64) throw new InvalidDataException("Invalid effect acknowledgement count");
            var acknowledgements = new List<string>(); for (var i = 0; i < ackCount; i++) acknowledgements.Add(reader.ReadString());
            var message = reader.ReadString(); var requireAll = journalVersion >= 3 && reader.ReadBoolean();
            return new StorageEffectRecord(version, world, operation, actor, target, context, capture, cost, remainder, escrow, delivery,
                requested, accepted, remaining, stage, acknowledgements, message, requireAll);
        }

        private static void WriteDescriptor(BinaryWriter writer, StorageEffectDescriptor descriptor)
        {
            writer.Write(descriptor != null); if (descriptor == null) return;
            writer.Write(descriptor.Kind); writer.Write(descriptor.TargetId); writer.Write(descriptor.ActorId); writer.Write(descriptor.Data);
        }

        private static StorageEffectDescriptor ReadDescriptor(BinaryReader reader) => reader.ReadBoolean()
            ? new StorageEffectDescriptor(reader.ReadString(), reader.ReadString(), reader.ReadInt64(), reader.ReadString()) : null;

        private static bool IsJournalObject(ZDO zdo, string world) => zdo.Persistent &&
            zdo.GetPrefab() == JournalPrefab.GetStableHashCode() && zdo.GetString(JournalWorldKey, "") == world;

        private static bool IsRoot(ZDO zdo, string world) => IsJournalObject(zdo, world) &&
            (zdo.GetBool(JournalRootKey, false) || !string.IsNullOrEmpty(zdo.GetString(JournalKey, "")));

        private void EnsureContext()
        {
            var manager = ZDOMan.instance;
            var session = manager != null ? ZDOMan.GetSessionID() : 0L;
            var world = ZNet.instance != null && ZNet.instance.GetWorld() != null ? ZNet.instance.GetWorldUID() : 0L;
            if (ReferenceEquals(manager, _manager) && session == _session && world == _contextWorld) return;
            // Native IDs refer to a per-manager table. Never inspect an old ID before this reset.
            ClearCache();
            _manager = manager; _session = session; _contextWorld = world;
        }

        private void ClearCache()
        {
            _records.Clear(); _effects.Clear(); _recordZdos.Clear(); _pendingDeletes.Clear();
            _compacted = new StorageReplayFilter(8192); _loaded = false; _worldId = ZDOID.None;
            _journalZdo = null; _journalWorld = 0L;
        }

        private void EnsureLoaded()
        {
            var world = WorldZdo();
            if (world == null)
            {
                if (_loaded) ClearCache();
                return;
            }
            if (_loaded && _worldId != world.m_uid)
            { _records.Clear(); _effects.Clear(); _recordZdos.Clear(); _compacted = new StorageReplayFilter(8192); _loaded = false; }
            if (_loaded) return;
            _worldId = world.m_uid;
            _loaded = true; LoadAll();
        }
    }
}
