using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SmartCraftStorage.Config;
using SmartCraftStorage.Storage.Runtime;
using SmartCraftStorage.Storage.UI;

namespace SmartCraftStorage.Storage.Integration
{
    internal static class CraftingStoragePatch
    {
        private const string PendingKey = "scs.craft.pending.v1";
        private const string PreparedResultKey = "scs.craft.result.v1";
        private static Inventory _prepared;
        private static Player _replayingPlayer;
        private static readonly AccessTools.FieldRef<Humanoid, Inventory> PlayerInventory =
            AccessTools.FieldRefAccess<Humanoid, Inventory>(nameof(Humanoid.m_inventory));

        internal static void Setup() => StorageFacade.Service.RegisterEffect(new CraftEffect());
        internal static bool IsReplaying(Player player) => _prepared != null && player == _replayingPlayer;
        internal static Inventory PreparedInventory => _prepared;

        internal static StorageContext Context(Player player) => new StorageContext(player,
            player.transform.position, ModConfig.CraftingChestRadius.Value, StorageScope.Crafting,
            player.GetCurrentCraftingStation()?.GetComponent<ZNetView>());

        internal static List<ItemDrop.ItemData> Available(Player player, bool includePlayer)
        {
            if (IsReplaying(player)) return _prepared.GetAllItems().ToList();
            var items = includePlayer ? player.GetInventory().GetAllItems().ToList() : new List<ItemDrop.ItemData>();
            items.AddRange(StorageFacade.Service.Query(Context(player)).Rows.Select(row =>
            {
                var item = row.Sample.Clone();
                item.m_stack = row.Amount;
                return item;
            }));
            return items.Where(item => item != null).ToList();
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        private static class CraftGate
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(InventoryGui __instance, Player player)
            {
                if (IsReplaying(player) || player != Player.m_localPlayer || player.GetCurrentCraftingStation() == null ||
                    player.InPlaceMode() || player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)) return true;
                if (__instance.m_craftRecipe == null) return false;
                if (player.m_customData.TryGetValue(PendingKey, out var pendingId))
                {
                    var pending = StorageFacade.Service.GetOperation(pendingId);
                    if (!pending.IsFinal) { StorageFacade.Service.Resume(pendingId); return false; }
                    player.m_customData.Remove(PendingKey);
                }
                if (StorageFacade.Service.IsBusy(player.m_nview)) return false;
                var request = CraftRequest.Capture(__instance, player);
                if (request == null) return false;
                var personal = player.GetInventory().GetAllItems().Select((item, index) => GameInventoryAdapter.ToStack(item, index)).ToList();
                if (StationOperationController.CanPay(personal, request.Cost)) return true;
                if (!StorageFacade.Service.Ready) return false;
                string id = StorageFacade.NewPlayerOperation(player, "craft");
                player.m_customData[PendingKey] = id;
                var operation = StorageFacade.Service.PrepareCost(Context(player), request.Cost, true,
                    new StorageEffectDescriptor("craft", "player:" + player.GetPlayerID(), player.GetPlayerID(), request.Encode()), id);
                if (operation.IsFinal && operation.Status != StorageOperationStatus.Confirmed)
                    player.m_customData.Remove(PendingKey);
                if (operation.Status != StorageOperationStatus.Confirmed)
                    player.Message(MessageHud.MessageType.Center, TerminalTranslations.Text(operation.IsFinal ? "rejected" : "pending"));
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
        private static class SelectRequiredItem
        {
            private static bool Prefix(Player __instance, Inventory inventory, Recipe recipe, int qualityLevel,
                ref int amount, ref int extraAmount, int craftMultiplier, ref ItemDrop.ItemData __result)
            {
                if (__instance != Player.m_localPlayer || inventory != __instance.GetInventory() ||
                    __instance.GetCurrentCraftingStation() == null || __instance.InPlaceMode()) return true;
                var items = Available(__instance, true);
                var choices = Choices(recipe, __instance, qualityLevel, craftMultiplier);
                var selection = StationOperationController.SelectSingleIngredient(
                    items.Select((item, index) => GameInventoryAdapter.ToStack(item, index)).ToList(), choices.Select(c => c.Cost).ToList());
                amount = 0;
                extraAmount = 0;
                __result = null;
                if (selection == null) return false;
                var choice = choices[selection.RequirementIndex];
                amount = choice.Cost.Amount;
                extraAmount = choice.Extra;
                __result = items.First(item => GameInventoryAdapter.Identity(item) == selection.Sample.Identity);
                return false;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.ItemCheated), new[] { typeof(Piece.Requirement[]), typeof(int), typeof(bool) })]
        private static class PreparedCheatedFlag
        {
            private static void Postfix(Inventory __instance, ref bool __result)
            {
                if (_prepared != null && _replayingPlayer != null && __instance == _replayingPlayer.GetInventory())
                    __result |= _prepared.GetAllItems().Any(item => item.m_cheated);
            }
        }

