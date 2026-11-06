using UnityEngine;

namespace LastTide
{
    /// <summary>Lets gameplay keys step aside while the player types.</summary>
    public static class UiState
    {
        public static bool IsTyping { get; set; }

        /// <summary>The frame in which Esc closed a window, so it doesn't also release the mouse.</summary>
        public static int EscapeHandledFrame { get; set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsTyping = false;
            EscapeHandledFrame = -1;
        }
    }
}
