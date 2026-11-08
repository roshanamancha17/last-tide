using System;
using System.Linq;
using NUnit.Framework;

namespace LastTide.Tests
{
    public class SoundTests
    {
        private static float Peak(float[] samples) => samples.Max(s => Math.Abs(s));

        private static float Rms(float[] samples) => (float)Math.Sqrt(samples.Average(s => (double)s * s));

        public static GameSound[] AllSounds => ((GameSound[])Enum.GetValues(typeof(GameSound))).Where(s => s != GameSound.None).ToArray();

        [TestCaseSource(nameof(AllSounds))]
        public void EverySoundIsAudibleShortAndNeverClips(GameSound sound)
        {
            float[] samples = SoundSynth.Make(sound);
            Assert.Greater(samples.Length, 0);
            Assert.LessOrEqual(samples.Length, SoundSynth.SampleRate * 2, "effects stay short");
            Assert.LessOrEqual(Peak(samples), 1f, "no clipping");
            Assert.Greater(Rms(samples), 0.01f, "not silent");
            Assert.IsTrue(samples.All(float.IsFinite));
        }

        [Test]
        public void SoundsAreTheSameEveryTime()
        {
            CollectionAssert.AreEqual(SoundSynth.Make(GameSound.Chop), SoundSynth.Make(GameSound.Chop));
        }

        [Test]
        public void FootstepsVaryButStayQuiet()
        {
            CollectionAssert.AreNotEqual(SoundSynth.Footstep(1), SoundSynth.Footstep(2));
            Assert.LessOrEqual(Peak(SoundSynth.Footstep(3)), 0.5f + 1e-4f);
        }

        [Test]
        public void TheOceanAndWindLoopWithoutAClick()
        {
            foreach (float[] loop in new[] { SoundSynth.Ocean(8f), SoundSynth.Wind(6f) })
            {
                float largestStep = 0f;
                for (int i = 1; i < loop.Length; i++)
                {
                    largestStep = Math.Max(largestStep, Math.Abs(loop[i] - loop[i - 1]));
                }

                float seam = Math.Abs(loop[0] - loop[loop.Length - 1]);
                Assert.LessOrEqual(seam, largestStep, "the jump from the end back to the start is no bigger than a normal step");
                Assert.Greater(Rms(loop), 0.02f);
            }
        }

        [TestCase("Collected Wood", GameSound.Pickup)]
        [TestCase("Chopped a palm", GameSound.Chop)]
        [TestCase("Made a Knife", GameSound.Craft)]
        [TestCase("Built a campfire", GameSound.Craft)]
        [TestCase("Ate a coconut", GameSound.Eat)]
        [TestCase("Found the beacon lens", GameSound.Discovery)]
        [TestCase("Dug Marlow out of the sand", GameSound.Discovery)]
        [TestCase("Lit the signal beacon", GameSound.Fanfare)]
        [TestCase("Slept through a night by the fire", GameSound.Rest)]
        [TestCase("Collapsed from hunger", GameSound.Hurt)]
        [TestCase("Something new", GameSound.None)]
        [TestCase(null, GameSound.None)]
        public void EachGameEventMakesTheRightSound(string gameEvent, GameSound expected)
        {
            Assert.AreEqual(expected, SoundEvents.For(gameEvent));
        }
    }
}
