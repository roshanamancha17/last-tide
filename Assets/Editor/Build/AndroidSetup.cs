using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace LastTide.EditorTools
{
    /// <summary>The Android settings Google Play needs, applied and checked in one place.</summary>
    public static class AndroidSetup
    {
        /// <summary>Permanent once published: Play identifies the game by this name forever.</summary>
        public const string PackageName = "com.roshanamancha.lasttide";

        /// <summary>Android 16, required for new Play apps since 31 August 2026.</summary>
        public const int TargetApi = 36;

        /// <summary>Android 8.0, the oldest version supported.</summary>
        public const int MinimumApi = 26;

        public static void Apply()
        {
            PlayerSettings.productName = "Last Tide";
            PlayerSettings.companyName = "Roshan Amancha";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinimumApi;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApi;

            // Landscape only, either way up.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
        }

        /// <summary>Everything that would stop Play accepting the build. Empty when all is well.</summary>
        public static List<string> Problems(bool forPlayStore)
        {
            var problems = new List<string>();
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != PackageName)
                problems.Add($"Package name should be {PackageName}.");
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                problems.Add("Scripting backend must be IL2CPP (Play needs 64-bit code).");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                problems.Add("ARM64 must be included.");
            if ((int)PlayerSettings.Android.targetSdkVersion != TargetApi)
                problems.Add($"Target API level must be {TargetApi}.");
            if (PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown)
                problems.Add("Portrait must be turned off; the game is landscape only.");

            if (forPlayStore)
            {
                if (!PlayerSettings.Android.useCustomKeystore || !File.Exists(PlayerSettings.Android.keystoreName))
                    problems.Add("No upload keystore. Create one in Project Settings > Player > Android > Publishing Settings > Keystore Manager.");
                else if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                    problems.Add("Enter the keystore and key passwords in Publishing Settings (Unity forgets them when it closes).");
            }

            return problems;
        }
    }
}
