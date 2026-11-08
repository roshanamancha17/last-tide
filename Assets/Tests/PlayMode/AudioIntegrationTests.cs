using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace LastTide.Tests
{
    public class AudioIntegrationTests
    {
        [UnitySetUp]
        public IEnumerator LoadIsland()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Island.unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Reset()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private static AudioSource[] Sources => GameObject.Find("Game Audio").GetComponents<AudioSource>();

        [UnityTest]
        public IEnumerator TheSeaAndWindArePlayingOnALoop()
        {
            AudioSource[] loops = Sources.Where(s => s.loop).ToArray();
            Assert.AreEqual(2, loops.Length);
            Assert.IsTrue(loops.All(s => s.isPlaying && s.clip != null && s.clip.length > 4f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AGameEventPlaysItsSound()
        {
            AudioSource effects = Sources.First(s => !s.loop);
            Assert.IsFalse(effects.isPlaying);

            // GameEvents lives in the game's own code, reached by name like the other integration tests.
            Type events = Type.GetType("LastTide.GameEvents, Assembly-CSharp");
            events.GetMethod("Log").Invoke(null, new object[] { "Collected Wood" });
            yield return null;
            Assert.IsTrue(effects.isPlaying, "picking something up makes a sound");
        }
    }
}
