using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SmartCraftStorage.Storage.Core
{
    public sealed class StorageQueryRow
    {
        public string Identity { get; }
        public StorageStack Sample { get; }
        public int Amount { get; }
        public StorageQueryRow(string identity, StorageStack sample, int amount)
        { Identity = identity; Sample = sample; Amount = amount; }
    }

    public sealed class StorageQueryResult
    {
        public IReadOnlyList<StorageInventory> Inventories { get; }
        public IReadOnlyList<StorageQueryRow> Rows { get; }
        public int UsedSlots { get; }
        public int TotalSlots { get; }
        public StorageQueryResult(IEnumerable<StorageInventory> inventories, IEnumerable<StorageQueryRow> rows)
        {
            Inventories = new ReadOnlyCollection<StorageInventory>(inventories.ToList());
            Rows = new ReadOnlyCollection<StorageQueryRow>(rows.ToList());
            UsedSlots = Inventories.Sum(x => x.Items.Count);
            TotalSlots = Inventories.Sum(x => x.Capacity);
        }
    }

    public sealed class StoragePlanResult
    {
        public IReadOnlyList<StorageInventory> Inventories { get; }
        public int Requested { get; }
        public int Accepted { get; }
        public int Remaining { get; }
        public int Moves { get; }
        public StoragePlanResult(IEnumerable<StorageInventory> inventories, int requested, int accepted, int moves)
        {
            Inventories = new ReadOnlyCollection<StorageInventory>(inventories.ToList());
            Requested = requested;
            Accepted = accepted;
            Remaining = Math.Max(0, requested - accepted);
            Moves = moves;
        }
    }

    public static class StoragePlanner
    {
        public static StorageQueryResult Query(IEnumerable<StorageInventory> inventories)
        {
            var unique = Distinct(inventories);
            var rows = unique.SelectMany(x => x.Items)
                .GroupBy(x => x.Identity, StringComparer.Ordinal)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new StorageQueryRow(x.Key, x.OrderBy(i => i.Payload, StringComparer.Ordinal).First(), x.Sum(i => i.Amount)));
            return new StorageQueryResult(unique, rows);
        }

        public static StoragePlanResult Deposit(IEnumerable<StorageInventory> inventories, StorageStack source, int amount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            amount = Math.Max(0, Math.Min(amount, source.Amount));
            var current = Mutable(Distinct(inventories));
            var remaining = amount;

            foreach (var inventory in current)
            foreach (var item in inventory.Items.Where(x => x.Identity == source.Identity && x.Amount < x.MaxStack).OrderBy(x => x.Slot))
            {
                var accepted = Math.Min(remaining, item.MaxStack - item.Amount);
                item.Amount += accepted;
                remaining -= accepted;
                if (remaining == 0) return Finish(current, amount, remaining);
            }

            foreach (var inventory in current.OrderBy(x => x.Items.Count == 0 ? 1 : 0).ThenBy(x => x.Index))
            {
                foreach (var slot in FreeSlots(inventory))
                {
                    var accepted = Math.Min(remaining, source.MaxStack);
                    inventory.Items.Add(new MutableStack(source.At(slot, accepted)));
                    remaining -= accepted;
                    if (remaining == 0) return Finish(current, amount, remaining);
                }
            }
            return Finish(current, amount, remaining);
        }

        public static StoragePlanResult Withdraw(IEnumerable<StorageInventory> inventories, StorageInventory destination,
            string identity, int amount, int destinationSlot = -1)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            amount = Math.Max(0, amount);
            var sources = Mutable(Distinct(inventories));
            var dest = new MutableInventory(destination, sources.Count);
            var candidates = sources.SelectMany(x => x.Items.Select(i => (Owner: x, Item: i)))
                .Where(x => x.Item.Identity == identity).OrderBy(x => x.Owner.Index).ThenBy(x => x.Item.Slot).ToList();
            var available = candidates.Sum(x => x.Item.Amount);
            var sample = candidates.Select(x => x.Item).FirstOrDefault();
            var capacity = sample == null ? 0 : destinationSlot == -1 ? CapacityFor(dest, sample) : CapacityAt(dest, sample, destinationSlot);
            var accepted = Math.Min(amount, Math.Min(available, capacity));
            var remainingToMove = accepted;

            foreach (var candidate in candidates)
            {
                if (remainingToMove == 0) break;
                var take = Math.Min(remainingToMove, candidate.Item.Amount);
                candidate.Item.Amount -= take;
                if (destinationSlot == -1) AddExact(dest, candidate.Item, take);
                else
                {
                    var target = dest.Items.FirstOrDefault(x => x.Slot == destinationSlot);
                    if (target == null) dest.Items.Add(new MutableStack(candidate.Item.Freeze(destinationSlot, take)));
                    else target.Amount += take;
                }
                remainingToMove -= take;
                if (remainingToMove == 0) break;
            }
            foreach (var source in sources) source.Items.RemoveAll(x => x.Amount == 0);
            sources.Add(dest);
            return Finish(sources, amount, amount - accepted);
        }

        public static StoragePlanResult Organize(IEnumerable<StorageInventory> inventories)
        {
            var original = Distinct(inventories);
            var ordered = original.OrderByDescending(x => x.Capacity).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
            var target = ordered.Select((x, i) => new MutableInventory(x.Id, x.Revision, x.Width, x.Height, i)).ToList();
            var grouped = original.SelectMany(x => x.Items)
                .GroupBy(x => x.Identity, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new { Sample = x.OrderBy(i => i.Payload, StringComparer.Ordinal).First(), Amount = x.Sum(i => i.Amount) }).ToList();
            var cursor = 0;
            foreach (var group in grouped)
            {
                var remaining = group.Amount;
                while (remaining > 0)
                {
                    while (cursor >= target.Sum(x => x.Capacity)) throw new InvalidOperationException("Inventory contents exceed physical capacity.");
                    var owner = target.First(x => cursor < target.Take(x.Index + 1).Sum(y => y.Capacity));
                    var ownerStart = target.Take(owner.Index).Sum(x => x.Capacity);
                    var slot = cursor - ownerStart;
                    var take = Math.Min(remaining, group.Sample.MaxStack);
                    owner.Items.Add(new MutableStack(group.Sample.At(slot, take)));
                    remaining -= take;
                    cursor++;
                }
            }
            var finished = Freeze(target);
            var moves = CountChangedSlots(original.OrderBy(x => x.Id, StringComparer.Ordinal).ToList(), finished.OrderBy(x => x.Id, StringComparer.Ordinal).ToList());
            return new StoragePlanResult(finished, 0, 0, moves);
        }

        private static List<StorageInventory> Distinct(IEnumerable<StorageInventory> inventories) =>
            (inventories ?? Enumerable.Empty<StorageInventory>()).Where(x => x != null)
            .GroupBy(x => x.Id, StringComparer.Ordinal).Select(x => x.First()).ToList();

        private static List<MutableInventory> Mutable(IReadOnlyList<StorageInventory> source) =>
            source.Select((x, i) => new MutableInventory(x, i)).ToList();

        private static IEnumerable<int> FreeSlots(MutableInventory inventory)
        {
            var used = new HashSet<int>(inventory.Items.Select(x => x.Slot));
            for (var i = 0; i < inventory.Capacity; i++) if (!used.Contains(i)) yield return i;
        }

        private static int CapacityFor(MutableInventory inventory, MutableStack sample) =>
            inventory.Items.Where(x => x.Identity == sample.Identity).Sum(x => Math.Max(0, x.MaxStack - x.Amount)) +
            FreeSlots(inventory).Count() * sample.MaxStack;

        private static void AddExact(MutableInventory destination, MutableStack sample, int amount)
        {
            if (amount <= 0) return;
            foreach (var item in destination.Items.Where(x => x.Identity == sample.Identity && x.Amount < x.MaxStack).OrderBy(x => x.Slot))
            {
                var add = Math.Min(amount, item.MaxStack - item.Amount); item.Amount += add; amount -= add;
                if (amount == 0) return;
            }
            foreach (var slot in FreeSlots(destination).ToList())
            {
                var add = Math.Min(amount, sample.MaxStack); destination.Items.Add(new MutableStack(sample.Freeze(slot, add))); amount -= add;
                if (amount == 0) return;
            }
            if (amount != 0) throw new InvalidOperationException("Planned destination capacity was not available.");
        }

        private static int CapacityAt(MutableInventory inventory, MutableStack sample, int slot)
        {
            if (slot < 0 || slot >= inventory.Capacity) return 0;
            var target = inventory.Items.FirstOrDefault(x => x.Slot == slot);
            return target == null ? sample.MaxStack : target.Identity == sample.Identity ? Math.Max(0, target.MaxStack - target.Amount) : 0;
        }

        private static StoragePlanResult Finish(List<MutableInventory> inventories, int requested, int remaining) =>
            new StoragePlanResult(Freeze(inventories), requested, requested - remaining, 0);
        private static List<StorageInventory> Freeze(IEnumerable<MutableInventory> source) => source.Select(x => x.Freeze()).ToList();

        private static int CountChangedSlots(IReadOnlyList<StorageInventory> before, IReadOnlyList<StorageInventory> after)
        {
            var moves = 0;
            for (var i = 0; i < before.Count; i++)
            for (var slot = 0; slot < before[i].Capacity; slot++)
            {
                var a = before[i].Items.FirstOrDefault(x => x.Slot == slot);
                var b = after[i].Items.FirstOrDefault(x => x.Slot == slot);
                if (a == null && b == null) continue;
                if (a == null || b == null || a.Identity != b.Identity || a.Amount != b.Amount || a.Payload != b.Payload) moves++;
            }
            return moves;
        }

        private sealed class MutableInventory
        {
            public string Id; public string Revision; public int Width; public int Height; public int Index;
            public int Capacity => checked(Width * Height);
            public List<MutableStack> Items = new List<MutableStack>();
            public MutableInventory(StorageInventory source, int index) : this(source.Id, source.Revision, source.Width, source.Height, index)
            { Items.AddRange(source.Items.Select(x => new MutableStack(x))); }
            public MutableInventory(string id, string revision, int width, int height, int index)
            { Id = id; Revision = revision; Width = width; Height = height; Index = index; }
            public StorageInventory Freeze() => new StorageInventory(Id, Revision, Width, Height, Items.OrderBy(x => x.Slot).Select(x => x.Freeze()));
        }

        private sealed class MutableStack
        {
            public string Identity, Prefab, SharedName, Payload; public int Quality, WorldLevel, Amount, MaxStack, Slot;
            public MutableStack(StorageStack source) { Identity = source.Identity; Prefab = source.Prefab; SharedName = source.SharedName; Quality = source.Quality; WorldLevel = source.WorldLevel; Amount = source.Amount; MaxStack = source.MaxStack; Slot = source.Slot; Payload = source.Payload; }
            public StorageStack Freeze() => Freeze(Slot, Amount);
            public StorageStack Freeze(int slot, int amount) => new StorageStack(Identity, Prefab, SharedName, Quality, WorldLevel, amount, MaxStack, slot, Payload);
        }
    }
}
