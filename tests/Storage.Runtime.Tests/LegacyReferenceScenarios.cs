using System;
using System.IO;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;
using SmartCraftStorage.Storage.Runtime;

internal static class LegacyReferenceScenarios
{
    internal static void EmbeddedRootNeverClaimsReusedId()
    {
        var foreign = ForeignObject();
        var root = JournalObject(); root.Set("scs.storage.journal.root.v1", true);
        root.Set("scs.storage.journal.v1", Encoded(writer =>
        {
            writer.Write(1); writer.Write(1); // Original embedded journal, one transaction.
            writer.Write("old-root"); writer.Write("cost"); writer.Write((int)StorageOperationStatus.Applying);
            writer.Write(""); writer.Write(1); writer.Write(foreign.m_uid.ToString()); writer.Write("old-revision"); writer.Write("layout");
        }));
        var record = new StorageJournal().Load("old-root");
        Require(record != null && record.Participants.Single().ParticipantId.StartsWith("unresolved:", StringComparison.Ordinal),
            "legacy root migration must leave a stale-but-valid native reference unresolved");
        Require(foreign.GetString("scs.storage.identity.v1", "") == "",
            "legacy root migration must never stamp the unrelated object currently occupying an old ID");
    }

    internal static void SeparateRecordsNeverClaimReusedId()
    {
        var foreign = ForeignObject();
        var transaction = JournalObject();
        transaction.Set("scs.storage.journal.record.v1", "old-record");
        transaction.Set("scs.storage.journal.record.type.v1", "transaction");
        transaction.Set("scs.storage.journal.record.data.v1", Encoded(writer =>
        {
            writer.Write("old-record"); writer.Write("cost"); writer.Write((int)StorageOperationStatus.Applying);
            writer.Write(""); writer.Write(1); writer.Write(1); writer.Write(7L); writer.Write(1);
            writer.Write(foreign.m_uid.ToString()); writer.Write("old-revision"); writer.Write("layout"); writer.Write("before");
        }));
        var effect = JournalObject();
        effect.Set("scs.storage.journal.record.v1", "old-effect");
        effect.Set("scs.storage.journal.record.type.v1", "effect");
        effect.Set("scs.storage.journal.record.data.v1", Encoded(writer =>
        {
            writer.Write(1); writer.Write("42"); writer.Write("old-effect"); writer.Write(7L);
            writer.Write(foreign.m_uid.ToString()); writer.Write("3|0|0|0|10|" + foreign.m_uid);
            writer.Write(true); writer.Write("processor.capture"); writer.Write(foreign.m_uid.ToString()); writer.Write(7L); writer.Write("capture-data");
            writer.Write(false); writer.Write(false); writer.Write("custody"); writer.Write("old-record");
            writer.Write(1); writer.Write(0); writer.Write(1); writer.Write((int)StorageEffectStage.Captured);
            writer.Write(0); writer.Write(""); writer.Write(false);
        }));
        var journal = new StorageJournal();
        Require(journal.Load("old-record").Participants.Single().ParticipantId.StartsWith("unresolved:", StringComparison.Ordinal),
            "a legacy transaction without a reference trailer must not resolve a reused raw ID");
        var captured = journal.LoadEffect("old-effect");
        Require(captured.TargetId.StartsWith("unresolved:", StringComparison.Ordinal) &&
            captured.Capture.TargetId.StartsWith("unresolved:", StringComparison.Ordinal) && captured.Context.Contains("|unresolved:"),
            "legacy effect target, descriptor and context references must all fail closed");
        Require(captured.OperationId == "old-effect" && captured.Escrow == "custody" && captured.Remaining == 1,
            "unresolved legacy references preserve the operation and accounted item custody");
        Require(foreign.GetString("scs.storage.identity.v1", "") == "", "reading old records must not identify the unrelated current object");
    }

    private static ZDO ForeignObject()
    {
        new ZDOMan();
        var foreign = ZDOMan.instance.CreateNewZDO(new UnityEngine.Vector3(), "unrelated-chest".GetStableHashCode());
        foreign.Persistent = true; return foreign;
    }
    private static ZDO JournalObject()
    {
        var zdo = ZDOMan.instance.CreateNewZDO(new UnityEngine.Vector3(), "SCS_StorageJournal".GetStableHashCode());
        zdo.Persistent = true; zdo.Set("scs.storage.journal.world.v1", "42"); return zdo;
    }
    private static string Encoded(Action<BinaryWriter> write)
    {
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
        { write(writer); return Convert.ToBase64String(stream.ToArray()); }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
