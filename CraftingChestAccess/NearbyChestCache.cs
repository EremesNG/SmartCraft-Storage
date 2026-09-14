using System.Collections.Generic;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.CraftingChestAccess
{
    /// <summary>
    /// Vanilla calls Inventory.CountItems/HaveItem many times per frame while the
    /// crafting panel or the build piece list is open (once per resource type, and
    /// once per piece when the build menu first opens or switches category). Without
    /// this, every one of those calls re-ran a full nearby-chest physics scan, which
    /// could add up to a real per-frame cost and stall the host long enough to
    /// disconnect other players. Scoped to this namespace only: other features
    /// (stations, animal feeder) call NearbyContainers.Find directly on their own
    /// slow ticks and never hit this problem.
    /// </summary>
    internal static class NearbyChestCache
    {
        private const float CacheDuration = 0.1f;

        private static float _cacheTime = -1f;
        private static List<Container> _cachedContainers;

        public static List<Container> Get(Vector3 origin, float radius, Player player)
        {
            if (_cachedContainers != null && Time.time - _cacheTime < CacheDuration)
            {
                return _cachedContainers;
            }

            _cachedContainers = new List<Container>(NearbyContainers.Find(origin, radius, player));
            _cacheTime = Time.time;
            return _cachedContainers;
        }

        /// <summary>
        /// Called right after RemoveItemPatch actually consumes something from a
        /// chest, so a second craft/build in the same cache window sees the real,
        /// just-updated amount instead of what was cached before the consumption.
        /// </summary>
        public static void Invalidate()
        {
            _cacheTime = -1f;
        }
    }
}
