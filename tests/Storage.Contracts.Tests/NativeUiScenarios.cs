using System;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.UI;

internal static class NativeUiScenarios
{
    public static void Run(Action<string, Action> register)
    {
        register("native grid shows aggregate counts with legal movable stacks and exact identities", Projection);
        register("native bulk submits once, waits for confirmation and discards only unsent work on close", Bulk);
        register("unknown replies retain custody and bulk rejection stops remaining steps", BulkRejectsSafely);
    }
    private static void Projection()
    {
        var model = new TerminalInteractionModel();
        model.Refresh(new[] { new TerminalCell("wood|normal", "Wood", 240, 50), new TerminalCell("wood|custom", "Wood", 70, 50), new TerminalCell("stone", "Stone", 9, 50) }, "wood", false, false);
        Equal(2, model.Cells.Count); Equal("wood|custom", model.Cells[0].Identity);
        Equal(240, model.Cells[1].Total); Equal(50, model.Cells[1].StackAmount);
        model.Refresh(new[] { new TerminalCell("stone", "Stone", 5, 50) }, "", false, true);
        Equal("wood|custom", model.Cells[0].Identity);
    }
    private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Bulk()
    {
        var model = new TerminalInteractionModel(); var calls = 0;
        model.Enqueue(new[] { new TerminalTransfer("wood", 50), new TerminalTransfer("stone", 50) });
        StorageOperation Send(TerminalTransfer transfer) { calls++; return new StorageOperation("move:" + calls, StorageOperationStatus.Requested); }
        Equal(true, model.SubmitNext(Send)); Equal(false, model.SubmitNext(Send)); Equal(1, calls);
        Equal(false, model.Refresh(new[] { new TerminalCell("stone", "Stone", 5, 50) }, "", false, false));
        model.Observe(new StorageOperation("unrelated", StorageOperationStatus.Confirmed)); Equal(true, model.Busy);
        model.Close();
        model.Observe(new StorageOperation("move:1", StorageOperationStatus.Confirmed, 50, 50));
        Equal(false, model.SubmitNext(Send)); Equal(1, calls);
    }
    private static void BulkRejectsSafely()
    {
        var model = new TerminalInteractionModel(); var calls = 0;
        model.Enqueue(new[] { new TerminalTransfer("wood", 50), new TerminalTransfer("stone", 20), new TerminalTransfer("ore", 5) });
        StorageOperation Send(TerminalTransfer transfer) { calls++; return new StorageOperation("bulk:" + calls, StorageOperationStatus.Requested); }
        model.SubmitNext(Send);
        model.Observe(new StorageOperation("bulk:1", StorageOperationStatus.Unavailable));
        Equal(true, model.Busy); Equal(false, model.SubmitNext(Send));
        model.Observe(new StorageOperation("bulk:1", StorageOperationStatus.Confirmed, 50, 50));
        Equal(true, model.SubmitNext(Send)); Equal(2, calls);
        model.Observe(new StorageOperation("bulk:2", StorageOperationStatus.Rejected));
        Equal(false, model.SubmitNext(Send)); Equal(false, model.HasQueued); Equal(2, calls);
    }
}
