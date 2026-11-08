using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class VirtualJoystickTests
    {
        private const float Radius = 100f;

        private static VirtualJoystick Pushed(Vector2 offset)
        {
            var joystick = new VirtualJoystick(Radius);
            joystick.Begin(new Vector2(200f, 200f));
            joystick.Drag(new Vector2(200f, 200f) + offset);
            return joystick;
        }

        [Test]
        public void ThumbOnTheCentreGivesNoMovement()
        {
            Assert.AreEqual(Vector2.zero, Pushed(Vector2.zero).Value);
        }

        [Test]
        public void SmallWobbleInsideTheDeadZoneGivesNoMovement()
        {
            Assert.AreEqual(Vector2.zero, Pushed(new Vector2(8f, 5f)).Value);
        }

        [Test]
        public void FullPushRightMovesFullyRight()
        {
            Vector2 value = Pushed(new Vector2(Radius, 0f)).Value;
            Assert.AreEqual(1f, value.x, 0.001f);
            Assert.AreEqual(0f, value.y, 0.001f);
        }

        [Test]
        public void PushingPastTheEdgeIsCappedAndTheKnobStaysOnTheRing()
        {
            VirtualJoystick joystick = Pushed(new Vector2(0f, 500f));
            Assert.AreEqual(1f, joystick.Value.magnitude, 0.001f);
            Assert.AreEqual(Radius, Vector2.Distance(joystick.Origin, joystick.Knob), 0.001f);
        }

        [Test]
        public void HalfPushGivesPartialSpeedInTheRightDirection()
        {
            Vector2 value = Pushed(new Vector2(-50f, 0f)).Value;
            Assert.Less(value.x, -0.3f);
            Assert.Greater(value.x, -0.7f);
        }

        [Test]
        public void LettingGoStopsMovement()
        {
            VirtualJoystick joystick = Pushed(new Vector2(Radius, 0f));
            joystick.End();
            Assert.IsFalse(joystick.Active);
            Assert.AreEqual(Vector2.zero, joystick.Value);
        }

        [Test]
        public void DraggingBeforeTouchingDoesNothing()
        {
            var joystick = new VirtualJoystick(Radius);
            joystick.Drag(new Vector2(500f, 500f));
            Assert.AreEqual(Vector2.zero, joystick.Value);
        }
    }
}
