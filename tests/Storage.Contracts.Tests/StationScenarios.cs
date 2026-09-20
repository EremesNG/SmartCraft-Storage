using System;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Integration;
using SmartCraftStorage.Storage.UI;

public static class StationScenarios
{
    public static void Run(Action<string, Action> register)
    {
        register("A craft with an incomplete prepared cost creates no result", IncompleteCost);
        register("A network-only single ingredient preserves its real quality and payload", NetworkOnlyIngredient);
        register("Native crafting receives paid resources before producing its result", CompletePaymentBeforeResult);
        register("Repeated processor ticks resume the existing operation and respect input capacity", ProcessorTicks);
        register("Unknown output stays pending and partial output uses only its confirmed remainder", OutputRemainder);
        register("Native reconciliation recognizes committed state after an exception without replay", NativeCommittedAfterException);
        register("Native reconciliation preserves uncertain or metadata-divergent effects", NativeUncertainState);
        register("Bulk deposit survives replaced item instances without taking newly added quantities", BulkDepositSelection);
    }

    private static void BulkDepositSelection()
    {
        var selected = new StorageStack("wood|maker-A", "Wood", "$wood", 1, 0, 12, 50, 3, "original");
        var current = new[]
        {
            new StorageStack("wood|maker-B", "Wood", "$wood", 1, 0, 50, 50, 1, "different-metadata"),
            new StorageStack("wood|maker-A", "Wood", "$wood", 1, 0, 25, 50, 3, "reloaded-instance")
        };
        var resolved = DepositSelection.Resolve(selected, current);
        if (resolved == null || resolved.Slot != 3 || resolved.Amount != 12 || resolved.Payload != "reloaded-instance")
            throw new Exception("Bulk deposit used a stale reference or included items added after the user's selection.");
        if (DepositSelection.Resolve(selected, new[] { current[0] }) != null ||
            DepositSelection.Resolve(selected, new[] { current[1].At(4, 25) }) != null)
            throw new Exception("Bulk deposit moved a replacement or relocated item without a new selection.");
        var partial = DepositSelection.Resolve(selected, new[] { current[1].At(3, 7) });
        if (partial == null || partial.Amount != 7) throw new Exception("Bulk deposit ignored a reduced remaining stack.");
    }

    private static void NativeCommittedAfterException()
    {
        int nativeCalls = 0;
        string current = "ore-queue:iron,copper";
        string before = current, proposed = "ore-queue:iron,copper,tin";
        try { nativeCalls++; current = proposed; throw new InvalidOperationException("visual effect failed after queue mutation"); }
        catch (InvalidOperationException) { }
        var result = StationOperationController.ReconcileNativeState(before, proposed, current);
        if (result == StorageEffectRecovery.NotApplied) nativeCalls++;
        if (result != StorageEffectRecovery.Applied || nativeCalls != 1)
            throw new Exception("Confirmed native state was replayed after a post-mutation exception.");
    }

    private static void NativeUncertainState()
    {
        if (StationOperationController.ReconcileNativeState("old:q1:maker-A", "new:q2:maker-A", "new:q2:maker-B") != StorageEffectRecovery.Uncertain)
            throw new Exception("A different item with matching quality was treated as the original effect.");
        if (StationOperationController.ReconcileNativeState("fuel:1", "fuel:2", "fuel:1") != StorageEffectRecovery.NotApplied)
            throw new Exception("A proven unchanged native state could not be retried safely.");
        if (StationOperationController.ReconcileNativeState("fuel:1", null, "fuel:2") != StorageEffectRecovery.Uncertain ||
            StationOperationController.ReconcileNativeState(null, "fuel:2", "fuel:2") != StorageEffectRecovery.Uncertain)
            throw new Exception("A missing persisted intent was guessed from current state.");
    }

    private static void IncompleteCost()
    {
        int results = 0;
        var stock = new[] { new StorageStack("wood", "Wood", "$wood", 1, 0, 9, 50, 0, "original") };
        var result = StationOperationController.ApplyPaidEffect(stock,
            new[] { new StorageRequirement("$wood", 10) }, true,
            () => { results++; return StorageEffectResult.Applied; });
        if (result != StorageEffectResult.Rejected || results != 0)
            throw new Exception("Incomplete payment produced a craft result.");
        if (stock[0].Amount != 9) throw new Exception("Rejected payment lost resources.");
    }