        private static List<IngredientChoice> Choices(Recipe recipe, Player player, int quality, int multiplier)
        {
            var result = new List<IngredientChoice>();
            bool upgrader = player.GetCurrentCraftingStation() != null && player.GetCurrentCraftingStation().m_upgrader;
            foreach (var resource in recipe.m_resources)
            {
                if (resource.m_resItem == null || resource.m_upgraderResource != upgrader) continue;
                int amount = checked(resource.GetAmount(quality) * multiplier);
                if (amount <= 0) continue;
                for (int itemQuality = 0; itemQuality <= resource.m_resItem.m_itemData.m_shared.m_maxQuality; itemQuality++)
                    result.Add(new IngredientChoice(new StorageRequirement(resource.m_resItem.m_itemData.m_shared.m_name,
                        amount, itemQuality, worldLevel: Game.m_worldLevel), resource.m_extraAmountOnlyOneIngredient));
            }
            return result;
        }

        private sealed class IngredientChoice
        {
            internal readonly StorageRequirement Cost;
            internal readonly int Extra;
            internal IngredientChoice(StorageRequirement cost, int extra) { Cost = cost; Extra = extra; }
        }

        private sealed class CraftRequest
        {
            internal string RecipeName, StationId, UpgradeIdentity;
            internal int Quality, Multiplier, Variant, UpgradeX, UpgradeY;
            internal readonly List<StorageRequirement> Cost = new List<StorageRequirement>();

            internal static CraftRequest Capture(InventoryGui gui, Player player)
            {
                var recipe = gui.m_craftRecipe;
                var upgrade = gui.m_craftUpgradeItem;
                int quality = upgrade == null ? 1 : upgrade.m_quality + 1;
                int multiplier = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
                if (multiplier < 1 || !player.RequiredCraftingStation(recipe, quality, true)) return null;
                var station = player.GetCurrentCraftingStation()?.GetComponent<ZNetView>();
                if (station == null || !station.IsValid()) return null;
                var request = new CraftRequest
                {
                    RecipeName = recipe.name, StationId = StorageDiscovery.Id(station), Quality = quality,
                    Multiplier = multiplier, Variant = gui.m_craftVariant,
                    UpgradeIdentity = upgrade == null ? "" : GameInventoryAdapter.Identity(upgrade),
                    UpgradeX = upgrade?.m_gridPos.x ?? -1, UpgradeY = upgrade?.m_gridPos.y ?? -1
                };
                var stock = Available(player, true).Select((item, index) => GameInventoryAdapter.ToStack(item, index)).ToList();
                if (recipe.m_requireOnlyOneIngredient)
                {
                    var choices = Choices(recipe, player, quality, multiplier);
                    var choice = StationOperationController.SelectSingleIngredient(stock, choices.Select(c => c.Cost).ToList());
                    if (choice == null) return null;
                    request.Cost.Add(choices[choice.RequirementIndex].Cost);
                }
                else
                {
                    bool upgrader = player.GetCurrentCraftingStation().m_upgrader;
                    foreach (var resource in recipe.m_resources)
                    {
                        if (resource.m_resItem == null || resource.m_upgraderResource != upgrader) continue;
                        int amount = checked(resource.GetAmount(quality) * multiplier);
                        if (amount <= 0) continue;
                        StorageRequirement selected = null;
                        for (int itemQuality = 1; itemQuality <= resource.m_resItem.m_itemData.m_shared.m_maxQuality; itemQuality++)
                        {
                            var cost = new StorageRequirement(resource.m_resItem.m_itemData.m_shared.m_name,
                                amount, itemQuality, worldLevel: Game.m_worldLevel);
                            if (!StationOperationController.CanPay(stock, new[] { cost })) continue;
                            selected = cost;
                            break;
                        }
                        if (selected == null) return null;
                        request.Cost.Add(selected);
                    }
                }
                return StationOperationController.CanPay(stock, request.Cost) ? request : null;
            }

