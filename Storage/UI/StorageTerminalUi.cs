using System;
using System.Collections.Generic;
using Jotunn.Managers;
using SmartCraftStorage.Storage.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCraftStorage.Storage.UI
{
    // Only the small naming dialog is custom. Item interaction lives in InventoryGui.
    internal sealed class StorageTerminalUi : MonoBehaviour
    {
        private static StorageTerminalUi _instance;
        private static readonly Dictionary<string, string> Drafts = new Dictionary<string, string>(StringComparer.Ordinal);
        private ZNetView _target;
        private Player _player;
        private InputField _name;
        private Button _save;
        private Text _status;
        private GameObject _panel;
        private string _draftKey;
        private StorageOperation _operation;
        private bool _returnToTerminal;
        private float _refreshAt;

        internal static bool NamingOpen => _instance != null;
        public static bool IsOpen => NamingOpen || NativeTerminalInventory.IsOpen;
        private bool Pending => _operation != null && !_operation.IsFinal && _operation.Status != StorageOperationStatus.Unavailable;

        public static void Open(ZNetView target, Player player, bool namingOnly, bool returnToTerminal = false)
        {
            if (target == null || !target.IsValid() || player == null) return;
            if (!namingOnly) { NativeTerminalInventory.Open(target, player); return; }
            if (GUIManager.CustomGUIFront == null) return;
            if (_instance != null) _instance.Close(false);
            NativeTerminalInventory.CloseCurrent(true);
            InventoryGui.instance?.Hide();
            var root = new GameObject("SCS network name", typeof(RectTransform), typeof(Image), typeof(StorageTerminalUi));
            root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, .45f);
            _instance = root.GetComponent<StorageTerminalUi>();
            _instance._target = target; _instance._player = player; _instance._returnToTerminal = returnToTerminal;
            _instance._draftKey = TargetKey(target, player);
            _instance._operation = StorageFacade.Service.GetPendingOperation(target, "name");
            _instance.Build();
            GUIManager.BlockInput(true);
            _instance.Refresh();
        }

        internal static string TargetKey(ZNetView target, Player player) =>
            (ZNet.instance != null ? ZNet.instance.GetWorldUID().ToString() : "") + ":" + player.GetPlayerID() + ":" + target.GetZDO().m_uid;

        private void Build()
        {
            _panel = GUIManager.Instance.CreateWoodpanel(transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, 540, 235, false);
            Label(_panel.transform, T("rename"), 24, 18, 490, 30, 24);
            Label(_panel.transform, T("naming_help"), 24, 56, 490, 40, 15);
            _name = Input(_panel.transform, T("name"), 24, 102, 358, 36);
            _name.characterLimit = 48;
            _name.text = Drafts.TryGetValue(_draftKey, out var draft) ? draft : StorageFacade.Service.GetNetworkName(_target);
            _name.onValueChanged.AddListener(value => Drafts[_draftKey] = value);
            _save = MakeButton(_panel.transform, T("save"), 394, 102, 122, 36, SaveName);
            _status = Label(_panel.transform, "", 24, 151, 490, 30, 15);
            MakeButton(_panel.transform, T("close"), 388, 189, 128, 30, () => Close(true));
        }

        private void SaveName()
        {
            if (Pending) return;
            Drafts[_draftKey] = _name.text;
            _operation = StorageFacade.Service.SetNetworkName(_target, _player, _name.text.Trim());
            Refresh();
        }

        private void Update()
        {
            if (_target == null || !_target.IsValid() || _player == null || _player.IsDead() ||
                Vector3.Distance(_player.transform.position, _target.transform.position) > 6f ||
                !PrivateArea.CheckAccess(_target.transform.position, 0f, false))
            { Close(false); return; }
            if (ZInput.GetKeyDown(KeyCode.Escape)) { Close(true); return; }
            _panel.transform.localScale = Vector3.one * Mathf.Min(1f, Screen.width / (570f * transform.lossyScale.x));
            if (Time.unscaledTime < _refreshAt) return;
            _refreshAt = Time.unscaledTime + .25f;
            if (Pending)
            {
                var current = StorageFacade.Service.GetOperation(_operation.Id);
                if (current.Status == StorageOperationStatus.Unavailable) StorageFacade.Service.Resume(_operation.Id);
                else _operation = current;
            }
            Refresh();
        }

        private void Refresh()
        {
            _save.interactable = _name.interactable = !Pending;
            _status.text = _operation == null ? T("ready") : OperationText(_operation, "name");
            if (_operation?.Status == StorageOperationStatus.Confirmed && StorageFacade.Service.GetNetworkName(_target) == StorageDiscovery.Normalize(_name.text))
                Drafts.Remove(_draftKey);
        }

        internal static string OperationText(StorageOperation operation, string kind = "")
        {
            switch (operation.Status)
            {
                case StorageOperationStatus.Confirmed:
                    return kind == "name" ? T("linked") : kind == "organize" ? T("organized") :
                        T("done", operation.Accepted.ToString(), operation.Remaining.ToString());
                case StorageOperationStatus.Rejected:
                case StorageOperationStatus.Aborted: return T("rejected");
                case StorageOperationStatus.Unavailable: return T("unavailable");
                case StorageOperationStatus.RecoveryPending: return T(kind == "name" ? "name_pending" : "recovering");
                default: return T(kind == "name" ? "name_pending" : "pending");
            }
        }

        private void Close(bool returnToTerminal)
        {
            if (_instance != this) return;
            if (_operation?.Status != StorageOperationStatus.Confirmed) Drafts[_draftKey] = _name.text;
            if (Drafts.Count > 128) Drafts.Clear();
            _instance = null;
            GUIManager.BlockInput(false);
            gameObject.SetActive(false); Destroy(gameObject);
            if (returnToTerminal && _returnToTerminal && _target != null && _target.IsValid() && _player != null)
                NativeTerminalInventory.Open(_target, _player);
        }

        private void OnDestroy()
        { if (_instance == this) { _instance = null; GUIManager.BlockInput(false); } }

        internal static string T(string key, params string[] values) => TerminalTranslations.Text(key, values);
        internal static Text Label(Transform parent, string text, float x, float y, float width, float height, int size = 16)
        {
            var obj = GUIManager.Instance.CreateText(text, parent, Vector2.up, Vector2.up, Vector2.zero,
                GUIManager.Instance.AveriaSerif, size, GUIManager.Instance.ValheimBeige, false, Color.black, width, height, false);
            Position(obj, x, y, width, height);
            var label = obj.GetComponent<Text>();
            label.alignment = TextAnchor.UpperLeft; label.supportRichText = false; label.raycastTarget = false;
            return label;
        }
        internal static InputField Input(Transform parent, string placeholder, float x, float y, float width, float height)
        {
            var obj = GUIManager.Instance.CreateInputField(parent, Vector2.up, Vector2.up, Vector2.zero,
                InputField.ContentType.Standard, placeholder, 16, width, height);
            Position(obj, x, y, width, height);
            var input = obj.GetComponent<InputField>(); input.textComponent.supportRichText = false;
            return input;
        }
        internal static Button MakeButton(Transform parent, string label, float x, float y, float width, float height, Action action)
        {
            var obj = GUIManager.Instance.CreateButton(label, parent, Vector2.up, Vector2.up, Vector2.zero, width, height);
            Position(obj, x, y, width, height);
            var button = obj.GetComponent<Button>(); button.onClick.AddListener(() => action()); return button;
        }
        internal static void Position(GameObject obj, float x, float y, float width, float height)
        {
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, -y);
        }
    }
}
