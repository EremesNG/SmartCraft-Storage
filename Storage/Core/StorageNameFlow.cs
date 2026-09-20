using System;

namespace SmartCraftStorage.Storage.Core
{
    public static class StorageNameFlow
    {
        public static bool ApplyOnce(Func<bool> hasReceipt, Func<bool> apply, Action rememberReceipt)
        {
            if (hasReceipt()) return true;
            if (!apply()) return false;
            rememberReceipt(); return true;
        }
        public static void Dispatch(string id, Func<StorageOperation, StorageOperation> remember, Action send)
        {
            remember(new StorageOperation(id, StorageOperationStatus.RecoveryPending, message: "Awaiting target owner"));
            send();
        }
    }
}
