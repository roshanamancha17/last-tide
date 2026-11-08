using UnityEngine;

namespace LastTide
{
    /// <summary>Everything the player asked for in one frame, whatever device it came from.</summary>
    public struct InputFrame
    {
        /// <summary>Movement, x = right, y = forward, length at most 1.</summary>
        public Vector2 Move;

        /// <summary>Camera turn in mouse-pixel units.</summary>
        public Vector2 Look;

        public bool Sprint;
        public bool Jump;
        public bool Interact;
        public bool Eat;
        public bool Craft;
        public bool Talk;

        /// <summary>Recipe chosen this frame: 1 to 3, or 0 for none.</summary>
        public int Recipe;

        /// <summary>The player asked to report Marlow's last AI reply.</summary>
        public bool Report;

        /// <summary>Esc, the Android back button, or the on-screen Pause button.</summary>
        public bool Pause;

        /// <summary>Merges two devices, so keyboard and touch can be used at the same time.</summary>
        public static InputFrame Combine(InputFrame a, InputFrame b)
        {
            return new InputFrame
            {
                Move = Vector2.ClampMagnitude(a.Move + b.Move, 1f),
                Look = a.Look + b.Look,
                Sprint = a.Sprint || b.Sprint,
                Jump = a.Jump || b.Jump,
                Interact = a.Interact || b.Interact,
                Eat = a.Eat || b.Eat,
                Craft = a.Craft || b.Craft,
                Talk = a.Talk || b.Talk,
                Recipe = a.Recipe != 0 ? a.Recipe : b.Recipe,
                Report = a.Report || b.Report,
                Pause = a.Pause || b.Pause,
            };
        }
    }

    /// <summary>The input for the current frame. Gameplay scripts read this instead of a specific device.</summary>
    public static class GameInput
    {
        public static InputFrame Current { get; private set; }

        public static void Set(InputFrame frame)
        {
            Current = frame;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = default;
        }
    }
}
