using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class MenuAndSettingsTests
    {
        private sealed class MemoryPrefs : IPrefs
        {
            public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
            public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
            public int Saves;
            public float GetFloat(string key, float fallback) => Floats.TryGetValue(key, out float v) ? v : fallback;
            public int GetInt(string key, int fallback) => Ints.TryGetValue(key, out int v) ? v : fallback;
            public void SetFloat(string key, float value) => Floats[key] = value;
            public void SetInt(string key, int value) => Ints[key] = value;
            public void Save() => Saves++;
        }

        [Test]
        public void ANewPlayerGetsSensibleDefaults()
        {
            GameSettings settings = GameSettings.Load(new MemoryPrefs(), 2);
            Assert.AreEqual(0.8f, settings.Volume);
            Assert.AreEqual(1f, settings.CameraSpeed);
            Assert.IsFalse(settings.InvertLook);
            Assert.AreEqual(GameSettings.PlatformQuality, settings.Quality);
        }

        [Test]
        public void SettingsSurviveARestart()
        {
            var prefs = new MemoryPrefs();
            new GameSettings { Volume = 0.3f, CameraSpeed = 2f, InvertLook = true, Quality = 1 }.Save(prefs);
            GameSettings loaded = GameSettings.Load(prefs, 2);
            Assert.AreEqual(0.3f, loaded.Volume);
            Assert.AreEqual(2f, loaded.CameraSpeed);
            Assert.IsTrue(loaded.InvertLook);
            Assert.AreEqual(1, loaded.Quality);
            Assert.AreEqual(1, prefs.Saves, "saved to disk once");
        }

        [Test]
        public void DamagedValuesFallBackSafely()
        {
            var prefs = new MemoryPrefs();
            prefs.Floats["volume"] = float.NaN;
            prefs.Floats["cameraSpeed"] = 50f;
            prefs.Ints["quality"] = 7;
            GameSettings loaded = GameSettings.Load(prefs, 2);
            Assert.AreEqual(0.8f, loaded.Volume);
            Assert.AreEqual(GameSettings.MaxCameraSpeed, loaded.CameraSpeed);
            Assert.AreEqual(GameSettings.PlatformQuality, loaded.Quality, "a quality level that doesn't exist is ignored");
        }

        [Test]
        public void CameraSpeedAndInvertChangeTheLook()
        {
            var settings = new GameSettings { CameraSpeed = 2f, InvertLook = true };
            Assert.AreEqual(new Vector2(6f, -4f), settings.ApplyToLook(new Vector2(3f, 2f)));
        }

        [Test]
        public void TheGameStartsOnTheMainMenuAndPlayStartsIt()
        {
            var menu = new MenuFlow(startOnMainMenu: true);
            Assert.AreEqual(MenuScreen.Main, menu.Screen);
            Assert.IsTrue(menu.GamePaused);
            menu.BackPressed();
            Assert.AreEqual(MenuScreen.Main, menu.Screen, "Back can't leave the main menu into a game");
            menu.Play();
            Assert.AreEqual(MenuScreen.None, menu.Screen);
            Assert.IsFalse(menu.GamePaused);
        }

        [Test]
        public void BackPausesAndResumes()
        {
            var menu = new MenuFlow(false);
            menu.BackPressed();
            Assert.AreEqual(MenuScreen.Pause, menu.Screen);
            menu.BackPressed();
            Assert.AreEqual(MenuScreen.None, menu.Screen);
        }

        [Test]
        public void SettingsReturnToWhereTheyWereOpened()
        {
            var fromPause = new MenuFlow(false);
            fromPause.BackPressed();
            fromPause.OpenSettings();
            Assert.AreEqual(MenuScreen.Settings, fromPause.Screen);
            fromPause.BackPressed();
            Assert.AreEqual(MenuScreen.Pause, fromPause.Screen);

            var fromMain = new MenuFlow(true);
            fromMain.OpenSettings();
            fromMain.BackPressed();
            Assert.AreEqual(MenuScreen.Main, fromMain.Screen);
        }

        [Test]
        public void SettingsCantOpenInTheMiddleOfPlay()
        {
            var menu = new MenuFlow(false);
            menu.OpenSettings();
            Assert.AreEqual(MenuScreen.None, menu.Screen);
        }
    }
}
