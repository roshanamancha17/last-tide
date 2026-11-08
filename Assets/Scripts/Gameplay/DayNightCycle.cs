using UnityEngine;

namespace LastTide
{
    /// <summary>Moves the sun and moon, and fades the sky, ambient light and water with the time of day.</summary>
    [RequireComponent(typeof(Light))]
    public class DayNightCycle : MonoBehaviour
    {
        private const float SunriseHour = 6f;
        private const float SunsetHour = 18f;
        private static readonly int NightProperty = Shader.PropertyToID("_LastTideNight");

        [SerializeField] private float startHour = 8f;
        [Tooltip("Real seconds per in-game hour in daylight.")]
        [SerializeField] private float secondsPerDayHour = 40f;
        [Tooltip("Real seconds per in-game hour at night.")]
        [SerializeField] private float secondsPerNightHour = 20f;
        [SerializeField] private float sunIntensity = 1.25f;
        [SerializeField] private float moonIntensity = 0.2f;
        [SerializeField] private Color noonColor = new Color(1f, 0.95f, 0.85f);
        [SerializeField] private Color lowSunColor = new Color(1f, 0.55f, 0.28f);
        [SerializeField] private Color moonColor = new Color(0.55f, 0.68f, 1f);
        [SerializeField] private float dayExposure = 1.25f;
        [SerializeField] private float nightExposure = 0.1f;

        private Light sun;
        private Material sky;

        public float Hour { get; private set; }
        public int Day { get; private set; } = 1;
        public bool IsNight => Hour < SunriseHour || Hour >= SunsetHour;

        public string TimeText
        {
            get
            {
                int minutes = Mathf.FloorToInt(Hour * 60f) % 60;
                return $"Day {Day}   {Mathf.FloorToInt(Hour):00}:{minutes:00}";
            }
        }

        public void Advance(float hours)
        {
            Hour += hours;
            while (Hour >= 24f)
            {
                Hour -= 24f;
                Day++;
            }
            Apply();
        }

        /// <summary>Jumps to a saved day and hour.</summary>
        public void SetTime(int day, float hour)
        {
            Day = Mathf.Max(1, day);
            Hour = Mathf.Repeat(hour, 24f);
            Apply();
        }

        public void SkipToMorning()
        {
            float target = SunriseHour + 1f;
            Advance(Hour < target ? target - Hour : 24f - Hour + target);
        }

        private void Awake()
        {
            sun = GetComponent<Light>();
            if (RenderSettings.skybox != null)
            {
                // A copy, so playing never changes the saved sky material.
                sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            Hour = startHour;
            Apply();
        }

        private void Update()
        {
            float secondsPerHour = IsNight ? secondsPerNightHour : secondsPerDayHour;
            Advance(Time.deltaTime / secondsPerHour);
        }

        private void OnDestroy()
        {
            Shader.SetGlobalFloat(NightProperty, 0f);
        }

        private void Apply()
        {
            bool night = IsNight;
            float start = night ? SunsetHour : SunriseHour;
            float progress = Mathf.Repeat(Hour - start, 24f) / 12f;
            float angle = progress * Mathf.PI;
            float height = Mathf.Sin(angle);
            Vector3 toLight = new Vector3(Mathf.Cos(angle), height * 0.85f, 0.45f * height + 0.2f).normalized;
            transform.rotation = Quaternion.LookRotation(-toLight, Vector3.up);

            float strength = Mathf.SmoothStep(0f, 1f, height / 0.25f);
            if (night)
            {
                sun.intensity = moonIntensity * strength;
                sun.color = moonColor;
            }
            else
            {
                sun.intensity = sunIntensity * strength;
                sun.color = Color.Lerp(lowSunColor, noonColor, Mathf.SmoothStep(0f, 1f, height / 0.6f));
            }

            float daylight = night ? 0f : Mathf.SmoothStep(0f, 1f, height / 0.35f);
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.05f, 0.08f, 0.16f), new Color(0.62f, 0.74f, 0.92f), daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.04f, 0.06f, 0.11f), new Color(0.58f, 0.60f, 0.58f), daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.02f, 0.03f, 0.05f), new Color(0.36f, 0.33f, 0.26f), daylight);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.1f, 1f, daylight);
            Shader.SetGlobalFloat(NightProperty, 1f - daylight);
            if (sky != null)
            {
                sky.SetFloat("_Exposure", Mathf.Lerp(nightExposure, dayExposure, daylight));
            }
        }
    }
}
