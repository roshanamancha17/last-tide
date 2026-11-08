using System.Collections.Generic;
using UnityEngine;

namespace LastTide
{
    public enum TouchAction { Jump, Interact, Eat, Sprint, Craft, Talk, Recipe1, Recipe2, Recipe3 }

    /// <summary>
    /// Works out where the on-screen buttons go for a given screen, safe area (notches) and pixel density.
    /// Pure maths, so every phone shape can be checked in tests.
    /// </summary>
    public sealed class TouchLayout
    {
        /// <summary>The smallest comfortable touch target, in density-independent pixels.</summary>
        public const float MinButtonDp = 48f;

        private const float ReferenceHeight = 1080f;
        private const float ReferenceWidth = 1920f;
        private const float SmallestReferenceButton = 110f;

        // Centre (from the safe area's bottom-right corner) and diameter, in 1080p reference units.
        private static readonly (TouchAction action, Vector2 centre, float diameter)[] Design =
        {
            (TouchAction.Jump, new Vector2(-150f, 150f), 170f),
            (TouchAction.Interact, new Vector2(-370f, 120f), 140f),
            (TouchAction.Eat, new Vector2(-560f, 105f), 110f),
            (TouchAction.Sprint, new Vector2(-150f, 370f), 120f),
            (TouchAction.Craft, new Vector2(-150f, 530f), 110f),
            (TouchAction.Talk, new Vector2(-150f, 680f), 110f),
            // Only shown while the crafting list is open.
            (TouchAction.Recipe1, new Vector2(-340f, 370f), 110f),
            (TouchAction.Recipe2, new Vector2(-340f, 530f), 110f),
            (TouchAction.Recipe3, new Vector2(-340f, 690f), 110f),
        };

        public static bool IsRecipe(TouchAction action)
        {
            return action == TouchAction.Recipe1 || action == TouchAction.Recipe2 || action == TouchAction.Recipe3;
        }

        private readonly Dictionary<TouchAction, Rect> buttons = new Dictionary<TouchAction, Rect>();

        public TouchLayout(Vector2 screenSize, Rect safeArea, float dpi)
        {
            ScreenSize = screenSize;
            SafeArea = safeArea;
            float fit = Mathf.Min(safeArea.height / ReferenceHeight, safeArea.width / ReferenceWidth);
            // Grow the whole cluster on dense screens, so the smallest button is still big enough for a thumb.
            Scale = Mathf.Max(fit, MinButtonPixels(dpi) / SmallestReferenceButton);
            JoystickRadius = 110f * Scale;
            Vector2 corner = new Vector2(safeArea.xMax, safeArea.yMin);
            foreach ((TouchAction action, Vector2 centre, float diameter) in Design)
            {
                float size = diameter * Scale;
                Vector2 middle = corner + centre * Scale;
                buttons[action] = new Rect(middle.x - size * 0.5f, middle.y - size * 0.5f, size, size);
            }
        }

        public Vector2 ScreenSize { get; }
        public Rect SafeArea { get; }
        public float Scale { get; }
        public float JoystickRadius { get; }
        public IReadOnlyDictionary<TouchAction, Rect> Buttons => buttons;

        public static float MinButtonPixels(float dpi)
        {
            return MinButtonDp * (dpi > 0f ? dpi : 160f) / 160f;
        }

        /// <summary>The visible button under a screen point, using the round button shape.</summary>
        public bool TryGetButton(Vector2 point, out TouchAction action, bool recipesVisible = false)
        {
            foreach (KeyValuePair<TouchAction, Rect> pair in buttons)
            {
                if (!recipesVisible && IsRecipe(pair.Key))
                {
                    continue;
                }

                float radius = pair.Value.width * 0.5f;
                if ((point - pair.Value.center).sqrMagnitude <= radius * radius)
                {
                    action = pair.Key;
                    return true;
                }
            }

            action = default;
            return false;
        }
    }
}
