using System;
using System.Collections.Generic;

namespace SmartCraftStorage.Storage.Integration
{
    // Production decision boundary used by the native craft and machine adapters.
    // Game callbacks run only after the complete prepared cost has been checked.
    public static class StationOperationController
    {
        // The intent is persisted before native mutation. Unknown state is never
        // evidence for replay, success, or a refund of the paid resources.
        public static StorageEffectRecovery ReconcileNativeState(string before, string proposed, string current)
        {
            if (before == null || current == null) return StorageEffectRecovery.Uncertain;
            if (proposed != null && proposed != before && string.Equals(current, proposed, StringComparison.Ordinal))
                return StorageEffectRecovery.Applied;
            return string.Equals(current, before, StringComparison.Ordinal)
                ? StorageEffectRecovery.NotApplied : StorageEffectRecovery.Uncertain;
        }

        public static StorageOutputDecision OutputDecision(StorageOperation operation, StorageProducer producer)
        {
            if (operation == null) return new StorageOutputDecision(StorageOutputKind.Wait, 0);
            if (!operation.Captured)
                return new StorageOutputDecision(StorageOutputKind.Original, operation.Remaining);
            if (operation.Status != StorageOperationStatus.Confirmed)
                return new StorageOutputDecision(StorageOutputKind.Wait, 0);
            if (operation.Remaining == 0) return new StorageOutputDecision(StorageOutputKind.Complete, 0);
            return new StorageOutputDecision(producer == StorageProducer.BeehiveAutomatic
                ? StorageOutputKind.Retain : StorageOutputKind.Drop, operation.Remaining);
        }

        public static StorageWorkDecision InputDecision(StorageOperation pending, int freeCapacity)
        {
            if (pending != null && !pending.IsFinal) return StorageWorkDecision.Resume;
            return freeCapacity > 0 ? StorageWorkDecision.Start : StorageWorkDecision.Wait;
        }

        public static StorageIngredientSelection SelectSingleIngredient(IReadOnlyList<StorageStack> available,
            IReadOnlyList<StorageRequirement> alternatives)
        {
            for (int index = 0; index < alternatives.Count; index++)
            {
                var requirement = alternatives[index];
                if (requirement.Amount <= 0 || !CanPay(available, new[] { requirement })) continue;
                foreach (var item in available)
                    if (item.Amount > 0 && Matches(item, requirement))
                        return new StorageIngredientSelection(index, item);
            }
            return null;
        }

        public static StorageEffectResult ApplyPaidEffect(IReadOnlyList<StorageStack> escrow,
            IReadOnlyList<StorageRequirement> cost, bool contextValid, Func<StorageEffectResult> apply)
        {
            if (!contextValid || !CanPay(escrow, cost)) return StorageEffectResult.Rejected;
            return apply();
        }

        public static bool CanPay(IReadOnlyList<StorageStack> stock, IReadOnlyList<StorageRequirement> cost)
        {
            var remaining = new int[stock.Count];
            for (int i = 0; i < stock.Count; i++) remaining[i] = stock[i].Amount;
            foreach (var requirement in cost)
            {
                if (requirement.Amount < 0) return false;
                int needed = requirement.Amount;
                for (int i = 0; i < stock.Count && needed > 0; i++)
                {
                    var item = stock[i];
                    if (!Matches(item, requirement)) continue;
                    int take = Math.Min(needed, remaining[i]);
                    needed -= take;
                    remaining[i] -= take;
                }
                if (needed != 0) return false;
            }
            return true;
        }

        private static bool Matches(StorageStack item, StorageRequirement requirement) =>
            item.SharedName == requirement.SharedName &&
            (requirement.Quality < 0 || item.Quality == requirement.Quality) &&
            (requirement.WorldLevel < 0 || item.WorldLevel >= requirement.WorldLevel) &&
            (requirement.Identity == null || item.Identity == requirement.Identity);
    }

    public sealed class StorageIngredientSelection
    {
        public int RequirementIndex { get; }
        public StorageStack Sample { get; }
        public StorageIngredientSelection(int requirementIndex, StorageStack sample)
        { RequirementIndex = requirementIndex; Sample = sample; }
    }

    public enum StorageWorkDecision { Wait, Resume, Start }
    public enum StorageProducer { Smelter, Kiln, Cooking, Fermenter, BeehiveAutomatic, BeehiveManual }
    public enum StorageOutputKind { Wait, Original, Complete, Drop, Retain }

    public sealed class StorageOutputDecision
    {
        public StorageOutputKind Kind { get; }
        public int Amount { get; }
        public StorageOutputDecision(StorageOutputKind kind, int amount) { Kind = kind; Amount = amount; }
    }
}
