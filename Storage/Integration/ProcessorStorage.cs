using System;
using System.Collections.Generic;
using System.Linq;
using SmartCraftStorage.Stations;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

namespace SmartCraftStorage.Storage.Integration
{
    internal enum ProcessorInput { SmelterOre, SmelterFuel, CookingFood, CookingFuel, FireplaceFuel, FermenterBase }

    internal static partial class ProcessorStorage
    {
        private const string PendingInput = "scs.processor.input.pending.";
        private const string InputSequence = "scs.processor.input.sequence.v1";

        internal static void Setup()
        {
            StorageFacade.Service.RegisterEffect(new InputEffect());
            StorageFacade.Service.RegisterEffect(new CaptureEffect());
            StorageFacade.Service.RegisterEffect(new RemainderEffect());
        }

        internal static void Refuel(Smelter machine)
        {
            var actor = Player.m_localPlayer;
            if (!Usable(machine.m_nview, actor)) return;
            bool kiln = KilnDetection.IsKiln(machine);
            if (!(kiln ? StationConfig.KilnAutoRefuel.Value : StationConfig.SmelterAutoRefuel.Value)) return;
            var context = Context(machine.m_nview, actor, StationConfig.SmelterKilnRadius.Value);
            var view = StorageFacade.Service.Query(context);
            int limit = kiln ? Math.Min(machine.m_maxOre, StationConfig.KilnWoodBuffer.Value) : machine.m_maxOre;
            string regularWood = kiln && StationConfig.KilnRegularWoodOnly.Value ? KilnDetection.GetRegularWoodItem(machine)?.m_itemData.m_shared.m_name : null;
            if (!kiln || !CoalCapReached(machine, view))
                RequestInput(context, view, ProcessorInput.SmelterOre, limit - machine.GetQueueSize(), row =>
                    row.Sample.m_dropPrefab != null && machine.IsItemAllowed(row.Sample.m_dropPrefab.name) &&
                    (regularWood == null || row.Sample.m_shared.m_name == regularWood));
            if (machine.m_maxFuel > 0 && machine.m_fuelItem != null)
                RequestInput(context, view, ProcessorInput.SmelterFuel,
                    machine.m_maxFuel - Mathf.CeilToInt(machine.GetFuel()), row => row.Sample.m_shared.m_name == machine.m_fuelItem.m_itemData.m_shared.m_name);
        }

        internal static void Refuel(CookingStation machine)
        {
            var actor = Player.m_localPlayer;
            if (!StationConfig.CookingStationAutoRefuel.Value || !Usable(machine.m_nview, actor)) return;
            var context = Context(machine.m_nview, actor, StationConfig.CookingStationRadius.Value);
            var view = StorageFacade.Service.Query(context);
            RequestInput(context, view, ProcessorInput.CookingFood, machine.GetFreeSlot() == -1 ? 0 : 1,
                row => row.Sample.m_dropPrefab != null && machine.IsItemAllowed(row.Sample.m_dropPrefab.name));
            if (machine.m_useFuel && machine.m_fuelItem != null)
                RequestInput(context, view, ProcessorInput.CookingFuel,
                    machine.m_maxFuel - Mathf.CeilToInt(machine.GetFuel()), row => row.Sample.m_shared.m_name == machine.m_fuelItem.m_itemData.m_shared.m_name);
        }

        internal static void Refuel(Fireplace machine)
        {
            var actor = Player.m_localPlayer;
            if (!StationConfig.FireplaceAutoRefuel.Value || !Usable(machine.m_nview, actor) || machine.m_fuelItem == null) return;
            var context = Context(machine.m_nview, actor, StationConfig.FireplaceRadius.Value);
            RequestInput(context, StorageFacade.Service.Query(context), ProcessorInput.FireplaceFuel,
                Mathf.FloorToInt(machine.m_maxFuel) - Mathf.CeilToInt(machine.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel)),
                row => row.Sample.m_shared.m_name == machine.m_fuelItem.m_itemData.m_shared.m_name);
        }

        internal static void Refuel(Fermenter machine)
        {
            var actor = Player.m_localPlayer;
            if (!StationConfig.FermenterAutoProcess.Value || !Usable(machine.m_nview, actor)) return;
            var context = Context(machine.m_nview, actor, StationConfig.FermenterRadius.Value);
            RequestInput(context, StorageFacade.Service.Query(context), ProcessorInput.FermenterBase,
                machine.GetStatus() == Fermenter.Status.Empty && machine.m_hasRoof && !machine.m_exposed ? 1 : 0,
                row => machine.IsItemAllowed(row.Sample));
        }

