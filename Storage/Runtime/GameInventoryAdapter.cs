using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SmartCraftStorage.Storage.Core;

namespace SmartCraftStorage.Storage.Runtime
{
    internal static class GameInventoryAdapter
    {
        private const string ItemPayloadPrefix = "scs-item:1:";
        internal static string Revision(Inventory inventory) => string.Join(";", inventory.GetAllItems()
            .OrderBy(x => x.m_gridPos.y).ThenBy(x => x.m_gridPos.x)
            .Select(x => Identity(x) + ":" + x.m_stack + "@" + x.m_gridPos.x + "," + x.m_gridPos.y));

        internal static StorageInventory Snapshot(string id, string revision, Inventory inventory)
        {
            var items = inventory.GetAllItems().Select(item => ToStack(item, Slot(item, inventory.GetWidth()))).ToList();
            return new StorageInventory(id, revision, inventory.GetWidth(), inventory.GetHeight(), items);
        }

        internal static StorageStack ToStack(ItemDrop.ItemData item, int slot)
        {
            var prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : string.Empty;
            return new StorageStack(Identity(item), prefab, item.m_shared.m_name, item.m_quality,
                item.m_worldLevel, item.m_stack, item.m_shared.m_maxStackSize, slot, SerializeItem(item));
        }

        internal static string Identity(ItemDrop.ItemData item)
        {
            var custom = string.Join("&", item.m_customData.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => Escape(x.Key) + "=" + Escape(x.Value)));
            return string.Join("|", new[] {
                Escape(item.m_dropPrefab != null ? item.m_dropPrefab.name : ""),
                ((int)item.m_shared.m_itemType).ToString(CultureInfo.InvariantCulture),
                item.m_quality.ToString(CultureInfo.InvariantCulture), item.m_variant.ToString(CultureInfo.InvariantCulture),
                item.m_worldLevel.ToString(CultureInfo.InvariantCulture), item.m_durability.ToString("R", CultureInfo.InvariantCulture),
                item.m_crafterID.ToString(CultureInfo.InvariantCulture), Escape(item.m_crafterName),
                item.m_cheated ? "1" : "0", custom });
        }

        internal static ItemDrop.ItemData DeserializeItem(string payload)
        {
            if (!payload.StartsWith(ItemPayloadPrefix, StringComparison.Ordinal))
            {
                // Read persisted 0.7.0 payloads through the fully initialized native path.
                var inventory = new Inventory("SCS payload", null, 1, 1);
                inventory.Load(new ZPackage(Convert.FromBase64String(payload)));
                var legacy = inventory.GetAllItems().Single().Clone();
                legacy.m_equipped = false;
                return legacy;
            }
            using (var reader = new BinaryReader(new MemoryStream(Convert.FromBase64String(payload.Substring(ItemPayloadPrefix.Length)))))
            {
                var prefabName = reader.ReadString();
                var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
                var template = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData : null;
                if (template?.m_shared == null) throw new InvalidDataException("Item prefab unavailable: " + prefabName);
                var item = template.Clone();
                item.m_dropPrefab = prefab; item.m_stack = 1; item.m_gridPos = Vector2i.zero; item.m_equipped = false;
                item.m_durability = reader.ReadSingle(); item.m_quality = reader.ReadInt32(); item.m_variant = reader.ReadInt32();
                item.m_worldLevel = reader.ReadByte(); item.m_crafterID = reader.ReadInt64(); item.m_crafterName = reader.ReadString();
                item.m_cheated = reader.ReadBoolean(); item.m_pickedUp = reader.ReadBoolean();
                var count = reader.ReadInt32();
                if (count < 0 || count > 16384) throw new InvalidDataException("Invalid item custom data count");
                item.m_customData = new Dictionary<string, string>();
                for (var i = 0; i < count; i++) item.m_customData.Add(reader.ReadString(), reader.ReadString());
                if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing item payload data");
                return item;
            }
        }

        internal static void ApplyLayout(Inventory inventory, StorageInventory layout)
        {
            if (inventory == null || layout == null || layout.Width != inventory.GetWidth() || layout.Height != inventory.GetHeight())
                throw new InvalidOperationException("Inventory dimensions changed");
            var current = inventory.GetAllItems();
            var staged = new List<Tuple<ItemDrop.ItemData, StorageStack, bool>>();
            var slots = new HashSet<int>();
            foreach (var target in layout.Items.OrderBy(x => x.Slot))
            {
                if (target.Slot < 0 || target.Slot >= layout.Capacity || !slots.Add(target.Slot) ||
                    target.Amount <= 0 || target.Amount > target.MaxStack)
                    throw new InvalidOperationException("Invalid target layout");
                var item = current.FirstOrDefault(x => Slot(x, layout.Width) == target.Slot && Identity(x) == target.Identity);
                var created = item == null;
                if (created)
                {
                    item = DeserializeItem(target.Payload);
                    if (Identity(item) != target.Identity || item.m_shared.m_maxStackSize != target.MaxStack)
                        throw new InvalidOperationException("Item payload identity changed");
                }
                staged.Add(Tuple.Create(item, target, created));
            }
            var retained = new HashSet<ItemDrop.ItemData>(staged.Select(x => x.Item1));
            if (current.Any(x => x.m_equipped && !retained.Contains(x)))
                throw new InvalidOperationException("Layout would remove or replace an equipped item");
            foreach (var entry in staged)
            {
                entry.Item1.m_stack = entry.Item2.Amount;
                entry.Item1.m_gridPos = new Vector2i(entry.Item2.Slot % layout.Width, entry.Item2.Slot / layout.Width);
                if (entry.Item3) entry.Item1.m_equipped = false;
            }
            current.Clear();
            current.AddRange(staged.Select(x => x.Item1));
            inventory.Changed();
        }

        internal static ItemDrop.ItemData DisplayItem(StorageStack stack)
        {
            var item = DeserializeItem(stack.Payload);
            item.m_stack = stack.Amount;
            return item;
        }

        private static string SerializeItem(ItemDrop.ItemData item)
        {
            // Native saves quantize durability and omit a crafter name when ID is zero.
            // Transfers need the exact identity, including those values and mod custom data.
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(item.m_dropPrefab != null ? item.m_dropPrefab.name : "");
                writer.Write(item.m_durability); writer.Write(item.m_quality); writer.Write(item.m_variant);
                writer.Write((byte)item.m_worldLevel); writer.Write(item.m_crafterID); writer.Write(item.m_crafterName ?? "");
                writer.Write(item.m_cheated); writer.Write(item.m_pickedUp); writer.Write(item.m_customData.Count);
                foreach (var pair in item.m_customData.OrderBy(x => x.Key, StringComparer.Ordinal)) { writer.Write(pair.Key); writer.Write(pair.Value); }
                return ItemPayloadPrefix + Convert.ToBase64String(stream.ToArray());
            }
        }
        private static int Slot(ItemDrop.ItemData item, int width) => item.m_gridPos.y * width + item.m_gridPos.x;
        private static string Escape(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }
}
