using System;
using System.Collections.Generic;
using System.Linq;
using SmartCraftStorage.Config;
using SmartCraftStorage.ItemMarking;
using SmartCraftStorage.Shared;
using SmartCraftStorage.Storage.Runtime;
using SmartCraftStorage.Storage.UI;

namespace SmartCraftStorage.Storage.Integration
{
    // Shift+E and restock still address only ordinary chests inside their old
    // radius. The common transport supplies owner coordination, not network reach.
    internal static class DirectPlayerTransfers
    {
        private const string PendingKey = "scs.direct.pending.v1";
        private static readonly Queue<Func<StorageOperation>> Work = new Queue<Func<StorageOperation>>();
        private static Player _actor;
        private static string _pending;
        private static bool _restock;
        private static int _moved;

        internal static void Start(Player player, bool restock)
        {
            if (player == null) return;
            if (_pending != null || Work.Count > 0 || StorageFacade.Service.IsBusy(player.m_nview))
            { player.Message(MessageHud.MessageType.Center, TerminalTranslations.Text("pending")); return; }
            if (player.m_customData.TryGetValue(PendingKey, out var previous))
            {
                var operation = StorageFacade.Service.GetOperation(previous);
                if (!operation.IsFinal)
                {
                    _actor = player; _pending = previous; _restock = restock; _moved = 0;
                    StorageFacade.Service.Resume(previous);
                    player.Message(MessageHud.MessageType.Center, TerminalTranslations.Text("recovering"));
                    return;
                }
                player.m_customData.Remove(PendingKey);
            }
            _actor = player; _restock = restock; _moved = 0;
            var context = Context(player);
            if (restock)
            {
                var names = RestockList.GetAll(player);
                if (names.Count == 0) { Message("$smartcraft_restock_no_items_marked"); _actor = null; return; }
                foreach (string name in names)
                    foreach (var row in StorageFacade.Service.Query(context).Rows.Where(row => row.Sample.m_shared.m_name == name))
                    {
                        string identity = row.Identity;
                        int maxStack = row.Sample.m_shared.m_maxStackSize;
                        Work.Enqueue(() =>
                        {
                            int owned = player.GetInventory().GetAllItems().Where(item => item.m_shared.m_name == name).Sum(item => item.m_stack);
                            int need = maxStack - owned;
                            return need <= 0 ? null : Submit(id => StorageFacade.Service.Withdraw(Context(player), identity, need, id));
                        });
                    }
            }
            else
            {
                var containers = new List<Container>(NearbyContainers.Find(player.transform.position, ModConfig.QuickStackRadius.Value, player));
                if (containers.Count == 0) { Message("$smartcraft_no_chest_nearby"); _actor = null; return; }
                foreach (var carried in player.GetInventory().GetAllItems().ToArray())
                {
                    if (carried.m_equipped || ItemFlags.IsLocked(carried)) continue;
                    string identity = GameInventoryAdapter.Identity(carried);
                    var slot = carried.m_gridPos;
                    foreach (var container in containers)
                    {
                        Work.Enqueue(() =>
                        {
                            var item = player.GetInventory().GetAllItems().FirstOrDefault(candidate => candidate.m_gridPos == slot &&
                                GameInventoryAdapter.Identity(candidate) == identity);
                            if (item == null || item.m_equipped || ItemFlags.IsLocked(item) || container == null ||
                                !container.GetInventory().GetAllItems().Any(candidate => candidate.m_shared.m_name == item.m_shared.m_name &&
                                    candidate.m_quality == item.m_quality)) return null;
                            return Submit(id => StorageFacade.Service.Deposit(Context(player, container.m_nview), item, item.m_stack, id));
                        });
                    }
                }
            }
            Tick();
        }

        internal static void Tick()
        {
            if (_actor == null || _actor != Player.m_localPlayer)
            { Work.Clear(); _actor = null; _pending = null; return; }
            if (_pending != null)
            {
                var result = StorageFacade.Service.GetOperation(_pending);
                if (!result.IsFinal) { StorageFacade.Service.Resume(_pending); return; }
                if (result.Status == StorageOperationStatus.Confirmed) _moved += result.Accepted;
                _actor.m_customData.Remove(PendingKey);
                _pending = null;
            }
            if (_actor.IsDead()) Work.Clear();
            if (StorageFacade.Service.IsBusy(_actor.m_nview)) return;
            // Bound submissions per frame; pending work resumes without resubmitting.
            for (int attempts = 0; attempts < 8 && Work.Count > 0; attempts++)
            {
                var operation = Work.Dequeue()();
                if (operation == null) continue;
                _pending = operation.Id;
                return;
            }
            if (Work.Count > 0) return;
            Message(_restock
                ? (_moved > 0 ? "$smartcraft_restock_success" : "$smartcraft_restock_nothing")
                : (_moved > 0 ? "$smartcraft_quickstack_success" : "$smartcraft_quickstack_nothing"));
            _actor = null;
        }

        private static StorageContext Context(Player player, ZNetView chest = null) =>
            new StorageContext(player, player.transform.position, ModConfig.QuickStackRadius.Value, StorageScope.Direct, chest);

        private static StorageOperation Submit(Func<string, StorageOperation> submit)
        {
            string id = StorageFacade.NewPlayerOperation(_actor, "direct");
            _actor.m_customData[PendingKey] = id;
            var result = submit(id);
            if (string.IsNullOrEmpty(result.Id)) { _actor.m_customData.Remove(PendingKey); return null; }
            return result;
        }

        private static void Message(string token) =>
            _actor?.Message(MessageHud.MessageType.Center, Localization.instance.Localize(token, _moved.ToString()));
    }
}
