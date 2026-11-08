using UnityEngine;

namespace LastTide
{
    /// <summary>Somewhere to keep small values on the device. PlayerPrefs in the game, a dictionary in tests.</summary>
    public interface IPrefs
    {
        float GetFloat(string key, float fallback);
        int GetInt(string key, int fallback);
        void SetFloat(string key, float value);
        void SetInt(string key, int value);
        void Save();
    }

    /// <summary>The player's settings. Out-of-range or damaged values fall back to safe defaults.</summary>
    public class GameSettings
    {
        public const float MinCameraSpeed = 0.3f;
        public const float MaxCameraSpeed = 3f;

        /// <summary>Use the platform's own graphics level (Mobile on phones and the web, PC on computers).</summary>
        public const int PlatformQuality = -1;

        public float Volume = 0.8f;
        public float CameraSpeed = 1f;
        public bool InvertLook;
        public int Quality = PlatformQuality;

        public static GameSettings Load(IPrefs prefs, int qualityLevels)
        {
            var settings = new GameSettings();
            settings.Volume = Clean(prefs.GetFloat("volume", settings.Volume), 0f, 1f, settings.Volume);
            settings.CameraSpeed = Clean(prefs.GetFloat("cameraSpeed", settings.CameraSpeed), MinCameraSpeed, MaxCameraSpeed, settings.CameraSpeed);
            settings.InvertLook = prefs.GetInt("invertLook", 0) == 1;
            int quality = prefs.GetInt("quality", PlatformQuality);
            settings.Quality = quality >= 0 && quality < qualityLevels ? quality : PlatformQuality;
            return settings;
        }

        public void Save(IPrefs prefs)
        {
            prefs.SetFloat("volume", Volume);
            prefs.SetFloat("cameraSpeed", CameraSpeed);
            prefs.SetInt("invertLook", InvertLook ? 1 : 0);
            prefs.SetInt("quality", Quality);
            prefs.Save();
        }

        /// <summary>Applies the camera settings to one frame of mouse or finger movement.</summary>
        public Vector2 ApplyToLook(Vector2 look)
        {
            return new Vector2(look.x * CameraSpeed, look.y * CameraSpeed * (InvertLook ? -1f : 1f));
        }

        private static float Clean(float value, float min, float max, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
        }
    }

    public enum MenuScreen { None, Main, Pause, Settings }

    /// <summary>Which menu is showing, and where Back and Pause lead. The game is paused whenever a menu shows.</summary>
    public sealed class MenuFlow
    {
        private MenuScreen settingsReturn = MenuScreen.Pause;

        public MenuFlow(bool startOnMainMenu)
        {
            Screen = startOnMainMenu ? MenuScreen.Main : MenuScreen.None;
        }

        public MenuScreen Screen { get; private set; }
        public bool GamePaused => Screen != MenuScreen.None;

        /// <summary>Esc on a keyboard, the Android back button, or the Pause button.</summary>
        public void BackPressed()
        {
            switch (Screen)
            {
                case MenuScreen.None: Screen = MenuScreen.Pause; break;
                case MenuScreen.Pause: Screen = MenuScreen.None; break;
                case MenuScreen.Settings: Screen = settingsReturn; break;
                case MenuScreen.Main: break; // the main menu has nothing behind it
            }
        }

        public void Play()
        {
            if (Screen == MenuScreen.Main || Screen == MenuScreen.Pause)
            {
                Screen = MenuScreen.None;
            }
        }

        public void OpenSettings()
        {
            if (Screen == MenuScreen.Main || Screen == MenuScreen.Pause)
            {
                settingsReturn = Screen;
                Screen = MenuScreen.Settings;
            }
        }
    }
}
