using System;
using System.Collections.Generic;
using UnityEngine;

namespace BepInEx.Bootstrap
{
    public static class Chainloader { public static readonly Dictionary<string, object> PluginInfos = new Dictionary<string, object>(); }
}

namespace BepInEx.Configuration
{
    public readonly struct KeyboardShortcut
    {
        public KeyCode MainKey { get; }
        public IEnumerable<KeyCode> Modifiers { get; }
        public KeyboardShortcut(KeyCode mainKey, params KeyCode[] modifiers) { MainKey = mainKey; Modifiers = modifiers; }
    }
}

namespace SmartCraftStorage
{
    internal static class Plugin { public const string PluginGuid = "test"; }
}

namespace SmartCraftStorage.Hotkeys
{
    using BepInEx.Configuration;
    internal sealed class Setting<T> { public T Value; public Setting(T value) { Value = value; } }
    internal static class HotkeyConfig
    {
        public static readonly Setting<KeyboardShortcut> LockClickShortcut = new Setting<KeyboardShortcut>(new KeyboardShortcut(KeyCode.LeftAlt));
        public static readonly Setting<KeyboardShortcut> RestockMarkClickShortcut = new Setting<KeyboardShortcut>(new KeyboardShortcut(KeyCode.LeftControl));
    }
}

namespace SmartCraftStorage.ItemMarking
{
    internal static class RestockList { public static void Toggle(Player player, string name) { } }
}

public static class ZInput { public static KeyCode Held; public static bool GetKey(KeyCode key) => key == Held; }
public struct Vector2i { public int x, y; public Vector2i(int x, int y) { this.x = x; this.y = y; } }
public sealed class UIInputHandler { public readonly GameObject gameObject = new GameObject(); }
public sealed class InventoryGrid
{
    public readonly Inventory m_inventory;
    private readonly ItemDrop.ItemData _item;
    public InventoryGrid(Inventory inventory, ItemDrop.ItemData item) { m_inventory = inventory; _item = item; }
    public Vector2i GetButtonPos(GameObject gameObject) => new Vector2i();
    public ItemDrop.ItemData GetItemAt(int x, int y) => _item;
}

public static class InventoryGridExtensions { }
