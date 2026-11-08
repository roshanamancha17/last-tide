using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastTide.EditorTools
{
    /// <summary>
    /// Lets tools outside Unity ask this editor to refresh assets or run the unit tests by creating trigger files
    /// in Temp/automation, so nothing has to click the editor's window. Temp is never committed.
    /// </summary>
    [InitializeOnLoad]
    internal static class AutomationBridge
    {
        private const string Folder = "Temp/automation";
        private const string RefreshTrigger = Folder + "/refresh.trigger";
        private const string TestTrigger = Folder + "/test.trigger";
        private const string IntegrationTrigger = Folder + "/integration.trigger";
        private const string PendingIntegrationKey = "LastTide.AutomationBridge.PendingIntegration";
        private const string PendingTestsKey = "LastTide.AutomationBridge.PendingTests";
        private static double nextCheck;

        static AutomationBridge()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextCheck)
            {
                return;
            }

            nextCheck = EditorApplication.timeSinceStartup + 2.0;
            if (File.Exists(TestTrigger))
            {
                File.Delete(TestTrigger);
                SessionState.SetBool(PendingTestsKey, true);
            }

            if (File.Exists(IntegrationTrigger))
            {
                File.Delete(IntegrationTrigger);
                SessionState.SetBool(PendingIntegrationKey, true);
            }

            if (File.Exists(RefreshTrigger))
            {
                File.Delete(RefreshTrigger);
                Debug.Log("[Automation] Refreshing assets.");
                AssetDatabase.Refresh();
                return;
            }

            // Tests wait until any compile and Play session has finished.
            if (SessionState.GetBool(PendingTestsKey, false) && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(PendingTestsKey, false);
                Debug.Log("[Automation] Running unit tests.");
                EditorApplication.ExecuteMenuItem("LastTide/Run Unit Tests");
                return;
            }

            if (SessionState.GetBool(PendingIntegrationKey, false) && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(PendingIntegrationKey, false);
                Debug.Log("[Automation] Running integration tests.");
                EditorApplication.ExecuteMenuItem("LastTide/Run Integration Tests");
            }
        }
    }
}