    private static void NetworkOnlyIngredient()
    {
        var available = new[] { new StorageStack("fish|q3", "Fish1", "$fish", 3, 0, 2, 20, 0, "rare-fish-metadata") };
        var chosen = StationOperationController.SelectSingleIngredient(available, new[]
        {
            new StorageRequirement("$fish", 2, 1), new StorageRequirement("$fish", 2, 2), new StorageRequirement("$fish", 2, 3)
        });
        if (chosen == null || chosen.RequirementIndex != 2 || chosen.Sample.Quality != 3 || chosen.Sample.Payload != "rare-fish-metadata")
            throw new Exception("Recipe did not receive the actual selected network ingredient.");
    }

    private static void CompletePaymentBeforeResult()
    {
        int resultCount = 0;
        var escrow = new System.Collections.Generic.List<StorageStack>
        { new StorageStack("wood", "Wood", "$wood", 1, 0, 10, 50, 0, "original") };
        var cost = new[] { new StorageRequirement("$wood", 10) };
        Func<StorageEffectResult> nativeCraft = () =>
        {
            if (escrow.Count != 1 || escrow[0].Amount != 10) throw new Exception("Native craft ran before complete payment.");
            resultCount++; // Vanilla creates its result before calling ConsumeResources.
            escrow.Clear();
            return StorageEffectResult.Applied;
        };
        StationOperationController.ApplyPaidEffect(escrow, cost, true, nativeCraft);
        StationOperationController.ApplyPaidEffect(escrow, cost, true, nativeCraft);
        if (resultCount != 1 || escrow.Count != 0) throw new Exception("Prepared cost was not consumed exactly once.");
    }

    private static void ProcessorTicks()
    {
        var pending = new StorageOperation("ore:42", StorageOperationStatus.RecoveryPending);
        if (StationOperationController.InputDecision(pending, 2) != StorageWorkDecision.Resume ||
            StationOperationController.InputDecision(pending, 0) != StorageWorkDecision.Resume ||
            StationOperationController.InputDecision(null, 0) != StorageWorkDecision.Wait ||
            StationOperationController.InputDecision(null, 1) != StorageWorkDecision.Start)
            throw new Exception("Processor duplicated pending input or ignored finite input capacity.");
        var fuel = new[] { new StorageStack("coal", "Coal", "$coal", 1, 0, 1, 50, 0, "fuel") };
        int machineFuel = 10;
        var result = StationOperationController.ApplyPaidEffect(fuel,
            new[] { new StorageRequirement("$coal", 1) }, machineFuel < 10,
            () => { machineFuel++; return StorageEffectResult.Applied; });
        if (result != StorageEffectResult.Rejected || machineFuel != 10 || fuel[0].Amount != 1)
            throw new Exception("Capacity changed while preparing, but fuel was still consumed.");
    }

    private static void OutputRemainder()
    {
        var unknown = new StorageOperation("output:7", StorageOperationStatus.RecoveryPending, 10, 8, 2, captured: true);
        if (StationOperationController.OutputDecision(unknown, StorageProducer.Smelter).Kind != StorageOutputKind.Wait)
            throw new Exception("An uncertain deposit was also dropped.");
        var confirmed = new StorageOperation("output:7", StorageOperationStatus.Confirmed, 10, 8, 2, captured: true);
        foreach (var producer in new[] { StorageProducer.Smelter, StorageProducer.Kiln, StorageProducer.Cooking, StorageProducer.Fermenter, StorageProducer.BeehiveManual })
        {
            var action = StationOperationController.OutputDecision(confirmed, producer);
            if (action.Kind != StorageOutputKind.Drop || action.Amount != 2)
                throw new Exception(producer + " did not preserve the exact confirmed remainder.");
        }
        var hive = StationOperationController.OutputDecision(confirmed, StorageProducer.BeehiveAutomatic);
        if (hive.Kind != StorageOutputKind.Retain || hive.Amount != 2)
            throw new Exception("Automatic honey collection dropped its unaccepted production.");
        var rejected = new StorageOperation("output:8", StorageOperationStatus.Rejected, 10, 0, 10);
        if (StationOperationController.OutputDecision(rejected, StorageProducer.Cooking).Kind != StorageOutputKind.Original)
            throw new Exception("Capture refusal swallowed the original native production.");
        var merelyRequested = new StorageOperation("output:9", StorageOperationStatus.Requested, 10, 0, 10);
        if (StationOperationController.OutputDecision(merelyRequested, StorageProducer.Smelter).Kind != StorageOutputKind.Original)
            throw new Exception("A queued request without durable capture swallowed the native production.");
    }
}
