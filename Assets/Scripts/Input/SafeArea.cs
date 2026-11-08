using UnityEngine;

namespace LastTide
{
    /// <summary>Converts the screen's safe area (clear of notches and camera holes) into UI anchors.</summary>
    public static class SafeArea
    {
        public static void Anchors(Rect safeArea, Vector2 screenSize, out Vector2 min, out Vector2 max)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }

            min = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            max = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
        }
    }

    /// <summary>Keeps a full-screen UI panel inside the safe area, following rotation and resizes.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        private Rect applied;
        private Vector2 appliedScreen;

        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            SafeArea.Anchors(safeArea, screenSize, out Vector2 min, out Vector2 max);
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            applied = safeArea;
            appliedScreen = screenSize;
        }

        private void Update()
        {
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (Screen.safeArea != applied || screen != appliedScreen)
            {
                Apply(Screen.safeArea, screen);
            }
        }
    }
}
