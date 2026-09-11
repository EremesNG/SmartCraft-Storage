using System.Collections.Generic;
using UnityEngine;

namespace SmartCraftStorage.Shared
{
    internal static class NearbyContainers
    {
        public static IEnumerable<Container> Find(Vector3 origin, float radius)
        {
            var result = new List<Container>();
            var hits = Physics.OverlapSphere(origin, radius);

            foreach (var hit in hits)
            {
                var container = hit.GetComponentInParent<Container>();
                if (container == null)
                {
                    continue;
                }
                if (container.IsInUse())
                {
                    continue;
                }
                if (!PrivateArea.CheckAccess(container.transform.position, 0f, false))
                {
                    continue;
                }
                if (!result.Contains(container))
                {
                    result.Add(container);
                }
            }

            return result;
        }
    }
}
