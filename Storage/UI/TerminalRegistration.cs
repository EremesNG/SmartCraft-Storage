using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

namespace SmartCraftStorage.Storage.UI
{
    internal static class TerminalRegistration
    {
        public static void Setup() => PrefabManager.OnVanillaPrefabsAvailable += Register;

        private static void Register()
        {
            // A real network prefab lets the normal world save/load and scene
            // lifecycle preserve the journal even on a dedicated server.
            if (PrefabManager.Instance.GetPrefab("SCS_StorageJournal") == null)
            {
                var journal = PrefabManager.Instance.CreateEmptyPrefab("SCS_StorageJournal", true);
                foreach (var renderer in journal.GetComponents<Renderer>()) Object.DestroyImmediate(renderer);
                foreach (var collider in journal.GetComponents<Collider>()) Object.DestroyImmediate(collider);
                var mesh = journal.GetComponent<MeshFilter>();
                if (mesh != null) Object.DestroyImmediate(mesh);
                var journalView = journal.GetComponent<ZNetView>();
                journalView.m_persistent = true;
                journalView.m_distant = true;
                PrefabManager.Instance.AddPrefab(new CustomPrefab(journal, false));
            }

            var prefab = PrefabManager.Instance.CreateClonedPrefab(StorageFacade.TerminalPrefab, "piece_chest_wood");
            if (prefab == null) return;
            var container = prefab.GetComponent<Container>();
            if (container != null) Object.DestroyImmediate(container);
            prefab.AddComponent<StorageTerminal>();
            var configuration = new PieceConfig
            {
                Name = "$scs_terminal_title",
                Description = "$scs_terminal_description",
                PieceTable = "Hammer",
                Category = "Furniture",
                CraftingStation = "piece_workbench",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 10, 0, true),
                    new RequirementConfig("Bronze", 2, 0, true)
                }
            };
            PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, configuration));
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
        }
    }
}
