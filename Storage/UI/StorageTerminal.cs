using HarmonyLib;
using System.Linq;
using SmartCraftStorage.Hotkeys;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;

namespace SmartCraftStorage.Storage.UI
{
    internal sealed class StorageTerminal : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _view;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            if (_view != null && _view.IsValid() && _view.IsOwner())
                _view.GetZDO().Set(StorageFacade.TerminalMarkerKey, true);
        }

        public string GetHoverName() => TerminalTranslations.Text("title");
        public float GetHoverOffset() => 0f;

        public string GetHoverText()
        {
            var name = StorageFacade.Service.GetNetworkName(_view);
            if (string.IsNullOrEmpty(name)) name = TerminalTranslations.Text("unnamed");
            return Localization.instance.Localize(GetHoverName() + " — " + name.Replace("<", "").Replace(">", "") +
                "\n[<color=yellow><b>$KEY_Use</b></color>] " + TerminalTranslations.Text("open") + StorageChestNaming.ShortcutHint);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || !(user is Player player) || player != Player.m_localPlayer) return false;
            if (HotkeyConfig.QuickStackShortcut.Value.IsPressed() || HotkeyConfig.RestockShortcut.Value.IsPressed()) return false;
            if (!PrivateArea.CheckAccess(transform.position, 0f, true)) return false;
            StorageTerminalUi.Open(_view, player, false);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }

    internal static class StorageChestNaming
    {
        internal static string ShortcutHint => HotkeyConfig.NetworkNameShortcut.Value.MainKey == KeyCode.None
            ? string.Empty
            : "\n[<color=yellow>" + HotkeyConfig.NetworkNameShortcut.Value + "</color>] " + TerminalTranslations.Text("rename");

        internal static bool IsEligible(Container chest)
        {
            if (chest == null) return false;
            var view = chest.m_nview;
            var piece = chest.GetComponent<Piece>();
            if (view == null || !view.IsValid() || piece == null || !piece.IsPlacedByPlayer() ||
                chest.GetComponent<TombStone>() != null || chest.GetComponentInParent<Ship>() != null ||
                chest.GetComponentInParent<Vagon>() != null) return false;
            return !chest.GetComponents<Component>().Any(component => component != null &&
                component.GetType().FullName == "BottomlessChest.Core.BottomlessContainer");
        }

        internal static void OpenHovered(Player player)
        {
            // The game's hover target already enforces interaction distance and line of sight.
            var hovered = player.GetHoverObject();
            if (hovered == null) return;
            var terminal = hovered.GetComponentInParent<StorageTerminal>();
            if (terminal != null)
            {
                if (PrivateArea.CheckAccess(terminal.transform.position, 0f, true))
                    StorageTerminalUi.Open(terminal.GetComponent<ZNetView>(), player, true);
                else
                    player.Message(MessageHud.MessageType.Center, TerminalTranslations.Text("denied"));
                return;
            }

            var chest = hovered.GetComponentInParent<Container>();
            if (!IsEligible(chest)) return;
            if (!chest.CheckAccess(player.GetPlayerID()) ||
                !PrivateArea.CheckAccess(chest.transform.position, 0f, true))
                player.Message(MessageHud.MessageType.Center, TerminalTranslations.Text("denied"));
            else if (StorageFacade.Service.IsBusy(chest.m_nview) || chest.IsInUse())
                player.Message(MessageHud.MessageType.Center, TerminalTranslations.Text("busy"));
            else
                StorageTerminalUi.Open(chest.m_nview, player, true);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class StorageChestNameHint
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (!StorageChestNaming.IsEligible(__instance)) return;
            string name = StorageFacade.Service.GetNetworkName(__instance.m_nview);
            if (!string.IsNullOrEmpty(name)) __result += "\n" + TerminalTranslations.Text("network") + ": " + name.Replace("<", "").Replace(">", "");
            __result += StorageChestNaming.ShortcutHint;
            __result = Localization.instance.Localize(__result);
        }
    }
}
