using System;
using System.Linq;
using SmartCraftStorage.Stations;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

namespace SmartCraftStorage.Storage.Integration
{
    internal static partial class ProcessorStorage
    {
        private const string OutputSequence = "scs.processor.output.sequence.v1";
        // Only the synchronous, authoritative native production event can create
        // new escrow. Recovery authenticates the persisted capture receipt instead.
        private static OutputRequest _capturing;

        internal static bool Collect(Smelter machine, string ore, int amount)
        {
            bool kiln = KilnDetection.IsKiln(machine);
            if (!(kiln ? StationConfig.KilnAutoCollect.Value : StationConfig.SmelterAutoCollect.Value) ||
                !Usable(machine.m_nview, Player.m_localPlayer) || amount <= 0) return false;
            var conversion = machine.GetItemConversion(ore);
            if (conversion?.m_to == null) return false;
            var item = NewOutput(conversion.m_to, machine.m_nview.GetZDO().GetBool(ZDOVars.s_cheatedQueued) ||
                machine.m_nview.GetZDO().GetBool(ZDOVars.s_cheated));
            var request = OutputRequest.Create(machine.m_nview, kiln ? StorageProducer.Kiln : StorageProducer.Smelter,
                item, amount, machine.m_outputPoint.position);
            request.Source = ore;
            return Capture(machine.m_nview, StationConfig.SmelterKilnRadius.Value, item, request);
        }

        internal static bool Collect(CookingStation machine, Vector3 userPoint, int amount)
        {
            if (!StationConfig.CookingStationAutoCollect.Value || !Usable(machine.m_nview, Player.m_localPlayer) || amount <= 0) return false;
            for (int slot = 0; slot < machine.m_slots.Length; slot++)
            {
                machine.GetSlot(slot, out var name, out _, out _, out var cheated);
                if (string.IsNullOrEmpty(name) || !machine.IsItemDone(name)) continue;
                var prefab = ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>();
                if (prefab == null) return false;
                var item = NewOutput(prefab, cheated);
                if (machine.m_spawnFullDurability) item.m_durability = item.m_shared.m_maxDurability;
                if (machine.m_recordCrafter)
                {
                    item.m_crafterID = Player.m_localPlayer.GetPlayerID();
                    item.m_crafterName = Player.m_localPlayer.GetPlayerName();
                }
                var position = machine.m_spawnPoint != null ? machine.m_spawnPoint.position : machine.m_slots[slot].position + Vector3.up * 0.25f;
                var request = OutputRequest.Create(machine.m_nview, StorageProducer.Cooking, item,
                    checked(amount * Math.Max(1, prefab.m_itemData.m_stack)), position);
                request.Source = name;
                request.Slot = slot;
                return Capture(machine.m_nview, StationConfig.CookingStationRadius.Value, item, request);
            }
            return false;
        }

        internal static bool Collect(Fermenter machine)
        {
            if (!StationConfig.FermenterAutoProcess.Value || !Usable(machine.m_nview, Player.m_localPlayer) ||
                machine.GetStatus() != Fermenter.Status.Ready) return false;
            int content = machine.GetContent();
            var conversion = machine.GetItemConversion(content);
            if (conversion?.m_to == null) return false;
            var item = NewOutput(conversion.m_to, machine.m_nview.GetZDO().GetBool(ZDOVars.s_cheatedQueued) ||
                machine.m_nview.GetZDO().GetBool(ZDOVars.s_cheated));
            var request = OutputRequest.Create(machine.m_nview, StorageProducer.Fermenter, item,
                checked(conversion.m_producedItems * Math.Max(1, conversion.m_to.m_itemData.m_stack)), machine.m_outputPoint.position);
            request.Content = content;
            return Capture(machine.m_nview, StationConfig.FermenterRadius.Value, item, request);
        }

