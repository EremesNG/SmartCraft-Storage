using System.Collections.Generic;
using UnityEngine;

namespace SmartCraftStorage.Storage.Runtime
{
    internal sealed class StorageSettings
    {
        public readonly bool Enabled;
        public readonly float TerminalRadius;
        public readonly int MaxMembers;
        public readonly int MaxPendingOperations;
        public readonly int OperationsPerTick;

        public StorageSettings(bool enabled, float terminalRadius, int maxMembers,
            int maxPendingOperations, int operationsPerTick)
        {
            Enabled = enabled;
            TerminalRadius = terminalRadius;
            MaxMembers = maxMembers;
            MaxPendingOperations = maxPendingOperations;
            OperationsPerTick = operationsPerTick;
        }
    }

    internal sealed class StorageContext
    {
        public readonly Player Actor;
        public readonly Vector3 Origin;
        public readonly float Radius;
        public readonly StorageScope Scope;
        public readonly ZNetView Anchor;

        public StorageContext(Player actor, Vector3 origin, float radius, StorageScope scope,
            ZNetView anchor = null)
        {
            Actor = actor;
            Origin = origin;
            Radius = radius;
            Scope = scope;
            Anchor = anchor;
        }
    }

    internal sealed class StorageRow
    {
        public readonly string Identity;
        public readonly ItemDrop.ItemData Sample;
        public readonly int Amount;

        public StorageRow(string identity, ItemDrop.ItemData sample, int amount)
        {
            Identity = identity;
            Sample = sample;
            Amount = amount;
        }
    }

    internal sealed class StorageView
    {
        public readonly IReadOnlyList<StorageRow> Rows;
        public readonly IReadOnlyList<string> MemberIds;
        public readonly int UsedSlots;
        public readonly int TotalSlots;
        public readonly bool Available;
        public readonly string Message;

        public StorageView(IReadOnlyList<StorageRow> rows, IReadOnlyList<string> memberIds,
            int usedSlots, int totalSlots, bool available = true, string message = "")
        {
            Rows = rows;
            MemberIds = memberIds;
            UsedSlots = usedSlots;
            TotalSlots = totalSlots;
            Available = available;
            Message = message;
        }

        public static StorageView Unavailable(string message = "Storage unavailable") =>
            new StorageView(new StorageRow[0], new string[0], 0, 0, false, message);
    }

    internal interface IStorageEffectHandler
    {
        string Kind { get; }
        // Only the current participant owner may execute. NotReady retains escrow.
        StorageEffectResult Validate(StorageEffectDescriptor effect, Inventory escrow, out string reason);
        // Persist this state with a Started receipt BEFORE invoking Apply. On a
        // retry, reconcile first: only proven NotApplied may be executed again.
        string CaptureState(string operationId, StorageEffectDescriptor effect, Inventory escrow);
        StorageEffectRecovery Reconcile(string operationId, StorageEffectDescriptor effect,
            Inventory escrow, string beforeState);
        StorageEffectResult Apply(string operationId, StorageEffectDescriptor effect, Inventory escrow);
    }

    internal interface IStorageService
    {
        bool Ready { get; }
        StorageView Query(StorageContext context);
        StorageOperation Deposit(StorageContext context, ItemDrop.ItemData item, int amount, string operationKey = null);
        StorageOperation Withdraw(StorageContext context, string identity, int amount, string operationKey = null, int destinationSlot = -1);
        StorageOperation Organize(StorageContext context, string operationKey = null);
        StorageOperation PrepareCost(StorageContext context, IReadOnlyList<StorageRequirement> requirements,
            bool includePlayer, StorageEffectDescriptor effect, string operationKey);
        // Captured=true means durable producer escrow exists and original drop may
        // be suppressed. Never infer this merely from a queued request or timeout.
        StorageOperation CaptureOutput(StorageContext context, ItemDrop.ItemData item, int amount,
            StorageEffectDescriptor captureEffect, StorageEffectDescriptor remainderEffect,
            string operationKey, bool requireAll = false);
        StorageOperation GetOperation(string operationId);
        StorageOperation GetPendingOperation(ZNetView participant, string kind);
        void Resume(string operationId);
        void RegisterEffect(IStorageEffectHandler handler);
        string GetNetworkName(ZNetView target);
        StorageOperation SetNetworkName(ZNetView target, Player actor, string name);
        bool IsBusy(ZNetView participant);
        // Synchronous direct writers can proceed only with current uncontested
        // authority. It must not claim ownership across an outstanding reservation.
        bool TryClaimDirectWrite(ZNetView participant, Player actor);
        void Tick();
        void Dispose();
    }

    internal static class StorageFacade
    {
        public const string TerminalPrefab = "piece_scs_terminal";
        public const string NetworkNameKey = "scs.network.name.v1";
        public const string TerminalMarkerKey = "scs.terminal.v1";
        public static IStorageService Service { get; set; } = new UnavailableService();

        internal static string NewPlayerOperation(Player actor, string kind)
        {
            string key = "scs.operation.sequence.v1." + kind;
            long previous = 0;
            if (actor.m_customData.TryGetValue(key, out var encoded) &&
                (!long.TryParse(encoded, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out previous) || previous < 0))
                throw new System.InvalidOperationException("Invalid saved storage operation sequence");
            long sequence = checked(previous + 1);
            actor.m_customData[key] = sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "scs:" + ZNet.instance.GetWorldUID().ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                kind + ":" + actor.GetPlayerID().ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private sealed class UnavailableService : IStorageService
        {
            public bool Ready => false;
            public StorageView Query(StorageContext context) => StorageView.Unavailable();
            public StorageOperation Deposit(StorageContext context, ItemDrop.ItemData item, int amount, string operationKey = null) => StorageOperation.Unavailable();
            public StorageOperation Withdraw(StorageContext context, string identity, int amount, string operationKey = null, int destinationSlot = -1) => StorageOperation.Unavailable();
            public StorageOperation Organize(StorageContext context, string operationKey = null) => StorageOperation.Unavailable();
            public StorageOperation PrepareCost(StorageContext context, IReadOnlyList<StorageRequirement> requirements, bool includePlayer, StorageEffectDescriptor effect, string operationKey) => StorageOperation.Unavailable();
            public StorageOperation CaptureOutput(StorageContext context, ItemDrop.ItemData item, int amount, StorageEffectDescriptor captureEffect, StorageEffectDescriptor remainderEffect, string operationKey, bool requireAll = false) => StorageOperation.Unavailable();
            public StorageOperation GetOperation(string operationId) => StorageOperation.Unavailable();
            public StorageOperation GetPendingOperation(ZNetView participant, string kind) => null;
            public void Resume(string operationId) { }
            public void RegisterEffect(IStorageEffectHandler handler) { }
            public string GetNetworkName(ZNetView target) => target != null && target.IsValid()
                ? target.GetZDO().GetString(NetworkNameKey, "") : "";
            public StorageOperation SetNetworkName(ZNetView target, Player actor, string name) => StorageOperation.Unavailable();
            public bool IsBusy(ZNetView participant) => true;
            public bool TryClaimDirectWrite(ZNetView participant, Player actor) => false;
            public void Tick() { }
            public void Dispose() { }
        }
    }
}
