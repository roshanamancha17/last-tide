using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastTide.EditorTools
{
    /// <summary>Makes the browser build for itch.io.</summary>
    public static class LastTideBuild
    {
        private const string ScenePath = "Assets/Scenes/Island.unity";
        private const string OutputFolder = "Builds/Web";

        [MenuItem("LastTide/Build for Web (itch.io)")]
        public static void BuildWeb()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[LastTide] Web build support isn't installed. In Unity Hub, open Installs, click the gear on " +
                               "6000.6.0f1, choose Add modules, and tick Web Build Support.");
                return;
            }

            PlayerSettings.productName = "Last Tide";
            PlayerSettings.companyName = "Roshan Amancha";
            // itch.io serves files without compression headers, so Unity unpacks them itself.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[LastTide] Web build {summary.result} with {summary.totalErrors} errors.");
                return;
            }

            Debug.Log($"[LastTide] Web build done in {summary.totalTime.TotalMinutes:F1} min: " +
                      $"{summary.totalSize / (1024f * 1024f):F1} MB at {Path.GetFullPath(OutputFolder)}");
            EditorUtility.RevealInFinder(Path.Combine(OutputFolder, "index.html"));
        }
    }
}
