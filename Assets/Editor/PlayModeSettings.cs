using UnityEditor;

namespace LastTide.EditorTools
{
    /// <summary>
    /// Makes every Play session in the editor start from a freshly loaded scene, like a real build does.
    /// Skipping the reload is faster, but leftover state from the last session made tests misleading.
    /// </summary>
    internal static class PlayModeSettings
    {
        [InitializeOnLoadMethod]
        private static void ReloadOnPlay()
        {
            if (EditorSettings.enterPlayModeOptionsEnabled)
            {
                EditorSettings.enterPlayModeOptionsEnabled = false;
                UnityEngine.Debug.Log("[LastTide] Play mode now reloads the scene and scripts each time.");
            }
        }
    }
}
