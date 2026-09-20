using System;
using System.Collections.Generic;

namespace SmartCraftStorage.Storage.Core
{
    // Transient timing only: delaying a retry never removes durable work or custody.
    public sealed class StorageRetrySchedule
    {
        private readonly Dictionary<string, Attempt> _attempts = new Dictionary<string, Attempt>(StringComparer.Ordinal);

        public bool TryBegin(string operationId, double now)
        {
            if (string.IsNullOrEmpty(operationId) || double.IsNaN(now) || double.IsInfinity(now)) return false;
            if (!_attempts.TryGetValue(operationId, out var attempt))
                _attempts[operationId] = attempt = new Attempt();
            else if (now < attempt.Due) return false;
            attempt.Due = now + attempt.Delay;
            attempt.Delay = Math.Min(8d, attempt.Delay * 2d);
            return true;
        }

        public void Progress(string operationId) => Forget(operationId);
        public void Forget(string operationId) { if (operationId != null) _attempts.Remove(operationId); }
        public void Clear() => _attempts.Clear();

        private sealed class Attempt
        {
            internal double Due;
            internal double Delay = 0.5d;
        }
    }
}
