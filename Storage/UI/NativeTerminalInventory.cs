using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SmartCraftStorage.ItemMarking;
using SmartCraftStorage.Storage.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SmartCraftStorage.Storage.UI.StorageTerminalUi;
using static SmartCraftStorage.Storage.UI.NativeTerminalLayout;

namespace SmartCraftStorage.Storage.UI
{
    internal sealed class NativeTerminalInventory : MonoBehaviour
    {
        private static NativeTerminalInventory _instance;
        private static readonly ConditionalWeakTable<Inventory, object> Projections = new ConditionalWeakTable<Inventory, object>();
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, object> ProjectedItems = new ConditionalWeakTable<ItemDrop.ItemData, object>();
        private readonly TerminalInteractionModel _model = new TerminalInteractionModel();
        private readonly Dictionary<ItemDrop.ItemData, TerminalCell> _cells = new Dictionary<ItemDrop.ItemData, TerminalCell>();
        private readonly Dictionary<ItemDrop.ItemData, string> _tooltips = new Dictionary<ItemDrop.ItemData, string>();
        private readonly List<RectState> _layout = new List<RectState>();
        private InventoryGui _gui;
        private Player _player;
        private ZNetView _target;
        private Inventory _projection, _previousInventory;
        private StorageView _view;
        private GameObject _toolbar;
        private InputField _search;
        private Text _capacity, _status;
        private Button _sort, _organize, _rename;
        private bool _sortByAmount, _closing;
        private bool _resetView = true;
        private bool _takeEnabled, _stackEnabled;
        private Vector2i _previousSelection;
        private int _previousGroup;
        private string _previousName, _previousWeight, _pendingKey, _operationKind;
        private TextAlignmentOptions _previousNameAlignment;
        private float _refreshAt, _networkWeight;
        private const int Columns = 8;

        internal static bool IsOpen => _instance != null;
        private static bool SearchHasFocus => _instance != null && !_instance._closing &&
            _instance._search != null && _instance._search.isActiveAndEnabled && _instance._search.isFocused;
        internal static bool IsProjection(Inventory inventory) => inventory != null && Projections.TryGetValue(inventory, out _);
        internal static bool IsProjectedItem(ItemDrop.ItemData item) => item != null && ProjectedItems.TryGetValue(item, out _);
        private StorageContext Context => new StorageContext(_player, _target.transform.position, 0f, StorageScope.Terminal, _target);
        private bool Interacting => _gui.m_dragGo != null || _gui.m_splitDialog.IsActive;
        private bool CanAct => !_closing && !_model.Busy && !_model.HasQueued && _view != null && _view.Available && !_player.IsTeleporting();

        internal static void Open(ZNetView target, Player player)
        {
            if (InventoryGui.instance == null || target == null || !target.IsValid() || player == null) return;
            CloseCurrent(false);
            var gui = InventoryGui.instance;
            gui.CloseContainer();
            gui.Show(null);
            var root = new GameObject("SCS native terminal", typeof(NativeTerminalInventory));
            root.transform.SetParent(gui.transform, false);
            var terminal = root.GetComponent<NativeTerminalInventory>();
            _instance = terminal;
            terminal._gui = gui; terminal._target = target; terminal._player = player;
            try { terminal.Initialize(); }
            catch (Exception error) { terminal.Fail(error); }
        }

