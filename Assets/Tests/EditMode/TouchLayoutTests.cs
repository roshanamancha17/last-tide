using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class TouchLayoutTests
    {
        // Screen size, safe area and pixel density of real phone and tablet shapes.
        public static IEnumerable<TestCaseData> Screens()
        {
            yield return new TestCaseData(new Vector2(2400, 1080), new Rect(0, 0, 2400, 1080), 400f).SetName("Modern phone 2400x1080");
            yield return new TestCaseData(new Vector2(1600, 720), new Rect(0, 0, 1600, 720), 270f).SetName("Budget phone 1600x720");
            yield return new TestCaseData(new Vector2(1280, 720), new Rect(0, 0, 1280, 720), 320f).SetName("Small phone 1280x720");
            yield return new TestCaseData(new Vector2(2340, 1080), new Rect(100, 0, 2140, 1080), 400f).SetName("Notched phone 2340x1080");
            yield return new TestCaseData(new Vector2(2048, 1536), new Rect(0, 0, 2048, 1536), 264f).SetName("Tablet 2048x1536");
            yield return new TestCaseData(new Vector2(1920, 1080), new Rect(0, 0, 1920, 1080), 0f).SetName("Editor 1920x1080 unknown dpi");
        }

        [TestCaseSource(nameof(Screens))]
        public void ButtonsFitTheSafeAreaAreBigEnoughAndDoNotOverlap(Vector2 screen, Rect safe, float dpi)
        {
            var layout = new TouchLayout(screen, safe, dpi);
            List<KeyValuePair<TouchAction, Rect>> buttons = layout.Buttons.ToList();
            Assert.AreEqual(11, buttons.Count, "6 buttons, 3 recipe buttons, Report and Pause");
            float minimum = TouchLayout.MinButtonPixels(dpi);
            foreach (KeyValuePair<TouchAction, Rect> button in buttons)
            {
                Rect rect = button.Value;
                Assert.GreaterOrEqual(rect.width + 0.01f, minimum, $"{button.Key} is too small for a thumb");
                Assert.IsTrue(rect.xMin >= safe.xMin && rect.xMax <= safe.xMax && rect.yMin >= safe.yMin && rect.yMax <= safe.yMax,
                    $"{button.Key} {rect} leaves the safe area {safe}");
                Assert.Greater(rect.xMin, screen.x * 0.5f, $"{button.Key} is on the joystick's side of the screen");
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                for (int j = i + 1; j < buttons.Count; j++)
                {
                    Rect a = buttons[i].Value, b = buttons[j].Value;
                    float gap = Vector2.Distance(a.center, b.center) - (a.width + b.width) * 0.5f;
                    Assert.GreaterOrEqual(gap, 0f, $"{buttons[i].Key} overlaps {buttons[j].Key}");
                }
            }
        }

        [TestCaseSource(nameof(Screens))]
        public void TappingAButtonsCentreFindsThatButtonAndTheLeftSideFindsNone(Vector2 screen, Rect safe, float dpi)
        {
            var layout = new TouchLayout(screen, safe, dpi);
            foreach (KeyValuePair<TouchAction, Rect> button in layout.Buttons)
            {
                Assert.IsTrue(layout.TryGetButton(button.Value.center, out TouchAction found, recipesVisible: true, reportVisible: true));
                Assert.AreEqual(button.Key, found);
            }

            Assert.IsFalse(layout.TryGetButton(new Vector2(screen.x * 0.2f, screen.y * 0.3f), out _));
        }

        [TestCaseSource(nameof(Screens))]
        public void HiddenRecipeButtonsCannotBeTapped(Vector2 screen, Rect safe, float dpi)
        {
            var layout = new TouchLayout(screen, safe, dpi);
            Assert.IsFalse(layout.TryGetButton(layout.Buttons[TouchAction.Recipe2].center, out _));
            Assert.IsFalse(layout.TryGetButton(layout.Buttons[TouchAction.Report].center, out _));
            Assert.IsTrue(layout.TryGetButton(layout.Buttons[TouchAction.Craft].center, out TouchAction craft));
            Assert.AreEqual(TouchAction.Craft, craft);
        }
    }
}