            internal string Encode()
            {
                var p = new ZPackage();
                p.Write(1); p.Write(RecipeName); p.Write(StationId); p.Write(UpgradeIdentity);
                p.Write(Quality); p.Write(Multiplier); p.Write(Variant); p.Write(UpgradeX); p.Write(UpgradeY);
                p.Write(Cost.Count);
                foreach (var cost in Cost) { p.Write(cost.SharedName); p.Write(cost.Amount); p.Write(cost.Quality); p.Write(cost.WorldLevel); }
                return Convert.ToBase64String(p.GetArray());
            }

            internal static CraftRequest Decode(string data)
            {
                var p = new ZPackage(Convert.FromBase64String(data));
                if (p.ReadInt() != 1) throw new InvalidOperationException("Unsupported craft request version");
                var result = new CraftRequest
                {
                    RecipeName = p.ReadString(), StationId = p.ReadString(), UpgradeIdentity = p.ReadString(),
                    Quality = p.ReadInt(), Multiplier = p.ReadInt(), Variant = p.ReadInt(), UpgradeX = p.ReadInt(), UpgradeY = p.ReadInt()
                };
                int count = p.ReadInt();
                if (count < 0 || count > 64) throw new InvalidOperationException("Invalid craft cost count");
                for (int i = 0; i < count; i++) result.Cost.Add(new StorageRequirement(p.ReadString(), p.ReadInt(), p.ReadInt(), worldLevel: p.ReadInt()));
                return result;
            }

            internal bool StillSelected(Player player, InventoryGui gui)
            {
                if (gui == null || gui.m_craftRecipe == null || gui.m_craftRecipe.name != RecipeName ||
                    gui.m_selectedRecipe.Recipe == null || gui.m_selectedRecipe.Recipe.name != RecipeName ||
                    player.GetCurrentCraftingStation() == null || player.IsDead() || player.InPlaceMode() ||
                    (gui.m_multiCrafting ? gui.m_multiCraftAmount : 1) != Multiplier || gui.m_craftVariant != Variant) return false;
                var station = player.GetCurrentCraftingStation().GetComponent<ZNetView>();
                if (station == null || !station.IsValid() || StorageDiscovery.Id(station) != StationId) return false;
                if (!player.RequiredCraftingStation(gui.m_craftRecipe, Quality, true)) return false;
                if (UpgradeIdentity.Length == 0) return gui.m_craftUpgradeItem == null;
                var item = gui.m_craftUpgradeItem;
                return item != null && player.GetInventory().ContainsItem(item) && item.m_gridPos.x == UpgradeX && item.m_gridPos.y == UpgradeY &&
                    GameInventoryAdapter.Identity(item) == UpgradeIdentity && item.m_quality + 1 == Quality;
            }
        }

        private sealed class CraftEffect : IStorageEffectHandler
        {
            public string Kind => "craft";

            public string CaptureState(string operationId, StorageEffectDescriptor effect, Inventory escrow) =>
                GameInventoryAdapter.Revision(Player.m_localPlayer.GetInventory());

            public StorageEffectRecovery Reconcile(string operationId, StorageEffectDescriptor effect, Inventory escrow, string beforeState)
            {
                var player = Player.m_localPlayer;
                if (player == null || player.GetPlayerID() != effect.ActorId) return StorageEffectRecovery.Uncertain;
                var result = ReadPrepared(player, operationId, effect.Data);
                return StationOperationController.ReconcileNativeState(beforeState, result?.Revision,
                    GameInventoryAdapter.Revision(player.GetInventory()));
            }