        private void Initialize()
        {
            _pendingKey = "scs.terminal.pending.v2." + TargetKey(_target, _player);
            _previousInventory = _gui.m_containerGrid.GetInventory();
            _previousSelection = _gui.m_containerGrid.m_selected; _previousGroup = _gui.m_activeGroup;
            _previousName = _gui.m_containerName.text; _previousWeight = _gui.m_containerWeight.text;
            _previousNameAlignment = _gui.m_containerName.alignment;
            _takeEnabled = _gui.m_takeAllButton.interactable; _stackEnabled = _gui.m_stackAllButton.interactable;
            foreach (var rect in new[] { _gui.m_container, (RectTransform)_gui.m_containerGrid.transform,
                _gui.m_containerGrid.m_gridRoot, _gui.m_containerName.rectTransform,
                (RectTransform)_gui.m_takeAllButton.transform, (RectTransform)_gui.m_stackAllButton.transform })
                _layout.Add(new RectState(rect));
            if (_gui.m_containerGrid.m_scrollbar != null)
                _layout.Add(new RectState((RectTransform)_gui.m_containerGrid.m_scrollbar.transform));
            NativeTerminalLayout.InitializeNativeLayout(_gui.m_containerGrid);
            BuildToolbar();
            _gui.m_containerGrid.m_width = 0; _gui.m_containerGrid.m_height = 0;
            var pending = StorageFacade.Service.GetPendingOperation(_target, "");
            if (pending == null && _player.m_customData.TryGetValue(_pendingKey, out var id))
            {
                pending = StorageFacade.Service.GetOperation(id);
                if (pending.Status == StorageOperationStatus.Unavailable)
                {
                    pending = new StorageOperation(id, StorageOperationStatus.RecoveryPending);
                    StorageFacade.Service.Resume(id);
                }
            }
            _model.Observe(pending);
            _projection = new Inventory("SCS network", null, Columns, 1);
            Projections.Add(_projection, new object());
            Refresh();
            _gui.SetActiveGroup(0, false);
        }

        private void BuildToolbar()
        {
            float width = NativeTerminalLayout.Configure(_gui.m_container,
                (RectTransform)_gui.m_containerGrid.transform, Columns, _gui.m_containerGrid.m_elementSpace,
                _gui.m_containerGrid.m_scrollbar?.transform as RectTransform);
            float footer = FooterTop(_gui.m_containerGrid.m_elementSpace);
            float actionWidth = ActionWidth(width);
            Position(_gui.m_containerName.gameObject, Padding, 4, width - 138, 30);
            _gui.m_containerName.alignment = TextAlignmentOptions.MidlineLeft;
            _toolbar = new GameObject("SCS network tools", typeof(RectTransform));
            _toolbar.transform.SetParent(_gui.m_container, false);
            var rect = (RectTransform)_toolbar.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            _capacity = Label(rect, "", Padding, 35, width / 2 - Padding, 18, 14);
            _search = Input(rect, T("search"), Padding, 56, width - Padding * 2 - 144 - Gap, 28);
            _search.onValueChanged.AddListener(_ => { _refreshAt = 0f; _resetView = true; });
            _rename = MakeButton(rect, T("network"), width - Padding - 90, 6, 90, 27, () =>
            { if (CanAct && !Interacting) StorageTerminalUi.Open(_target, _player, true, true); });
            _sort = MakeButton(rect, T("sort_name"), width - Padding - 144, 56, 144, 28, () =>
            {
                if (!CanAct || Interacting) return;
                _sortByAmount = !_sortByAmount;
                _sort.GetComponentInChildren<Text>().text = T(_sortByAmount ? "sort_amount" : "sort_name");
                _refreshAt = 0f; _resetView = true;
            });
            _organize = MakeButton(rect, T("organize"), Padding + (actionWidth + Gap) * 2, footer, actionWidth, ButtonHeight, () =>
            { if (CanAct && !Interacting) BeginOperation(() => StorageFacade.Service.Organize(Context), "organize"); });
            PositionActions(width, MaxVisibleRows);
            _status = Label(rect, "", width / 2 + Gap, 35, width / 2 - Padding - Gap, 18, 14);
            _status.alignment = TextAnchor.UpperRight;
            _status.resizeTextForBestFit = true; _status.resizeTextMinSize = 10; _status.resizeTextMaxSize = 14;
            _gui.m_container.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            // Keep the native pane on screen at smaller window sizes.
            var corners = new Vector3[4]; _gui.m_container.GetWorldCorners(corners);
            float adjustment = corners[0].y < 12 ? 12 - corners[0].y : corners[1].y > Screen.height - 12 ? Screen.height - 12 - corners[1].y : 0;
            _gui.m_container.position += new Vector3(0f, adjustment, 0f);
        }

