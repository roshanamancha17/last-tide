using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class TouchRouterTests
    {
        private const float Width = 1000f;
        private TouchRouter router;

        [SetUp]
        public void Create()
        {
            router = new TouchRouter(new VirtualJoystick(100f));
        }

        private void Frame(params TouchSample[] touches)
        {
            router.Process(new List<TouchSample>(touches), Width, position => position.x > 850f && position.y < 150f);
        }

        [Test]
        public void LeftThumbDrivesTheJoystick()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(200f, 200f)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(200f, 300f)));
            Assert.AreEqual(1f, router.Joystick.Value.y, 0.001f);
            Assert.AreEqual(Vector2.zero, router.LookDelta);
        }

        [Test]
        public void RightThumbTurnsTheCameraOnlyWhileMoving()
        {
            Frame(new TouchSample(2, TouchStage.Began, new Vector2(700f, 400f)));
            Frame(new TouchSample(2, TouchStage.Moved, new Vector2(730f, 400f), new Vector2(30f, 0f)));
            Assert.AreEqual(new Vector2(30f, 0f), router.LookDelta);
            Frame(new TouchSample(2, TouchStage.Held, new Vector2(730f, 400f)));
            Assert.AreEqual(Vector2.zero, router.LookDelta);
            Assert.IsFalse(router.Joystick.Active);
        }

        [Test]
        public void CameraTurnComesFromFingerPositionsEvenIfTheDeviceReportsNoDelta()
        {
            Frame(new TouchSample(2, TouchStage.Began, new Vector2(700f, 400f)));
            Frame(new TouchSample(2, TouchStage.Moved, new Vector2(740f, 390f)));
            Assert.AreEqual(new Vector2(40f, -10f), router.LookDelta);
            Frame(new TouchSample(2, TouchStage.Moved, new Vector2(760f, 390f)));
            Assert.AreEqual(new Vector2(20f, 0f), router.LookDelta);
        }

        [Test]
        public void MovingAndLookingWorkAtTheSameTime()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(200f, 200f)),
                  new TouchSample(2, TouchStage.Began, new Vector2(700f, 400f)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(300f, 200f)),
                  new TouchSample(2, TouchStage.Moved, new Vector2(700f, 420f), new Vector2(0f, 20f)));
            Assert.AreEqual(1f, router.Joystick.Value.x, 0.001f);
            Assert.AreEqual(new Vector2(0f, 20f), router.LookDelta);
        }

        [Test]
        public void TouchesThatStartOnAButtonAreLeftToTheButton()
        {
            Frame(new TouchSample(3, TouchStage.Began, new Vector2(900f, 100f)));
            Frame(new TouchSample(3, TouchStage.Moved, new Vector2(880f, 100f), new Vector2(-20f, 0f)));
            Assert.AreEqual(Vector2.zero, router.LookDelta);
            Assert.IsFalse(router.IsLooking);
        }

        [Test]
        public void ASecondLeftThumbDoesNotStealTheJoystick()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(200f, 200f)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(300f, 200f)),
                  new TouchSample(4, TouchStage.Began, new Vector2(100f, 100f)));
            Assert.AreEqual(new Vector2(200f, 200f), router.Joystick.Origin);
            Assert.AreEqual(1f, router.Joystick.Value.x, 0.001f);
        }

        [Test]
        public void LiftingTheThumbStopsMovement()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(200f, 200f)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(300f, 200f)));
            Frame(new TouchSample(1, TouchStage.Ended, new Vector2(300f, 200f)));
            Assert.AreEqual(Vector2.zero, router.Joystick.Value);
        }

        [Test]
        public void AFingerThatVanishesIsReleased()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(200f, 200f)),
                  new TouchSample(2, TouchStage.Began, new Vector2(700f, 400f)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(300f, 200f)));
            Frame();
            Assert.IsFalse(router.Joystick.Active);
            Assert.IsFalse(router.IsLooking);
        }
    }
}