        private static bool Usable(ZNetView view, Player actor) => actor != null && view != null && view.IsValid() &&
            view.IsOwner() && StorageFacade.Service.Ready && !StorageFacade.Service.IsBusy(view);

        internal static StorageContext Context(ZNetView view, Player actor, float radius) =>
            new StorageContext(actor, view.transform.position, radius, StorageScope.Processor, view);

        private static bool CoalCapReached(Smelter kiln, StorageView view)
        {
            string coal = KilnDetection.GetCoalItemName(kiln);
            return coal != null && view.Rows.Where(row => row.Sample.m_shared.m_name == coal && row.Sample.m_worldLevel >= Game.m_worldLevel)
                .Sum(row => row.Amount) >= StationConfig.KilnMaxCoalInChest.Value;
        }

        private static void RequestInput(StorageContext context, StorageView view, ProcessorInput kind,
            int capacity, Func<StorageRow, bool> matches)
        {
            var zdo = context.Anchor.GetZDO();
            string key = PendingInput + (int)kind;
            string pendingId = zdo.GetString(key, "");
            StorageOperation pending = pendingId.Length == 0 ? null : StorageFacade.Service.GetOperation(pendingId);
            var decision = StationOperationController.InputDecision(pending, capacity);
            if (decision == StorageWorkDecision.Resume) { StorageFacade.Service.Resume(pendingId); return; }
            if (pending != null && pending.IsFinal) zdo.Set(key, "");
            if (decision != StorageWorkDecision.Start || !view.Available) return;
            var row = view.Rows.FirstOrDefault(item => item.Amount > 0 && item.Sample != null &&
                item.Sample.m_worldLevel >= Game.m_worldLevel && matches(item));
            if (row == null) return;
            long sequence = checked(zdo.GetLong(InputSequence, 0) + 1);
            zdo.Set(InputSequence, sequence);
            string operationId = StorageDiscovery.Id(context.Anchor) + ":input:" + sequence;
            zdo.Set(key, operationId);
            var request = new InputRequest(kind, row.Sample.m_dropPrefab.name, row.Sample.m_shared.m_name,
                row.Identity, row.Sample.m_quality, Game.m_worldLevel, context.Radius);
            var result = StorageFacade.Service.PrepareCost(context, new[] { request.Cost }, false,
                new StorageEffectDescriptor("processor.input", StorageDiscovery.Id(context.Anchor),
                    context.Actor.GetPlayerID(), request.Encode()), operationId);
            if (result.IsFinal && result.Status != StorageOperationStatus.Confirmed) zdo.Set(key, "");
        }