        private void PositionActions(float width, int rows)
        {
            float footer = FooterTop(_gui.m_containerGrid.m_elementSpace, rows);
            float actionWidth = ActionWidth(width);
            Position(_gui.m_takeAllButton.gameObject, Padding, footer, actionWidth, ButtonHeight);
            Position(_gui.m_stackAllButton.gameObject, Padding + actionWidth + Gap, footer, actionWidth, ButtonHeight);
            Position(_organize.gameObject, Padding + (actionWidth + Gap) * 2, footer, actionWidth, ButtonHeight);
        }

        private void Update()
        {
            if (_closing) return;
            try
            {
                if (_gui == null || _target == null || !_target.IsValid() || _player == null || _player.IsDead() ||
                    !_gui.m_animator.GetBool("visible") || _gui.m_currentContainer != null ||
                    Vector3.Distance(_player.transform.position, _target.transform.position) > 6f ||
                    !PrivateArea.CheckAccess(_target.transform.position, 0f, false))
                { Close(true); return; }
                if (Time.unscaledTime < _refreshAt) return;
                _refreshAt = Time.unscaledTime + .2f;
                if (_model.Busy && _model.Operation != null)
                {
                    var current = StorageFacade.Service.GetOperation(_model.Operation.Id);
                    if (current.Status == StorageOperationStatus.Unavailable) StorageFacade.Service.Resume(_model.Operation.Id);
                    else _model.Observe(current);
                }
                if (!Interacting && _model.SubmitNext(SubmitBulk)) RememberOperation();
                Refresh();
            }
            catch (Exception error) { Fail(error); }
        }

        private void Refresh()
        {
            // Hold the projection still during a click without disabling the control being pressed.
            bool frozen = _model.Busy || Interacting || ZInput.GetMouseButton(0);
            if (!frozen)
            {
                _view = StorageFacade.Service.Query(Context);
                // Count the complete network, including stacks hidden by the search filter.
                _networkWeight = _view.Rows.Sum(row => row.Sample.GetWeight(row.Amount));
                var rows = _view.Rows.ToDictionary(x => x.Identity, StringComparer.Ordinal);
                if (_model.Refresh(rows.Values.Select(x => new TerminalCell(x.Identity,
                    Localization.instance.Localize(x.Sample.m_shared.m_name), x.Amount, x.Sample.m_shared.m_maxStackSize)), _search.text, _sortByAmount, false))
                {
                    var projection = new Inventory("SCS network", null, Columns, Math.Max(1, (_model.Cells.Count + Columns - 1) / Columns));
                    if (_projection == null || _projection.GetHeight() != projection.GetHeight()) _resetView = true;
                    _cells.Clear(); _tooltips.Clear();
                    for (int index = 0; index < _model.Cells.Count; index++)
                    {
                        var cell = _model.Cells[index]; var item = rows[cell.Identity].Sample.Clone();
                        item.m_stack = cell.StackAmount; item.m_equipped = false; item.m_gridPos = new Vector2i(index % Columns, index / Columns);
                        projection.GetAllItems().Add(item); _cells[item] = cell;
                        _tooltips[item] = item.GetTooltip() + "\n\n" + T("stored_total", cell.Total.ToString("N0"));
                        ProjectedItems.Add(item, new object());
                    }
                    Projections.Add(projection, new object()); _projection = projection;
                }
                var name = StorageFacade.Service.GetNetworkName(_target);
                _gui.m_containerName.text = string.IsNullOrEmpty(name) ? T("title") : name.Replace("<", "").Replace(">", "");
                _capacity.text = T("capacity", _view.MemberIds.Count.ToString(), _view.UsedSlots.ToString(), _view.TotalSlots.ToString());
            }
            bool available = CanAct && !Interacting;
            _search.interactable = !_model.Busy && !Interacting;
            _sort.interactable = _organize.interactable = _gui.m_takeAllButton.interactable = _gui.m_stackAllButton.interactable = available;
            _rename.interactable = !_model.Busy && !_model.HasQueued && !Interacting;
            _status.text = _model.Operation != null ? OperationText(_model.Operation, _operationKind) :
                string.IsNullOrEmpty(StorageFacade.Service.GetNetworkName(_target)) ? T("no_network") :
                _view?.Available != true ? T("unavailable") : _model.Cells.Count == 0 ? T("empty") : "";
            RememberOperation();
        }

