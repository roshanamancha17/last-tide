using UnityEngine;
using UnityEngine.UI;

namespace LastTide
{
    /// <summary>Points the player to the buried robot: a bobbing arrow above it, and an arrow at the screen edge when it's off-screen.</summary>
    public class GuideMarker : MonoBehaviour
    {
        [SerializeField] private Marlow marlow;
        [SerializeField] private Transform player;
        [SerializeField] private Transform worldArrow;
        [SerializeField] private float arrowHeight = 2.2f;
        [SerializeField] private string label = "Robot";

        private Camera mainCamera;
        private RectTransform edgeArrow;
        private RectTransform distanceLabel;
        private Text distanceText;
        private Canvas canvas;

        public void Configure(Marlow robot, Transform playerTransform, Transform arrow)
        {
            marlow = robot;
            player = playerTransform;
            worldArrow = arrow;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            BuildUi();
        }

        private void LateUpdate()
        {
            bool active = marlow != null && !marlow.IsAwake && player != null;
            if (worldArrow != null && worldArrow.gameObject.activeSelf != active)
            {
                worldArrow.gameObject.SetActive(active);
            }

            if (!active || mainCamera == null)
            {
                SetUi(false, false);
                if (!active && marlow != null && marlow.IsAwake)
                {
                    enabled = false;
                }
                return;
            }

            Vector3 target = marlow.transform.position;
            float bob = Mathf.Sin(Time.time * 3f) * 0.25f;
            worldArrow.position = target + Vector3.up * (arrowHeight + bob);
            worldArrow.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);

            float distance = Vector3.Distance(player.position, target);
            distanceText.text = $"{label}  {distance:0} m";

            Vector3 labelPoint = mainCamera.WorldToScreenPoint(worldArrow.position + Vector3.up * 0.9f);
            bool onScreen = labelPoint.z > 0f && labelPoint.x > 0f && labelPoint.x < Screen.width &&
                            labelPoint.y > 0f && labelPoint.y < Screen.height;
            float scale = canvas.scaleFactor;
            if (onScreen)
            {
                distanceLabel.anchoredPosition = new Vector2(labelPoint.x, labelPoint.y) / scale;
                SetUi(false, true);
                return;
            }

            // Off-screen: put an arrow on the screen edge, pointing toward the robot.
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(target);
            if (screenPoint.z < 0f)
            {
                // Behind the camera the projection is mirrored, so flip it back.
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }

            Vector2 direction = new Vector2(screenPoint.x - Screen.width * 0.5f, screenPoint.y - Screen.height * 0.5f);
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.down;
            }

            direction.Normalize();
            Vector2 centre = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 margin = new Vector2(Screen.width * 0.5f - 90f * scale, Screen.height * 0.5f - 90f * scale);
            float t = Mathf.Min(margin.x / Mathf.Max(Mathf.Abs(direction.x), 0.0001f), margin.y / Mathf.Max(Mathf.Abs(direction.y), 0.0001f));
            Vector2 position = centre + direction * t;
            // Stay above the interaction prompt and the survival bars at the bottom of the screen.
            position.y = Mathf.Clamp(position.y, 230f * scale, Screen.height - 150f * scale);
            edgeArrow.anchoredPosition = position / scale;
            edgeArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            distanceLabel.anchoredPosition = (position - direction * 70f * scale) / scale;
            SetUi(true, true);
        }

        private void SetUi(bool arrow, bool text)
        {
            if (edgeArrow == null)
            {
                return;
            }

            if (edgeArrow.gameObject.activeSelf != arrow) edgeArrow.gameObject.SetActive(arrow);
            if (distanceLabel.gameObject.activeSelf != text) distanceLabel.gameObject.SetActive(text);
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("Guide Marker UI");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject arrowObject = new GameObject("Edge Arrow");
            edgeArrow = arrowObject.AddComponent<RectTransform>();
            edgeArrow.SetParent(canvasObject.transform, false);
            edgeArrow.anchorMin = edgeArrow.anchorMax = Vector2.zero;
            edgeArrow.sizeDelta = new Vector2(64f, 64f);
            Image image = arrowObject.AddComponent<Image>();
            image.sprite = ArrowSprite();
            image.color = new Color(1f, 0.82f, 0.25f);
            Outline outline = arrowObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelObject = new GameObject("Distance");
            distanceLabel = labelObject.AddComponent<RectTransform>();
            distanceLabel.SetParent(canvasObject.transform, false);
            distanceLabel.anchorMin = distanceLabel.anchorMax = Vector2.zero;
            distanceLabel.sizeDelta = new Vector2(260f, 40f);
            distanceText = labelObject.AddComponent<Text>();
            distanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            distanceText.fontSize = 26;
            distanceText.alignment = TextAnchor.MiddleCenter;
            distanceText.color = new Color(1f, 0.9f, 0.55f);
            Outline textOutline = labelObject.AddComponent<Outline>();
            textOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            SetUi(false, false);
        }

        // A filled arrow pointing up, drawn into a small texture.
        private static Sprite ArrowSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size;
                    bool head = v > 0.42f && Mathf.Abs(u) < (1f - v) * 0.85f;
                    bool shaft = v <= 0.46f && v > 0.06f && Mathf.Abs(u) < 0.13f;
                    texture.SetPixel(x, y, head || shaft ? Color.white : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