        internal static ZNetView FindView(string id)
        {
            if (ZNetScene.instance == null || string.IsNullOrEmpty(id)) return null;
            var parts = id.Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var userId) || !uint.TryParse(parts[1], out var objectId)) return null;
            var obj = ZNetScene.instance.FindInstance(new ZDOID(userId, objectId));
            return obj != null ? obj.GetComponent<ZNetView>() : null;
        }

        private sealed class InputRequest
        {
            internal readonly ProcessorInput Kind;
            internal readonly string Prefab, Name, Identity;
            internal readonly int Quality, WorldLevel;
            internal readonly float Radius;
            internal StorageRequirement Cost => new StorageRequirement(Name, 1, Quality, Identity, WorldLevel);
            internal InputRequest(ProcessorInput kind, string prefab, string name, string identity, int quality, int worldLevel, float radius)
            { Kind = kind; Prefab = prefab; Name = name; Identity = identity; Quality = quality; WorldLevel = worldLevel; Radius = radius; }

            internal string Encode()
            {
                var p = new ZPackage();
                p.Write(1); p.Write((int)Kind); p.Write(Prefab); p.Write(Name); p.Write(Identity); p.Write(Quality); p.Write(WorldLevel); p.Write(Radius);
                return Convert.ToBase64String(p.GetArray());
            }

            internal static InputRequest Decode(string data)
            {
                var p = new ZPackage(Convert.FromBase64String(data));
                if (p.ReadInt() != 1) throw new InvalidOperationException("Unsupported processor request version");
                var kind = (ProcessorInput)p.ReadInt();
                if (!Enum.IsDefined(typeof(ProcessorInput), kind)) throw new InvalidOperationException("Invalid processor input kind");
                return new InputRequest(kind, p.ReadString(), p.ReadString(), p.ReadString(), p.ReadInt(), p.ReadInt(), p.ReadSingle());
            }

            internal bool HasCapacity(ZNetView view, ItemDrop.ItemData item, long actorId)
            {
                if (item == null || item.m_dropPrefab == null || item.m_dropPrefab.name != Prefab || GameInventoryAdapter.Identity(item) != Identity) return false;
                switch (Kind)
                {
                    case ProcessorInput.SmelterOre:
                    case ProcessorInput.SmelterFuel:
                        var smelter = view.GetComponent<Smelter>();
                        if (smelter == null) return false;
                        bool kiln = KilnDetection.IsKiln(smelter);
                        if (!(kiln ? StationConfig.KilnAutoRefuel.Value : StationConfig.SmelterAutoRefuel.Value)) return false;
                        if (Kind == ProcessorInput.SmelterFuel) return smelter.m_fuelItem != null && Name == smelter.m_fuelItem.m_itemData.m_shared.m_name &&
                            Mathf.CeilToInt(smelter.GetFuel()) < smelter.m_maxFuel;
                        if (!smelter.IsItemAllowed(Prefab) || smelter.GetQueueSize() >= (kiln ? Math.Min(smelter.m_maxOre, StationConfig.KilnWoodBuffer.Value) : smelter.m_maxOre)) return false;
                        if (!kiln) return true;
                        var wood = KilnDetection.GetRegularWoodItem(smelter);
                        if (StationConfig.KilnRegularWoodOnly.Value && wood != null && Name != wood.m_itemData.m_shared.m_name) return false;
                        var actor = Player.GetPlayer(actorId);
                        return actor != null && !CoalCapReached(smelter, StorageFacade.Service.Query(Context(view, actor, Radius)));
                    case ProcessorInput.CookingFood:
                    case ProcessorInput.CookingFuel:
                        var cooking = view.GetComponent<CookingStation>();
                        if (cooking == null || !StationConfig.CookingStationAutoRefuel.Value) return false;
                        if (Kind == ProcessorInput.CookingFood) return cooking.GetFreeSlot() != -1 && cooking.IsItemAllowed(Prefab);
                        return cooking.m_useFuel && cooking.m_fuelItem != null && Name == cooking.m_fuelItem.m_itemData.m_shared.m_name &&
                            Mathf.CeilToInt(cooking.GetFuel()) < cooking.m_maxFuel;
                    case ProcessorInput.FireplaceFuel:
                        var fire = view.GetComponent<Fireplace>();
                        return fire != null && StationConfig.FireplaceAutoRefuel.Value && fire.m_fuelItem != null && Name == fire.m_fuelItem.m_itemData.m_shared.m_name &&
                            Mathf.CeilToInt(view.GetZDO().GetFloat(ZDOVars.s_fuel)) < fire.m_maxFuel;
                    case ProcessorInput.FermenterBase:
                        var fermenter = view.GetComponent<Fermenter>();
                        return fermenter != null && StationConfig.FermenterAutoProcess.Value && fermenter.m_hasRoof && !fermenter.m_exposed &&
                            fermenter.GetStatus() == Fermenter.Status.Empty && fermenter.IsItemAllowed(item);
                    default: return false;
                }
            }

            internal void Apply(ZNetView view, ItemDrop.ItemData item)
            {
                long sender = ZNet.GetUID();
                switch (Kind)
                {
                    case ProcessorInput.SmelterOre: view.GetComponent<Smelter>().RPC_AddOre(sender, Prefab, item.m_cheated); break;
                    case ProcessorInput.SmelterFuel: view.GetComponent<Smelter>().RPC_AddFuel(sender); break;
                    case ProcessorInput.CookingFood: view.GetComponent<CookingStation>().RPC_AddItem(sender, Prefab, item.m_cheated); break;
                    case ProcessorInput.CookingFuel: view.GetComponent<CookingStation>().RPC_AddFuel(sender); break;
                    case ProcessorInput.FireplaceFuel: view.GetComponent<Fireplace>().RPC_AddFuel(sender); break;
                    case ProcessorInput.FermenterBase: view.GetComponent<Fermenter>().RPC_AddItem(sender, Prefab.GetStableHashCode(), item.m_cheated); break;
                }
            }
        }

        private sealed class InputEffect : IStorageEffectHandler
        {
            public string Kind => "processor.input";
            public string CaptureState(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                var view = FindView(effect.TargetId);
                var request = InputRequest.Decode(effect.Data);
                int slot = request.Kind == ProcessorInput.CookingFood ? view.GetComponent<CookingStation>().GetFreeSlot() : -1;
                var p = new ZPackage();
                p.Write(slot); p.Write(InputState(view, request, slot, false)); p.Write(InputState(view, request, slot, true));
                return Convert.ToBase64String(p.GetArray());
            }

            public StorageEffectRecovery Reconcile(string operationId, StorageEffectDescriptor effect, Inventory escrow, string beforeState)
            {
                var view = FindView(effect.TargetId);
                if (view == null || !view.IsValid() || !view.IsOwner()) return StorageEffectRecovery.Uncertain;
                var request = InputRequest.Decode(effect.Data);
                var p = new ZPackage(Convert.FromBase64String(beforeState));
                int slot = p.ReadInt();
                var result = StationOperationController.ReconcileNativeState(p.ReadString(), p.ReadString(), InputState(view, request, slot, false));
                if (result == StorageEffectRecovery.Applied) view.GetZDO().Set(PendingInput + (int)request.Kind, "");
                return result;
            }

            private static string InputState(ZNetView view, InputRequest request, int slot, bool after)
            {
                var p = new ZPackage();
                var zdo = view.GetZDO();
                switch (request.Kind)
                {
                    case ProcessorInput.SmelterOre:
                        int count = view.GetComponent<Smelter>().GetQueueSize();
                        p.Write(count + (after ? 1 : 0));
                        for (int i = 0; i < count; i++) p.Write(zdo.GetString("item" + i));
                        if (after) p.Write(request.Prefab);
                        break;
                    case ProcessorInput.CookingFood:
                        p.Write(after ? request.Prefab : zdo.GetString("slot" + slot));
                        break;
                    case ProcessorInput.FermenterBase:
                        p.Write(after ? request.Prefab.GetStableHashCode() : zdo.GetInt(ZDOVars.s_content));
                        break;
                    default:
                        float fuel = zdo.GetFloat(ZDOVars.s_fuel);
                        float proposed = fuel + 1f;
                        if (request.Kind == ProcessorInput.FireplaceFuel)
                            proposed = Mathf.Clamp(Mathf.Clamp(fuel, 0, view.GetComponent<Fireplace>().m_maxFuel) + 1f, 0, view.GetComponent<Fireplace>().m_maxFuel);
                        p.Write(after ? proposed : fuel);
                        break;
                }
                return Convert.ToBase64String(p.GetArray());
            }

            public StorageEffectResult Validate(StorageEffectDescriptor effect, Inventory escrow, out string reason)
            {
                reason = "";
                var view = FindView(effect.TargetId);
                if (view == null || !view.IsValid() || !view.IsOwner()) return StorageEffectResult.NotReady;
                var request = InputRequest.Decode(effect.Data);
                var item = escrow.GetAllItems().FirstOrDefault(stack => GameInventoryAdapter.Identity(stack) == request.Identity);
                if (!request.HasCapacity(view, item, effect.ActorId)) { reason = "Processor input capacity or settings changed"; return StorageEffectResult.Rejected; }
                return StorageEffectResult.Applied;
            }

            public StorageEffectResult Apply(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                var view = FindView(effect.TargetId);
                if (view == null || !view.IsValid() || !view.IsOwner()) return StorageEffectResult.NotReady;
                var request = InputRequest.Decode(effect.Data);
                var item = escrow.GetAllItems().FirstOrDefault(stack => GameInventoryAdapter.Identity(stack) == request.Identity);
                return StationOperationController.ApplyPaidEffect(GameInventoryAdapter.Snapshot("processor-escrow", "", escrow).Items,
                    new[] { request.Cost }, request.HasCapacity(view, item, effect.ActorId), () =>
                    {
                        request.Apply(view, item); // Native owner method completes in this frame, without a second queued RPC.
                        escrow.RemoveItem(item, 1);
                        view.GetZDO().Set(PendingInput + (int)request.Kind, "");
                        return StorageEffectResult.Applied;
                    });
            }
        }
    }
}
