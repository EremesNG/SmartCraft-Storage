using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmartCraftStorage.Storage.Core
{
    public enum StorageEffectStage
    {
        CapturePending,
        Captured,
        CostPreparing,
        CostPrepared,
        DeliveryPending,
        Delivered,
        RemainderPending,
        Completed,
        Rejected
    }

    public sealed class StorageEffectRecord
    {
        public int Version { get; }
        public string WorldId { get; }
        public string OperationId { get; }
        public long ActorId { get; }
        public string TargetId { get; }
        public string Context { get; }
        public StorageEffectDescriptor Capture { get; }
        public StorageEffectDescriptor Cost { get; }
        public StorageEffectDescriptor Remainder { get; }
        public string Escrow { get; }
        public string DeliveryTransactionId { get; }
        public int Requested { get; }
        public int Accepted { get; }
        public int Remaining { get; }
        public StorageEffectStage Stage { get; }
        public IReadOnlyList<string> Acknowledgements { get; }
        public string Message { get; }
        public bool RequireAll { get; }

        public StorageEffectRecord(int version, string worldId, string operationId, long actorId,
            string targetId, string context, StorageEffectDescriptor capture, StorageEffectDescriptor cost,
            StorageEffectDescriptor remainder, string escrow, string deliveryTransactionId, int requested,
            int accepted, int remaining, StorageEffectStage stage, IEnumerable<string> acknowledgements = null,
            string message = "", bool requireAll = false)
        {
            if (version != 1) throw new ArgumentOutOfRangeException(nameof(version), "Unsupported effect record version.");
            if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World ID is required.", nameof(worldId));
            if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID is required.", nameof(operationId));
            if (requested < 0 || accepted < 0 || remaining < 0 || accepted + remaining != requested)
                throw new ArgumentOutOfRangeException(nameof(accepted), "Effect quantities must conserve the requested amount.");
            Version = version;
            WorldId = worldId;
            OperationId = operationId;
            ActorId = actorId;
            TargetId = targetId ?? string.Empty;
            Context = context ?? string.Empty;
            Capture = capture;
            Cost = cost;
            Remainder = remainder;
            Escrow = escrow ?? string.Empty;
            DeliveryTransactionId = deliveryTransactionId ?? string.Empty;
            Requested = requested;
            Accepted = accepted;
            Remaining = remaining;
            Stage = stage;
            Acknowledgements = new ReadOnlyCollection<string>(new List<string>(acknowledgements ?? Array.Empty<string>()));
            Message = message ?? string.Empty;
            RequireAll = requireAll;
        }

        public StorageEffectRecord At(StorageEffectStage stage, int? accepted = null, int? remaining = null,
            string acknowledgement = null, string message = null)
        {
            var acknowledgements = new List<string>(Acknowledgements);
            if (!string.IsNullOrEmpty(acknowledgement) && !acknowledgements.Contains(acknowledgement)) acknowledgements.Add(acknowledgement);
            return new StorageEffectRecord(Version, WorldId, OperationId, ActorId, TargetId, Context,
                Capture, Cost, Remainder, Escrow, DeliveryTransactionId, Requested,
                accepted ?? Accepted, remaining ?? Remaining, stage, acknowledgements, message ?? Message, RequireAll);
        }
    }

    public sealed class StorageEffectStepResult
    {
        public bool IsAccepted { get; }
        public bool IsRejected { get; }
        public int Accepted { get; }
        public int Remaining { get; }
        public string Message { get; }
        private StorageEffectStepResult(bool accepted, bool rejected, int exactAccepted, int remainder, string message)
        { IsAccepted = accepted; IsRejected = rejected; Accepted = exactAccepted; Remaining = remainder; Message = message ?? string.Empty; }
        public static StorageEffectStepResult Applied(int accepted = 0, int remaining = 0) => new StorageEffectStepResult(true, false, accepted, remaining, "");
        public static StorageEffectStepResult Rejected(string message) => new StorageEffectStepResult(false, true, 0, 0, message);
        public static StorageEffectStepResult Unknown(string message) => new StorageEffectStepResult(false, false, 0, 0, message);
    }

    public interface IStorageEffectStore
    {
        StorageEffectRecord LoadEffect(string operationId);
        void SaveEffect(StorageEffectRecord record);
    }

    public interface IStorageEffectPort
    {
        StorageEffectStepResult Receipt(StorageEffectRecord record, StorageEffectStage stage);
        StorageEffectStepResult Apply(StorageEffectRecord record, StorageEffectStage stage);
    }

    public sealed class StorageEffects
    {
        public static StorageOperation AcceptStatus(StorageOperation current, StorageOperation incoming) =>
            current != null && current.IsFinal ? current : incoming;

        public static StorageOperation PublicStatus(StorageEffectRecord record)
        {
            if (record == null) return StorageOperation.Unavailable("Effect operation not found");
            var status = record.Stage == StorageEffectStage.Completed ? StorageOperationStatus.Confirmed
                : record.Stage == StorageEffectStage.Rejected ? StorageOperationStatus.Rejected
                : record.Stage == StorageEffectStage.CostPrepared || record.Stage == StorageEffectStage.CostPreparing ? StorageOperationStatus.AwaitingEffect
                : StorageOperationStatus.RecoveryPending;
            return new StorageOperation(record.OperationId, status, record.Requested,
                record.Stage == StorageEffectStage.Completed ? record.Accepted : 0,
                record.Stage == StorageEffectStage.Completed ? record.Remaining : record.Requested,
                record.Message, record.Capture != null);
        }

        private readonly IStorageEffectStore _store;
        private readonly IStorageEffectPort _port;
        public StorageEffects(IStorageEffectStore store, IStorageEffectPort port)
        { _store = store ?? throw new ArgumentNullException(nameof(store)); _port = port ?? throw new ArgumentNullException(nameof(port)); }

        public StorageEffectRecord Begin(StorageEffectRecord record)
        {
            var existing = _store.LoadEffect(record.OperationId);
            if (existing != null) return Resume(existing.OperationId);
            _store.SaveEffect(record);
            return Resume(record.OperationId);
        }

        public StorageEffectRecord Resume(string operationId)
        {
            var record = _store.LoadEffect(operationId);
            if (record == null || record.Stage == StorageEffectStage.Completed || record.Stage == StorageEffectStage.Rejected) return record;
            if (record.Stage == StorageEffectStage.Captured)
            {
                var preparation = _port.Receipt(record, StorageEffectStage.Captured);
                if (!preparation.IsAccepted) preparation = _port.Apply(record, StorageEffectStage.Captured);
                if (preparation.IsRejected)
                {
                    record = record.At(StorageEffectStage.Rejected, message: preparation.Message); _store.SaveEffect(record); return record;
                }
                if (!preparation.IsAccepted) return _store.LoadEffect(operationId) ?? record;
                if (preparation.Accepted < 0 || preparation.Remaining < 0 || preparation.Accepted + preparation.Remaining != record.Requested)
                    return record;
                record = record.At(StorageEffectStage.DeliveryPending, preparation.Accepted, preparation.Remaining, "delivery-prepared");
                _store.SaveEffect(record);
            }
            if (record.Stage == StorageEffectStage.CostPreparing)
            {
                var preparation = _port.Receipt(record, StorageEffectStage.CostPreparing);
                if (!preparation.IsAccepted) preparation = _port.Apply(record, StorageEffectStage.CostPreparing);
                if (preparation.IsRejected)
                { record = record.At(StorageEffectStage.Rejected, message: preparation.Message); _store.SaveEffect(record); return record; }
                if (!preparation.IsAccepted) return _store.LoadEffect(operationId) ?? record;
                record = record.At(StorageEffectStage.CostPrepared, acknowledgement: "cost-prepared");
                _store.SaveEffect(record);
            }
            var result = _port.Receipt(record, record.Stage);
            if (!result.IsAccepted) result = _port.Apply(record, record.Stage);
            if (result.IsRejected)
            {
                record = record.Stage == StorageEffectStage.CapturePending || record.Stage == StorageEffectStage.CostPreparing
                    ? record.At(StorageEffectStage.Rejected, message: result.Message)
                    : record.At(record.Stage, message: result.Message);
                _store.SaveEffect(record);
                return record;
            }
            if (!result.IsAccepted) return _store.LoadEffect(operationId) ?? record;

            if (record.Stage == StorageEffectStage.CapturePending)
                record = record.At(StorageEffectStage.Captured, acknowledgement: "capture");
            else if (record.Stage == StorageEffectStage.CostPrepared)
                record = record.At(StorageEffectStage.Completed, acknowledgement: "cost");
            else if (record.Stage == StorageEffectStage.DeliveryPending)
            {
                if (result.Accepted < 0 || result.Remaining < 0 || result.Accepted + result.Remaining != record.Requested)
                    return record;
                record = record.At(StorageEffectStage.Delivered, result.Accepted, result.Remaining, "delivery");
            }
            else if (record.Stage == StorageEffectStage.RemainderPending)
                record = record.At(StorageEffectStage.Completed, acknowledgement: "remainder");
            _store.SaveEffect(record);

            if (record.Stage == StorageEffectStage.Captured) return Resume(operationId);
            if (record.Stage == StorageEffectStage.Delivered)
            {
                record = record.Remaining > 0 && record.Remainder != null
                    ? record.At(StorageEffectStage.RemainderPending)
                    : record.At(StorageEffectStage.Completed);
                _store.SaveEffect(record);
                if (record.Stage == StorageEffectStage.RemainderPending) return Resume(operationId);
            }
            return record;
        }
    }
}
