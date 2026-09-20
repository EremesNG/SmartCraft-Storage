using System;
using System.Collections.Generic;
using System.Linq;
using SmartCraftStorage.Storage.Core;

namespace SmartCraftStorage.Storage.UI
{
    public sealed class TerminalCell
    {
        public string Identity { get; }
        public string Name { get; }
        public int Total { get; }
        public int StackAmount { get; }
        public TerminalCell(string identity, string name, int total, int maxStack)
        { Identity = identity; Name = name; Total = total; StackAmount = Math.Min(Math.Max(0, total), Math.Max(1, maxStack)); }
    }

    // State shared by native gestures and bulk commands; it never owns real items.
    public sealed class TerminalInteractionModel
    {
        public IReadOnlyList<TerminalCell> Cells { get; private set; } = Array.Empty<TerminalCell>();
        public StorageOperation Operation { get; private set; }
        private readonly Queue<TerminalTransfer> _queue = new Queue<TerminalTransfer>();
        private bool _closed, _submitting;
        public bool Busy => _submitting || (Operation != null && !Operation.IsFinal && Operation.Status != StorageOperationStatus.Unavailable);
        public bool HasQueued => _queue.Count > 0;
        public void Enqueue(IEnumerable<TerminalTransfer> transfers)
        {
            if (_closed || Busy) return;
            _queue.Clear(); Operation = null;
            foreach (var transfer in transfers) _queue.Enqueue(transfer);
        }
        public bool SubmitNext(Func<TerminalTransfer, StorageOperation> submit)
        {
            if (_closed || Busy || _queue.Count == 0) return false;
            if (Operation != null && Operation.Status != StorageOperationStatus.Confirmed) { _queue.Clear(); return false; }
            var next = _queue.Dequeue();
            return Submit(() => submit(next));
        }
        public bool Submit(Func<StorageOperation> submit)
        {
            if (_closed || Busy) return false;
            _submitting = true;
            try { Operation = submit(); return true; }
            finally { _submitting = false; }
        }
        public void Observe(StorageOperation operation)
        {
            if (operation == null || (Operation != null && operation.Id != Operation.Id)) return;
            if (Operation != null && operation.Status == StorageOperationStatus.Unavailable) return;
            Operation = StorageEffects.AcceptStatus(Operation, operation);
            if (Operation.IsFinal && Operation.Status != StorageOperationStatus.Confirmed) _queue.Clear();
        }
        public void Close() { _closed = true; _queue.Clear(); }
        public bool Refresh(IEnumerable<TerminalCell> cells, string filter, bool byAmount, bool interacting)
        {
            if (Busy || interacting) return false;
            var visible = cells.Where(x => x.Total > 0 && x.Name.IndexOf((filter ?? "").Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);
            Cells = (byAmount ? visible.OrderByDescending(x => x.Total).ThenBy(x => x.Name) : visible.OrderBy(x => x.Name))
                .ThenBy(x => x.Identity, StringComparer.Ordinal).ToList();
            return true;
        }
    }

    public sealed class TerminalTransfer
    {
        public string Identity { get; }
        public int Amount { get; }
        public StorageStack Deposit { get; }
        public TerminalTransfer(string identity, int amount, StorageStack deposit = null)
        { Identity = identity; Amount = amount; Deposit = deposit; }
    }
}
