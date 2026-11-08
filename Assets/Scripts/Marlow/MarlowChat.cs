using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastTide
{
    /// <summary>The typed chat window: gathers the game state, asks the server, and applies checked actions.</summary>
    [RequireComponent(typeof(Marlow))]
    public class MarlowChat : MonoBehaviour
    {
        private const int MaxTypedLength = 200;
        private const int TranscriptLines = 8;
        private const int HistoryLines = 6;

        [SerializeField] private string serverUrl = "https://last-tide-marlow.onrender.com/marlow";
        [SerializeField] private GameObject player;
        [SerializeField] private DayNightCycle cycle;
        [SerializeField] private QuestManager quest;

        private readonly List<string> transcript = new List<string>();
        private readonly List<string> history = new List<string>();
        private string typed = string.Empty;
        private string playerId;
        private bool waiting;
        private float backspaceRepeatAt;
        private ChatFlow flow;
        private bool touchMode;
        private TouchScreenKeyboard phoneKeyboard;
        private Text helpText;

        private Marlow marlow;
        private Inventory inventory;
        private SurvivalStats stats;
        private Font font;
        private GameObject hintPanel;
        private GameObject chatPanel;
        private Text transcriptText;
        private Text inputText;

        public void Configure(GameObject playerObject, DayNightCycle dayNight)
        {
            player = playerObject;
            cycle = dayNight;
        }

        public void SetQuest(QuestManager manager)
        {
            quest = manager;
        }

        public void Introduce()
        {
            AddLine("Marlow", "Systems online. Mostly. I'm Marlow, and you look terrible.");
            AddLine("Marlow", "There's a signal beacon on the peak, but it needs three parts. Find them and we can call for rescue.");
            EnsureFlow();
            flow.Introduce();
            Sync();
        }

        private void Awake()
        {
            marlow = GetComponent<Marlow>();
            inventory = player.GetComponent<Inventory>();
            stats = player.GetComponent<SurvivalStats>();
            playerId = Guid.NewGuid().ToString("N");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi();
        }

        private void Start()
        {
            StartCoroutine(MarlowClient.Wake(serverUrl));
        }

        private void OnEnable()
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput += OnTextInput;
            }
        }

        private void OnDisable()
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= OnTextInput;
            }
            UiState.IsTyping = false;
            CloseKeyboard();
        }

        private void OnTextInput(char character)
        {
            bool typingOnKeyboard = flow != null && flow.State == ChatState.Typing && phoneKeyboard == null;
            if (typingOnKeyboard && !char.IsControl(character) && typed.Length < MaxTypedLength)
            {
                typed += character;
                RefreshInput();
            }
        }

        private void Update()
        {
            if (!marlow.IsAwake)
            {
                return;
            }

            EnsureFlow();
            InputFrame input = GameInput.Current;
            if (input.Talk)
            {
                Apply(flow.TalkPressed());
            }

            // On phones, walking away closes a chat that is only being read.
            if (input.Move.sqrMagnitude > 0.1f)
            {
                flow.PlayerMoved();
            }

            // Esc on a keyboard, and the Android back button, which Unity reports as Esc.
            Keyboard keyboard = Keyboard.current;
            if (flow.PanelVisible && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                UiState.EscapeHandledFrame = Time.frameCount;
                Apply(flow.BackPressed());
            }
            else if (phoneKeyboard != null)
            {
                ReadPhoneKeyboard();
            }
            else if (flow.State == ChatState.Typing && keyboard != null)
            {
                HandleBackspace(keyboard);
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    Apply(flow.SubmitPressed());
                }
            }

            Sync();
        }

        private void EnsureFlow()
        {
            if (flow != null)
            {
                return;
            }

            touchMode = InputCollector.TouchActive;
            flow = new ChatFlow(touchMode);
            if (touchMode)
            {
                // Top middle of the screen, clear of the touch buttons on the right.
                RectTransform rect = (RectTransform)chatPanel.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -90f);
                rect.sizeDelta = new Vector2(760f, 380f);
                helpText.text = "Tap Talk to reply    Walk away to close";
            }
        }

        private void Apply(ChatCommand command)
        {
            switch (command)
            {
                case ChatCommand.Send:
                    Send();
                    break;
                case ChatCommand.OpenKeyboard:
                    // The editor's phone simulator has no on-screen keyboard; the PC keyboard is used there instead.
                    if (TouchScreenKeyboard.isSupported)
                    {
                        phoneKeyboard = TouchScreenKeyboard.Open(typed, TouchScreenKeyboardType.Default, true, false, false, false,
                            "Talk to Marlow", MaxTypedLength);
                    }
                    break;
                case ChatCommand.CloseKeyboard:
                    CloseKeyboard();
                    break;
            }
        }

        private void ReadPhoneKeyboard()
        {
            string text = phoneKeyboard.text ?? string.Empty;
            typed = text.Length > MaxTypedLength ? text.Substring(0, MaxTypedLength) : text;
            switch (phoneKeyboard.status)
            {
                case TouchScreenKeyboard.Status.Done:
                    phoneKeyboard = null;
                    Apply(flow.SubmitPressed());
                    break;
                case TouchScreenKeyboard.Status.Canceled:
                case TouchScreenKeyboard.Status.LostFocus:
                    phoneKeyboard = null;
                    flow.KeyboardCanceled();
                    break;
            }
        }

        private void CloseKeyboard()
        {
            if (phoneKeyboard != null)
            {
                phoneKeyboard.active = false;
                phoneKeyboard = null;
            }
        }

        private void Sync()
        {
            bool visible = flow.PanelVisible;
            UiState.IsTyping = flow.BlocksGameplayKeys;
            if (chatPanel.activeSelf != visible)
            {
                chatPanel.SetActive(visible);
                RefreshTranscript();
            }

            // Phones have a Talk button instead of the [T] hint.
            bool hint = !visible && marlow.IsAwake && !touchMode;
            if (hintPanel.activeSelf != hint)
            {
                hintPanel.SetActive(hint);
            }

            if (!visible)
            {
                CloseKeyboard();
            }

            RefreshInput();
        }

        private void HandleBackspace(Keyboard keyboard)
        {
            bool pressed = keyboard.backspaceKey.wasPressedThisFrame;
            bool repeating = keyboard.backspaceKey.isPressed && Time.unscaledTime >= backspaceRepeatAt;
            if (!pressed && !repeating)
            {
                return;
            }

            backspaceRepeatAt = Time.unscaledTime + (pressed ? 0.4f : 0.05f);
            if (typed.Length > 0)
            {
                typed = typed.Substring(0, typed.Length - 1);
                RefreshInput();
            }
        }

        private void Send()
        {
            string message = typed.Trim();
            if (waiting || message.Length == 0)
            {
                return;
            }

            typed = string.Empty;
            AddLine("You", message);
            waiting = true;
            RefreshInput();
            StartCoroutine(MarlowClient.Ask(serverUrl, BuildRequest(message), response => OnResponse(message, response)));
        }

        private void OnResponse(string message, MarlowResponse response)
        {
            waiting = false;
            MarlowResponse used = response ?? MarlowFallback.Reply(message, inventory, stats, quest);
            AddLine("Marlow", used.reply);
            history.Add($"Player: {message}");
            history.Add($"Marlow: {used.reply}");
            while (history.Count > HistoryLines)
            {
                history.RemoveAt(0);
            }

            ApplyAction(used.action);
            RefreshInput();
        }

        private void ApplyAction(string action)
        {
            switch (action)
            {
                case "point_wood": marlow.PointAt(ItemType.Wood); break;
                case "point_stone": marlow.PointAt(ItemType.Stone); break;
                case "point_vine": marlow.PointAt(ItemType.Vine); break;
                case "point_coconut": marlow.PointAt(ItemType.Coconut); break;
                case "point_wreck": if (quest != null) marlow.PointAt(quest.WreckTarget); break;
                case "point_shelter": if (quest != null) marlow.PointAt(quest.ShelterTarget); break;
                case "point_peak": if (quest != null) marlow.PointAt(quest.PeakTarget); break;
                case "point_beacon": if (quest != null) marlow.PointAt(quest.BeaconTarget); break;
            }
        }

        private MarlowRequest BuildRequest(string message)
        {
            List<string> items = new List<string>();
            foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
            {
                int count = inventory.Count(item);
                if (count > 0)
                {
                    items.Add($"{item}: {count}");
                }
            }

            return new MarlowRequest
            {
                playerId = playerId,
                message = message,
                day = cycle != null ? cycle.Day : 1,
                hour = cycle != null ? Mathf.FloorToInt(cycle.Hour) : 12,
                hunger = stats != null ? Mathf.RoundToInt(stats.Hunger) : 100,
                energy = stats != null ? Mathf.RoundToInt(stats.Energy) : 100,
                inventory = items.ToArray(),
                events = GameEvents.Recent,
                history = history.ToArray(),
                quest = quest != null ? quest.DescribeForMarlow() : string.Empty,
            };
        }

        private void AddLine(string speaker, string line)
        {
            string colour = speaker == "Marlow" ? "#7fe8ff" : "#ffd27f";
            transcript.Add($"<color={colour}><b>{speaker}:</b></color> {line}");
            while (transcript.Count > TranscriptLines)
            {
                transcript.RemoveAt(0);
            }
            RefreshTranscript();
        }

        private void RefreshTranscript()
        {
            transcriptText.text = string.Join("\n", transcript);
        }

        private void RefreshInput()
        {
            if (waiting)
            {
                inputText.text = "<i>Marlow is thinking...</i>";
            }
            else if (flow != null && flow.State == ChatState.Reading)
            {
                inputText.text = "<i>Tap Talk to reply</i>";
            }
            else
            {
                inputText.text = $"> {typed}_";
            }
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("Marlow Chat");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            hintPanel = Panel(canvasObject.transform, "Talk Hint", new Vector2(300f, 60f));
            Label(hintPanel.transform, "Hint", TextAnchor.MiddleCenter, 26, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).text = "[T] Talk to Marlow";
            hintPanel.SetActive(false);

            chatPanel = Panel(canvasObject.transform, "Chat", new Vector2(760f, 470f));
            transcriptText = Label(chatPanel.transform, "Transcript", TextAnchor.LowerLeft, 26, Vector2.zero, Vector2.one,
                new Vector2(18f, 92f), new Vector2(-18f, -14f));
            inputText = Label(chatPanel.transform, "Input", TextAnchor.MiddleLeft, 26, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(18f, 40f), new Vector2(-18f, 86f));
            helpText = Label(chatPanel.transform, "Help", TextAnchor.MiddleLeft, 20, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(18f, 8f), new Vector2(-18f, 38f));
            helpText.text = "[Enter] Send    [Esc] Close";
            helpText.color = new Color(1f, 1f, 1f, 0.6f);
            chatPanel.SetActive(false);
        }

        private static GameObject Panel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 24f);
            rect.sizeDelta = size;
            panel.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.82f);
            return panel;
        }

        private Text Label(Transform parent, string name, TextAnchor alignment, int size, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject labelObject = new GameObject(name);
            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = labelObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
