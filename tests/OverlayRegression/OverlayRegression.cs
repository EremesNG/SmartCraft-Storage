using System;
using System.IO;
using System.Runtime.Serialization;
using BepInEx;
using HarmonyLib;
using SmartCraftStorage.ItemMarking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Only loaded by scripts/test-overlays.ps1 in a separate Unity process.
[BepInPlugin("smartcraft.tests.overlays", "SmartCraft Overlay Regression", "1.0.0")]
public sealed class OverlayRegression : BaseUnityPlugin
{
    private string _report;

    private void Awake()
    {
        _report = Path.Combine(Paths.GameRootPath, "overlay-results.txt");
        File.WriteAllText(_report, "START\n");
        try
        {
            Run();
            File.AppendAllText(_report, "DONE\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(_report, "ERROR " + ex + "\n");
        }
        finally
        {
            Player.m_localPlayer = null;
            Application.Quit();
        }
    }

    private void Run()
    {
        // Keep the platform input boundary offline. The actual InventoryGrid,
        // Harmony patch, Inventory, Images and RectTransforms run inside Unity.
        ZInput.m_instance = (ZInput)FormatterServices.GetUninitializedObject(typeof(ZInput));
        var harmony = new Harmony("smartcraft.tests.overlays");
        harmony.Patch(AccessTools.PropertyGetter(typeof(ZInput), "pointerPosition"),
            prefix: new HarmonyMethod(typeof(OverlayRegression), nameof(PointerPosition)));
        harmony.CreateClassProcessor(typeof(SlotOverlayPatch)).Patch();
        if (EventSystem.current == null)
            new GameObject("TestEventSystem").AddComponent<EventSystem>();

        var playerObject = new GameObject("TestPlayer");
        playerObject.SetActive(false); // Do not start gameplay/network callbacks.
        Player.m_localPlayer = playerObject.AddComponent<Player>();
        var inventory = new Inventory("Test", null, 2, 2);
        var grid = MakeGrid(2, 2);
        var root = grid.gameObject;

        var item = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData() };
        item.m_shared.m_name = "TestArrow";
        item.m_shared.m_teleportable = true;
        item.m_shared.m_icons = new[] { Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero) };
        item.m_gridPos = new Vector2i(0, 0);
        inventory.m_inventory.Add(item);
        ItemFlags.SetLocked(item, true);
        RestockList.Toggle(Player.m_localPlayer, "TestArrow");
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 2, "Both marks visible before move");

        // Shield Me Bruh's shield/excluded decorations clone m_icon this way.
        // Image.enabled=false leaves any copied child Images visible.
        foreach (string name in new[] { "shield", "excluded" })
        {
            var decoration = UnityEngine.Object.Instantiate(grid.m_elements[0].m_icon, grid.m_elements[0].transform);
            decoration.name = name;
            decoration.enabled = false;
            Assert(VisibleMarks(decoration) == 0, name + " decoration does not copy marks");
        }

        item.m_gridPos = new Vector2i(0, 1);
        var extraSlot = grid.m_elements[2];
        // ExtraSlots uses inventory rows beyond the regular cells and relocates
        // those same InventoryElement roots into its equipment panel.
        ((RectTransform)extraSlot.transform).anchoredPosition = new Vector2(300, 0);
        grid.UpdateInventory(inventory, null, null);
        Canvas.ForceUpdateCanvases();
        Assert(VisibleMarks(grid.m_elements[0]) == 0, "Moving to ExtraSlots clears the old cell");
        Assert(VisibleMarks(extraSlot) == 2, "Both marks follow the item to ExtraSlots");
        root.SetActive(false);
        root.SetActive(true);
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 0 && VisibleMarks(extraSlot) == 2,
            "Reopening keeps only the occupied extra slot marked");

        foreach (float scale in new[] { 0.2f, 0.75f, 2f })
        {
            extraSlot.m_icon.transform.localScale = Vector3.one * scale;
            Canvas.ForceUpdateCanvases();
            Assert(MarksFitSlot(extraSlot), "Marks fit the slot at icon scale " + scale);
        }
        Assert(MarksPreserveInputAndLabels(extraSlot), "Marks allow clicks and stay behind stack text");

        var secondStack = new ItemDrop.ItemData { m_shared = item.m_shared, m_gridPos = new Vector2i(1, 0) };
        inventory.m_inventory.Add(secondStack);
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[1]) == 1 && VisibleMarks(extraSlot) == 2,
            "Restock applies by name and lock applies only to the marked stack");
        ItemFlags.SetLocked(item, false);
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(extraSlot) == 1, "Unlocking the extra-slot item preserves restock");
        RestockList.Toggle(Player.m_localPlayer, "TestArrow");
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(extraSlot) == 0 && VisibleMarks(grid.m_elements[1]) == 0,
            "Removing restock clears both panels");
        ItemFlags.SetLocked(item, true);
        RestockList.Toggle(Player.m_localPlayer, "TestArrow");
        item.m_gridPos = new Vector2i(0, 0);
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 2 && VisibleMarks(extraSlot) == 0,
            "Moving back to regular inventory clears the extra slot");

        CheckFoodMove(item.m_shared.m_icons);
    }

    private void CheckFoodMove(Sprite[] icons)
    {
        var grid = MakeGrid(2, 1); // Two regular cells, no extra slots.
        var inventory = new Inventory("Food", null, 2, 1);
        var food = new ItemDrop.ItemData
        {
            m_gridPos = new Vector2i(0, 0),
            m_shared = new ItemDrop.ItemData.SharedData
            {
                m_name = "TestFood",
                m_itemType = ItemDrop.ItemData.ItemType.Consumable,
                m_food = 40f,
                m_foodStamina = 10f,
                m_teleportable = true,
                m_icons = icons
            }
        };
        inventory.m_inventory.Add(food);
        RestockList.Toggle(Player.m_localPlayer, "TestFood");
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 1, "Food shows only its restock mark");
        var decoration = UnityEngine.Object.Instantiate(grid.m_elements[0].m_icon, grid.m_elements[0].transform);
        decoration.name = "excluded";
        decoration.enabled = false;
        food.m_gridPos = new Vector2i(1, 0);
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 0 && VisibleMarks(grid.m_elements[1]) == 1,
            "Moving food within regular inventory clears the old restock dot");
        grid.gameObject.SetActive(false);
        grid.gameObject.SetActive(true);
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 0 && VisibleMarks(grid.m_elements[1]) == 1,
            "Reopening after a regular food move leaves no copied restock dot");
        RestockList.Toggle(Player.m_localPlayer, "TestFood");
        grid.UpdateInventory(inventory, null, null);
        Assert(VisibleMarks(grid.m_elements[0]) == 0 && VisibleMarks(grid.m_elements[1]) == 0,
            "Unmarking food leaves both regular cells clear");
    }

    private void Assert(bool condition, string name) =>
        File.AppendAllText(_report, (condition ? "PASS " : "FAIL ") + name + "\n");

    private static bool PointerPosition(ref Vector3 __result)
    {
        __result = new Vector3(10000, 10000, 0);
        return false;
    }

    private static bool IsMark(Image image) => image.name.StartsWith("SmartCraft_");

    private static int VisibleMarks(Component root)
    {
        int count = 0;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
            if (IsMark(image) && image.isActiveAndEnabled)
                count++;
        return count;
    }

    private static bool MarksFitSlot(InventoryElement element)
    {
        var slotCorners = new Vector3[4];
        var markCorners = new Vector3[4];
        ((RectTransform)element.transform).GetWorldCorners(slotCorners);
        foreach (var image in element.GetComponentsInChildren<Image>(true))
        {
            if (!IsMark(image)) continue;
            image.rectTransform.GetWorldCorners(markCorners);
            for (int i = 0; i < 4; i++)
                if (Vector3.Distance(slotCorners[i], markCorners[i]) > 0.01f)
                    return false;
        }
        return VisibleMarks(element) == 2;
    }

    private static bool MarksPreserveInputAndLabels(InventoryElement element)
    {
        foreach (var image in element.GetComponentsInChildren<Image>(true))
            if (IsMark(image) && (image.raycastTarget ||
                image.transform.GetSiblingIndex() >= element.m_amount.transform.GetSiblingIndex()))
                return false;
        return VisibleMarks(element) == 2;
    }

    private static InventoryGrid MakeGrid(int width, int height)
    {
        var root = new GameObject("TestGrid", typeof(RectTransform));
        root.SetActive(false);
        root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var grid = root.AddComponent<InventoryGrid>();
        grid.m_gridRoot = (RectTransform)root.transform;
        grid.m_uiGroup = root.AddComponent<UIGroupHandler>();
        grid.m_uiGroup.m_active = false;
        grid.m_width = width;
        grid.m_height = height;
        grid.CanDropDragOntoItem = _ => true;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            grid.m_elements.Add(MakeElement(root.transform, x, y));
        root.SetActive(true);
        return grid;
    }

    private static InventoryElement MakeElement(Transform parent, int x, int y)
    {
        var go = new GameObject("Slot" + x + y, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(64, 64);
        rect.anchoredPosition = new Vector2(x * 70, -y * 70);
        var element = go.AddComponent<InventoryElement>();
        element.m_button = go.AddComponent<Button>();
        element.m_touchRect = rect;
        element.m_icon = MakeImage(go.transform, "icon");
        element.m_equiped = MakeImage(go.transform, "equipped");
        element.m_queued = MakeImage(go.transform, "queued");
        element.m_noteleport = MakeImage(go.transform, "noteleport");
        element.m_food = MakeImage(go.transform, "food");
        element.m_dropFocus = MakeImage(go.transform, "dropFocus");
        element.m_selected = MakeImage(go.transform, "selected").gameObject;
        element.m_amount = MakeText(go.transform, "amount");
        element.m_quality = MakeText(go.transform, "quality");
        element.m_durability = MakeImage(go.transform, "durability").gameObject.AddComponent<GuiBar>();
        element.m_tooltip = go.AddComponent<UITooltip>();
        element.Initialize(x, y);
        return element;
    }

    private static Image MakeImage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(64, 64);
        return go.AddComponent<Image>();
    }

    private static TMP_Text MakeText(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<TextMeshProUGUI>();
    }
}
