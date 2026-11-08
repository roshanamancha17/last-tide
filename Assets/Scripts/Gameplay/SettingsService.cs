using UnityEngine;

namespace LastTide
{
    /// <summary>Loads the player's settings at start-up, applies them, and saves changes.</summary>
    public static class SettingsService
    {
        private sealed class PlayerPrefsStore : IPrefs
        {
            public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat("settings." + key, fallback);
            public int GetInt(string key, int fallback) => PlayerPrefs.GetInt("settings." + key, fallback);
            public void SetFloat(string key, float value) => PlayerPrefs.SetFloat("settings." + key, value);
            public void SetInt(string key, int value) => PlayerPrefs.SetInt("settings." + key, value);
            public void Save() => PlayerPrefs.Save();
        }

        private static readonly PlayerPrefsStore Store = new PlayerPrefsStore();
        private static int platformQuality = -1;

        public static GameSettings Current { get; private set; } = new GameSettings();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            platformQuality = QualitySettings.GetQualityLevel();
            Current = GameSettings.Load(Store, QualitySettings.names.Length);
            Apply();
        }

        public static void SaveAndApply()
        {
            Current.Save(Store);
            Apply();
        }

        public static void Apply()
        {
            AudioListener.volume = Current.Volume;
            int quality = Current.Quality == GameSettings.PlatformQuality ? platformQuality : Current.Quality;
            if (quality >= 0 && quality != QualitySettings.GetQualityLevel())
            {
                QualitySettings.SetQualityLevel(quality, true);
            }
        }
    }
}
