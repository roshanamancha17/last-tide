using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTide
{
    public enum TouchStage { Began, Moved, Held, Ended }

    public struct TouchSample
    {
        public int Id;
        public TouchStage Stage;
        public Vector2 Position;
        public Vector2 Delta;

        public TouchSample(int id, TouchStage stage, Vector2 position, Vector2 delta = default)
        {
            Id = id;
            Stage = stage;
            Position = position;
            Delta = delta;
        }
    }

    /// <summary>
    /// Decides what each finger does: the left half of the screen moves, the right half turns the camera,
    /// and fingers that start on a button are left to the button.
    /// </summary>
    public sealed class TouchRouter
    {
        private const int None = -1;
        private readonly HashSet<int> seen = new HashSet<int>();
        private int moveFinger = None;
        private int lookFinger = None;

        public TouchRouter(VirtualJoystick joystick)
        {
            Joystick = joystick;
        }

        public VirtualJoystick Joystick { get; }

        /// <summary>Camera turn gathered from the look finger during the last Process call.</summary>
        public Vector2 LookDelta { get; private set; }

        public bool IsLooking => lookFinger != None;

        public void Process(IReadOnlyList<TouchSample> touches, float screenWidth, Func<Vector2, bool> isOverButton)
        {
            LookDelta = Vector2.zero;
            seen.Clear();
            foreach (TouchSample touch in touches)
            {
                seen.Add(touch.Id);
                switch (touch.Stage)
                {
                    case TouchStage.Began:
                        Begin(touch, screenWidth, isOverButton);
                        break;
                    case TouchStage.Moved:
                    case TouchStage.Held:
                        if (touch.Id == moveFinger) Joystick.Drag(touch.Position);
                        if (touch.Id == lookFinger) LookDelta += touch.Delta;
                        break;
                    case TouchStage.Ended:
                        Release(touch.Id);
                        break;
                }
            }

            // A finger can vanish without an end event, for example when the app is paused.
            if (moveFinger != None && !seen.Contains(moveFinger)) Release(moveFinger);
            if (lookFinger != None && !seen.Contains(lookFinger)) Release(lookFinger);
        }

        public void Reset()
        {
            Release(moveFinger);
            Release(lookFinger);
            LookDelta = Vector2.zero;
        }

        private void Begin(TouchSample touch, float screenWidth, Func<Vector2, bool> isOverButton)
        {
            if (isOverButton != null && isOverButton(touch.Position))
            {
                return;
            }

            bool leftHalf = touch.Position.x < screenWidth * 0.5f;
            if (leftHalf && moveFinger == None)
            {
                moveFinger = touch.Id;
                Joystick.Begin(touch.Position);
            }
            else if (!leftHalf && lookFinger == None)
            {
                lookFinger = touch.Id;
            }
        }

        private void Release(int id)
        {
            if (id == None)
            {
                return;
            }

            if (id == moveFinger)
            {
                moveFinger = None;
                Joystick.End();
            }

            if (id == lookFinger)
            {
                lookFinger = None;
            }
        }
    }
}