        private void Render()
        {
            if (_projection == null || _closing) return;
            _gui.m_container.gameObject.SetActive(true);
            _gui.m_containerGrid.UpdateInventory(_projection, null, _gui.m_dragItem);
            var grid = _gui.m_containerGrid;
            NativeTerminalLayout.ArrangeGrid(_gui.m_container, grid, Columns);
            PositionActions(_gui.m_container.rect.width, _projection.GetHeight());
            if (_resetView) { grid.ResetView(); _resetView = false; }
            foreach (var pair in _cells)
            {
                var element = _gui.m_containerGrid.GetElement(pair.Key.m_gridPos.x, pair.Key.m_gridPos.y, Columns);
                if (element == null) continue;
                element.m_amount.enabled = true; element.m_amount.text = pair.Value.Total.ToString("N0");
                element.m_tooltip.m_text = _tooltips[pair.Key];
            }
            _gui.m_containerWeight.text = Mathf.CeilToInt(_networkWeight).ToString();
        }

        // Return true only for gestures entirely inside the real player inventory.
        private bool Select(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier modifier)
        {
            bool network = grid == _gui.m_containerGrid;
            bool fromNetwork = IsProjection(_gui.m_dragInventory);
            bool player = grid != null && grid.GetInventory() == _player.GetInventory();
            if (!network && !fromNetwork && (!player || modifier != InventoryGrid.Modifier.Move || _gui.m_dragGo != null)) return true;
            if (!CanAct) return false;
            if (_gui.m_dragGo != null)
            {
                var dragged = _gui.m_dragItem; var amount = _gui.m_dragAmount;
                if (fromNetwork && player && _cells.TryGetValue(dragged, out var cell))
                {
                    var destination = _player.GetInventory();
                    var target = destination.GetItemAt(pos.x, pos.y);
                    CancelDrag();
                    if (pos.x < 0 || pos.y < 0 || pos.x >= destination.GetWidth() || pos.y >= destination.GetHeight() ||
                        (target != null && (Protected(target) || GameInventoryAdapter.Identity(target) != cell.Identity || target.m_stack >= target.m_shared.m_maxStackSize)))
                    { _status.text = T("incompatible_slot"); return false; }
                    BeginOperation(() => StorageFacade.Service.Withdraw(Context, cell.Identity, Math.Min(amount, cell.StackAmount),
                        destinationSlot: pos.y * destination.GetWidth() + pos.x), "withdraw");
                }
                else if (network && !fromNetwork && _gui.m_dragInventory == _player.GetInventory())
                { CancelDrag(); Deposit(dragged, amount); }
                else CancelDrag();
                return false;
            }
            if (item == null) return false;
            _gui.SetActiveGroup(grid.m_uiGroup, false);
            if (network && _cells.TryGetValue(item, out var selected))
            {
                switch (modifier)
                {
                    case InventoryGrid.Modifier.Move:
                        BeginOperation(() => StorageFacade.Service.Withdraw(Context, selected.Identity, selected.StackAmount), "withdraw"); break;
                    case InventoryGrid.Modifier.Drop: break;
                    case InventoryGrid.Modifier.Split:
                        if (item.m_stack > 1) _gui.ShowSplitDialog(item, _projection);
                        else _gui.SetupDragItem(item, _projection, item.m_stack);
                        break;
                    default: _gui.SetupDragItem(item, _projection, item.m_stack); break;
                }
            }
            else if (player && modifier == InventoryGrid.Modifier.Move) Deposit(item, item.m_stack);
            return false;
        }

