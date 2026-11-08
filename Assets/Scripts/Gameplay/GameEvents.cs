using System.Collections.Generic;
using UnityEngine;

namespace LastTide
{
    /// <summary>A short memory of what the player has done, which Marlow can refer to.</summary>
    public static class GameEvents
    {
        private const int MaxEvents = 12;
        private static readonly List<string> Events = new List<string>();

        public static string[] Recent => Events.ToArray();

        /// <summary>Raised for each new event as it happens (not when a save is loaded), e.g. to play a sound.</summary>
        public static event System.Action<string> Logged;

        public static void Log(string text)
        {
            Remember(text);
            Logged?.Invoke(text);
        }

        /// <summary>Puts back events from a saved game without announcing them again.</summary>
        public static void Restore(string[] saved)
        {
            Events.Clear();
            foreach (string text in saved)
            {
                Remember(text);
            }
        }

        private static void Remember(string text)
        {
            Events.Remove(text);
            Events.Add(text);
            if (Events.Count > MaxEvents)
            {
                Events.RemoveAt(0);
            }
        }

        public static void Clear()
        {
            Events.Clear();
        }

        public static bool Happened(string text)
        {
            return Events.Contains(text);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Events.Clear();
        }
    }
}
