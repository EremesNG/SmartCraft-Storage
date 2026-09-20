using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SmartCraftStorage.Hotkeys;
using SmartCraftStorage.Storage.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Run ONLY in a separate headless runtime. No world, character or real inventory is loaded.
[BepInPlugin("smartcraft.tests.nativegrid", "SmartCraft Native Grid Harness", "1.0.0")]
public sealed class NativeGridHarness : BaseUnityPlugin
{
    private string _report;
    private int _failures;
    private Action _afterCanvasUpdate;
    private static CachedNativeLayout _cachedNativeLayout;

    private void Awake()
    {
        _report = Path.Combine(Paths.GameRootPath, "native-grid-results.txt");
        File.WriteAllText(_report, "START\n");
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            _failures++;
            File.AppendAllText(_report, "ENGINE ERROR " + message + "\n" + trace + "\n");
        };
        try
        {
            var harmony = new Harmony("smartcraft.tests.nativegrid");
            harmony.Patch(AccessTools.Method(typeof(FejdStartup), "Awake"), prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(SkipStartup)));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("PlatformInitializer"), "InitializePlatform"),
                prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(SkipStartup)));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("EntryPointSceneLoader"), "Start"),
                prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(SkipStartup)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(ZInput), "pointerPosition"), prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(PointerPosition)));
            harmony.Patch(AccessTools.Method(typeof(InventoryGrid), "UpdateGui"),
                prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(ReplayNativeLayout)));
            harmony.Patch(AccessTools.Method(typeof(ZInput), "GetKey", new[] { typeof(KeyCode), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(NoInput)));
            harmony.Patch(AccessTools.Method(typeof(ZInput), "GetButton"),
                prefix: new HarmonyMethod(typeof(NativeGridHarness), nameof(NoInput)));
            ZInput.m_instance = (ZInput)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ZInput));
            var eventSystem = new GameObject("TestEventSystem");
            eventSystem.AddComponent<EventSystem>(); DontDestroyOnLoad(eventSystem);
            CheckGrid();
            CheckHotkeys();
        }
        catch (Exception error)
        {
            _failures++;
            File.AppendAllText(_report, "ERROR " + error + "\n");
        }
        if (_afterCanvasUpdate != null && _failures == 0) StartCoroutine(CheckAfterCanvasUpdate());
        else Finish();
    }

    private IEnumerator CheckAfterCanvasUpdate()
    {
        // GraphicRaycaster needs a real Canvas update; in Awake every depth is -1.
        yield return null;
        yield return null;
        try { _afterCanvasUpdate(); }
        catch (Exception error) { _failures++; File.AppendAllText(_report, "ERROR " + error + "\n"); }
        Finish();
    }
    private void Finish()
    {
        File.AppendAllText(_report, "DONE failures=" + _failures + "\n");
        Application.Quit(_failures == 0 ? 0 : 1);
    }

    private void CheckGrid()
    {
        var canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
        DontDestroyOnLoad(canvas);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var panel = Rect(canvas.transform, "Container");
        panel.sizeDelta = new Vector2(600f, 418f);
        var viewport = Rect(panel, "ContainerGrid");
        viewport.gameObject.SetActive(false);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(16f, 102f); viewport.offsetMax = new Vector2(-24f, -106f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var grid = viewport.gameObject.AddComponent<InventoryGrid>();
        grid.m_uiGroup = viewport.gameObject.AddComponent<UIGroupHandler>();
        grid.m_uiGroup.m_active = false;
        grid.CanDropDragOntoItem = _ => false;
        grid.m_elementSpace = 70f;
        grid.m_gridRoot = Rect(viewport, "Root");
        grid.m_gridRoot.anchorMin = Vector2.zero; grid.m_gridRoot.anchorMax = Vector2.one;
        grid.m_gridRoot.pivot = new Vector2(0f, .5f); grid.m_gridRoot.sizeDelta = Vector2.zero;
        var prototypes = new GameObject("Prototypes"); prototypes.SetActive(false); DontDestroyOnLoad(prototypes);
        grid.m_elementPrefab = MakeElement(prototypes.transform);
        grid.m_width = grid.m_height = 0;
        viewport.gameObject.SetActive(true);
        var inventory = new Inventory("Projection", null, 5, 2);
        grid.UpdateInventory(inventory, null, null);
        grid.ResetView(); Canvas.ForceUpdateCanvases();
        File.AppendAllText(_report, $"control width={viewport.rect.width}; height={viewport.rect.height}; elements={grid.m_elements.Count}; visible={VisibleCells(grid, viewport)}\n");
        Assert(grid.m_elements.Count == 10 && VisibleCells(grid, viewport) == 10, "control: native inventory creates ten visible cells");

        var panelState = new NativeTerminalLayout.RectState(panel);
        var viewportState = new NativeTerminalLayout.RectState(viewport);
        var rootState = new NativeTerminalLayout.RectState(grid.m_gridRoot);
        float width = NativeTerminalLayout.Configure(panel, viewport, 5, 70f);
        grid.m_width = grid.m_height = 0;
        grid.UpdateInventory(inventory, null, null); grid.ResetView(); Canvas.ForceUpdateCanvases();
        Assert(VisibleCells(grid, viewport) == 10, "empty terminal exposes native cells without other layout mods");
        // Valheim Plus' installed LayoutContainerScrollbar restores a cached panel width
        // and a right inset on this very same viewport before native UpdateGui runs.
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
        viewport.offsetMax = new Vector2(-24f, viewport.offsetMax.y);
        grid.m_width = grid.m_height = 0;
        grid.UpdateInventory(inventory, null, null);
        grid.ResetView(); Canvas.ForceUpdateCanvases();
        File.AppendAllText(_report, $"viewport width={viewport.rect.width}; height={viewport.rect.height}; elements={grid.m_elements.Count}; visible={VisibleCells(grid, viewport)}; toolbarWidth={width}\n");
        Assert(viewport.rect.width >= 350f && VisibleCells(grid, viewport) == 10,
            "terminal cells stay visible after an external native right-inset update");

        var icon = Sprite.Create(Texture2D.whiteTexture, new UnityEngine.Rect(0, 0, 1, 1), Vector2.zero);
        for (int index = 0; index < 4; index++)
            inventory.GetAllItems().Add(new ItemDrop.ItemData
            {
                m_stack = 20 + index, m_gridPos = new Vector2i(index, 0),
                m_shared = new ItemDrop.ItemData.SharedData
                { m_name = "Stored item " + index, m_maxStackSize = 50, m_teleportable = true, m_icons = new[] { icon } }
            });
        grid.UpdateInventory(inventory, null, null); Canvas.ForceUpdateCanvases();
        Assert(grid.m_elements.Count(cell => cell.m_used && cell.m_icon.enabled && cell.m_icon.sprite == icon) == 4 &&
            grid.m_elements[0].m_amount.text.Contains("20"), "populated terminal draws four native item icons and stack counts");

        var scrollbarParent = Rect(panel, "Original scrollbar parent");
        var scrollbar = Rect(scrollbarParent, "Scrollbar");
        scrollbar.anchorMin = Vector2.zero; scrollbar.anchorMax = Vector2.one;
        scrollbar.offsetMin = new Vector2(570, 30); scrollbar.offsetMax = new Vector2(-12, -60);
        var scrollbarState = new NativeTerminalLayout.RectState(scrollbar);
        var originalBarSize = scrollbar.sizeDelta;
        NativeTerminalLayout.Configure(panel, viewport, 8, 70f, scrollbar);
        grid.m_width = grid.m_height = 0;
        grid.UpdateInventory(new Inventory("Compact terminal", null, 8, 6), null, null);
        grid.ResetView(); Canvas.ForceUpdateCanvases();
        File.AppendAllText(_report, $"compact width={viewport.rect.width}; height={viewport.rect.height}; visible={VisibleCells(grid, viewport)}\n");
        Assert(VisibleCells(grid, viewport) == 32,
            "compact terminal displays eight columns and four complete rows without widening side gutters");
        var lastTopRow = Bounds((RectTransform)grid.m_elements[31].transform);
        Assert(Bounds(viewport).Contains(lastTopRow.min) && Bounds(viewport).Contains(lastTopRow.max),
            "the eighth cell in the fourth row is fully visible");
        var footer = Rect(panel, "Actions");
        footer.anchorMin = footer.anchorMax = footer.pivot = Vector2.up;
        footer.anchoredPosition = new Vector2(NativeTerminalLayout.Padding, -NativeTerminalLayout.FooterTop(70f));
        footer.sizeDelta = new Vector2(panel.rect.width - NativeTerminalLayout.Padding * 2, NativeTerminalLayout.ButtonHeight);
        Assert(!Bounds(scrollbar).Overlaps(Bounds(footer)) && !Bounds(viewport).Overlaps(Bounds(footer)) &&
            Mathf.Approximately(Bounds(scrollbar).yMin, Bounds(viewport).yMin) &&
            Mathf.Approximately(Bounds(scrollbar).yMax, Bounds(viewport).yMax) &&
            Bounds(scrollbar).xMin >= Bounds(viewport).xMax,
            "scrollbar stays beside the four rows and above the action buttons");
        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = grid.m_gridRoot;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.verticalNormalizedPosition = 0f; Canvas.ForceUpdateCanvases();
        var lastCell = Bounds((RectTransform)grid.m_elements[47].transform);
        Assert(Bounds(viewport).Contains(lastCell.min) && Bounds(viewport).Contains(lastCell.max),
            "native scrolling reaches the last row without the footer covering it");
        var scrolledPosition = grid.m_gridRoot.anchoredPosition;
        for (int frame = 0; frame < 30; frame++)
        {
            viewport.offsetMax = new Vector2(-24f, viewport.offsetMax.y);
            NativeTerminalLayout.ArrangeGrid(panel, grid, 8);
        }
        var firstCellRect = (RectTransform)grid.m_elements[0].transform;
        Assert(Mathf.Approximately(firstCellRect.anchoredPosition.x, 0f) &&
            grid.m_gridRoot.anchoredPosition == scrolledPosition,
            "repeated external inset updates neither drift columns nor reset scrolling");
        scroll.enabled = false;

        panelState.Restore(); viewportState.Restore(); rootState.Restore(); scrollbarState.Restore();
        Assert(scrollbar.parent == scrollbarParent && scrollbar.sizeDelta == originalBarSize &&
            scrollbar.anchorMin == Vector2.zero && scrollbar.anchorMax == Vector2.one,
            "closing restores the native scrollbar parent, anchors and size");
        grid.m_width = grid.m_height = 0;
        grid.UpdateInventory(new Inventory("Ordinary chest", null, 5, 3), null, null);
        grid.ResetView(); Canvas.ForceUpdateCanvases();
        Assert(Mathf.Approximately(panel.rect.width, 600f) && viewport.anchorMin == Vector2.zero &&
            viewport.anchorMax == Vector2.one && Mathf.Approximately(viewport.rect.width, 560f) &&
            grid.m_elements.Count == 15 && VisibleCells(grid, viewport) == 15,
            "restoring the terminal layout leaves all ordinary chest cells visible");

        // Reproduce first-use caching from the inspected Valheim Plus hook. The
        // previous test only replayed a right inset and missed this lifetime.
        scrollbar.SetParent(panel, false);
        scrollbar.anchorMin = scrollbar.anchorMax = scrollbar.pivot = new Vector2(.5f, 1f);
        scrollbar.anchoredPosition = new Vector2(284f, -106f);
        scrollbar.sizeDelta = new Vector2(12f, 210f);
        var nativeBarState = new NativeTerminalLayout.RectState(scrollbar);
        var ordinaryInventory = grid.GetInventory();
        _cachedNativeLayout = new CachedNativeLayout(panel, viewport, scrollbar, grid);
        grid.m_inventory = null; // Terminal is the first container view of this game UI.
        NativeTerminalLayout.InitializeNativeLayout(grid);
        NativeTerminalLayout.Configure(panel, viewport, 8, 70f, scrollbar);
        grid.UpdateInventory(new Inventory("First terminal", null, 8, 4), null, null);
        panelState.Restore(); viewportState.Restore(); rootState.Restore(); nativeBarState.Restore();
        grid.m_width = grid.m_height = 0;
        grid.UpdateInventory(ordinaryInventory, null, null); grid.ResetView(); Canvas.ForceUpdateCanvases();
        File.AppendAllText(_report, $"after first terminal: panel={panel.rect.width}; scrollbar X={scrollbar.anchoredPosition.x}\n");
        Assert(Mathf.Approximately(panel.rect.width, 600f) && Mathf.Approximately(scrollbar.anchoredPosition.x, 284f),
            "opening the terminal first preserves the ordinary chest's cached scrollbar baseline");
        for (int opening = 0; opening < 3; opening++)
        {
            NativeTerminalLayout.InitializeNativeLayout(grid);
            NativeTerminalLayout.Configure(panel, viewport, 8, 70f, scrollbar);
            grid.UpdateInventory(new Inventory("Repeated terminal", null, 8, 6), null, null);
            panelState.Restore(); viewportState.Restore(); rootState.Restore(); nativeBarState.Restore();
            grid.m_width = grid.m_height = 0;
            grid.UpdateInventory(ordinaryInventory, null, null);
        }
        Assert(Mathf.Approximately(panel.rect.width, 600f) && Mathf.Approximately(scrollbar.anchoredPosition.x, 284f) &&
            Mathf.Approximately(viewport.offsetMax.x, -24f),
            "repeated terminal/chest alternation preserves native cached coordinates");
        _cachedNativeLayout = null;

        var grouped = new Inventory("30 occupied physical slots, 16 grouped entries", null, 8, 2);
        for (int index = 0; index < 16; index++)
            grouped.GetAllItems().Add(new ItemDrop.ItemData
            {
                m_stack = 1, m_gridPos = new Vector2i(index % 8, index / 8),
                m_shared = new ItemDrop.ItemData.SharedData
                { m_name = "Group " + index, m_maxStackSize = 50, m_teleportable = true, m_icons = new[] { icon } }
            });
        grid.UpdateInventory(grouped, null, null);
        NativeTerminalLayout.ArrangeGrid(panel, grid, 8); grid.ResetView(); Canvas.ForceUpdateCanvases();
        Assert(Mathf.Approximately(viewport.rect.height, 140f) && VisibleFaces(grid, viewport) == 16,
            "sixteen grouped entries use two rows without extra empty rows");
        grouped.GetAllItems().RemoveAt(15);
        grid.UpdateInventory(grouped, null, null);
        NativeTerminalLayout.ArrangeGrid(panel, grid, 8); Canvas.ForceUpdateCanvases();
        Assert(VisibleFaces(grid, viewport) == 15,
            "rectangular padding does not display an empty capacity cell");
        grid.UpdateInventory(new Inventory("Empty search result", null, 8, 1), null, null);
        NativeTerminalLayout.ArrangeGrid(panel, grid, 8); grid.ResetView(); Canvas.ForceUpdateCanvases();
        Assert(Mathf.Approximately(viewport.rect.height, 70f) && VisibleFaces(grid, viewport) == 0,
            "an empty network or search keeps one deposit row without visible empty cells");
        var emptyCell = grid.m_elements[0];
        var raycaster = canvas.AddComponent<GraphicRaycaster>();
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current)
        { position = Bounds((RectTransform)emptyCell.transform).center };
        _afterCanvasUpdate = () =>
        {
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            File.AppendAllText(_report, $"padding raycast: hits={hits.Count}; depth={emptyCell.m_button.targetGraphic.depth}; pointer={pointer.position}; screen={Screen.width}x{Screen.height}\n");
            Assert(hits.Any(hit => hit.gameObject == emptyCell.gameObject),
                "an invisible padding cell remains a real native pointer target");
            bool selectedEmptyCell = false;
            grid.m_onSelected = (source, item, position, modifier) =>
                selectedEmptyCell = source == grid && item == null && position == new Vector2i(0, 0) && modifier == InventoryGrid.Modifier.Select;
            var input = emptyCell.GetComponent<UIInputHandler>();
            input.m_onLeftDown(input);
            Assert(selectedEmptyCell, "clicking invisible padding dispatches the native deposit selection callback");
            grid.m_onSelected = null;
            grid.GetInventory().GetAllItems().Add(grouped.GetAllItems()[0]);
            grid.UpdateInventory(grid.GetInventory(), null, null);
            NativeTerminalLayout.ArrangeGrid(panel, grid, 8); Canvas.ForceUpdateCanvases();
            Assert(VisibleFaces(grid, viewport) == 1,
                "a previously empty cell becomes visible when a deposited item appears");
            panelState.Restore(); viewportState.Restore(); rootState.Restore(); nativeBarState.Restore();
            grid.m_width = grid.m_height = 0;
            grid.UpdateInventory(ordinaryInventory, null, null); grid.ResetView(); Canvas.ForceUpdateCanvases();
            Assert(VisibleFaces(grid, viewport) == 15,
                "ordinary chests retain their visible empty slots after terminal padding was hidden");
        };
    }

    private static void ReplayNativeLayout(InventoryGrid __instance) => _cachedNativeLayout?.Apply(__instance);
    private sealed class CachedNativeLayout
    {
        private readonly RectTransform _panel, _viewport, _scrollbar;
        private readonly InventoryGrid _grid;
        private bool _initialized;
        private float _width, _inset, _barX;
        internal CachedNativeLayout(RectTransform panel, RectTransform viewport, RectTransform scrollbar, InventoryGrid grid)
        { _panel = panel; _viewport = viewport; _scrollbar = scrollbar; _grid = grid; }
        internal void Apply(InventoryGrid grid)
        {
            if (grid != _grid) return;
            if (!_initialized)
            {
                _width = _panel.sizeDelta.x; _inset = _viewport.offsetMax.x; _barX = _scrollbar.anchoredPosition.x;
                _initialized = true;
            }
            float required = grid.GetInventory().GetWidth() * grid.m_elementSpace + 24f + _scrollbar.sizeDelta.x;
            bool expanded = required > _width;
            _panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, expanded ? required : _width);
            _viewport.offsetMax = new Vector2(expanded ? -_scrollbar.sizeDelta.x - 8f : _inset, _viewport.offsetMax.y);
            _scrollbar.anchoredPosition = new Vector2(expanded ? required / 2f - 8f - _scrollbar.sizeDelta.x / 2f : _barX,
                _scrollbar.anchoredPosition.y);
        }
    }

    private void CheckHotkeys()
    {
        // Each run uses new files inside the isolated fixture, never the user's configuration.
        var directory = Path.Combine(Paths.GameRootPath, "shortcut-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var fresh = new ConfigFile(Path.Combine(directory, "fresh.cfg"), true);
        HotkeyConfig.Bind(fresh);
        Assert(HotkeyConfig.NetworkNameShortcut.Value.Equals(new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt)),
            "new configuration defaults to Alt+N");
        foreach (var shortcut in new[] { new KeyboardShortcut(KeyCode.T, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.K, KeyCode.LeftControl), KeyboardShortcut.Empty })
        {
            var path = Path.Combine(directory, "existing-" + shortcut.MainKey + ".cfg");
            var old = new ConfigFile(path, true);
            old.Bind("Hotkeys", "NetworkNameShortcut", shortcut, "Existing choice"); old.Save();
            var upgraded = new ConfigFile(path, true);
            HotkeyConfig.Bind(upgraded);
            var expected = shortcut.MainKey == KeyCode.T ? new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt) : shortcut;
            Assert(HotkeyConfig.NetworkNameShortcut.Value.Equals(expected),
                "upgrade migrates only the previous default: " + shortcut);
            HotkeyConfig.NetworkNameShortcut.Value = new KeyboardShortcut(KeyCode.T, KeyCode.LeftAlt);
            upgraded.Save();
            HotkeyConfig.Bind(new ConfigFile(path, true));
            Assert(HotkeyConfig.NetworkNameShortcut.Value.Equals(new KeyboardShortcut(KeyCode.T, KeyCode.LeftAlt)),
                "a later explicit Alt+T choice persists across reload: " + shortcut);
        }
    }

    private static int VisibleCells(InventoryGrid grid, RectTransform viewport)
    {
        var view = Bounds(viewport);
        return grid.m_elements.Count(element => element.gameObject.activeInHierarchy && view.Overlaps(Bounds((RectTransform)element.transform)));
    }
    private static int VisibleFaces(InventoryGrid grid, RectTransform viewport) => grid.m_elements.Count(element =>
        element.gameObject.activeInHierarchy && Bounds(viewport).Overlaps(Bounds((RectTransform)element.transform)) &&
        (element.GetComponent<CanvasGroup>() == null || element.GetComponent<CanvasGroup>().alpha > 0f));
    private static UnityEngine.Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return new UnityEngine.Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
    }
    private void Assert(bool condition, string message)
    { if (!condition) _failures++; File.AppendAllText(_report, (condition ? "PASS " : "FAIL ") + message + "\n"); }
    private static bool SkipStartup() => false;
    private static bool NoInput(ref bool __result) { __result = false; return false; }
    private static bool PointerPosition(ref Vector3 __result) { __result = new Vector3(-10000, -10000, 0); return false; }

    private static RectTransform Rect(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }
    private static GameObject MakeElement(Transform parent)
    {
        var rect = Rect(parent, "InventoryElement");
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up; rect.sizeDelta = new Vector2(64f, 64f);
        var obj = rect.gameObject; var element = obj.AddComponent<InventoryElement>();
        var background = obj.AddComponent<Image>();
        element.m_button = obj.AddComponent<Button>(); element.m_button.targetGraphic = background; element.m_touchRect = rect;
        obj.AddComponent<UIInputHandler>(); obj.AddComponent<UIDragHandler>();
        element.m_icon = Image(rect, "icon"); element.m_equiped = Image(rect, "equipped");
        element.m_queued = Image(rect, "queued"); element.m_noteleport = Image(rect, "noteleport");
        element.m_food = Image(rect, "food"); element.m_dropFocus = Image(rect, "dropFocus");
        element.m_selected = Image(rect, "selected").gameObject;
        element.m_amount = Text(rect, "amount"); element.m_quality = Text(rect, "quality"); Text(rect, "binding");
        element.m_durability = Image(rect, "durability").gameObject.AddComponent<GuiBar>();
        element.m_durability.m_bar = (RectTransform)element.m_durability.transform;
        element.m_tooltip = obj.AddComponent<UITooltip>();
        return obj;
    }
    private static Image Image(Transform parent, string name)
    {
        var rect = Rect(parent, name);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<Image>(); image.raycastTarget = false; return image;
    }
    private static TMP_Text Text(Transform parent, string name) => Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
}
