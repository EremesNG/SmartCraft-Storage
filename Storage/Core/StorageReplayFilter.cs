using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SmartCraftStorage.Storage.Core
{
    // Exact bounded replay rejection for monotonic operation streams.
    public sealed class StorageReplayFilter
    {
        private readonly Dictionary<string, long> _highWater = new Dictionary<string, long>(StringComparer.Ordinal);

        public StorageReplayFilter(int ignoredCapacity) { }

        public StorageReplayFilter(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return;
            using (var reader = new BinaryReader(new MemoryStream(Convert.FromBase64String(encoded))))
            {
                var count = reader.ReadInt32(); if (count < 0 || count > 65536) throw new InvalidDataException("Invalid replay watermark count");
                for (var i = 0; i < count; i++) _highWater[reader.ReadString()] = reader.ReadInt64();
            }
        }

        public bool CanRemember(string operationId) => TrySplit(operationId, out _, out _);

        public void Remember(string operationId)
        {
            if (!TrySplit(operationId, out var stream, out var sequence)) throw new ArgumentException("Operation ID has no monotonic sequence", nameof(operationId));
            if (!_highWater.TryGetValue(stream, out var current) || sequence > current) _highWater[stream] = sequence;
        }

        public bool Contains(string operationId)
        {
            return TrySplit(operationId, out var stream, out var sequence) && _highWater.TryGetValue(stream, out var retired) && sequence <= retired;
        }

        public string Export()
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(_highWater.Count);
                foreach (var value in _highWater.OrderBy(x => x.Key, StringComparer.Ordinal)) { writer.Write(value.Key); writer.Write(value.Value); }
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private static bool TrySplit(string value, out string stream, out long sequence)
        {
            stream = string.Empty; sequence = 0; value = value ?? string.Empty; var separator = value.LastIndexOf(':');
            if (separator <= 0 || separator == value.Length - 1 || !long.TryParse(value.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out sequence) || sequence < 0) return false;
            stream = value.Substring(0, separator); return true;
        }
    }
}
