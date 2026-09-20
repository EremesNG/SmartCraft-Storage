// External inventory boundary: temporary Load returns bare data and drops cheated,
// as Inventory.AddTempItem does in the inspected Valheim assembly. This fake is not
// a replacement SCS serializer: all new payload encoding/hydration runs in production.
using System;
using System.Collections.Generic;
using System.Linq;
public struct Vector2i { public int x, y; public Vector2i(int x, int y) { this.x = x; this.y = y; } public static Vector2i zero => default; }
namespace UnityEngine
{
    public sealed class GameObject { public string name; public ItemDrop Item; public T GetComponent<T>() where T : class => Item as T; }
}
public sealed class ItemDrop
{
    public ItemData m_itemData;
    public enum ItemType { Material }
    public sealed class SharedData { public string m_name; public int m_maxStackSize; public ItemType m_itemType; public string[] m_icons; }
    public sealed class ItemData
    {
        public SharedData m_shared;
        public UnityEngine.GameObject m_dropPrefab;
        public int m_stack, m_quality, m_variant;
        public byte m_worldLevel;
        public float m_durability;
        public long m_crafterID;
        public string m_crafterName = "";
        public bool m_cheated, m_pickedUp, m_equipped;
        public Vector2i m_gridPos;
        public Dictionary<string, string> m_customData = new();
        public ItemData Clone() { var value = (ItemData)MemberwiseClone(); value.m_customData = new(m_customData); return value; }
    }
}
public sealed class ObjectDB
{
    public static ObjectDB instance = new();
    public readonly Dictionary<string, UnityEngine.GameObject> Items = new();
    public UnityEngine.GameObject GetItemPrefab(string name) => Items.TryGetValue(name, out var item) ? item : null;
}
public sealed class ZPackage
{
    public byte[] Bytes;
    public ZPackage() { }
    public ZPackage(byte[] bytes) => Bytes = bytes;
    public byte[] GetArray() => Bytes;
}
public sealed class Inventory
{
    private readonly bool temporary;
    private readonly int width = 1, height = 1;
    private readonly List<ItemDrop.ItemData> items = new();
    public Inventory(bool value) => temporary = true;
    public Inventory(string name, object background, int w, int h) { width = w; height = h; }
    public List<ItemDrop.ItemData> GetAllItems() => items;
    public int GetWidth() => width;
    public int GetHeight() => height;
    public void Changed() { }
    public void AddItem(ItemDrop.ItemData item) => items.Add(item);
    public void Save(ZPackage package) => package.Bytes = System.Text.Encoding.UTF8.GetBytes(items.Single().m_dropPrefab.name);
    public void Load(ZPackage package)
    {
        var prefab = ObjectDB.instance.GetItemPrefab(System.Text.Encoding.UTF8.GetString(package.Bytes));
        var item = temporary ? new ItemDrop.ItemData() : prefab.Item.m_itemData.Clone();
        item.m_dropPrefab = prefab;
        items.Add(item);
    }
}