        private void Deposit(ItemDrop.ItemData item, int amount)
        {
            if (item == null || Protected(item) || !_player.GetInventory().ContainsItem(item)) { _status.text = T("locked"); return; }
            BeginOperation(() => StorageFacade.Service.Deposit(Context, item, Math.Min(amount, item.m_stack)), "deposit");
        }
        private static bool Protected(ItemDrop.ItemData item) => item.m_equipped || item.m_shared.m_questItem || ItemFlags.IsLocked(item);
        private void BeginOperation(Func<StorageOperation> submit, string kind)
        {
            _operationKind = kind;
            if (_model.Submit(submit)) { RememberOperation(); _refreshAt = 0f; }
        }
        private void RememberOperation()
        {
            var operation = _model.Operation;
            if (operation == null || string.IsNullOrEmpty(operation.Id)) return;
            if (!operation.IsFinal && operation.Status != StorageOperationStatus.Unavailable) _player.m_customData[_pendingKey] = operation.Id;
            else if (_player.m_customData.TryGetValue(_pendingKey, out var id) && id == operation.Id) _player.m_customData.Remove(_pendingKey);
        }

        private void Bulk(bool deposit)
        {
            if (!CanAct || Interacting) return;
            var view = StorageFacade.Service.Query(Context); if (!view.Available) return;
            if (deposit)
            {
                var identities = new HashSet<string>(view.Rows.Select(x => x.Identity), StringComparer.Ordinal);
                var inventory = _player.GetInventory();
                _model.Enqueue(inventory.GetAllItems().Where(x => !Protected(x)).Select(x => GameInventoryAdapter.ToStack(x, x.m_gridPos.y * inventory.GetWidth() + x.m_gridPos.x))
                    .Where(x => identities.Contains(x.Identity)).Select(x => new TerminalTransfer(x.Identity, x.Amount, x)));
                _operationKind = "deposit";
            }
            else { _model.Enqueue(view.Rows.Select(x => new TerminalTransfer(x.Identity, x.Amount))); _operationKind = "withdraw"; }
            _refreshAt = 0f;
        }
        private StorageOperation SubmitBulk(TerminalTransfer transfer)
        {
            var view = StorageFacade.Service.Query(Context);
            if (!view.Available) return StorageOperation.Unavailable();
            if (transfer.Deposit == null)
            {
                var row = view.Rows.FirstOrDefault(x => x.Identity == transfer.Identity);
                return StorageFacade.Service.Withdraw(Context, transfer.Identity, Math.Min(transfer.Amount, row?.Amount ?? 0));
            }
            var inventory = _player.GetInventory();
            var current = GameInventoryAdapter.Snapshot("player", "", inventory);
            var selection = DepositSelection.Resolve(transfer.Deposit, current.Items);
            var item = selection == null ? null : inventory.GetItemAt(selection.Slot % current.Width, selection.Slot / current.Width);
            if (item == null || Protected(item)) return new StorageOperation("", StorageOperationStatus.Rejected);
            return StorageFacade.Service.Deposit(Context, item, selection.Amount);
        }

