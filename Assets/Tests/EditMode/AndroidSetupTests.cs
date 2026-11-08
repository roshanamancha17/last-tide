using NUnit.Framework;
using LastTide.EditorTools;
using UnityEditor;
using UnityEditor.Build;

namespace LastTide.Tests
{
    public class AndroidSetupTests
    {
        [OneTimeSetUp]
        public void ApplySettings()
        {
            AndroidSetup.Apply();
        }

        [Test]
        public void AfterApplyingNothingBlocksATestBuild()
        {
            CollectionAssert.IsEmpty(AndroidSetup.Problems(forPlayStore: false));
        }

        [Test]
        public void UsesThePermanentPackageName()
        {
            Assert.AreEqual("com.roshanamancha.lasttide", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        }

        [Test]
        public void MeetsPlayTechnicalRequirements()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.AreEqual(36, (int)PlayerSettings.Android.targetSdkVersion);
        }

        [Test]
        public void IsLandscapeOnly()
        {
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void ThePlayStoreCheckAsksForAKeystoreWhenThereIsNone()
        {
            if (PlayerSettings.Android.useCustomKeystore)
            {
                Assert.Ignore("A keystore is already set up on this machine.");
            }

            StringAssert.Contains("keystore", AndroidSetup.Problems(forPlayStore: true)[0]);
        }
    }
}
