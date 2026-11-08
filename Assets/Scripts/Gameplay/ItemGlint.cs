using UnityEngine;

namespace LastTide
{
    /// <summary>A small sparkle above a loose item, so it's easier to spot. Fades out when far away.</summary>
    public class ItemGlint : MonoBehaviour
    {
        [SerializeField] private float height = 0.65f;
        [SerializeField] private float visibleDistance = 22f;

        private static Transform cameraTransform;
        private Vector3 basePosition;
        private float offset;
        private Renderer glintRenderer;

        private void Start()
        {
            basePosition = transform.localPosition;
            offset = Random.value * 10f;
            glintRenderer = GetComponent<Renderer>();
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (cameraTransform == null)
            {
                return;
            }

            bool near = (cameraTransform.position - transform.position).sqrMagnitude < visibleDistance * visibleDistance;
            if (glintRenderer.enabled != near)
            {
                glintRenderer.enabled = near;
            }

            if (!near)
            {
                return;
            }

            float time = Time.time + offset;
            transform.localPosition = basePosition + Vector3.up * (height + Mathf.Sin(time * 2.4f) * 0.08f);
            transform.localRotation = Quaternion.Euler(0f, time * 120f, 0f);
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 4f);
            transform.localScale = Vector3.one * pulse;
        }
    }
}
