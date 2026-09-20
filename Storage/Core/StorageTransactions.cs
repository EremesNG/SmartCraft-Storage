using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SmartCraftStorage.Storage.Core
{
    public sealed class StorageParticipantPlan
    {
        public string ParticipantId { get; }
        public string ExpectedRevision { get; }
        public string Payload { get; }
        public string ExpectedPayload { get; }
        public StorageParticipantPlan(string participantId, string expectedRevision, string payload, string expectedPayload = "")
        { ParticipantId = participantId; ExpectedRevision = expectedRevision; Payload = payload; ExpectedPayload = expectedPayload ?? string.Empty; }
    }

    public sealed class StorageTransactionRequest
    {
        public string Id { get; }
        public string Kind { get; }
        public IReadOnlyList<StorageParticipantPlan> Participants { get; }
        public int Requested { get; }
        public int Accepted { get; }
        public long ActorId { get; }
        public StorageTransactionRequest(string id, string kind, IEnumerable<StorageParticipantPlan> participants,
            int requested = 0, int accepted = 0, long actorId = 0L)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Stable operation ID is required.", nameof(id));
            if (requested < 0 || accepted < 0 || accepted > requested) throw new ArgumentOutOfRangeException(nameof(accepted));
            Id = id; Kind = kind ?? string.Empty;
            Requested = requested; Accepted = accepted;
            ActorId = actorId;
            Participants = new ReadOnlyCollection<StorageParticipantPlan>((participants ?? Enumerable.Empty<StorageParticipantPlan>())
                .OrderBy(x => x.ParticipantId, StringComparer.Ordinal).ToList());
        }
    }

    public sealed class StorageTransactionRecord
    {
        public string Id { get; }
        public string Kind { get; }
        public StorageOperationStatus Status { get; }
        public IReadOnlyList<StorageParticipantPlan> Participants { get; }
        public string Message { get; }
        public int Requested { get; }
        public int Accepted { get; }
        public long ActorId { get; }
        public StorageTransactionRecord(string id, string kind, StorageOperationStatus status,
            IEnumerable<StorageParticipantPlan> participants, string message = "", int requested = 0, int accepted = 0, long actorId = 0L)
        {
            if (requested < 0 || accepted < 0 || accepted > requested) throw new ArgumentOutOfRangeException(nameof(accepted));
            Id = id; Kind = kind; Status = status; Participants = new ReadOnlyCollection<StorageParticipantPlan>(participants.ToList());
            Message = message ?? string.Empty; Requested = requested; Accepted = accepted; ActorId = actorId;
        }
        public StorageTransactionRecord At(StorageOperationStatus status, string message = "") =>
            new StorageTransactionRecord(Id, Kind, status, Participants, message, Requested, Accepted, ActorId);
    }

    public sealed class StorageParticipantResult
    {
        public bool IsAccepted { get; }
        public bool IsRejected { get; }
        public string Message { get; }
        private StorageParticipantResult(bool accepted, bool rejected, string message) { IsAccepted = accepted; IsRejected = rejected; Message = message ?? string.Empty; }
        public static StorageParticipantResult Accepted() => new StorageParticipantResult(true, false, "");
        public static StorageParticipantResult Rejected(string message) => new StorageParticipantResult(false, true, message);
        public static StorageParticipantResult Unknown(string message) => new StorageParticipantResult(false, false, message);
    }

    public interface IStorageTransactionStore
    {
        StorageTransactionRecord Load(string id);
        void Save(StorageTransactionRecord record);
    }

    public interface IStorageTransactionParticipant
    {
        string Id { get; }
        string Revision { get; }
        StorageParticipantResult Prepare(string operationId, string expectedRevision);
        StorageParticipantResult Apply(string operationId, string payload);
        StorageParticipantResult Receipt(string operationId);
        StorageParticipantResult Release(string operationId);
    }

    public sealed class StorageTransactions
    {
        private readonly IStorageTransactionStore _store;
        private readonly Dictionary<string, IStorageTransactionParticipant> _participants;
        private readonly bool _duplicateParticipants;
        public StorageTransactions(IStorageTransactionStore store, IEnumerable<IStorageTransactionParticipant> participants)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            var supplied = (participants ?? Enumerable.Empty<IStorageTransactionParticipant>()).Where(x => x != null).ToList();
            _duplicateParticipants = supplied.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() > 1);
            _participants = supplied.GroupBy(x => x.Id, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        }

        public StorageOperation Execute(StorageTransactionRequest request)
        {
            var existing = _store.Load(request.Id);
            if (existing != null) return ResumeRecord(existing);
            var duplicatePlans = request.Participants.GroupBy(x => x.ParticipantId, StringComparer.Ordinal).Any(x => x.Count() > 1);
            var record = new StorageTransactionRecord(request.Id, request.Kind, StorageOperationStatus.Preparing,
                request.Participants, requested: request.Requested, accepted: request.Accepted, actorId: request.ActorId);
            _store.Save(record);
            if (_duplicateParticipants || duplicatePlans)
                return Reject(record, Array.Empty<IStorageTransactionParticipant>(), "Duplicate participant identity");
            return Prepare(record);
        }

        private StorageOperation Prepare(StorageTransactionRecord record)
        {
            var prepared = new List<IStorageTransactionParticipant>();
            foreach (var plan in record.Participants)
            {
                if (!_participants.TryGetValue(plan.ParticipantId, out var participant)) return Pending(record.At(StorageOperationStatus.Preparing, "Participant unavailable"));
                var result = participant.Prepare(record.Id, plan.ExpectedRevision);
                if (result.IsRejected) return Reject(record, prepared, result.Message);
                if (!result.IsAccepted) return Pending(record.At(StorageOperationStatus.Preparing, result.Message));
                prepared.Add(participant);
            }
            record = record.At(StorageOperationStatus.Prepared); _store.Save(record);
            record = record.At(StorageOperationStatus.CommitDecided); _store.Save(record);
            return Apply(record);
        }

        public StorageOperation Resume(string operationId)
        {
            var record = _store.Load(operationId);
            return record == null ? StorageOperation.Unavailable("Operation not found") : ResumeRecord(record);
        }

        public static StorageOperation GetStatus(IStorageTransactionStore store, string operationId)
        {
            var record = store.Load(operationId);
            return record == null ? StorageOperation.Unavailable("Operation not found") : ToOperation(record);
        }

        private StorageOperation ResumeRecord(StorageTransactionRecord record)
        {
            if (record.Status == StorageOperationStatus.Preparing) return Prepare(record);
            if (record.Status == StorageOperationStatus.Prepared)
            {
                record = record.At(StorageOperationStatus.CommitDecided); _store.Save(record);
                return Apply(record);
            }
            if (record.Status == StorageOperationStatus.RecoveryPending && record.Message.StartsWith("Abort pending: ", StringComparison.Ordinal))
                return ReleaseAbort(record, record.Message.Substring("Abort pending: ".Length));
            if (record.Status == StorageOperationStatus.CommitDecided || record.Status == StorageOperationStatus.Applying || record.Status == StorageOperationStatus.RecoveryPending)
                return Apply(record);
            return ToOperation(record);
        }

        private StorageOperation Apply(StorageTransactionRecord record)
        {
            record = record.At(StorageOperationStatus.Applying); _store.Save(record);
            foreach (var plan in record.Participants)
            {
                if (!_participants.TryGetValue(plan.ParticipantId, out var participant)) return Pending(record.At(StorageOperationStatus.RecoveryPending, "Participant unavailable"));
                var receipt = participant.Receipt(record.Id);
                var result = receipt.IsAccepted ? receipt : participant.Apply(record.Id, plan.Payload);
                if (result.IsRejected || !result.IsAccepted) return Pending(record.At(StorageOperationStatus.RecoveryPending, result.Message));
            }
            foreach (var plan in record.Participants)
            {
                if (!_participants.TryGetValue(plan.ParticipantId, out var participant))
                    return Pending(record.At(StorageOperationStatus.RecoveryPending, "Participant unavailable during release"));
                var released = participant.Release(record.Id);
                if (!released.IsAccepted)
                    return Pending(record.At(StorageOperationStatus.RecoveryPending, released.Message));
            }
            record = record.At(StorageOperationStatus.Confirmed); _store.Save(record);
            return ToOperation(record);
        }

        private StorageOperation Reject(StorageTransactionRecord record, IEnumerable<IStorageTransactionParticipant> prepared, string message)
        {
            record = record.At(StorageOperationStatus.RecoveryPending, "Abort pending: " + message); _store.Save(record);
            return ReleaseAbort(record, message);
        }
        private StorageOperation ReleaseAbort(StorageTransactionRecord record, string reason)
        {
            foreach (var plan in record.Participants)
            {
                if (!_participants.TryGetValue(plan.ParticipantId, out var participant)) return Pending(record.At(StorageOperationStatus.RecoveryPending, "Abort pending: " + reason));
                var released = participant.Release(record.Id);
                if (!released.IsAccepted) return Pending(record.At(StorageOperationStatus.RecoveryPending, "Abort pending: " + reason));
            }
            record = record.At(StorageOperationStatus.Rejected, reason); _store.Save(record); return ToOperation(record);
        }
        private StorageOperation Pending(StorageTransactionRecord record) { _store.Save(record); return ToOperation(record); }
        private static StorageOperation ToOperation(StorageTransactionRecord record)
        {
            var final = record.Status == StorageOperationStatus.Confirmed;
            return new StorageOperation(record.Id, record.Status, record.Requested,
                final ? record.Accepted : 0, final ? record.Requested - record.Accepted : record.Requested, record.Message);
        }
    }
}
