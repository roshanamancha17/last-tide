using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class TouchControlsTests
    {
        private GameObject host;
        private TouchControls controls;

        [SetUp]
        public void Create()
        {
            host = new GameObject("Touch Controls Test");
            controls = host.AddComponent<TouchControls>();
            controls.Build(new Vector2(1920, 1080), new Rect(0, 0, 1920, 1080), 160f);
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(host);
        }

        private Vector2 Centre(TouchAction action) => controls.Layout.Buttons[action].center;

        private void Frame(params TouchSample[] touches) => controls.Process(touches);

        [Test]
        public void TappingJumpJumpsExactlyOnce()
        {
            Frame(new TouchSample(1, TouchStage.Began, Centre(TouchAction.Jump)));
            Assert.IsTrue(controls.Collect().Jump);
            Frame(new TouchSample(1, TouchStage.Held, Centre(TouchAction.Jump)));
            Assert.IsFalse(controls.Collect().Jump);
        }

        [Test]
        public void EachButtonSendsItsOwnAction()
        {
            Frame(new TouchSample(1, TouchStage.Began, Centre(TouchAction.Interact)),
                  new TouchSample(2, TouchStage.Began, Centre(TouchAction.Eat)));
            InputFrame frame = controls.Collect();
            Assert.IsTrue(frame.Interact && frame.Eat);
            Assert.IsFalse(frame.Jump || frame.Craft || frame.Talk);
        }

        [Test]
        public void SprintTogglesAndOnlyCountsWhileMoving()
        {
            Frame(new TouchSample(1, TouchStage.Began, Centre(TouchAction.Sprint)));
            Assert.IsTrue(controls.SprintOn);
            Assert.IsFalse(controls.Collect().Sprint, "standing still is not sprinting");

            float radius = controls.Layout.JoystickRadius;
            Frame(new TouchSample(2, TouchStage.Began, new Vector2(300, 300)));
            Frame(new TouchSample(2, TouchStage.Moved, new Vector2(300 + radius, 300)));
            InputFrame moving = controls.Collect();
            Assert.IsTrue(moving.Sprint);
            Assert.AreEqual(1f, moving.Move.x, 0.001f);

            Frame(new TouchSample(2, TouchStage.Held, new Vector2(300 + radius, 300)),
                  new TouchSample(3, TouchStage.Began, Centre(TouchAction.Sprint)));
            Assert.IsFalse(controls.SprintOn);
        }

        [Test]
        public void AFingerStartingOnAButtonDoesNotTurnTheCamera()
        {
            Vector2 craft = Centre(TouchAction.Craft);
            Frame(new TouchSample(1, TouchStage.Began, craft));
            Assert.IsTrue(controls.Collect().Craft);
            Frame(new TouchSample(1, TouchStage.Moved, craft + new Vector2(-200, 0), new Vector2(-200, 0)));
            InputFrame frame = controls.Collect();
            Assert.AreEqual(Vector2.zero, frame.Look);
            Assert.IsFalse(frame.Craft, "a held button presses only once");
        }

        [Test]
        public void SwipingTheRightSideTurnsTheCameraByScreenWidth()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(1200, 600)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(1392, 600), new Vector2(192, 0)));
            Assert.AreEqual(150f, controls.Collect().Look.x, 0.01f);
        }

        [Test]
        public void ReleasingAllFingersStopsMovement()
        {
            Frame(new TouchSample(1, TouchStage.Began, new Vector2(300, 300)));
            Frame(new TouchSample(1, TouchStage.Moved, new Vector2(400, 300)));
            controls.ReleaseAll();
            Assert.AreEqual(Vector2.zero, controls.Collect().Move);
        }

        [Test]
        public void AskingForInputBeforeTheScreenIsKnownGivesNothingInsteadOfAnError()
        {
            GameObject fresh = new GameObject("Unbuilt Touch Controls");
            try
            {
                TouchControls unbuilt = fresh.AddComponent<TouchControls>();
                InputFrame frame = default;
                Assert.DoesNotThrow(() => frame = unbuilt.Collect());
                Assert.AreEqual(Vector2.zero, frame.Move);
                Assert.IsFalse(frame.Jump);
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }
        }

        [Test]
        public void BuildsSixButtonsAndAHiddenJoystick()
        {
            Transform ui = host.transform.Find("Touch Controls UI");
            Assert.IsNotNull(ui);
            foreach (TouchAction action in System.Enum.GetValues(typeof(TouchAction)))
            {
                Assert.IsNotNull(ui.Find($"{action} Button"), $"{action} button is missing");
            }

            Assert.IsFalse(ui.Find("Joystick Base").gameObject.activeSelf);
        }
    }
}
