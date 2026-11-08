using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LastTide.Tests
{
    /// <summary>Runs the edit-mode unit tests from the LastTide menu and writes each result to the console.</summary>
    [InitializeOnLoad]
    public static class UnitTestMenu
    {
        // Attached every time scripts load, because PlayMode tests reload scripts mid-run and would drop it.
        static UnitTestMenu()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Reporter());
        }

        [MenuItem("LastTide/Run Unit Tests")]
        public static void Run()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        [MenuItem("LastTide/Run Integration Tests")]
        public static void RunIntegration()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
        }

        private sealed class Reporter : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) => Debug.Log("[UnitTest] Run started.");

            public void RunFinished(ITestResultAdaptor result) =>
                Debug.Log($"[UnitTest] Finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                {
                    return;
                }

                string line = $"[UnitTest] {result.TestStatus.ToString().ToUpper()} {result.Test.FullName}";
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogWarning($"{line}\n{result.Message}");
                }
                else
                {
                    Debug.Log(line);
                }
            }
        }
    }
}
