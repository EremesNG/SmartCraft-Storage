using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartCraftStorage.Storage.Core;
using UnityEngine;

namespace SmartCraftStorage.Storage.Runtime
{
    internal sealed class StorageDiscovery
    {
        private Collider[] _hits = new Collider[512];
        private readonly Dictionary<string, IReadOnlyList<Collider>> _sweepCache = new Dictionary<string, IReadOnlyList<Collider>>(StringComparer.Ordinal);
        private int _cacheFrame = -1;
        internal string LastUnavailableReason { get; private set; }

        internal IReadOnlyList<Container> Find(StorageContext context, StorageSettings settings)
        {
            if (context?.Actor == null) return Array.Empty<Container>();
            LastUnavailableReason = "";
            var result = new Dictionary<string, Container>(StringComparer.Ordinal);
            if (context.Scope == StorageScope.Direct && context.Anchor != null)
            {
                var directTarget = context.Anchor.GetComponent<Container>();
                var selection = StorageDiscoveryPolicy.SelectDirect(new[] { Candidate(directTarget, context.Actor, context.Actor.transform.position) }, context.Radius);
                return (context.Actor.transform.position - context.Origin).sqrMagnitude <= 4f && selection.Ids.Count == 1
                    ? new[] { directTarget } : Array.Empty<Container>();
            }
            if (context.Scope == StorageScope.Terminal)
            {
                if (!settings.Enabled) return Array.Empty<Container>();
                ExpandTerminal(context.Anchor, context.Actor, settings, result);
                return Complete(result, settings.MaxMembers);
            }

            var colliders = Sweep(context.Origin, context.Radius);
            var directContainers = UniqueContainers(colliders);
            var direct = StorageDiscoveryPolicy.SelectDirect(directContainers.Values.Select(x => Candidate(x, context.Actor, context.Origin)), context.Radius);
            foreach (var id in direct.Ids) result[id] = directContainers[id];
            if (settings.Enabled && context.Scope != StorageScope.Direct)
            {
                var terminals = colliders.Select(x => x.GetComponentInParent<ZNetView>()).Where(IsTerminal)
                    .GroupBy(Id, StringComparer.Ordinal).Select(x => x.First())
                    .OrderBy(x => (x.transform.position - context.Origin).sqrMagnitude).ThenBy(Id, StringComparer.Ordinal).ToList();
                foreach (var terminal in terminals) ExpandTerminal(terminal, context.Actor, settings, result);
            }
            return Complete(result, settings.MaxMembers)
                .OrderBy(x => (x.transform.position - context.Origin).sqrMagnitude).ThenBy(x => Id(x.m_nview), StringComparer.Ordinal).ToList();
        }

        private IReadOnlyList<Container> Complete(IDictionary<string, Container> result, int maxMembers)
        {
            var projection = StorageDiscoveryPolicy.CompleteProjection(result.Keys, maxMembers);
            if (projection.LimitExceeded) LastUnavailableReason = "Storage projection exceeds configured member limit";
            return LastUnavailableReason.Length == 0
                ? projection.Ids.Select(id => result[id]).ToList() : (IReadOnlyList<Container>)Array.Empty<Container>();
        }

        internal IReadOnlyList<ZNetView> FindTerminalProofs(StorageContext context, StorageSettings settings, IReadOnlyList<Container> members)
        {
            if (context == null || context.Scope == StorageScope.Direct || !settings.Enabled) return Array.Empty<ZNetView>();
            if (context.Scope == StorageScope.Terminal)
                return IsTerminal(context.Anchor) ? new[] { context.Anchor } : Array.Empty<ZNetView>();
            var terminals = Sweep(context.Origin, context.Radius).Select(x => x.GetComponentInParent<ZNetView>()).Where(IsTerminal)
                .GroupBy(Id, StringComparer.Ordinal).Select(x => x.First()).OrderBy(Id, StringComparer.Ordinal).ToList();
            var proofs = new Dictionary<string, ZNetView>(StringComparer.Ordinal);
            foreach (var member in members)
            {
                if ((member.transform.position - context.Origin).sqrMagnitude <= context.Radius * context.Radius) continue;
                var name = Normalize(member.m_nview.GetZDO().GetString(StorageFacade.NetworkNameKey, ""));
                var proof = terminals.FirstOrDefault(terminal => name.Length != 0 &&
                    Normalize(terminal.GetZDO().GetString(StorageFacade.NetworkNameKey, "")) == name &&
                    (member.transform.position - terminal.transform.position).sqrMagnitude <= settings.TerminalRadius * settings.TerminalRadius &&
                    StorageAccess.HasWardAccess(terminal.transform.position, context.Actor.GetPlayerID()));
                if (proof != null) proofs[Id(proof)] = proof;
            }
            return proofs.Values.ToList();
        }

