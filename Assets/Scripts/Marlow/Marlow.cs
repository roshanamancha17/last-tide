using UnityEngine;

namespace LastTide
{
    /// <summary>The robot companion: starts half buried, then floats beside the player.</summary>
    public class Marlow : Interactable
    {
        [SerializeField] private Transform player;
        [SerializeField] private float followSmoothing = 0.3f;
        [SerializeField] private float pointSeconds = 7f;
        [SerializeField] private Transform signalBeam;
        [SerializeField] private float beamSeconds = 20f;
        [SerializeField] private float maxFlyDistance = 25f;

        private Vector3 velocity;
        private Transform pointTarget;
        private float pointUntil;
        private float beamUntil;
        private MarlowChat chat;

        public bool IsAwake { get; private set; }

        public void Configure(Transform playerTransform, Transform beam)
        {
            player = playerTransform;
            signalBeam = beam;
        }

        private void Awake()
        {
            chat = GetComponent<MarlowChat>();
        }

        public override string GetPrompt(Inventory inventory)
        {
            return IsAwake ? null : "[E] Dig out the buried robot";
        }

        public override string Interact(Inventory inventory)
        {
            if (IsAwake)
            {
                return null;
            }

            IsAwake = true;
            GameEvents.Log("Dug Marlow out of the sand");
            if (chat != null)
            {
                chat.Introduce();
            }
            return "You pulled the robot out of the sand.";
        }

        /// <summary>Flies to the nearest item of this type. Returns false when none is left.</summary>
        public bool PointAt(ItemType item)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (Interactable candidate in Active)
            {
                if (candidate is Pickup pickup && pickup.Item == item)
                {
                    float distance = (candidate.transform.position - player.position).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate.transform;
                    }
                }
            }

            PointAt(best);
            return best != null;
        }

        /// <summary>Marks a place with a tall light beam, and flies there if it's close.</summary>
        public void PointAt(Transform target)
        {
            if (target == null)
            {
                return;
            }

            float distance = Vector3.Distance(target.position, player.position);
            pointTarget = distance <= maxFlyDistance ? target : null;
            pointUntil = Time.time + pointSeconds;
            if (signalBeam != null)
            {
                signalBeam.position = target.position;
                signalBeam.gameObject.SetActive(true);
                beamUntil = Time.time + beamSeconds;
            }
        }

        private void Update()
        {
            if (signalBeam != null && signalBeam.gameObject.activeSelf && Time.time >= beamUntil)
            {
                signalBeam.gameObject.SetActive(false);
            }

            if (!IsAwake || player == null)
            {
                return;
            }

            bool pointing = pointTarget != null && Time.time < pointUntil;
            float bob = Mathf.Sin(Time.time * 2.1f) * 0.08f;
            Vector3 destination = pointing
                ? pointTarget.position + Vector3.up * 1.5f
                : player.position + player.right * 1.5f + player.forward * 1f + Vector3.up * (2f + bob);
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, followSmoothing);

            Vector3 lookTarget = pointing ? pointTarget.position : player.position + Vector3.up * 1.5f;
            Vector3 look = lookTarget - transform.position;
            if (look.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(look.normalized, Vector3.up);
                target *= Quaternion.Euler(0f, 0f, Mathf.Clamp(-velocity.x * 4f, -20f, 20f));
                transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 4f);
            }
        }
    }
}
