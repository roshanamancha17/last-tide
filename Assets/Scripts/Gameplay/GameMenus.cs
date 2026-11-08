using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastTide
{
    /// <summary>The main menu, pause menu and settings screen. The game is paused while any of them shows.</summary>
    // Runs after Marlow's chat, so Back closes the chat first instead of also opening the pause menu.
    [DefaultExecutionOrder(50)]
    public class GameMenus : MonoBehaviour
    {
        /// <summary>Show the main menu when the island loads. Off when a new game was just chosen.</summary>
        private static bool showMainMenuOnLoad = true;

        private MenuFlow flow;
        private Font font;
        private GameObject mainPanel;
        private GameObject pausePanel;
        private GameObject settingsPanel;
        private GameObject backdrop;
        private Button continueButton;
        private Text playLabel;
        private Text qualityLabel;
        private MenuScreen shown = (MenuScreen)(-1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Spawn();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Spawn();
        }

        private static void Spawn()
        {
            if (FindAnyObjectByType<PlayerController>() != null && FindAnyObjectByType<GameMenus>() == null)
            {
                new GameObject("Game Menus").AddComponent<GameMenus>();
            }
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            flow = new MenuFlow(showMainMenuOnLoad);
            showMainMenuOnLoad = true;
            EnsureEventSystem();
            Build();
            Show();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            UiState.MenuOpen = false;
        }

        private void Update()
        {
            bool backHandledByChat = UiState.EscapeHandledFrame == Time.frameCount;
            if (GameInput.Current.Pause && !backHandledByChat && !UiState.IsTyping)
            {
                flow.BackPressed();
            }

            Show();
        }

        private void OnApplicationPause(bool paused)
        {
            // Coming back to a phone game mid-action is unfair: return to the pause menu instead.
            if (paused && flow.Screen == MenuScreen.None)
            {
                flow.BackPressed();
                Show();
            }
        }

        private void Show()
        {
            if (flow.Screen == shown)
            {
                return;
            }

            bool wasPaused = shown != MenuScreen.None && (int)shown >= 0;
            shown = flow.Screen;
            backdrop.SetActive(flow.GamePaused);
            mainPanel.SetActive(shown == MenuScreen.Main);
            pausePanel.SetActive(shown == MenuScreen.Pause);
            settingsPanel.SetActive(shown == MenuScreen.Settings);
            UiState.MenuOpen = flow.GamePaused;
            Time.timeScale = flow.GamePaused ? 0f : 1f;

            if (shown == MenuScreen.Main)
            {
                bool hasSave = SaveManager.HasSave;
                continueButton.gameObject.SetActive(hasSave);
                playLabel.text = hasSave ? "New Game" : "Play";
            }

            if (shown == MenuScreen.Settings)
            {
                RefreshQualityLabel();
            }

            // Back in the game on a computer: capture the mouse again for looking around.
            if (wasPaused && !flow.GamePaused && !InputCollector.TouchActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>Same as pressing Esc or the Android back button.</summary>
        public void Back()
        {
            flow.BackPressed();
            Show();
        }

        public MenuScreen CurrentScreen => flow.Screen;

        public void OpenSettings()
        {
            flow.OpenSettings();
            Show();
        }

        private void Continue()
        {
            flow.Play();
            Show();
        }

        private void NewGame()
        {
            SaveManager.DeleteSave();
            GameEvents.Clear();
            UiState.IsTyping = false;
            showMainMenuOnLoad = false;
            Reload();
        }

        private void ToMainMenu()
        {
            SaveManager saves = FindAnyObjectByType<SaveManager>();
            if (saves != null)
            {
                saves.Save();
            }

            GameEvents.Clear();
            showMainMenuOnLoad = true;
            Reload();
        }

        private static void Reload()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        private static bool CanQuit => !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;

        private static void Quit()
        {
            FindAnyObjectByType<SaveManager>()?.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CycleQuality()
        {
            GameSettings settings = SettingsService.Current;
            int levels = QualitySettings.names.Length;
            // Automatic, then each level in turn.
            settings.Quality = settings.Quality + 1 >= levels ? GameSettings.PlatformQuality : settings.Quality + 1;
            SettingsService.SaveAndApply();
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            int quality = SettingsService.Current.Quality;
            string name = quality == GameSettings.PlatformQuality ? "Automatic"
                : QualitySettings.names[quality] == "Mobile" ? "Low" : "High";
            qualityLabel.text = $"Graphics: {name}";
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject events = new GameObject("Event System");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void Build()
        {
            GameObject canvasObject = new GameObject("Menus");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            backdrop = new GameObject("Backdrop", typeof(RectTransform));
            backdrop.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)backdrop.transform);
            backdrop.AddComponent<Image>().color = new Color(0.02f, 0.06f, 0.1f, 0.55f);

            GameObject safe = new GameObject("Safe Area", typeof(RectTransform));
            safe.transform.SetParent(canvasObject.transform, false);
            safe.AddComponent<SafeAreaPanel>().Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));

            mainPanel = Panel(safe.transform, "Main Menu", "Last Tide", 64);
            continueButton = AddButton(mainPanel.transform, "Continue", Continue);
            playLabel = AddButton(mainPanel.transform, "Play", NewGame).GetComponentInChildren<Text>();
            AddButton(mainPanel.transform, "Settings", flow.OpenSettings);
            if (CanQuit) AddButton(mainPanel.transform, "Quit", Quit);

            pausePanel = Panel(safe.transform, "Pause Menu", "Paused", 52);
            AddButton(pausePanel.transform, "Resume", Continue);
            AddButton(pausePanel.transform, "Settings", flow.OpenSettings);
            AddButton(pausePanel.transform, "Main Menu", ToMainMenu);
            if (CanQuit) AddButton(pausePanel.transform, "Quit", Quit);

            settingsPanel = Panel(safe.transform, "Settings", "Settings", 52);
            GameSettings settings = SettingsService.Current;
            AddSlider(settingsPanel.transform, "Volume", 0f, 1f, settings.Volume, value =>
            {
                SettingsService.Current.Volume = value;
                SettingsService.SaveAndApply();
            });
            AddSlider(settingsPanel.transform, "Camera speed", GameSettings.MinCameraSpeed, GameSettings.MaxCameraSpeed, settings.CameraSpeed, value =>
            {
                SettingsService.Current.CameraSpeed = value;
                SettingsService.SaveAndApply();
            });
            Text invertLabel = null;
            invertLabel = AddButton(settingsPanel.transform, InvertText(), () =>
            {
                SettingsService.Current.InvertLook = !SettingsService.Current.InvertLook;
                SettingsService.SaveAndApply();
                invertLabel.text = InvertText();
            }).GetComponentInChildren<Text>();
            qualityLabel = AddButton(settingsPanel.transform, "Graphics", CycleQuality).GetComponentInChildren<Text>();
            AddButton(settingsPanel.transform, "Back", flow.BackPressed);
        }

        private static string InvertText()
        {
            return $"Invert look: {(SettingsService.Current.InvertLook ? "On" : "Off")}";
        }

        private GameObject Panel(Transform parent, string name, string title, int titleSize)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 760f);
            panel.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.9f);
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 40);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            Text heading = NewText(panel.transform, "Title", titleSize);
            heading.text = title;
            heading.fontStyle = FontStyle.Bold;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = titleSize + 40f;
            panel.SetActive(false);
            return panel;
        }

        private Button AddButton(Transform parent, string label, Action onClick)
        {
            GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.14f);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.85f, 0.55f, 1f);
            colours.pressedColor = new Color(1f, 0.7f, 0.3f, 1f);
            button.colors = colours;
            button.onClick.AddListener(() => { GameAudio.Play(GameSound.UiClick); onClick(); Show(); });
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 96f;

            Text text = NewText(buttonObject.transform, "Label", 36);
            text.text = label;
            Stretch(text.rectTransform);
            return button;
        }

        private void AddSlider(Transform parent, string label, float min, float max, float value, Action<float> onChange)
        {
            Text caption = NewText(parent, label + " Label", 30);
            caption.text = label;
            caption.alignment = TextAnchor.LowerLeft;
            caption.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            GameObject sliderObject = new GameObject(label + " Slider", typeof(RectTransform));
            sliderObject.transform.SetParent(parent, false);
            sliderObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            GameObject track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(sliderObject.transform, false);
            RectTransform trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0f, 0.4f);
            trackRect.anchorMax = new Vector2(1f, 0.6f);
            trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;
            track.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);

            GameObject area = new GameObject("Handle Area", typeof(RectTransform));
            area.transform.SetParent(sliderObject.transform, false);
            RectTransform areaRect = (RectTransform)area.transform;
            Stretch(areaRect);
            areaRect.offsetMin = new Vector2(24f, 0f);
            areaRect.offsetMax = new Vector2(-24f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(area.transform, false);
            RectTransform handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(48f, 0f);
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.82f, 0.45f, 1f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => onChange(v));
        }

        private Text NewText(Transform parent, string name, int size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
