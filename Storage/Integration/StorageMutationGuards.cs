using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SmartCraftStorage.Shared;
using SmartCraftStorage.Storage.Runtime;
using SmartCraftStorage.Storage.UI;

namespace SmartCraftStorage.Storage.Integration
{
    // Vanilla and MultiUserChest ultimately use these inventories. Reservations
    // remain owner-visible even when no terminal window is open on that peer.
    internal static class StorageMutationGuards
    {
        private static readonly ConditionalWeakTable<Inventory, ZNetView> Owners = new ConditionalWeakTable<Inventory, ZNetView>();

        internal static bool Busy(ZNetView view) => !StorageService.IsExecutingEffect && view != null &&
            view.IsValid() && StorageFacade.Service.IsBusy(view);

        private static bool Busy(Inventory inventory) => inventory != null &&
            Owners.TryGetValue(inventory, out var owner) && Busy(owner);

        private static void Track(Inventory inventory, ZNetView view)
        {
            if (inventory == null || view == null) return;
            Owners.Remove(inventory);
            Owners.Add(inventory, view);
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        private static class TrackContainer
        {
            private static void Postfix(Container __instance)
            {
                Track(__instance.GetInventory(), __instance.m_nview);
            }
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        private static class TrackPlayer
        {
            private static void Postfix(Player __instance)
            {
                Track(__instance.GetInventory(), __instance.m_nview);
            }
        }

        [HarmonyPatch]
        private static class ForgetOwner
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[]
            { AccessTools.Method(typeof(Container), "OnDestroyed"), AccessTools.Method(typeof(Player), "OnDestroy") };
            private static void Prefix(object __instance)
            {
                var inventory = (__instance as Container)?.GetInventory() ?? (__instance as Player)?.GetInventory();
                if (inventory != null) Owners.Remove(inventory);
                ChestCountCache.Invalidate();
            }
        }

        [HarmonyPatch]
        private static class InventoryWrites
        {
            private static readonly HashSet<string> Writes = new HashSet<string>
            { "AddItem", "RemoveItem", "RemoveAll", "MoveAll", "MoveItemToThis", "MoveInventoryToGrave", "StackAll" };
            private static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(Inventory))
                .Where(method => Writes.Contains(method.Name));

            [HarmonyPriority(Priority.First)]
            private static bool Prefix(Inventory __instance, object[] __args) =>
                !NativeTerminalInventory.IsProjection(__instance) &&
                !__args.OfType<Inventory>().Any(NativeTerminalInventory.IsProjection) &&
                !__args.OfType<ItemDrop.ItemData>().Any(NativeTerminalInventory.IsProjectedItem) &&
                !Busy(__instance) && !__args.OfType<Inventory>().Any(Busy);
        }

        [HarmonyPatch]
        private static class ContainerAccess
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[]
            { "Interact", "RPC_RequestOpen", "RPC_RequestTakeAll", "TakeAll" }
                .Select(name => AccessTools.Method(typeof(Container), name)).Where(method => method != null);

            [HarmonyPriority(Priority.First)]
            private static bool Prefix(Container __instance) => !Busy(__instance.m_nview);
        }

        [HarmonyPatch]
        private static class PlayerActions
        {
            private static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(Humanoid))
                .Where(method => new[] { "UseItem", "EquipItem", "UnequipItem", "DropItem", "Pickup" }.Contains(method.Name));

            [HarmonyPriority(Priority.First)]
            private static bool Prefix(Humanoid __instance, object[] __args) =>
                !__args.OfType<Inventory>().Any(NativeTerminalInventory.IsProjection) &&
                !__args.OfType<ItemDrop.ItemData>().Any(NativeTerminalInventory.IsProjectedItem) &&
                (!(__instance is Player player) || !Busy(player.m_nview));
        }

        [HarmonyPatch]
        private static class InventoryControls
        {
            private static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(InventoryGui))
                .Where(method => new[] { "OnSelectedItem", "OnReleasedItem", "OnRightClickItem", "OnDropOutside", "OnTakeAll", "OnStackAll" }.Contains(method.Name));

            [HarmonyPriority(Priority.First)]
            private static bool Prefix(InventoryGui __instance) => !Busy(Player.m_localPlayer?.m_nview) &&
                !Busy(__instance.m_currentContainer?.m_nview);
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        private static class ProjectionDrop
        {
            // Native incompatible swaps remove the real source before calling MoveItemToThis.
            // Guard the whole gesture so a read-only destination cannot strand that source.
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item) =>
                !NativeTerminalInventory.IsProjection(__instance.GetInventory()) &&
                !NativeTerminalInventory.IsProjection(fromInventory) && !NativeTerminalInventory.IsProjectedItem(item);
        }

        [HarmonyPatch]
        private static class ProcessorMutations
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new Dictionary<System.Type, string[]>
                {
                    [typeof(Smelter)] = new[] { "UpdateSmelter", "RPC_EmptyProcessed" },
                    [typeof(CookingStation)] = new[] { "UpdateCooking", "RPC_RemoveDoneItem" },
                    [typeof(Fermenter)] = new[] { "SlowUpdate", "RPC_Tap", "DelayedTap" },
                    [typeof(Fireplace)] = new[] { "UpdateFireplace" },
                    [typeof(Beehive)] = new[] { "UpdateBees", "RPC_Extract" }
                };
                foreach (var pair in methods)
                    foreach (string name in pair.Value)
                    {
                        var method = AccessTools.Method(pair.Key, name);
                        if (method != null) yield return method;
                    }
            }

            [HarmonyPriority(Priority.First)]
            private static bool Prefix(UnityEngine.Component __instance) => !Busy(__instance.GetComponent<ZNetView>());
        }

        [HarmonyPatch]
        private static class PreserveParticipant
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { "Destroy", "Remove", "RPC_Damage" }
                .Select(name => AccessTools.Method(typeof(WearNTear), name)).Where(method => method != null);
            private static bool Prefix(WearNTear __instance) => !Busy(__instance.GetComponent<ZNetView>());
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Destroy), new[] { typeof(UnityEngine.GameObject) })]
        private static class PreservePendingObject
        {
            private static bool Prefix(UnityEngine.GameObject go) => go == null || !Busy(go.GetComponent<ZNetView>());
        }
    }
}
