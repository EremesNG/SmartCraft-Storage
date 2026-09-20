using System;
using System.Linq;
using SmartCraftStorage.Storage;
using SmartCraftStorage.Storage.Core;
using SmartCraftStorage.Storage.Runtime;

internal static class DurableIdentityScenarios
{
    internal static void Run()
    {
        JournalRemapsDurableZdoReferences();
        JournalRefusesMemoryOnlyWrites();
    }

    private static void JournalRemapsDurableZdoReferences()
    {
        var manager = new ZDOMan();
        var participant = manager.CreateNewZDO(new UnityEngine.Vector3(), "chest".GetStableHashCode());
        participant.Persistent = true;
        var source = manager.CreateNewZDO(new UnityEngine.Vector3(), "cooking".GetStableHashCode());
        source.Persistent = true;
        var oldParticipant = participant.m_uid.ToString();
        var oldSource = source.m_uid.ToString();
        var journal = new StorageJournal();
        journal.Save(new StorageTransactionRecord("stable:delivery", "output", StorageOperationStatus.Preparing,
            new[] { new StorageParticipantPlan(oldParticipant, "1", "payload", "before") }, actorId: 7));
        var capture = new StorageEffectDescriptor("processor.capture", oldSource, 7, "native-proof");
        journal.SaveEffect(new StorageEffectRecord(1, "42", "stable:output", 7, oldSource,
            "2|0|0|10|" + oldSource, capture, null, capture, "outbox", "stable:delivery", 1, 0, 1,
            StorageEffectStage.Captured));

        new ZDOMan(manager.Objects);
        var remappedParticipant = ZDOMan.instance.Objects.Single(x => x.GetString("scs.storage.identity.v1", "") ==
            participant.GetString("scs.storage.identity.v1", ""));
        var remappedSource = ZDOMan.instance.Objects.Single(x => x.GetString("scs.storage.identity.v1", "") ==
            source.GetString("scs.storage.identity.v1", ""));
        var transaction = journal.Load("stable:delivery");
        var effect = journal.LoadEffect("stable:output");

        Equal(remappedParticipant.m_uid.ToString(), transaction.Participants.Single().ParticipantId,
            "transaction participant follows its persistent object after native ID remapping");
        Equal(remappedSource.m_uid.ToString(), effect.TargetId,
            "effect target follows its persistent object after native ID remapping");
        Equal(remappedSource.m_uid.ToString(), effect.Capture.TargetId,
            "effect descriptor follows its persistent object after native ID remapping");
        Equal("2|0|0|10|" + remappedSource.m_uid, effect.Context,
            "authority context anchor follows its persistent object after native ID remapping");
        Equal("stable:output", effect.OperationId, "operation identity remains opaque and unchanged");
    }

    private static void JournalRefusesMemoryOnlyWrites()
    {
        new ZDOMan();
        var scene = ZNetScene.instance;
        ZNetScene.instance = null;
        try
        {
            var journal = new StorageJournal();
            var saved = journal.TrySave(new StorageTransactionRecord("unavailable", "output",
                StorageOperationStatus.Preparing, Array.Empty<StorageParticipantPlan>()));
            Equal(false, saved, "journal reports persistence unavailable");
            Equal<StorageTransactionRecord>(null, journal.Load("unavailable"),
                "unavailable journal never admits a memory-only transaction");
        }
        finally { ZNetScene.instance = scene; }
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new Exception(message + $"; expected {expected}, got {actual}");
    }
}
