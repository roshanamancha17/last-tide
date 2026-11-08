using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>Third-person movement relative to the camera. Keeps the player out of deep water.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        [Header("Water")]
        [Tooltip("The player is stopped before their feet go below this height.")]
        [SerializeField] private float deepestWadeHeight = -0.9f;

        private CharacterController controller;
        private Transform cameraTransform;
        private float verticalVelocity;
        private Vector3 lastSafePosition;

        public bool SprintAllowed { get; set; } = true;
        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            lastSafePosition = position;
            verticalVelocity = 0f;
        }

        private void OnDisable()
        {
            IsMoving = false;
            IsSprinting = false;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastSafePosition = transform.position;
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            InputFrame input = GameInput.Current;
            Vector3 direction = CameraRelative(input.Move);
            IsMoving = direction.sqrMagnitude > 0.001f;
            IsSprinting = IsMoving && SprintAllowed && input.Sprint;
            float speed = IsSprinting ? sprintSpeed : walkSpeed;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            if (controller.isGrounded)
            {
                verticalVelocity = input.Jump
                    ? Mathf.Sqrt(2f * jumpHeight * -gravity)
                    : -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = direction * speed + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
            KeepOutOfDeepWater();
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            return forward.normalized * input.y + right.normalized * input.x;
        }

        private void KeepOutOfDeepWater()
        {
            float feet = transform.position.y + controller.center.y - controller.height * 0.5f;
            if (feet >= deepestWadeHeight)
            {
                if (controller.isGrounded)
                {
                    lastSafePosition = transform.position;
                }
                return;
            }

            controller.enabled = false;
            transform.position = lastSafePosition;
            controller.enabled = true;
            verticalVelocity = 0f;
        }
    }
}
