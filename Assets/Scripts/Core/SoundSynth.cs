using System;

namespace LastTide
{
    public enum GameSound { None, Footstep, Pickup, Chop, Craft, Eat, Discovery, Fanfare, Rest, Hurt, MarlowBlip, UiClick }

    /// <summary>Which sound each game event makes.</summary>
    public static class SoundEvents
    {
        public static GameSound For(string gameEvent)
        {
            if (string.IsNullOrEmpty(gameEvent)) return GameSound.None;
            if (gameEvent.StartsWith("Collected ")) return GameSound.Pickup;
            if (gameEvent == "Chopped a palm") return GameSound.Chop;
            if (gameEvent.StartsWith("Made a ") || gameEvent == "Built a campfire") return GameSound.Craft;
            if (gameEvent == "Ate a coconut") return GameSound.Eat;
            if (gameEvent.StartsWith("Found the beacon ") || gameEvent == "Dug Marlow out of the sand") return GameSound.Discovery;
            if (gameEvent == "Lit the signal beacon") return GameSound.Fanfare;
            if (gameEvent.StartsWith("Slept") || gameEvent.StartsWith("Rested")) return GameSound.Rest;
            if (gameEvent.StartsWith("Collapsed")) return GameSound.Hurt;
            return GameSound.None;
        }
    }

    /// <summary>
    /// Makes every sound in the game from maths, so there are no audio files to license or download.
    /// Pure functions returning samples between -1 and 1.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 22050;

        public static float[] Make(GameSound sound)
        {
            switch (sound)
            {
                case GameSound.Footstep: return Footstep(1);
                case GameSound.Pickup: return Chime(new[] { 660f, 990f }, 0.09f, 0.35f);
                case GameSound.Chop: return Chop();
                case GameSound.Craft: return Chime(new[] { 523f, 659f, 784f }, 0.08f, 0.45f);
                case GameSound.Eat: return Crunch();
                case GameSound.Discovery: return Chime(new[] { 587f, 740f, 880f, 1175f }, 0.11f, 0.6f);
                case GameSound.Fanfare: return Chime(new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.16f, 1.2f);
                case GameSound.Rest: return Chime(new[] { 440f, 349f, 294f }, 0.22f, 0.9f);
                case GameSound.Hurt: return Sweep(320f, 90f, 0.5f);
                case GameSound.MarlowBlip: return Blips(2);
                case GameSound.UiClick: return Chime(new[] { 1200f }, 0.01f, 0.06f);
                default: return new float[0];
            }
        }

        /// <summary>A soft sandy footstep. Different seeds sound slightly different, so steps don't repeat exactly.</summary>
        public static float[] Footstep(int seed)
        {
            var random = new Random(seed);
            float[] samples = new float[(int)(SampleRate * 0.12f)];
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                low += ((float)random.NextDouble() * 2f - 1f - low) * 0.18f;
                samples[i] = low * (float)Math.Exp(-t * 7f) * 0.9f;
            }

