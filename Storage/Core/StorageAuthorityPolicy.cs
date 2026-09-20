using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartCraftStorage.Storage.Core
{
    public sealed class StorageAuthorityNode
    {
        public string Id { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public bool Terminal { get; }
        public string Network { get; }
        public StorageAuthorityNode(string id, float x, float y, float z, bool terminal = false, string network = "")
        { Id = id ?? ""; X = x; Y = y; Z = z; Terminal = terminal; Network = network ?? ""; }
    }

    public static class StorageAuthorityPolicy
    {
        public static bool Validate(StorageScope scope, bool enabled, float configuredRadius, int maxMembers,
            float actorX, float actorY, float actorZ, float originX, float originY, float originZ, float requestedRadius,
            StorageAuthorityNode anchor, IReadOnlyList<StorageAuthorityNode> members, IReadOnlyList<StorageAuthorityNode> proofs)
        {
            members = members ?? Array.Empty<StorageAuthorityNode>(); proofs = proofs ?? Array.Empty<StorageAuthorityNode>();
            if (!Enum.IsDefined(typeof(StorageScope), scope) || configuredRadius < 0f || requestedRadius < 0f ||
                requestedRadius > configuredRadius || members.Count > maxMembers || members.Any(x => x == null || string.IsNullOrEmpty(x.Id))) return false;
            if (scope == StorageScope.Terminal && (!enabled || anchor == null || !anchor.Terminal || DistanceSquared(actorX, actorY, actorZ, anchor) > 100f)) return false;
            if ((scope == StorageScope.Crafting || scope == StorageScope.Direct) &&
                DistanceSquared(actorX, actorY, actorZ, originX, originY, originZ) > 4f) return false;
            if (scope == StorageScope.Processor && (anchor == null || DistanceSquared(originX, originY, originZ, anchor) > 4f)) return false;
            var terminals = proofs.Where(x => x != null && x.Terminal).GroupBy(x => x.Id, StringComparer.Ordinal).Select(x => x.First()).ToList();
            if (scope == StorageScope.Terminal && !terminals.Any(x => x.Id == anchor.Id)) return false;
            foreach (var member in members)
            {
                var direct = DistanceSquared(originX, originY, originZ, member) <= requestedRadius * requestedRadius;
                var viaTerminal = terminals.Any(terminal => enabled &&
                    (scope == StorageScope.Terminal ? terminal.Id == anchor.Id : DistanceSquared(originX, originY, originZ, terminal) <= requestedRadius * requestedRadius) &&
                    !string.IsNullOrEmpty(terminal.Network) && terminal.Network == member.Network &&
                    DistanceSquared(terminal.X, terminal.Y, terminal.Z, member) <= configuredRadius * configuredRadius);
                if (scope == StorageScope.Terminal ? !viaTerminal : !direct && !viaTerminal) return false;
            }
            return true;
        }

        private static float DistanceSquared(float x, float y, float z, StorageAuthorityNode node) => DistanceSquared(x, y, z, node.X, node.Y, node.Z);
        private static float DistanceSquared(float ax, float ay, float az, float bx, float by, float bz)
        { var x = ax - bx; var y = ay - by; var z = az - bz; return x * x + y * y + z * z; }
    }
}