            public StorageEffectResult Validate(StorageEffectDescriptor effect, Inventory escrow, out string reason)
            {
                reason = "";
                var player = Player.m_localPlayer;
                if (player == null || player.GetPlayerID() != effect.ActorId) return StorageEffectResult.NotReady;
                // A native outcome already staged for this request includes its
                // random bonuses/upgrade result and must not be rolled again.
                if (HasPrepared(player, effect.Data)) return StorageEffectResult.Applied;
                var request = CraftRequest.Decode(effect.Data);
                if (!request.StillSelected(player, InventoryGui.instance)) { reason = "Craft selection or station changed"; return StorageEffectResult.Rejected; }
                return StationOperationController.CanPay(GameInventoryAdapter.Snapshot("craft-escrow", "", escrow).Items, request.Cost)
                    ? StorageEffectResult.Applied : StorageEffectResult.Rejected;
            }

            public StorageEffectResult Apply(string operationId, StorageEffectDescriptor effect, Inventory escrow)
            {
                var player = Player.m_localPlayer;
                if (player == null || player.GetPlayerID() != effect.ActorId) return StorageEffectResult.NotReady;
                var request = CraftRequest.Decode(effect.Data);
                var preparedResult = ReadPrepared(player, operationId, effect.Data);
                if (preparedResult != null) return Commit(player, preparedResult);
                var result = StationOperationController.ApplyPaidEffect(GameInventoryAdapter.Snapshot("craft-escrow", "", escrow).Items,
                    request.Cost, request.StillSelected(player, InventoryGui.instance), () =>
                    {
                        var original = player.GetInventory();
                        var staged = new Inventory("SCS staged craft", null, original.GetWidth(), original.GetHeight());
                        staged.GetAllItems().AddRange(original.GetAllItems().Select(item => item.Clone()));
                        var gui = InventoryGui.instance;
                        var originalUpgrade = gui.m_craftUpgradeItem;
                        try
                        {
                            _prepared = escrow;
                            _replayingPlayer = player;
                            PlayerInventory(player) = staged;
                            if (originalUpgrade != null)
                                gui.m_craftUpgradeItem = staged.GetItemAt(originalUpgrade.m_gridPos.x, originalUpgrade.m_gridPos.y);
                            gui.DoCrafting(player);
                        }
                        finally
                        {
                            PlayerInventory(player) = original;
                            gui.m_craftUpgradeItem = originalUpgrade;
                            _prepared = null;
                            _replayingPlayer = null;
                        }
                        if (escrow.GetAllItems().Sum(item => item.m_stack) != 0) return StorageEffectResult.Rejected;
                        var layout = GameInventoryAdapter.Snapshot("player:" + player.GetPlayerID(), GameInventoryAdapter.Revision(staged), staged);
                        var p = new ZPackage();
                        p.Write(operationId); p.Write(effect.Data); p.Write(StorageRpc.Encode(layout));
                        // The visible inventory is still unchanged. Persist the
                        // exact native result before installing it, including a
                        // failed/broken upgrader outcome and crafting bonuses.
                        player.m_customData[PreparedResultKey] = Convert.ToBase64String(p.GetArray());
                        return Commit(player, layout);
                    });
                if (result == StorageEffectResult.Applied || result == StorageEffectResult.Rejected) player.m_customData.Remove(PendingKey);
                return result;
            }

            private static StorageEffectResult Commit(Player player, StorageInventory layout)
            {
                var inventory = player.GetInventory();
                foreach (var item in inventory.GetAllItems().Where(item => item.m_equipped).ToArray())
                {
                    int slot = item.m_gridPos.y * inventory.GetWidth() + item.m_gridPos.x;
                    if (!layout.Items.Any(stack => stack.Slot == slot && stack.Identity == GameInventoryAdapter.Identity(item)))
                        player.UnequipItem(item);
                }
                GameInventoryAdapter.ApplyLayout(inventory, layout);
                return GameInventoryAdapter.Revision(inventory) == layout.Revision ? StorageEffectResult.Applied : StorageEffectResult.Uncertain;
            }

            private static StorageInventory ReadPrepared(Player player, string operationId, string data)
            {
                if (!player.m_customData.TryGetValue(PreparedResultKey, out var encoded)) return null;
                var p = new ZPackage(Convert.FromBase64String(encoded));
                return p.ReadString() == operationId && p.ReadString() == data ? StorageRpc.Decode(p.ReadString()) : null;
            }

            private static bool HasPrepared(Player player, string data)
            {
                return player.m_customData.TryGetValue(PendingKey, out var operationId) &&
                    ReadPrepared(player, operationId, data) != null;
            }
        }
    }
}