        internal static bool Collect(Beehive machine, bool automatic)
        {
            if (!StationConfig.BeehiveAutoCollect.Value || !Usable(machine.m_nview, Player.m_localPlayer)) return false;
            int level = machine.GetHoneyLevel();
            if (level <= 0) return false;
            int amount = 0;
            for (int i = 0; i < level; i++) amount = checked(amount + Game.instance.ScaleDrops(machine.m_honeyItem.m_itemData, 1));
            var item = NewOutput(machine.m_honeyItem, false);
            var request = OutputRequest.Create(machine.m_nview,
                automatic ? StorageProducer.BeehiveAutomatic : StorageProducer.BeehiveManual,
                item, amount, machine.m_spawnPoint.position);
            request.Level = level;
            return Capture(machine.m_nview, StationConfig.BeehiveRadius.Value, item, request);
        }

        private static ItemDrop.ItemData NewOutput(ItemDrop prefab, bool cheated)
        {
            var item = prefab.m_itemData.Clone();
            item.m_dropPrefab = prefab.gameObject;
            item.m_stack = 1;
            item.m_equipped = false;
            item.m_worldLevel = Game.m_worldLevel;
            item.m_cheated = cheated && !PlayerProfile.s_bypassCheatChecks;
            return item;
        }

        private static bool Capture(ZNetView source, float radius, ItemDrop.ItemData item, OutputRequest request)
        {
            if (request.Amount <= 0) return false;
            var actor = Player.m_localPlayer;
            var data = request.Encode();
            _capturing = request;
            try
            {
                var result = StorageFacade.Service.CaptureOutput(Context(source, actor, radius), item, request.Amount,
                    new StorageEffectDescriptor("processor.capture", StorageDiscovery.Id(source), actor.GetPlayerID(), data),
                    new StorageEffectDescriptor("processor.remainder", StorageDiscovery.Id(source), actor.GetPlayerID(), data),
                    request.Id, request.Producer == StorageProducer.BeehiveAutomatic);
                // Requested alone is not custody. Only durable capture suppresses
                // vanilla production; all later delivery belongs to the journal.
                return StationOperationController.OutputDecision(result, request.Producer).Kind != StorageOutputKind.Original;
            }
            finally { _capturing = null; }
        }

        private sealed class OutputRequest
        {
            internal string Id, Prefab, Source = "";
            internal StorageProducer Producer;
            internal int Amount, Slot = -1, Content, Level;
            internal Vector3 Position;

            internal static OutputRequest Create(ZNetView view, StorageProducer producer, ItemDrop.ItemData item, int amount, Vector3 position)
            {
                long sequence = checked(view.GetZDO().GetLong(OutputSequence, 0) + 1);
                view.GetZDO().Set(OutputSequence, sequence);
                return new OutputRequest { Id = StorageDiscovery.Id(view) + ":output:" + sequence,
                    Producer = producer, Prefab = item.m_dropPrefab.name, Amount = amount, Position = position };
            }

            internal string Encode()
            {
                var p = new ZPackage();
                p.Write(1); p.Write(Id); p.Write((int)Producer); p.Write(Prefab); p.Write(Source);
                p.Write(Amount); p.Write(Slot); p.Write(Content); p.Write(Level); p.Write(Position);
                return Convert.ToBase64String(p.GetArray());
            }

            internal static OutputRequest Decode(string data)
            {
                var p = new ZPackage(Convert.FromBase64String(data));
                if (p.ReadInt() != 1) throw new InvalidOperationException("Unsupported production receipt");
                var request = new OutputRequest { Id = p.ReadString(), Producer = (StorageProducer)p.ReadInt(), Prefab = p.ReadString(),
                    Source = p.ReadString(), Amount = p.ReadInt(), Slot = p.ReadInt(), Content = p.ReadInt(), Level = p.ReadInt(), Position = p.ReadVector3() };
                if (!Enum.IsDefined(typeof(StorageProducer), request.Producer) || request.Amount <= 0)
                    throw new InvalidOperationException("Invalid production receipt");
                return request;
            }

