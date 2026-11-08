using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastTide
{
    /// <summary>Phone and web performance settings, applied each time the island loads.</summary>
    public static class PerformanceSettings
    {
        private static readonly string[] SceneryGroups = { "Island", "Items", "Trail Stones" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            // Android otherwise runs at 30 frames per second.
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyCulling();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyCulling();
        }

        // Only on the Mobile quality level, which phones and the web build use; PC draws everything.
        private static void ApplyCulling()
        {
            if (QualitySettings.names[QualitySettings.GetQualityLevel()] != "Mobile")
            {
                return;
            }

            int culled = 0;
            foreach (string name in SceneryGroups)
            {
                GameObject group = GameObject.Find(name);
                if (group != null)
                {
                    culled += DetailCulling.Apply(group.transform);
                }
            }

            Debug.Log($"[LastTide] Distance culling added to {culled} small objects.");
        }
    }
}
