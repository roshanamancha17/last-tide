using UnityEngine;

namespace LastTide
{
    /// <summary>Animates the jointed castaway model in code: walking, running, idle breathing and jumping.</summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Transform hips;
        [SerializeField] private Transform head;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private float strideLength = 1.6f;

        private CharacterController controller;
        private Quaternion hipsBase, headBase, armLeftBase, armRightBase, legLeftBase, legRightBase;
        private Vector3 hipsBasePosition;
        private Vector3 lastPosition;
        private float phase;
        private float speedSmoothed;
        private float airBlend;

        public void Configure(Transform model)
        {
            hips = Find(model, "Hips");
            head = Find(model, "Head");
            armLeft = Find(model, "ArmL");
            armRight = Find(model, "ArmR");
            legLeft = Find(model, "LegL");
            legRight = Find(model, "LegR");
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            Debug.LogWarning("[LastTide] Character joint not found: " + name);
            return null;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            hipsBase = hips.localRotation;
            hipsBasePosition = hips.localPosition;
            headBase = head.localRotation;
            armLeftBase = armLeft.localRotation;
            armRightBase = armRight.localRotation;
            legLeftBase = legLeft.localRotation;
            legRightBase = legRight.localRotation;
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 moved = transform.position - lastPosition;
            lastPosition = transform.position;
            moved.y = 0f;
            float speed = Mathf.Min(moved.magnitude / deltaTime, 12f);
            speedSmoothed = Mathf.Lerp(speedSmoothed, speed, 1f - Mathf.Exp(-12f * deltaTime));

            bool grounded = controller == null || controller.isGrounded;
            airBlend = Mathf.MoveTowards(airBlend, grounded ? 0f : 1f, deltaTime * 6f);

            phase += speedSmoothed / strideLength * Mathf.PI * deltaTime;
            float walk = Mathf.Clamp01(speedSmoothed / 2f);
            float run = Mathf.Clamp01((speedSmoothed - 5.5f) / 2.5f);
            float swing = Mathf.Sin(phase);

            float legAngle = swing * Mathf.Lerp(30f, 48f, run) * walk;
            float armAngle = swing * Mathf.Lerp(26f, 50f, run) * walk;
            float lean = Mathf.Lerp(3f, 14f, run) * walk;
            float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.05f * walk;
            float breathe = Mathf.Sin(Time.time * 2.2f) * (1f - walk);

            // In the air, legs tuck and arms lift.
            float airLegs = 22f * airBlend;
            float airArms = -60f * airBlend;

            // The hips go first, because the head and limbs hang from them.
            Vector3 right = transform.right;
            hips.localPosition = hipsBasePosition + hips.parent.InverseTransformVector(Vector3.up * (bob + breathe * 0.004f));
            Rotate(hips, hipsBase, lean + breathe * 1.2f, right);
            Rotate(head, headBase, -lean * 0.6f - breathe * 2f, right);
            Rotate(legLeft, legLeftBase, -legAngle * (1f - airBlend) - airLegs, right);
            Rotate(legRight, legRightBase, legAngle * (1f - airBlend) - airLegs * 0.4f, right);
            Rotate(armLeft, armLeftBase, armAngle * (1f - airBlend) + airArms, right, -4f - 6f * airBlend);
            Rotate(armRight, armRightBase, -armAngle * (1f - airBlend) + airArms, right, 4f + 6f * airBlend);
        }

        // Turns a joint about the character's right axis, plus an optional sideways tilt away from the body.
        private void Rotate(Transform joint, Quaternion baseLocal, float angle, Vector3 right, float outward = 0f)
        {
            Quaternion parent = joint.parent.rotation;
            Quaternion swing = Quaternion.AngleAxis(angle, right) * Quaternion.AngleAxis(outward, transform.forward);
            joint.rotation = swing * parent * baseLocal;
        }
    }
}
