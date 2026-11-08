using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastTide
{
    /// <summary>Builds and updates the on-screen inventory, prompts and crafting panel.</summary>
    [RequireComponent(typeof(Inventory))]
    public class GameHud : MonoBehaviour
    {
        private const float MessageSeconds = 2.5f;

        private Inventory inventory;
        private Font font;
        private Text inventoryText;
        private Text promptText;
        private Text messageText;
        private Text craftingText;
        private GameObject promptPanel;
        private GameObject craftingPanel;
        private RectTransform hungerFill;
        private RectTransform energyFill;
        private Image hungerImage;
        private Image energyImage;
        private Text timeText;
        private Image fadeImage;
        private GameObject objectivePanel;
        private Text objectiveText;
        private GameObject endingPanel;
        private Text endingText;
        private GameObject inventoryPanel;
        private GameObject hintPanel;
        private GameObject statsPanel;
        private float messageUntil;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
            inventory.Changed += RefreshInventory;
            RefreshInventory();
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.Changed -= RefreshInventory;
            }
        }

        private void Update()
        {
            // The key list in the corner isn't needed when the buttons are on screen.
            if (InputCollector.TouchActive && hintPanel.activeSelf)
            {
                hintPanel.SetActive(false);
            }

            if (messageText.gameObject.activeSelf && Time.time >= messageUntil)
            {
                messageText.gameObject.SetActive(false);
            }
        }

        public void SetPrompt(string prompt)
        {
            prompt = ControlHints.ForDevice(prompt, InputCollector.TouchActive);
            bool visible = !string.IsNullOrEmpty(prompt);
            if (promptPanel.activeSelf != visible)
            {
                promptPanel.SetActive(visible);
            }

            if (visible && promptText.text != prompt)
            {
                promptText.text = prompt;
            }
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            messageUntil = Time.time + MessageSeconds;
        }

        public void SetStats(float hunger, float energy, string time)
        {
            SetBar(hungerFill, hungerImage, hunger, new Color(0.95f, 0.62f, 0.2f));
            SetBar(energyFill, energyImage, energy, new Color(0.3f, 0.75f, 0.95f));
            if (timeText.text != time)
            {
                timeText.text = time;
            }
        }

        public void SetObjective(string objective)
        {
            bool visible = !string.IsNullOrEmpty(objective);
            if (objectivePanel.activeSelf != visible)
            {
                objectivePanel.SetActive(visible);
            }

            if (visible && objectiveText.text != objective)
            {
                objectiveText.text = objective;
            }
        }

        public void ShowEnding(string text)
        {
            text = ControlHints.ForDevice(text, InputCollector.TouchActive);
            endingText.text = text;
            endingPanel.SetActive(true);
            inventoryPanel.SetActive(false);
            hintPanel.SetActive(false);
            statsPanel.SetActive(false);
            craftingPanel.SetActive(false);
        }

        public void SetFade(float alpha)
        {
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            fadeImage.enabled = alpha > 0f;
        }

        private static void SetBar(RectTransform fill, Image image, float value, Color normal)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            image.color = value < 0.2f ? new Color(0.95f, 0.25f, 0.2f) : normal;
        }

        public void SetCraftingPanel(string text)
        {
            text = ControlHints.ForDevice(text, InputCollector.TouchActive);
            bool visible = !string.IsNullOrEmpty(text);
            craftingPanel.SetActive(visible);
            if (visible)
            {
                craftingText.text = text;
            }
        }

        private void RefreshInventory()
        {
            StringBuilder text = new StringBuilder();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
            {
                int count = inventory.Count(item);
                if (count > 0)
                {
                    text.Append(item >= ItemType.Knife ? $"{item}\n" : $"{item}  {count}\n");
                }
            }

            inventoryText.text = text.Length > 0 ? text.ToString().TrimEnd() : "Nothing yet";
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform screenRoot = canvasObject.transform;
            GameObject safeObject = new GameObject("Safe Area", typeof(RectTransform));
            safeObject.transform.SetParent(screenRoot, false);
            safeObject.AddComponent<SafeAreaPanel>().Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            Transform root = safeObject.transform;

            inventoryPanel = CreatePanel(root, "Inventory", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(240f, 330f));
            inventoryText = CreateText(inventoryPanel.transform, TextAnchor.UpperLeft, 28);

            hintPanel = CreatePanel(root, "Hint", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(360f, 60f));
            CreateText(hintPanel.transform, TextAnchor.MiddleCenter, 26).text = "[Tab] Craft    [C] Eat";

            statsPanel = CreatePanel(root, "Stats", new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(400f, 150f));
            timeText = CreateText(statsPanel.transform, TextAnchor.UpperLeft, 28);
            CreateBar(statsPanel.transform, "Hunger", 62f, out hungerFill, out hungerImage);
            CreateBar(statsPanel.transform, "Energy", 22f, out energyFill, out energyImage);

            promptPanel = CreatePanel(root, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(620f, 64f));
            promptText = CreateText(promptPanel.transform, TextAnchor.MiddleCenter, 30);
            promptPanel.SetActive(false);

            messageText = CreateText(root, TextAnchor.MiddleCenter, 34);
            RectTransform messageRect = messageText.rectTransform;
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0f);
            messageRect.pivot = new Vector2(0.5f, 0f);
            messageRect.anchoredPosition = new Vector2(0f, 200f);
            messageRect.sizeDelta = new Vector2(900f, 60f);
            Outline outline = messageText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            messageText.gameObject.SetActive(false);

            craftingPanel = CreatePanel(root, "Crafting", new Vector2(1f, 1f), new Vector2(-24f, -100f), new Vector2(560f, 260f));
            craftingText = CreateText(craftingPanel.transform, TextAnchor.UpperLeft, 28);
            craftingPanel.SetActive(false);

            objectivePanel = CreatePanel(root, "Objective", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(760f, 60f));
            objectiveText = CreateText(objectivePanel.transform, TextAnchor.MiddleCenter, 28);

            endingPanel = CreatePanel(screenRoot, "Ending", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            endingPanel.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.09f, 0.78f);
            endingText = CreateText(endingPanel.transform, TextAnchor.MiddleCenter, 44);
            endingPanel.SetActive(false);

            GameObject fadeObject = new GameObject("Fade");
            RectTransform fadeRect = fadeObject.AddComponent<RectTransform>();
            fadeRect.SetParent(screenRoot, false);
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeImage = fadeObject.AddComponent<Image>();
            SetFade(0f);
        }

        private void CreateBar(Transform parent, string label, float y, out RectTransform fill, out Image image)
        {
            Text text = CreateText(parent, TextAnchor.MiddleLeft, 24);
            text.text = label;
            RectTransform labelRect = text.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = Vector2.zero;
            labelRect.anchoredPosition = new Vector2(16f, y - 4f);
            labelRect.sizeDelta = new Vector2(110f, 32f);

            GameObject back = new GameObject(label + " Bar");
            RectTransform backRect = back.AddComponent<RectTransform>();
            backRect.SetParent(parent, false);
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = Vector2.zero;
            backRect.anchoredPosition = new Vector2(126f, y);
            backRect.sizeDelta = new Vector2(258f, 24f);
            back.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            GameObject front = new GameObject("Fill");
            fill = front.AddComponent<RectTransform>();
            fill.SetParent(backRect, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            image = front.AddComponent<Image>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.72f);
            return panel;
        }

        private Text CreateText(Transform parent, TextAnchor alignment, int size)
        {
            GameObject textObject = new GameObject("Text");
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 10f);
            rect.offsetMax = new Vector2(-16f, -10f);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
