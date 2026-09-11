using System.Collections.Generic;
using UnityEngine;

namespace SmartCraftStorage.Shared
{
    internal static class NearbyContainers
    {
        public static IEnumerable<Container> Find(Vector3 origin, float radius, Player player)
        {
            var result = new List<Container>();
            var hits = Physics.OverlapSphere(origin, radius);
            long playerId = player.GetPlayerID();

            foreach (var hit in hits)
            {
                var container = hit.GetComponentInParent<Container>();
                if (container == null || container.GetInventory() == null)
                {
                    continue;
                }
                if (container.GetComponent<TombStone>() != null)
                {
                    continue;
                }
                if (IsInUseByAnyone(container))
                {
                    continue;
                }
                if (!container.CheckAccess(playerId))
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

        public static bool TryClaimWriteAccess(Container container)
        {
            if (container.m_nview == null || !container.m_nview.IsValid())
            {
                return false;
            }
            if (container.IsOwner())
            {
                return true;
            }
            container.m_nview.ClaimOwnership();
            return container.IsOwner();
        }

        private static bool IsInUseByAnyone(Container container)
        {
            if (container.IsInUse())
            {
                return true;
            }
            return container.m_nview != null && container.m_nview.IsValid()
                && container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
        }
    }
}
