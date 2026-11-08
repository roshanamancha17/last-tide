using System;
using UnityEngine;

namespace LastTide
{
    /// <summary>Everything needed to resume a game. Plain data, stored as JSON.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string[] itemNames = new string[0];
        public int[] itemCounts = new int[0];
        public Vector3 playerPosition;
        public float playerYaw;
        public float hunger = 60f;
        public float energy = 100f;
        public int day = 1;
        public float hour = 8f;
        public bool marlowAwake;
        public string[] collected = new string[0];
        public string[] treeKeys = new string[0];
        public int[] treeChopsLeft = new int[0];
        public bool campfireBuilt;
        public Vector3 campfirePosition;
        public string[] events = new string[0];
    }

    /// <summary>Turns save data into JSON and back, refusing or repairing anything damaged.</summary>
    public static class SaveCodec
    {
        public static string ToJson(SaveData data)
        {
            return JsonUtility.ToJson(data);
        }

        /// <summary>The save, or null when the text isn't a save this version of the game can use.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (data == null || data.version < 1 || data.version > SaveData.CurrentVersion || !IsFinite(data.playerPosition))
            {
                return null;
            }

            Repair(data);
            return data;
        }

        private static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        // Keeps a slightly damaged save playable instead of throwing the player's progress away.
        private static void Repair(SaveData data)
        {
            data.itemNames ??= new string[0];
            data.itemCounts ??= new int[0];
            if (data.itemCounts.Length != data.itemNames.Length)
            {
                int length = Mathf.Min(data.itemNames.Length, data.itemCounts.Length);
                Array.Resize(ref data.itemNames, length);
                Array.Resize(ref data.itemCounts, length);
            }

            for (int i = 0; i < data.itemCounts.Length; i++)
            {
                data.itemCounts[i] = Mathf.Max(0, data.itemCounts[i]);
            }

            data.hunger = float.IsFinite(data.hunger) ? Mathf.Clamp(data.hunger, 0f, 100f) : 60f;
            data.energy = float.IsFinite(data.energy) ? Mathf.Clamp(data.energy, 0f, 100f) : 100f;
            data.hour = float.IsFinite(data.hour) ? Mathf.Repeat(data.hour, 24f) : 8f;
            data.day = Mathf.Max(1, data.day);
            data.playerYaw = float.IsFinite(data.playerYaw) ? data.playerYaw : 0f;
            data.collected ??= new string[0];
            data.events ??= new string[0];
            data.treeKeys ??= new string[0];
            data.treeChopsLeft ??= new int[0];
            if (data.treeKeys.Length != data.treeChopsLeft.Length)
            {
                data.treeKeys = new string[0];
                data.treeChopsLeft = new int[0];
            }

            if (!IsFinite(data.campfirePosition))
            {
                data.campfireBuilt = false;
            }
        }
    }

    /// <summary>Stable names for things placed in the world, so the same stone is recognised after loading.</summary>
    public static class WorldKey
    {
        public static string Of(string name, Vector3 position)
        {
            // Rounded to 10 cm so tiny float differences between runs don't matter.
            return $"{name}@{Mathf.RoundToInt(position.x * 10f)},{Mathf.RoundToInt(position.y * 10f)},{Mathf.RoundToInt(position.z * 10f)}";
        }
    }
}