            return Normalise(samples, 0.5f);
        }

        /// <summary>A seamless loop of waves washing on the shore.</summary>
        public static float[] Ocean(float seconds)
        {
            var random = new Random(11);
            int length = (int)(SampleRate * seconds);
            int overlap = SampleRate / 2;
            float[] samples = new float[length + overlap];
            float low = 0f, lower = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float white = (float)random.NextDouble() * 2f - 1f;
                low += (white - low) * 0.06f;
                lower += (low - lower) * 0.05f;
                // Swells whose periods divide the loop exactly, so they line up at the seam.
                float phase = (float)i / length;
                float swell = 0.55f + 0.3f * (float)Math.Sin(phase * Math.PI * 2 * 3) + 0.15f * (float)Math.Sin(phase * Math.PI * 2 * 5 + 1.3);
                samples[i] = lower * swell;
            }

            return Normalise(Loop(samples, length, overlap), 0.6f);
        }

        /// <summary>A seamless loop of gentle wind.</summary>
        public static float[] Wind(float seconds)
        {
            var random = new Random(23);
            int length = (int)(SampleRate * seconds);
            int overlap = SampleRate / 2;
            float[] samples = new float[length + overlap];
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                low += ((float)random.NextDouble() * 2f - 1f - low) * 0.02f;
                float phase = (float)i / length;
                samples[i] = low * (0.6f + 0.4f * (float)Math.Sin(phase * Math.PI * 2 * 2));
            }

            return Normalise(Loop(samples, length, overlap), 0.35f);
        }

        private static float[] Chime(float[] notes, float gap, float decay)
        {
            int length = (int)(SampleRate * (gap * (notes.Length - 1) + decay + 0.05f));
            float[] samples = new float[length];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(SampleRate * gap * n);
                for (int i = start; i < length; i++)
                {
                    float t = (float)(i - start) / SampleRate;
                    float envelope = (float)Math.Exp(-t / (decay * 0.35f)) * Math.Min(1f, t * 400f);
                    double phase = Math.PI * 2 * notes[n] * t;
                    samples[i] += (float)(Math.Sin(phase) + 0.3 * Math.Sin(phase * 2)) * envelope;
                }
            }

            return Normalise(samples, 0.55f);
        }

        private static float[] Chop()
        {
            var random = new Random(5);
            float[] samples = new float[(int)(SampleRate * 0.25f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                float thud = (float)Math.Sin(Math.PI * 2 * (140f - 300f * t) * t) * (float)Math.Exp(-t * 18f);
                float crack = ((float)random.NextDouble() * 2f - 1f) * (float)Math.Exp(-t * 60f);
                samples[i] = thud + crack * 0.6f;
            }

            return Normalise(samples, 0.7f);
        }

        private static float[] Crunch()
        {
            var random = new Random(9);
            float[] samples = new float[(int)(SampleRate * 0.45f)];
            for (int bite = 0; bite < 3; bite++)
            {
                int start = (int)(SampleRate * 0.14f * bite);
                for (int i = start; i < Math.Min(samples.Length, start + SampleRate / 10); i++)
                {
                    float t = (float)(i - start) / SampleRate;
                    samples[i] += ((float)random.NextDouble() * 2f - 1f) * (float)Math.Exp(-t * 35f);
                }
            }

            return Normalise(samples, 0.45f);
        }

        private static float[] Sweep(float from, float to, float seconds)
        {
            float[] samples = new float[(int)(SampleRate * seconds)];
            double phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Math.PI * 2 * (from + (to - from) * t) / SampleRate;
                samples[i] = (float)Math.Sin(phase) * (1f - t);
            }

            return Normalise(samples, 0.5f);
        }

        private static float[] Blips(int count)
        {
            var random = new Random(3);
            int each = (int)(SampleRate * 0.07f);
            float[] samples = new float[each * count + SampleRate / 20];
            for (int b = 0; b < count; b++)
            {
                float pitch = 700f + (float)random.NextDouble() * 500f;
                for (int i = 0; i < each; i++)
                {
                    float t = (float)i / SampleRate;
                    float square = Math.Sin(Math.PI * 2 * pitch * t) >= 0 ? 1f : -1f;
                    samples[b * each + i] = square * 0.4f * (1f - (float)i / each);
                }
            }

            return Normalise(samples, 0.35f);
        }

        // The extra audio generated past the loop's end is blended into its start,
        // so the last sample flows straight into the first and the loop has no click.
        private static float[] Loop(float[] samples, int length, int overlap)
        {
            float[] loop = new float[length];
            Array.Copy(samples, loop, length);
            for (int i = 0; i < overlap; i++)
            {
                float mix = (float)i / overlap;
                loop[i] = samples[i] * mix + samples[length + i] * (1f - mix);
            }

            return loop;
        }

        private static float[] Normalise(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float s in samples) max = Math.Max(max, Math.Abs(s));
            if (max <= 0f) return samples;
            float scale = peak / max;
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
            return samples;
        }
    }
}