        private void CancelDrag() { _gui.SetupDragItem(null, null, 1); _gui.IsSplitDropping = false; }
        internal static void CloseCurrent(bool hide) { if (_instance != null) _instance.Close(hide); }
        private void Close(bool hide)
        {
            if (_closing) return;
            _closing = true; _model.Close();
            if (_instance == this) _instance = null;
            if (_gui != null)
            {
                CancelDrag(); _gui.HideSplitDialog(); _gui.m_splitItem = null; _gui.m_splitInventory = null;
                _gui.ClearTouchSelection();
                foreach (var rect in _layout) rect.Restore();
                _gui.m_containerGrid.m_inventory = _previousInventory;
                _gui.m_containerGrid.m_width = 0; _gui.m_containerGrid.m_height = 0;
                _gui.m_containerGrid.m_selected = _previousSelection;
                _gui.SetActiveGroup(_previousGroup, false);
                _gui.m_containerName.text = _previousName; _gui.m_containerWeight.text = _previousWeight;
                _gui.m_containerName.alignment = _previousNameAlignment;
                _gui.m_takeAllButton.interactable = _takeEnabled; _gui.m_stackAllButton.interactable = _stackEnabled;
                _gui.m_container.gameObject.SetActive(false);
                if (hide) _gui.Hide();
            }
            if (_toolbar != null) { _toolbar.SetActive(false); Destroy(_toolbar); }
            Destroy(gameObject);
        }
        private void OnDestroy() { if (!_closing) Close(false); }
        private void Fail(Exception error)
        {
            ZLog.LogWarning("[SmartCraft-Storage] Terminal closed after error: " + error);
            _player?.Message(MessageHud.MessageType.Center, T("unavailable"));
            Close(true);
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
        private static class ContainerRender
        {
            private static bool Prefix(InventoryGui __instance)
            {
                var terminal = _instance;
                if (terminal == null || terminal._gui != __instance) return true;
                try { terminal.Render(); } catch (Exception error) { terminal.Fail(error); }
                return false;
            }
        }
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsContainerOpen))]
        private static class ContainerOpen
        { private static void Postfix(ref bool __result) { if (_instance != null) __result = true; } }
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        private static class Selected
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod) =>
                _instance == null || _instance.Select(grid, item, pos, mod);
        }
        [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
        private static class RightClick
        { private static bool Prefix(InventoryGrid grid, ItemDrop.ItemData item) => !IsProjection(grid.GetInventory()) && !IsProjectedItem(item); }
        [HarmonyPatch(typeof(InventoryGui), "OnDropOutside")]
        private static class DropOutside
        {
            private static bool Prefix(InventoryGui __instance)
            {
                if (!IsProjection(__instance.m_dragInventory) && !IsProjectedItem(__instance.m_dragItem)) return true;
                __instance.SetupDragItem(null, null, 1); __instance.IsSplitDropping = false; return false;
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
        private static class TakeAll
        { private static bool Prefix() { if (_instance == null) return true; _instance.Bulk(false); return false; } }
        [HarmonyPatch(typeof(InventoryGui), "OnStackAll")]
        private static class StackAll
        { private static bool Prefix() { if (_instance == null) return true; _instance.Bulk(true); return false; } }
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static class Hide
        { private static void Prefix() => CloseCurrent(false); }
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        private static class Show
        { private static void Prefix() => CloseCurrent(false); }
        [HarmonyPatch(typeof(global::TextInput), nameof(global::TextInput.IsVisible))]
        private static class SearchInput
        {
            private static void Postfix(ref bool __result)
            {
                // The native character controller checks this separately from inventory visibility.
                if (SearchHasFocus) __result = true;
            }
        }
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        private static class TextInput
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix()
            {
                var terminal = _instance;
                if (!SearchHasFocus) return true;
                if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(KeyCode.Return))
                { terminal._search.DeactivateInputField(); EventSystem.current?.SetSelectedGameObject(null); }
                // Typing E, Tab or hotbar digits must not invoke native inventory shortcuts.
                try { terminal._gui.UpdateInventory(terminal._player); terminal.Render(); }
                catch (Exception error) { terminal.Fail(error); }
                return false;
            }
        }
    }
}
