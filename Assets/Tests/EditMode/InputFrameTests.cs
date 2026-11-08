using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class InputFrameTests
    {
        [Test]
        public void KeyboardAndTouchMovementTogetherNeverExceedFullSpeed()
        {
            InputFrame both = InputFrame.Combine(new InputFrame { Move = Vector2.up }, new InputFrame { Move = Vector2.up });
            Assert.AreEqual(1f, both.Move.magnitude, 0.001f);
        }

        [Test]
        public void APressOnEitherDeviceCounts()
        {
            InputFrame both = InputFrame.Combine(new InputFrame { Jump = true }, new InputFrame { Interact = true, Sprint = true });
            Assert.IsTrue(both.Jump && both.Interact && both.Sprint);
            Assert.IsFalse(both.Eat || both.Craft || both.Talk);
        }

        [Test]
        public void ARecipeChosenOnEitherDeviceCounts()
        {
            Assert.AreEqual(3, InputFrame.Combine(new InputFrame(), new InputFrame { Recipe = 3 }).Recipe);
            Assert.AreEqual(1, InputFrame.Combine(new InputFrame { Recipe = 1 }, new InputFrame()).Recipe);
            Assert.AreEqual(0, InputFrame.Combine(new InputFrame(), new InputFrame()).Recipe);
        }

        [Test]
        public void AReportFromEitherDeviceCounts()
        {
            Assert.IsTrue(InputFrame.Combine(new InputFrame(), new InputFrame { Report = true }).Report);
        }

        [Test]
        public void CameraTurnFromMouseAndTouchAddsUp()
        {
            InputFrame both = InputFrame.Combine(new InputFrame { Look = new Vector2(3f, 1f) }, new InputFrame { Look = new Vector2(2f, -1f) });
            Assert.AreEqual(new Vector2(5f, 0f), both.Look);
        }
    }
}
