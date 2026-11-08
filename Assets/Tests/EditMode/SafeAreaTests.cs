using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class SafeAreaTests
    {
        [Test]
        public void FullScreenSafeAreaFillsTheScreen()
        {
            SafeArea.Anchors(new Rect(0, 0, 1920, 1080), new Vector2(1920, 1080), out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void ACameraHoleOnTheLeftPushesTheLeftEdgeIn()
        {
            SafeArea.Anchors(new Rect(117, 0, 2223, 1080), new Vector2(2340, 1080), out Vector2 min, out Vector2 max);
            Assert.AreEqual(0.05f, min.x, 0.0001f);
            Assert.AreEqual(0f, min.y);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void ANotchAndHomeBarShrinkBothSides()
        {
            SafeArea.Anchors(new Rect(132, 63, 2268, 1107), new Vector2(2532, 1170), out Vector2 min, out Vector2 max);
            Assert.AreEqual(132f / 2532f, min.x, 0.0001f);
            Assert.AreEqual(63f / 1170f, min.y, 0.0001f);
            Assert.AreEqual(2400f / 2532f, max.x, 0.0001f);
            Assert.AreEqual(1f, max.y, 0.0001f);
        }

        [Test]
        public void ABrokenReportFallsBackToTheWholeScreen()
        {
            SafeArea.Anchors(new Rect(0, 0, 0, 0), new Vector2(1920, 1080), out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void ThePanelFollowsTheSafeArea()
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform));
            GameObject panel = new GameObject("Safe Area", typeof(RectTransform));
            try
            {
                panel.transform.SetParent(canvas.transform, false);
                SafeAreaPanel safe = panel.AddComponent<SafeAreaPanel>();
                safe.Apply(new Rect(117, 0, 2223, 1080), new Vector2(2340, 1080));
                RectTransform rect = (RectTransform)panel.transform;
                Assert.AreEqual(0.05f, rect.anchorMin.x, 0.0001f);
                Assert.AreEqual(Vector2.zero, rect.offsetMin);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
