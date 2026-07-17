using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScritchyScratchyAP
{
    // In-game panel for setting host/port/slot/password. Toggled with F1. Submitting "Connect"
    // saves the fields to Plugin's ConfigEntrys and triggers a disconnect and reconnect via
    // APUpdateManager.RequestReconnect. Built entirely from real Unity UI:
    // Canvas/Image/TMP_Text/TMP_InputField/Button
    //
    // Layout uses VerticalLayoutGroup + ContentSizeFitter rather than manually computed
    // anchoredPosition offsets since it's much less error-prone than manual math against
    // Unity's anchor/pivot system.
    public class ConnectionGUI : MonoBehaviour
    {
        private GameObject _panelRoot;
        private RectTransform _panelRect;
        private TMP_InputField _hostField;
        private TMP_InputField _portField;
        private TMP_InputField _slotField;
        private TMP_InputField _passwordField;
        private TextMeshProUGUI _statusText;
        private Image _connectButtonImage;
        private TextMeshProUGUI _connectButtonText;
        private static readonly Color ConnectColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        private static readonly Color DisconnectColor = new Color(0.7f, 0.15f, 0.15f, 1f);
        private bool _visible = true; // Starts open so the player doesn't have to press F1 first

        private bool _isConnecting = false;
        private float _connectingElapsed = 0f;
        private const float ConnectTimeoutSeconds = 6f;

        // Always-visible "open connection menu" hint toggle
        private bool _hintBuilt = false;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
            {
                Toggle();
            }

            // Any left click outside the panel's own bounds while it's open
            // closes it, without needing to hunt for F1 again.
            if (_visible && _panelRect != null)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 screenPos = mouse.position.ReadValue();
                    if (!RectTransformUtility.RectangleContainsScreenPoint(_panelRect, screenPos, null))
                    {
                        _visible = false;
                        _panelRoot.SetActive(false);
                    }
                }
            }

            if (_isConnecting && _statusText != null)
            {
                if (ArchipelagoManager.Connected)
                {
                    _isConnecting = false;
                    _statusText.text = "Status: Connected";
                }
                else
                {
                    _connectingElapsed += Time.deltaTime;
                    if (_connectingElapsed > ConnectTimeoutSeconds)
                    {
                        _isConnecting = false;
                        _statusText.text = "Status: Disconnected (connect failed - check log)";
                    }
                }
            }

            if (!_hintBuilt) TryBuildHint();
            if (_panelRoot == null) TryBuildInitialPanel();

            // Skip refreshing during a connect attempt. The "Connecting..." status
            // text/timeout logic above already owns the button until it resolves,
            // so flipping the button underneath it would be confusing.
            if (!_isConnecting && _connectButtonImage != null)
            {
                bool connected = ArchipelagoManager.Connected;
                _connectButtonImage.color = connected ? DisconnectColor : ConnectColor;
                _connectButtonText.text = connected ? "Disconnect" : "Connect";
            }
        }

        // Builds the connect panel as soon as a font is available, without waiting
        // for the player to press F1. The menu should start open on launch.
        private void TryBuildInitialPanel()
        {
            var font = FindAnyFont();
            if (font == null) return; // Retry next frame, game UI may not have loaded yet

            try
            {
                BuildPanel();
                _panelRoot.SetActive(_visible);
                if (_visible) RefreshFieldsFromConfig();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: ConnectionGUI initial panel build failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void TryBuildHint()
        {
            var font = FindAnyFont();
            if (font == null) return; // Retry next frame, game UI may not have loaded yet

            try
            {
                var canvasGO = new GameObject("AP_HintCanvas");
                UnityEngine.Object.DontDestroyOnLoad(canvasGO);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                canvasGO.AddComponent<CanvasScaler>();

                var textGO = new GameObject("HintText");
                textGO.transform.SetParent(canvasGO.transform, false);
                var rect = textGO.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(16, 16);
                rect.sizeDelta = new Vector2(360, 40);

                var text = textGO.AddComponent<TextMeshProUGUI>();
                text.font = font;
                text.text = "F1 - Connection Menu";
                text.fontSize = 26;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.BottomLeft;

                // Dynamically-sized gray background behind the text
                text.ForceMeshUpdate();
                const float paddingX = 8f;
                const float paddingY = 6f;
                var bgGO = new GameObject("HintBackground");
                bgGO.transform.SetParent(canvasGO.transform, false);
                bgGO.transform.SetAsFirstSibling(); // Draw behind the text
                var bgRect = bgGO.AddComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0f, 0f);
                bgRect.anchorMax = new Vector2(0f, 0f);
                bgRect.pivot = new Vector2(0f, 0f);
                bgRect.anchoredPosition = new Vector2(16 - paddingX, 16 - paddingY);
                bgRect.sizeDelta = new Vector2(text.preferredWidth + paddingX * 2, text.preferredHeight + paddingY * 2);
                var bgImage = bgGO.AddComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

                _hintBuilt = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: ConnectionGUI hint build failed: {ex.Message}");
                _hintBuilt = true; // Don't retry every frame after a real failure
            }
        }

        private void Toggle()
        {
            try
            {
                if (_panelRoot == null)
                {
                    BuildPanel();
                }
                _visible = !_visible;
                _panelRoot.SetActive(_visible);
                if (_visible) RefreshFieldsFromConfig();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: ConnectionGUI toggle failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RefreshFieldsFromConfig()
        {
            _hostField.text = Plugin.ConfigHost.Value;
            _portField.text = Plugin.ConfigPort.Value.ToString();
            _slotField.text = Plugin.ConfigSlotName.Value;
            _passwordField.text = Plugin.ConfigPassword.Value;
            _isConnecting = false;
            _statusText.text = ArchipelagoManager.Connected ? "Status: Connected" : "Status: Disconnected";
        }

        // Runtime TMP components need a font asset, borrow whatever font an existing in-game TMP label is already using.
        private TMP_FontAsset FindAnyFont()
        {
            var existing = UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>(true);
            return existing != null ? existing.font : null;
        }

        private void EnsureEventSystem()
        {
            var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("AP_EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                Plugin.Log.LogInfo("AP: ConnectionGUI created a new EventSystem (none found in scene).");
            }
        }

        private void BuildPanel()
        {
            EnsureEventSystem();
            var font = FindAnyFont();

            var canvasGO = new GameObject("AP_ConnectionCanvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasGO);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520, 0);
            panelRect.anchoredPosition = Vector2.zero;
            _panelRect = panelRect;
            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.85f);

            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            CreateLabel(panelGO.transform, font, "Archipelago Connection", 30, 38);
            _statusText = CreateLabel(panelGO.transform, font, "Status: Disconnected", 22, 30);

            _hostField = CreateInputField(panelGO.transform, font, "Host");
            _portField = CreateInputField(panelGO.transform, font, "Port");
            _slotField = CreateInputField(panelGO.transform, font, "Slot Name");
            _passwordField = CreateInputField(panelGO.transform, font, "Password");

            CreateButton(panelGO.transform, font, "Connect", OnConnectButtonClicked,
                out _connectButtonImage, out _connectButtonText);

            _panelRoot = canvasGO;
            _panelRoot.SetActive(false);
        }

        private TextMeshProUGUI CreateLabel(Transform parent, TMP_FontAsset font, string text, int fontSize, float height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var layoutElem = go.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = height;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private TMP_InputField CreateInputField(Transform parent, TMP_FontAsset font, string placeholder)
        {
            var go = new GameObject($"Input_{placeholder}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var layoutElem = go.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 44;

            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            var textAreaGO = new GameObject("TextArea");
            textAreaGO.transform.SetParent(go.transform, false);
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(12, 4);
            textAreaRect.offsetMax = new Vector2(-12, -4);
            textAreaGO.AddComponent<RectMask2D>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComp = textGO.AddComponent<TextMeshProUGUI>();
            if (font != null) textComp.font = font;
            textComp.fontSize = 22;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.MidlineLeft;
            textComp.enableWordWrapping = false;

            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholderComp = placeholderGO.AddComponent<TextMeshProUGUI>();
            if (font != null) placeholderComp.font = font;
            placeholderComp.fontSize = 22;
            placeholderComp.color = new Color(1f, 1f, 1f, 0.4f);
            placeholderComp.text = placeholder;
            placeholderComp.alignment = TextAlignmentOptions.MidlineLeft;

            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComp;
            inputField.placeholder = placeholderComp;

            return inputField;
        }

        private void CreateButton(Transform parent, TMP_FontAsset font, string label, Action onClick,
            out Image buttonImage, out TextMeshProUGUI buttonText)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var layoutElem = go.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 54;

            buttonImage = go.AddComponent<Image>();
            buttonImage.color = ConnectColor;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonText = textGO.AddComponent<TextMeshProUGUI>();
            if (font != null) buttonText.font = font;
            buttonText.text = label;
            buttonText.fontSize = 22;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener((System.Action)onClick);
        }

        private void OnConnectButtonClicked()
        {
            if (ArchipelagoManager.Connected)
                Disconnect();
            else
                Connect();
        }

        private void Disconnect()
        {
            try
            {
                ArchipelagoManager.TryDisconnect();
                _isConnecting = false;
                _statusText.text = "Status: Disconnected";
                Plugin.Log.LogInfo("AP: Disconnected via ConnectionGUI.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: ConnectionGUI Disconnect failed: {ex.Message}");
            }
        }

        private void Connect()
        {
            try
            {
                if (!int.TryParse(_portField.text, out int port))
                {
                    _isConnecting = false;
                    _statusText.text = "Invalid port - must be a number.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(_slotField.text))
                {
                    _isConnecting = false;
                    _statusText.text = "Slot name is required.";
                    return;
                }

                Plugin.ConfigHost.Value = _hostField.text;
                Plugin.ConfigPort.Value = port;
                Plugin.ConfigSlotName.Value = _slotField.text;
                Plugin.ConfigPassword.Value = _passwordField.text;

                _isConnecting = true;
                _connectingElapsed = 0f;
                _statusText.text = "Connecting...";
                APUpdateManager.RequestReconnect(_hostField.text, port, _slotField.text, _passwordField.text);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: ConnectionGUI Connect failed: {ex.Message}");
            }
        }
    }
}
