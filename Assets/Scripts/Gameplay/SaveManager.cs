using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastTide
{
    /// <summary>
    /// Saves the game every 30 seconds, when the app goes to the background, and on quit, and resumes it on start.
    /// Phones close background apps without warning, so progress must never depend on the player quitting properly.
    /// </summary>
    // Runs its Start after the other gameplay scripts have set their starting values, so the save wins.
    [DefaultExecutionOrder(100)]
    public class SaveManager : MonoBehaviour
    {
        private const float AutosaveSeconds = 30f;

        private readonly Dictionary<string, Component> collectables = new Dictionary<string, Component>();
        private readonly Dictionary<string, ChoppableTree> trees = new Dictionary<string, ChoppableTree>();
        private readonly HashSet<string> collected = new HashSet<string>();
        private Inventory inventory;
        private PlayerController player;
        private SurvivalStats stats;
        private DayNightCycle cycle;
        private Marlow marlow;
        private CraftingSystem crafting;
        private QuestManager quest;
        private float nextSave;
        private bool finished;

        public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static bool HasSave => SaveCodec.FromJson(SaveStore.Read(SavePath)) != null;

        public static void DeleteSave()
        {
            SaveStore.Delete(SavePath);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            // Also after every reload (New Game, Main Menu, restarting after the rescue), not just at start-up.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Spawn();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Spawn();
        }

        private static void Spawn()
        {
            if (FindAnyObjectByType<PlayerController>() != null && FindAnyObjectByType<SaveManager>() == null)
            {
                new GameObject("Save Manager").AddComponent<SaveManager>();
            }
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            inventory = player.GetComponent<Inventory>();
            stats = player.GetComponent<SurvivalStats>();
            crafting = player.GetComponent<CraftingSystem>();
            cycle = FindAnyObjectByType<DayNightCycle>();
            marlow = FindAnyObjectByType<Marlow>();
            quest = FindAnyObjectByType<QuestManager>();

            foreach (Pickup pickup in FindObjectsByType<Pickup>()) Track(collectables, pickup);
            foreach (BeaconPart part in FindObjectsByType<BeaconPart>()) Track(collectables, part);
            foreach (ChoppableTree tree in FindObjectsByType<ChoppableTree>()) Track(trees, tree);

            Load();
            nextSave = Time.unscaledTime + AutosaveSeconds;
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            // After the rescue the next game starts fresh.
            if (quest != null && quest.Ended)
            {
                finished = true;
                DeleteSave();
                return;
            }

            if (Time.unscaledTime >= nextSave)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public void Save()
        {
            nextSave = Time.unscaledTime + AutosaveSeconds;
            if (finished || player == null)
            {
                return;
            }

            try
            {
                SaveStore.Write(SavePath, SaveCodec.ToJson(Capture()));
            }
            catch (Exception error)
            {
                // A full disk must not crash the game; the previous save is still intact.
                Debug.LogWarning($"[LastTide] Couldn't save: {error.Message}");
            }
        }

        private SaveData Capture()
        {
            var names = new List<string>();
            var counts = new List<int>();
            foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
            {
                int count = inventory.Count(item);
                if (count > 0)
                {
                    names.Add(item.ToString());
                    counts.Add(count);
                }
            }

            foreach (KeyValuePair<string, Component> pair in collectables)
            {
                // Unity reports destroyed objects as null: these were picked up.
                if (pair.Value == null)
                {
                    collected.Add(pair.Key);
                }
            }

            var treeKeys = new List<string>();
            var chops = new List<int>();
            foreach (KeyValuePair<string, ChoppableTree> pair in trees)
            {
                if (pair.Value != null)
                {
                    treeKeys.Add(pair.Key);
                    chops.Add(pair.Value.ChopsLeft);
                }
            }

            return new SaveData
            {
                itemNames = names.ToArray(),
                itemCounts = counts.ToArray(),
                playerPosition = player.transform.position,
                playerYaw = player.transform.eulerAngles.y,
                hunger = stats != null ? stats.Hunger : 60f,
                energy = stats != null ? stats.Energy : 100f,
                day = cycle != null ? cycle.Day : 1,
                hour = cycle != null ? cycle.Hour : 8f,
                marlowAwake = marlow != null && marlow.IsAwake,
                collected = new List<string>(collected).ToArray(),
                treeKeys = treeKeys.ToArray(),
                treeChopsLeft = chops.ToArray(),
                campfireBuilt = crafting != null && crafting.CampfireBuilt,
                campfirePosition = crafting != null ? crafting.CampfirePosition : Vector3.zero,
                events = GameEvents.Recent,
            };
        }

        private void Load()
        {
            SaveData data = SaveCodec.FromJson(SaveStore.Read(SavePath));
            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.itemNames.Length; i++)
            {
                // Items renamed or removed in a later version are skipped rather than breaking the load.
                if (Enum.TryParse(data.itemNames[i], out ItemType item) && data.itemCounts[i] > 0)
                {
                    inventory.Add(item, data.itemCounts[i]);
                }
            }

            foreach (string key in data.collected)
            {
                collected.Add(key);
                if (collectables.TryGetValue(key, out Component thing) && thing != null)
                {
                    Destroy(thing.gameObject);
                }
            }

            for (int i = 0; i < data.treeKeys.Length; i++)
            {
                if (trees.TryGetValue(data.treeKeys[i], out ChoppableTree tree) && tree != null)
                {
                    tree.ChopsLeft = data.treeChopsLeft[i];
                }
            }

            if (stats != null) stats.Restore(data.hunger, data.energy);
            if (cycle != null) cycle.SetTime(data.day, data.hour);
            if (marlow != null && data.marlowAwake) marlow.WakeFromSave();
            if (crafting != null && data.campfireBuilt) crafting.RestoreCampfire(data.campfirePosition);

            GameEvents.Restore(data.events);

            player.Teleport(data.playerPosition);
            player.transform.rotation = Quaternion.Euler(0f, data.playerYaw, 0f);
            Debug.Log($"[LastTide] Resumed saved game: day {data.day}, {data.itemNames.Length} item types, " +
                      $"{data.collected.Length} things collected, Marlow {(data.marlowAwake ? "awake" : "buried")}.");
        }

        private static void Track<T>(Dictionary<string, T> table, T thing) where T : Component
        {
            string key = WorldKey.Of(thing.name, thing.transform.position);
            // Two identical things in one spot would share a key; keep them apart.
            while (table.ContainsKey(key))
            {
                key += "+";
            }

            table[key] = thing;
        }
    }
}
