using System;
using System.Collections.Generic;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

namespace SmartCraftStorage.Storage.Integration
{
    // Assign stable world-object identities before publishing any remainder.
    // Recovery completes those exact objects, never calls Instantiate twice.
    internal static class NativeDropPlan
    {
        private const string ReceiptKey = "scs.processor.drops.v1";
        private const string DropKey = "scs.processor.drop.v1";

        internal static string Prepare(Inventory escrow, Vector3 position)
        {
            var entries = new List<Tuple<ZDOID, ItemDrop.ItemData>>();
            foreach (var item in escrow.GetAllItems())
                for (int remaining = item.m_stack; remaining > 0;)
                {
                    var clone = item.Clone();
                    clone.m_stack = Math.Min(remaining, Math.Max(1, item.m_shared.m_maxStackSize));
                    remaining -= clone.m_stack;
                    ZDOID id;
                    do { id = new ZDOID(ZDOMan.GetSessionID(), checked(ZDOMan.instance.m_nextUid++)); }
                    while (ZDOMan.instance.GetZDO(id) != null);
                    entries.Add(Tuple.Create(id, clone));
                }
            var p = new ZPackage();
            p.Write(1); p.Write(position); p.Write(entries.Count);
            foreach (var entry in entries)
            {
                p.Write(entry.Item1);
                p.Write(StorageRpc.Encode(new StorageInventory("drop", "", 1, 1,
                    new[] { GameInventoryAdapter.ToStack(entry.Item2, 0) })));
            }
            return Convert.ToBase64String(p.GetArray());
        }

        internal static StorageEffectRecovery Publish(string operationId, ZNetView source, string plan)
        {
            if (source == null || !source.IsValid() || !source.IsOwner() || ZDOMan.instance == null)
                return StorageEffectRecovery.Uncertain;
            var p = new ZPackage(Convert.FromBase64String(plan));
            if (p.ReadInt() != 1) return StorageEffectRecovery.Uncertain;
            var position = p.ReadVector3();
            int count = p.ReadInt();
            if (count < 0 || count > 1024) return StorageEffectRecovery.Uncertain;
            var receipt = source.GetZDO().GetString(ReceiptKey, "").Split('\n');
            int completed = receipt.Length == 2 && receipt[0] == operationId && int.TryParse(receipt[1], out var value) ? value : 0;
            for (int i = 0; i < count; i++)
            {
                var id = p.ReadZDOID();
                var item = GameInventoryAdapter.DisplayItem(StorageRpc.Decode(p.ReadString()).Items[0]);
                if (i < completed) continue;
                string marker = operationId + ":" + i;
                var zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null)
                    zdo = ZDOMan.instance.CreateNewZDO(id, position);
                else if (zdo.GetPrefab() != 0 && (zdo.GetString(DropKey, "") != marker ||
                    zdo.GetPrefab() != item.m_dropPrefab.name.GetStableHashCode()))
                    return StorageEffectRecovery.Uncertain;

                // A nonzero prefab is the publication boundary. Populate the
                // entire real ItemDrop payload first; its normal Awake will load
                // this data when the scene instantiates the existing ZDO.
                if (zdo.GetPrefab() == 0)
                {
                    zdo.Persistent = true;
                    zdo.SetRotation(Quaternion.identity);
                    zdo.Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
                    zdo.Set(DropKey, marker);
                    ItemDrop.SaveToZDO(item, zdo);
                    zdo.SetPrefab(item.m_dropPrefab.name.GetStableHashCode());
                }
                completed = i + 1;
                source.GetZDO().Set(ReceiptKey, operationId + "\n" + completed);
            }
            return StorageEffectRecovery.Applied;
        }
    }
}
