using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace LastTide
{
    /// <summary>On-screen joystick, camera drag and buttons. Turns fingers into an InputFrame.</summary>
    // Reads fingers before InputCollector (-500) asks for them, so taps aren't a frame late.
    [DefaultExecutionOrder(-600)]
    public class TouchControls : MonoBehaviour
    {
        /// <summary>A swipe across the whole screen width turns the camera about this many mouse-pixel units.</summary>
        [SerializeField] private float lookPerScreenWidth = 1500f;

        private readonly HashSet<TouchAction> pressed = new HashSet<TouchAction>();
        private readonly HashSet<int> buttonFingers = new HashSet<int>();
        private readonly List<TouchSample> samples = new List<TouchSample>();
        private readonly Dictionary<TouchAction, Image> buttonImages = new Dictionary<TouchAction, Image>();
        private TouchRouter router;
        private Canvas canvas;
        private RectTransform joystickBase;
        private RectTransform joystickKnob;
        private Rect builtSafeArea;
        private Vector2 builtScreen;

        public TouchLayout Layout { get; private set; }
        public bool SprintOn { get; private set; }

        /// <summary>Creates the layout and the on-screen graphics for one screen shape.</summary>
        public void Build(Vector2 screenSize, Rect safeArea, float dpi)
        {
            Layout = new TouchLayout(screenSize, safeArea, dpi);
            router = new TouchRouter(new VirtualJoystick(Layout.JoystickRadius));
            builtScreen = screenSize;
            builtSafeArea = safeArea;
            buttonFingers.Clear();
            BuildGraphics();
        }

        /// <summary>Handles one frame of fingers. Public so tests can feed made-up touches.</summary>
        public void Process(IReadOnlyList<TouchSample> touches)
        {
            foreach (TouchSample touch in touches)
            {
                if (touch.Stage == TouchStage.Began && Layout.TryGetButton(touch.Position, out TouchAction action))
                {
                    buttonFingers.Add(touch.Id);
                    if (action == TouchAction.Sprint)
                    {
                        SprintOn = !SprintOn;
                    }
                    else
                    {
                        pressed.Add(action);
                    }
                }
                else if (touch.Stage == TouchStage.Ended)
                {
                    buttonFingers.Remove(touch.Id);
                }
            }

            router.Process(touches, Layout.ScreenSize.x, point => Layout.TryGetButton(point, out _));
        }

        /// <summary>This frame's touch input. Button taps count once, then clear.</summary>
        public InputFrame Collect()
        {
            if (router == null)
            {
                return default;
            }

            InputFrame frame = new InputFrame
            {
                Move = router.Joystick.Value,
                Look = router.LookDelta * (lookPerScreenWidth / Mathf.Max(1f, Layout.ScreenSize.x)),
                Sprint = SprintOn && router.Joystick.Value.sqrMagnitude > 0f,
                Jump = pressed.Contains(TouchAction.Jump),
                Interact = pressed.Contains(TouchAction.Interact),
                Eat = pressed.Contains(TouchAction.Eat),
                Craft = pressed.Contains(TouchAction.Craft),
                Talk = pressed.Contains(TouchAction.Talk),
            };
            pressed.Clear();
            return frame;
        }

        /// <summary>Drops every finger, for example when the app loses focus.</summary>
        public void ReleaseAll()
        {
            router?.Reset();
            buttonFingers.Clear();
            pressed.Clear();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                ReleaseAll();
            }
        }

        private void Update()
        {
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (Layout == null || screen != builtScreen || Screen.safeArea != builtSafeArea)
            {
                Build(screen, Screen.safeArea, Screen.dpi);
            }

            samples.Clear();
            foreach (Touch touch in Touch.activeTouches)
            {
                samples.Add(new TouchSample(touch.finger.index, Stage(touch.phase), touch.screenPosition, touch.delta));
            }

            Process(samples);
            UpdateGraphics();
        }

        private static TouchStage Stage(UnityTouchPhase phase)
        {
            switch (phase)
            {
                case UnityTouchPhase.Began: return TouchStage.Began;
                case UnityTouchPhase.Moved: return TouchStage.Moved;
                case UnityTouchPhase.Ended:
                case UnityTouchPhase.Canceled: return TouchStage.Ended;
                default: return TouchStage.Held;
            }
        }

        private void BuildGraphics()
        {
            if (canvas != null)
            {
                DestroyImmediate(canvas.gameObject);
            }

            buttonImages.Clear();
            GameObject canvasObject = new GameObject("Touch Controls UI");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;

            Sprite circle = CircleSprite();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (KeyValuePair<TouchAction, Rect> pair in Layout.Buttons)
            {
                Image image = Circle($"{pair.Key} Button", canvas.transform, circle, new Color(1f, 1f, 1f, 0.22f));
                Place(image.rectTransform, pair.Value);
                buttonImages[pair.Key] = image;

                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(image.transform, false);
                Text label = labelObject.AddComponent<Text>();
                label.font = font;
                label.text = pair.Key.ToString();
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(1f, 1f, 1f, 0.9f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 40;
                label.raycastTarget = false;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0.1f, 0.25f);
                labelRect.anchorMax = new Vector2(0.9f, 0.75f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            float ring = Layout.JoystickRadius * 2f;
            joystickBase = Circle("Joystick Base", canvas.transform, circle, new Color(1f, 1f, 1f, 0.15f)).rectTransform;
            joystickBase.sizeDelta = new Vector2(ring, ring);
            joystickKnob = Circle("Joystick Knob", canvas.transform, circle, new Color(1f, 1f, 1f, 0.45f)).rectTransform;
            joystickKnob.sizeDelta = new Vector2(ring * 0.45f, ring * 0.45f);
            joystickBase.gameObject.SetActive(false);
            joystickKnob.gameObject.SetActive(false);
        }

        private void UpdateGraphics()
        {
            bool active = router.Joystick.Active;
            joystickBase.gameObject.SetActive(active);
            joystickKnob.gameObject.SetActive(active);
            if (active)
            {
                joystickBase.anchoredPosition = router.Joystick.Origin;
                joystickKnob.anchoredPosition = router.Joystick.Knob;
            }

            if (buttonImages.TryGetValue(TouchAction.Sprint, out Image sprint))
            {
                sprint.color = SprintOn ? new Color(1f, 0.75f, 0.3f, 0.5f) : new Color(1f, 1f, 1f, 0.22f);
            }
        }

        // The canvas isn't scaled, so screen pixels and canvas units are the same.
        private static void Place(RectTransform rect, Rect screenRect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = screenRect.center;
            rect.sizeDelta = screenRect.size;
        }

        private static Image Circle(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            return image;
        }

        private static Sprite CircleSprite()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance);
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(distance - (radius - 4f)) / 3f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * (0.55f + 0.45f * rim)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