        private void ExpandTerminal(ZNetView terminal, Player actor, StorageSettings settings, IDictionary<string, Container> result)
        {
            if (!IsTerminal(terminal)) return;
            var name = Normalize(terminal.GetZDO().GetString(StorageFacade.NetworkNameKey, ""));
            if (!StorageDiscoveryPolicy.CanExpandTerminal(IsTerminal(terminal), StorageAccess.HasWardAccess(terminal.transform.position, actor.GetPlayerID()), name)) return;
            var containers = UniqueContainers(Sweep(terminal.transform.position, settings.TerminalRadius));
            var selection = StorageDiscoveryPolicy.SelectBacking(containers.Values.Select(x => Candidate(x, actor, terminal.transform.position)), name,
                settings.TerminalRadius, settings.MaxMembers);
            if (selection.LimitExceeded) { LastUnavailableReason = "Network exceeds configured member limit"; return; }
            foreach (var id in selection.Ids) result[id] = containers[id];
        }

        private static Dictionary<string, Container> UniqueContainers(IEnumerable<Collider> colliders) =>
            colliders.Select(x => x.GetComponentInParent<Container>()).Where(x => x != null && x.m_nview != null && x.m_nview.IsValid())
                .GroupBy(x => Id(x.m_nview), StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

        internal static bool EligibleBacking(Container container, Player actor)
        {
            var candidate = Candidate(container, actor, container != null ? container.transform.position : Vector3.zero);
            return StorageDiscoveryPolicy.SelectBacking(new[] { candidate }, candidate.NetworkName, 0f, 1).Ids.Count == 1;
        }

        private static StorageDiscoveryCandidate Candidate(Container container, Player actor, Vector3 origin)
        {
            if (container == null || container.m_nview == null || !container.m_nview.IsValid())
                return new StorageDiscoveryCandidate("", "", float.MaxValue, false, false, false, false, false, false);
            var zdo = container.m_nview.GetZDO(); var piece = container.GetComponent<Piece>();
            var busy = !string.IsNullOrEmpty(zdo.GetString("scs.storage.active.v1", "")) ||
                       !string.IsNullOrEmpty(zdo.GetString("scs.storage.effect.active.v1", ""));
            return new StorageDiscoveryCandidate(Id(container.m_nview), Normalize(zdo.GetString(StorageFacade.NetworkNameKey, "")),
                (container.transform.position - origin).sqrMagnitude, StorageAccess.CanUse(container, actor), busy,
                IsTerminal(container.m_nview), container.GetComponent<TombStone>() != null,
                container.GetComponentInParent<Ship>() != null || container.GetComponentInParent<Vagon>() != null,
                piece != null && piece.GetCreator() != 0L,
                !container.GetComponents<Component>().Any(x => x != null && x.GetType().FullName == "BottomlessChest.Core.BottomlessContainer"));
        }

        private IReadOnlyList<Collider> Sweep(Vector3 origin, float radius)
        {
            if (_cacheFrame != Time.frameCount) { _sweepCache.Clear(); _cacheFrame = Time.frameCount; }
            var key = origin.x.ToString("R", CultureInfo.InvariantCulture) + "|" + origin.y.ToString("R", CultureInfo.InvariantCulture) + "|" +
                origin.z.ToString("R", CultureInfo.InvariantCulture) + "|" + radius.ToString("R", CultureInfo.InvariantCulture);
            if (_sweepCache.TryGetValue(key, out var cached)) return cached;
            while (true)
            {
                var count = Physics.OverlapSphereNonAlloc(origin, radius, _hits, ~0);
                if (count < _hits.Length || _hits.Length >= 8192)
                {
                    var snapshot = new Collider[count]; Array.Copy(_hits, snapshot, count);
                    if (_sweepCache.Count < 64) _sweepCache[key] = snapshot;
                    return snapshot;
                }
                _hits = new Collider[Math.Min(_hits.Length * 2, 8192)];
            }
        }
        internal static string Normalize(string name) => (name ?? string.Empty).Trim().ToLowerInvariant();
        internal static string Id(ZNetView view) => view.GetZDO().m_uid.ToString();
        private static bool IsTerminal(ZNetView view) => view != null && view.IsValid() && view.GetZDO().GetBool(StorageFacade.TerminalMarkerKey, false);
    }
}