            internal bool SourceMatches(ZNetView view)
            {
                switch (Producer)
                {
                    case StorageProducer.Smelter:
                    case StorageProducer.Kiln:
                        return view.GetComponent<Smelter>()?.GetItemConversion(Source)?.m_to?.gameObject.name == Prefab;
                    case StorageProducer.Cooking:
                        var cooking = view.GetComponent<CookingStation>();
                        if (cooking == null || Slot < 0 || Slot >= cooking.m_slots.Length) return false;
                        cooking.GetSlot(Slot, out var name, out _, out _, out _);
                        return name == Source && name == Prefab && cooking.IsItemDone(name);
                    case StorageProducer.Fermenter:
                        var fermenter = view.GetComponent<Fermenter>();
                        return fermenter != null && fermenter.GetStatus() == Fermenter.Status.Ready && fermenter.GetContent() == Content &&
                            fermenter.GetItemConversion(Content)?.m_to?.gameObject.name == Prefab;
                    case StorageProducer.BeehiveAutomatic:
                    case StorageProducer.BeehiveManual:
                        var hive = view.GetComponent<Beehive>();
                        return hive != null && hive.GetHoneyLevel() == Level && hive.m_honeyItem.gameObject.name == Prefab;
                    default: return false;
                }
            }

            internal void CaptureSource(ZNetView view)
            {
                switch (Producer)
                {
                    case StorageProducer.Cooking:
                        var cooking = view.GetComponent<CookingStation>();
                        cooking.SetSlot(Slot, "", 0, CookingStation.Status.NotDone, false);
                        view.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", Slot, "");
                        cooking.m_pickEffector.Create(Position, Quaternion.identity);
                        break;
                    case StorageProducer.Fermenter:
                        view.GetZDO().Set(ZDOVars.s_content, 0);
                        view.GetZDO().Set(ZDOVars.s_startTime, 0L);
                        view.GetZDO().Set(ZDOVars.s_cheatedQueued, false);
                        var fermenter = view.GetComponent<Fermenter>();
                        fermenter.m_tapEffects.Create(fermenter.transform.position, fermenter.transform.rotation);
                        break;
                    case StorageProducer.BeehiveAutomatic:
                    case StorageProducer.BeehiveManual:
                        var hive = view.GetComponent<Beehive>();
                        hive.ResetLevel();
                        hive.m_spawnEffect.Create(Position, Quaternion.identity);
                        break;
                    default:
                        var smelter = view.GetComponent<Smelter>();
                        smelter.m_produceEffects.Create(smelter.transform.position, smelter.transform.rotation);
                        break;
                }
            }
        }

        private sealed class CaptureEffect : IStorageEffectHandler
        {
            public string Kind => "processor.capture";
            public string CaptureState(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                var request = OutputRequest.Decode(effect.Data);
                var p = new ZPackage();
                p.Write(request.Id); p.Write(SourceState(request, FindView(effect.TargetId)));
                return Convert.ToBase64String(p.GetArray());
            }

            public StorageEffectRecovery Reconcile(string operationId, StorageEffectDescriptor effect, Inventory escrow, string beforeState)
            {
                var view = FindView(effect.TargetId);
                if (view == null || !view.IsValid() || !view.IsOwner()) return StorageEffectRecovery.Uncertain;
                var request = OutputRequest.Decode(effect.Data);
                var p = new ZPackage(Convert.FromBase64String(beforeState));
                if (p.ReadString() != operationId) return StorageEffectRecovery.Uncertain;
                var before = p.ReadString();
                // Smelter.Spawn is called only after native ore/production has
                // been debited. Durable custody of this event is the effect;
                // its visual effect has no additional resource transition.
                if (request.Producer == StorageProducer.Smelter || request.Producer == StorageProducer.Kiln)
                    return StorageEffectRecovery.Applied;
                return StationOperationController.ReconcileNativeState(before, "", SourceState(request, view));
            }

            private static string SourceState(OutputRequest request, ZNetView view)
            {
                switch (request.Producer)
                {
                    case StorageProducer.Cooking: return view.GetZDO().GetString("slot" + request.Slot);
                    case StorageProducer.Fermenter:
                        int content = view.GetZDO().GetInt(ZDOVars.s_content);
                        return content == 0 ? "" : content.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    case StorageProducer.BeehiveAutomatic:
                    case StorageProducer.BeehiveManual:
                        int level = view.GetComponent<Beehive>().GetHoneyLevel();
                        return level == 0 ? "" : level.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    default: return request.Id;
                }
            }

