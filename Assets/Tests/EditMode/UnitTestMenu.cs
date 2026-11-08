using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LastTide.Tests
{
    /// <summary>Runs the edit-mode unit tests from the LastTide menu and writes each result to the console.</summary>
    public static class UnitTestMenu
    {
        [MenuItem("LastTide/Run Unit Tests")]
        public static void Run()
        {
            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Reporter());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
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
