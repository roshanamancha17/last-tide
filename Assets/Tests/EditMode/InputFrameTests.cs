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
        public void CameraTurnFromMouseAndTouchAddsUp()
        {
            InputFrame both = InputFrame.Combine(new InputFrame { Look = new Vector2(3f, 1f) }, new InputFrame { Look = new Vector2(2f, -1f) });
            Assert.AreEqual(new Vector2(5f, 0f), both.Look);
        }
    }
}
