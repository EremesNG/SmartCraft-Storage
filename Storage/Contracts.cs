using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SmartCraftStorage.Storage
{
    // Values passed across the game boundary. Payload is the game's serialized
    // item, while Identity describes every property relevant to strict stacking.
    public sealed class StorageStack
    {
        public string Identity { get; }
        public string Prefab { get; }
        public string SharedName { get; }
        public int Quality { get; }
        public int WorldLevel { get; }
        public int Amount { get; }
        public int MaxStack { get; }
        public int Slot { get; }
        public string Payload { get; }

        public StorageStack(string identity, string prefab, string sharedName, int quality,
            int worldLevel, int amount, int maxStack, int slot, string payload)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            SharedName = sharedName ?? throw new ArgumentNullException(nameof(sharedName));
            Quality = quality;
            WorldLevel = worldLevel;
            Amount = amount;
            MaxStack = maxStack;
            Slot = slot;
            Payload = payload ?? string.Empty;
        }

        public StorageStack At(int slot, int amount) => new StorageStack(Identity, Prefab,
            SharedName, Quality, WorldLevel, amount, MaxStack, slot, Payload);
    }

    public sealed class StorageInventory
    {
        public string Id { get; }
        public string Revision { get; }
        public int Width { get; }
        public int Height { get; }
        public int Capacity => checked(Width * Height);
        public IReadOnlyList<StorageStack> Items { get; }

        public StorageInventory(string id, string revision, int width, int height,
            IEnumerable<StorageStack> items)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Revision = revision ?? string.Empty;
            Width = width;
            Height = height;
            Items = new ReadOnlyCollection<StorageStack>((items ?? Enumerable.Empty<StorageStack>()).ToList());
        }
    }

    public sealed class StorageRequirement
    {
        public string SharedName { get; }
        public int Amount { get; }
        public int Quality { get; }
        public string Identity { get; }
        // Minimum world level accepted by a requirement; -1 disables this filter.
        public int WorldLevel { get; }

        public StorageRequirement(string sharedName, int amount, int quality = -1,
            string identity = null, int worldLevel = -1)
        {
            SharedName = sharedName ?? throw new ArgumentNullException(nameof(sharedName));
            Amount = amount;
            Quality = quality;
            Identity = identity;
            WorldLevel = worldLevel;
        }
    }

    public enum StorageScope { Direct, Terminal, Crafting, Processor }

    public enum StorageOperationStatus
    {
        Unavailable, Requested, Preparing, Prepared, CommitDecided, Applying,
        AwaitingEffect, RecoveryPending, Confirmed, Rejected, Aborted
    }

    public sealed class StorageOperation
    {
        public string Id { get; }
        public StorageOperationStatus Status { get; }
        public int Requested { get; }
        public int Accepted { get; }
        public int Remaining { get; }
        public string Message { get; }
        public bool Captured { get; }
        public bool IsFinal => Status == StorageOperationStatus.Confirmed ||
                               Status == StorageOperationStatus.Rejected || Status == StorageOperationStatus.Aborted;

        public StorageOperation(string id, StorageOperationStatus status, int requested = 0,
            int accepted = 0, int remaining = 0, string message = "", bool captured = false)
        {
            Id = id ?? string.Empty;
            Status = status;
            Requested = requested;
            Accepted = accepted;
            Remaining = remaining;
            Message = message ?? string.Empty;
            Captured = captured;
        }

        public static StorageOperation Unavailable(string message = "Storage unavailable") =>
            new StorageOperation(string.Empty, StorageOperationStatus.Unavailable, message: message);
    }

    // Descriptor and receipt survive reconnect; delegates are never the journal.
    public sealed class StorageEffectDescriptor
    {
        public string Kind { get; }
        public string TargetId { get; }
        public long ActorId { get; }
        public string Data { get; }

        public StorageEffectDescriptor(string kind, string targetId, long actorId, string data = "")
        {
            Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            TargetId = targetId ?? string.Empty;
            ActorId = actorId;
            Data = data ?? string.Empty;
        }
    }

    public enum StorageEffectResult { Applied, NotReady, Rejected, Uncertain }
    public enum StorageEffectRecovery { NotApplied, Applied, Uncertain }
}
