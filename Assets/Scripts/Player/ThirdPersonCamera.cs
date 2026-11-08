using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>Mouse-orbit camera that pulls in when terrain or props get in the way.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float distance = 6f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -5f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float collisionRadius = 0.25f;
        [Tooltip("The camera always stays above this height, so it can't go under the water.")]
        [SerializeField] private float minCameraHeight = 0.5f;
        [SerializeField] private LayerMask collisionMask = ~(1 << 2);

        private float yaw;
        private float pitch = 15f;
        private int framesToIgnoreMouse;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
            }
            LockCursor(true);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleCursor();
            if (framesToIgnoreMouse > 0)
            {
                framesToIgnoreMouse--;
            }
            else
            {
                // Mouse (while the cursor is locked) and touch drags both arrive here.
                Vector2 delta = SettingsService.Current.ApplyToLook(GameInput.Current.Look);
                yaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + targetOffset;
            Vector3 back = rotation * Vector3.back;
            float allowed = distance;
            if (Physics.SphereCast(focus, collisionRadius, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                allowed = Mathf.Max(minDistance, hit.distance);
            }

            Vector3 position = focus + back * allowed;
            position.y = Mathf.Max(position.y, minCameraHeight);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void HandleCursor()
        {
            // With touch controls there is no cursor to capture, and menus need a free cursor.
            if (InputCollector.TouchActive || UiState.MenuOpen)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    LockCursor(false);
                }
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !UiState.IsTyping && UiState.EscapeHandledFrame != Time.frameCount)
            {
                LockCursor(false);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        private void LockCursor(bool locked)
        {
            // Locking the cursor reports one large mouse movement, which would swing the camera.
            framesToIgnoreMouse = locked ? 3 : 0;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
