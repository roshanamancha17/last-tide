using System.Collections.Generic;
using UnityEngine;

namespace LastTide
{
    /// <summary>Plays the island's ambience, footsteps and a sound for each game event. All sounds are generated at start-up.</summary>
    public class GameAudio : MonoBehaviour
    {
        private const float WalkStep = 0.42f;
        private const float SprintStep = 0.3f;

        private static GameAudio instance;
        private readonly Dictionary<GameSound, AudioClip> clips = new Dictionary<GameSound, AudioClip>();
        private readonly AudioClip[] footsteps = new AudioClip[4];
        private AudioSource effects;
        private PlayerController player;
        private CharacterController body;
        private float nextStep;
        private int stepIndex;

        public static void Play(GameSound sound, float volume = 1f)
        {
            if (instance != null && instance.clips.TryGetValue(sound, out AudioClip clip))
            {
                instance.effects.PlayOneShot(clip, volume);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance == null)
            {
                GameObject host = new GameObject("Game Audio");
                DontDestroyOnLoad(host);
                host.AddComponent<GameAudio>();
            }
        }

        private void Awake()
        {
            instance = this;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;

            foreach (GameSound sound in System.Enum.GetValues(typeof(GameSound)))
            {
                if (sound != GameSound.None && sound != GameSound.Footstep)
                {
                    clips[sound] = ToClip(sound.ToString(), SoundSynth.Make(sound));
                }
            }

            for (int i = 0; i < footsteps.Length; i++)
            {
                footsteps[i] = ToClip("Footstep " + i, SoundSynth.Footstep(i + 1));
            }

            Ambience("Ocean", SoundSynth.Ocean(8f), 0.45f);
            Ambience("Wind", SoundSynth.Wind(6f), 0.2f);
            GameEvents.Logged += OnGameEvent;
        }

        private void OnDestroy()
        {
            GameEvents.Logged -= OnGameEvent;
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnGameEvent(string gameEvent)
        {
            Play(SoundEvents.For(gameEvent));
        }

        private void Update()
        {
            if (player == null)
            {
                // The player is new after each scene reload.
                player = FindAnyObjectByType<PlayerController>();
                body = player != null ? player.GetComponent<CharacterController>() : null;
                return;
            }

            if (!player.IsMoving || body == null || !body.isGrounded || Time.timeScale == 0f)
            {
                return;
            }

            if (Time.time >= nextStep)
            {
                nextStep = Time.time + (player.IsSprinting ? SprintStep : WalkStep);
                stepIndex = (stepIndex + 1) % footsteps.Length;
                effects.PlayOneShot(footsteps[stepIndex], player.IsSprinting ? 0.8f : 0.6f);
            }
        }

        private void Ambience(string name, float[] samples, float volume)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = ToClip(name, samples);
            source.loop = true;
            source.volume = volume;
            source.Play();
        }

        private static AudioClip ToClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SoundSynth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