            public StorageEffectResult Validate(StorageEffectDescriptor effect, Inventory escrow, out string reason)
            {
                reason = "";
                var request = OutputRequest.Decode(effect.Data);
                var view = FindView(effect.TargetId);
                if (view == null || !view.IsValid() || !view.IsOwner()) return StorageEffectResult.NotReady;
                bool currentEvent = _capturing != null && _capturing.Id == request.Id;
                bool recordedIntent = view.GetZDO().GetString(StorageService.CaptureIntentKey, "") == request.Id + "\n" + effect.Data;
                if ((!currentEvent && !recordedIntent) || !request.SourceMatches(view) ||
                    escrow.GetAllItems().Any(item => item.m_dropPrefab.name != request.Prefab) ||
                    escrow.GetAllItems().Sum(item => item.m_stack) != request.Amount)
                { reason = "Production event is no longer current"; return StorageEffectResult.Rejected; }
                return StorageEffectResult.Applied;
            }

            public StorageEffectResult Apply(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                var validation = Validate(effect, escrow, out _);
                if (validation != StorageEffectResult.Applied) return validation;
                OutputRequest.Decode(effect.Data).CaptureSource(FindView(effect.TargetId));
                return StorageEffectResult.Applied;
            }
        }

        private sealed class RemainderEffect : IStorageEffectHandler
        {
            public string Kind => "processor.remainder";
            private const string PlanKey = "scs.processor.drop.plan.v1";
            public string CaptureState(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                string plan = NativeDropPlan.Prepare(escrow, OutputRequest.Decode(effect.Data).Position + Vector3.up * 0.25f);
                var p = new ZPackage(); p.Write(operationId); p.Write(plan);
                FindView(effect.TargetId).GetZDO().Set(PlanKey, Convert.ToBase64String(p.GetArray()));
                return plan;
            }

            public StorageEffectRecovery Reconcile(string operationId, StorageEffectDescriptor effect, Inventory escrow, string beforeState) =>
                OutputRequest.Decode(effect.Data).Producer == StorageProducer.BeehiveAutomatic
                    ? StorageEffectRecovery.Uncertain
                    : NativeDropPlan.Publish(operationId, FindView(effect.TargetId), beforeState);

            public StorageEffectResult Validate(StorageEffectDescriptor effect, Inventory escrow, out string reason)
            {
                reason = "";
                var view = FindView(effect.TargetId);
                if (view == null || !view.IsValid() || !view.IsOwner()) return StorageEffectResult.NotReady;
                var request = OutputRequest.Decode(effect.Data);
                if (request.Producer == StorageProducer.BeehiveAutomatic)
                { reason = "Automatic honey must retain its outbox until all of it fits"; return StorageEffectResult.NotReady; }
                if (escrow.GetAllItems().Any(item => item.m_dropPrefab == null || item.m_dropPrefab.name != request.Prefab))
                { reason = "Remainder item does not match its producer"; return StorageEffectResult.Rejected; }
                return StorageEffectResult.Applied;
            }

            public StorageEffectResult Apply(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                var validation = Validate(effect, escrow, out _);
                if (validation != StorageEffectResult.Applied) return validation;
                var request = OutputRequest.Decode(effect.Data);
                int remaining = escrow.GetAllItems().Sum(item => item.m_stack);
                var decision = StationOperationController.OutputDecision(new StorageOperation(operationId,
                    StorageOperationStatus.Confirmed, request.Amount, request.Amount - remaining, remaining, captured: true), request.Producer);
                if (decision.Kind == StorageOutputKind.Retain) return StorageEffectResult.NotReady;
                var p = new ZPackage(Convert.FromBase64String(FindView(effect.TargetId).GetZDO().GetString(PlanKey)));
                if (p.ReadString() != operationId) return StorageEffectResult.Uncertain;
                return NativeDropPlan.Publish(operationId, FindView(effect.TargetId), p.ReadString()) == StorageEffectRecovery.Applied
                    ? StorageEffectResult.Applied : StorageEffectResult.Uncertain;
            }
        }
    }
}
