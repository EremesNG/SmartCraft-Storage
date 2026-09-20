using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SmartCraftStorage.Storage.Core
{
    public sealed class StorageDiscoveryCandidate
    {
        public string Id { get; }
        public string NetworkName { get; }
        public float DistanceSquared { get; }
        public bool Accessible { get; }
        public bool Busy { get; }
        public bool IsTerminal { get; }
        public bool IsTombstone { get; }
        public bool IsMobile { get; }
        public bool IsCreatedPiece { get; }
        public bool IsSupportedBacking { get; }

        public StorageDiscoveryCandidate(string id, string networkName, float distanceSquared, bool accessible,
            bool busy, bool isTerminal, bool isTombstone, bool isMobile, bool isCreatedPiece, bool isSupportedBacking = true)
        {
            Id = id ?? string.Empty; NetworkName = networkName ?? string.Empty; DistanceSquared = distanceSquared;
            Accessible = accessible; Busy = busy; IsTerminal = isTerminal; IsTombstone = isTombstone;
            IsMobile = isMobile; IsCreatedPiece = isCreatedPiece;
            IsSupportedBacking = isSupportedBacking;
        }
    }

    public sealed class StorageDiscoverySelection
    {
        public IReadOnlyList<string> Ids { get; }
        public bool LimitExceeded { get; }
        internal StorageDiscoverySelection(IEnumerable<string> ids, bool limitExceeded)
        { Ids = new ReadOnlyCollection<string>(ids.ToList()); LimitExceeded = limitExceeded; }
    }

    public static class StorageDiscoveryPolicy
    {
        public static StorageDiscoverySelection CompleteProjection(IEnumerable<string> physicalIds, int maxMembers)
        {
            var ids = physicalIds.Distinct(StringComparer.Ordinal).Take(Math.Max(0, maxMembers) + 1).ToList();
            return ids.Count > Math.Max(0, maxMembers)
                ? new StorageDiscoverySelection(Array.Empty<string>(), true)
                : new StorageDiscoverySelection(ids, false);
        }

        public static bool CanExpandTerminal(bool isTerminal, bool wardAllowed, string normalizedName) =>
            isTerminal && wardAllowed && !string.IsNullOrEmpty(normalizedName);

        public static StorageDiscoverySelection SelectDirect(IEnumerable<StorageDiscoveryCandidate> candidates, float radius)
        {
            var limit = Math.Max(0f, radius); var ids = Eligible(candidates)
                .Where(x => x.DistanceSquared <= limit * limit)
                .OrderBy(x => x.DistanceSquared).ThenBy(x => x.Id, StringComparer.Ordinal).Select(x => x.Id);
            return new StorageDiscoverySelection(ids, false);
        }

        public static StorageDiscoverySelection SelectBacking(IEnumerable<StorageDiscoveryCandidate> candidates,
            string normalizedName, float radius, int maxMembers)
        {
            var limit = Math.Max(0f, radius);
            var eligible = Eligible(candidates).Where(x => !x.IsMobile && x.IsCreatedPiece && x.IsSupportedBacking &&
                    string.Equals(x.NetworkName, normalizedName, StringComparison.Ordinal) && x.DistanceSquared <= limit * limit)
                .OrderBy(x => x.DistanceSquared).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
            if (eligible.Count > Math.Max(0, maxMembers)) return new StorageDiscoverySelection(Array.Empty<string>(), true);
            return new StorageDiscoverySelection(eligible.Select(x => x.Id), false);
        }

        private static IEnumerable<StorageDiscoveryCandidate> Eligible(IEnumerable<StorageDiscoveryCandidate> candidates) =>
            (candidates ?? Enumerable.Empty<StorageDiscoveryCandidate>()).Where(x => x != null && x.Accessible && !x.Busy && !x.IsTerminal && !x.IsTombstone)
                .GroupBy(x => x.Id, StringComparer.Ordinal).Select(x => x.First());
    }
}
