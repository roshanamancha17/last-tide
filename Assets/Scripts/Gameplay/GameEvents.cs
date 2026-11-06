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

        public static void Log(string text)
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
