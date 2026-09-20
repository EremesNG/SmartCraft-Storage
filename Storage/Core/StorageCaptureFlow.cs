using System;

namespace SmartCraftStorage.Storage.Core
{
    // Shared by host and remote production. Persisted intent is the custody
    // boundary; an uncertain native effect may never restore vanilla fallback.
    public static class StorageCaptureFlow
    {
        public static StorageOperation Execute(string id, int amount, Func<StorageOperation> persistIntent,
            Func<StorageEffectStepResult> capture, Action forgetRejected, Func<StorageOperation> submit)
        {
            var prepared = persistIntent();
            if (prepared.IsFinal || prepared.Status == StorageOperationStatus.Unavailable) return prepared;
            StorageEffectStepResult native;
            try { native = capture(); }
            catch (Exception error) { native = StorageEffectStepResult.Unknown(error.Message); }
            if (native.IsRejected || (!native.IsAccepted && native.Message.StartsWith("Not ready:", StringComparison.Ordinal)))
            {
                forgetRejected();
                return new StorageOperation(id, StorageOperationStatus.Rejected, amount, 0, amount, native.Message);
            }
            StorageOperation queued;
            try { queued = submit(); }
            catch (Exception error) { queued = new StorageOperation(id, StorageOperationStatus.RecoveryPending, message: error.Message); }
            if (queued.Status == StorageOperationStatus.Confirmed)
                return new StorageOperation(id, queued.Status, amount, queued.Accepted, queued.Remaining, queued.Message, true);
            return new StorageOperation(id, StorageOperationStatus.RecoveryPending, amount, 0, amount, queued.Message, true);
        }
    }
}
