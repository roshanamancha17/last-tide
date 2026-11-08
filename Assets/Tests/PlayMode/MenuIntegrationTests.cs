using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace LastTide.Tests
{
    /// <summary>
    /// Integration tests: load the real island and drive the real menus, as a player would.
    /// Gameplay scripts live in Unity's default assembly, so they are reached by name, like a player sees them.
    /// </summary>
    public class MenuIntegrationTests
    {
        private const string ScenePath = "Assets/Scenes/Island.unity";

        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        private static IEnumerator LoadIsland()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Let the start-up scripts run.
            yield return null;
            yield return null;
        }

        private static Component Menus => GameObject.Find("Game Menus").GetComponent("GameMenus");

        private static bool PanelShowing(string name)
        {
            Transform panel = GameObject.Find("Game Menus").transform.Find("Menus/Safe Area/" + name);
            return panel != null && panel.gameObject.activeSelf;
        }

        private static IEnumerator Call(string method)
        {
            Menus.SendMessage(method);
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator StartFresh()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            yield return LoadIsland();
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            if (File.Exists(SavePath)) File.Delete(SavePath);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheGameOpensOnTheMainMenuWithTheIslandPaused()
        {
            Assert.IsTrue(PanelShowing("Main Menu"));
            Assert.AreEqual(0f, Time.timeScale, "nothing moves behind the main menu");
            Assert.IsFalse(GameObject.Find("Game Menus").transform.Find("Menus/Safe Area/Main Menu/Continue Button").gameObject.activeSelf,
                "with no save there is nothing to continue");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayStartsTheGame()
        {
            yield return Call("Continue");
            Assert.IsFalse(PanelShowing("Main Menu"));
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator BackPausesAndResumesAndSettingsReturnToThePauseMenu()
        {
            yield return Call("Continue");
            yield return Call("Back");
            Assert.IsTrue(PanelShowing("Pause Menu"));
            Assert.AreEqual(0f, Time.timeScale);

            yield return Call("OpenSettings");
            Assert.IsTrue(PanelShowing("Settings"));
            yield return Call("Back");
            Assert.IsTrue(PanelShowing("Pause Menu"));

            yield return Call("Back");
            Assert.IsFalse(PanelShowing("Pause Menu"));
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator AfterPlayingTheMainMenuOffersToContinue()
        {
            yield return Call("Continue");
            yield return new WaitForSeconds(0.2f);
            yield return Call("ToMainMenu");
            // The scene reloads with the main menu, and the game was saved on the way out.
            yield return null;
            yield return null;
            Assert.IsTrue(File.Exists(SavePath), "leaving to the menu saves the game");
            Assert.IsTrue(PanelShowing("Main Menu"));
            Assert.IsTrue(GameObject.Find("Game Menus").transform.Find("Menus/Safe Area/Main Menu/Continue Button").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator NewGameDeletesTheSaveAndStartsPlayingStraightAway()
        {
            File.WriteAllText(SavePath, "{\"version\":1,\"day\":5}");
            yield return Call("NewGame");
            yield return null;
            yield return null;
            Assert.IsFalse(File.Exists(SavePath) && File.ReadAllText(SavePath).Contains("\"day\":5"), "the old save is gone");
            Assert.IsFalse(PanelShowing("Main Menu"), "a new game starts without the menu");
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
