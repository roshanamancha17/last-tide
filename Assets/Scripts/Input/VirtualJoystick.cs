using UnityEngine;

namespace LastTide
{
    /// <summary>A joystick that appears where the thumb lands. Pure maths, so it can be tested without a screen.</summary>
    public sealed class VirtualJoystick
    {
        private readonly float radius;
        private readonly float deadZone;

        public VirtualJoystick(float radius, float deadZone = 0.12f)
        {
            this.radius = radius;
            this.deadZone = deadZone;
        }

        public bool Active { get; private set; }
        public Vector2 Origin { get; private set; }

        /// <summary>Where the knob is drawn, never further than the radius from the origin.</summary>
        public Vector2 Knob { get; private set; }

        /// <summary>Movement from 0 to 1 in length; 0 inside the dead zone.</summary>
        public Vector2 Value { get; private set; }

        public void Begin(Vector2 position)
        {
            Active = true;
            Origin = position;
            Knob = position;
            Value = Vector2.zero;
        }

        public void Drag(Vector2 position)
        {
            if (!Active)
            {
                return;
            }

            Vector2 offset = Vector2.ClampMagnitude(position - Origin, radius);
            Knob = Origin + offset;
            float amount = offset.magnitude / radius;
            Value = amount <= deadZone
                ? Vector2.zero
                : offset.normalized * Mathf.InverseLerp(deadZone, 1f, amount);
        }

        public void End()
        {
            Active = false;
            Knob = Origin;
            Value = Vector2.zero;
        }
    }
}
