using UnityEngine;

namespace LastTide
{
    /// <summary>Makes the campfire flame and its light waver.</summary>
    public class FlameFlicker : MonoBehaviour
    {
        [SerializeField] private Transform flame;
        [SerializeField] private Light fireLight;

        private float baseIntensity;

        public void Configure(Transform flameTransform, Light light)
        {
            flame = flameTransform;
            fireLight = light;
        }

        private void Awake()
        {
            baseIntensity = fireLight != null ? fireLight.intensity : 1f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(Time.time * 6f, 0.37f);
            if (flame != null)
            {
                float height = 0.85f + noise * 0.35f;
                float width = 1.05f - noise * 0.15f;
                flame.localScale = new Vector3(width, height, width);
            }

            if (fireLight != null)
            {
                fireLight.intensity = baseIntensity * (0.8f + noise * 0.4f);
            }
        }
    }
}
