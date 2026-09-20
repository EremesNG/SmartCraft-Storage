using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SmartCraftStorage.Storage.Runtime
{
    internal static class StorageAccess
    {
        private static readonly FieldInfo AreasField = typeof(PrivateArea).GetField("m_allAreas", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo EnabledMethod = typeof(PrivateArea).GetMethod("IsEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo InsideMethod = typeof(PrivateArea).GetMethod("IsInside", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo PermittedMethod = typeof(PrivateArea).GetMethod("IsPermitted", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool CanUse(Container container, Player actor)
        {
            return actor != null && CanUse(container, actor.GetPlayerID());
        }

        internal static bool CanUse(Container container, long actorId)
        {
            if (container == null || container.GetInventory() == null || container.m_nview == null || !container.m_nview.IsValid()) return false;
            if (container.IsInUse() || container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1) return false;
            var creator = container.GetComponent<Piece>()?.GetCreator() ?? 0L;
            if (container.m_privacy == Container.PrivacySetting.Private && creator != actorId) return false;
            if (container.m_privacy == Container.PrivacySetting.Group) return false;
            return !container.m_checkGuardStone || HasWardAccess(container.transform.position, actorId);
        }

        internal static bool HasWardAccess(Vector3 position, long actorId)
        {
            if (AreasField?.GetValue(null) is not IEnumerable areas) return false;
            var found = false;
            foreach (var value in areas)
            {
                if (!(value is PrivateArea area)) continue;
                var enabled = (bool)EnabledMethod.Invoke(area, null);
                var inside = (bool)InsideMethod.Invoke(area, new object[] { position, 0f });
                if (!enabled || !inside) continue;
                found = true;
                var creator = area.GetComponent<Piece>()?.GetCreator() ?? 0L;
                if (creator == actorId || (bool)PermittedMethod.Invoke(area, new object[] { actorId })) return true;
            }
            return !found;
        }
    }
}
