using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoloZero.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float height = 1.5f;
        [SerializeField, Min(0.1f)] private float distance = 3.5f;
        [SerializeField, Min(0f)] private float sensitivity = 0.15f;
        [SerializeField] private float initialPitch = 12f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField, Min(0f)] private float followSharpness = 12f;
        [SerializeField, Min(0f)] private float recoverySharpness = 5f;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.35f;
        [SerializeField, Min(0f)] private float wallPadding = 0.05f;

        private float yaw;
        private float pitch;
        private float currentDistance;
        private Vector3 pivot;
        private bool initialized;
        private bool wantsCursor;

        private void OnEnable()
        {
            initialized = false;
            if (Application.isPlaying)
            {
                wantsCursor = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || !wantsCursor) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            wantsCursor = false;
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            if (target == null) return;

            if (!initialized)
            {
                yaw = target.eulerAngles.y;
                pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
                pivot = target.position + Vector3.up * height;
                currentDistance = distance;
                initialized = true;
            }

            if (Application.isPlaying)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    wantsCursor = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else if (!wantsCursor && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    wantsCursor = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked && wantsCursor)
                ApplyMouseDelta(mouse.delta.ReadValue());

            float follow = 1f - Mathf.Exp(-followSharpness * Mathf.Max(0f, deltaTime));
            pivot = Vector3.Lerp(pivot, target.position + Vector3.up * height, follow);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            float safeDistance = SafeDistance(pivot, direction);
            if (safeDistance < currentDistance)
                currentDistance = safeDistance;
            else
                currentDistance = Mathf.Lerp(currentDistance, safeDistance,
                    1f - Mathf.Exp(-recoverySharpness * Mathf.Max(0f, deltaTime)));

            transform.SetPositionAndRotation(pivot + direction * currentDistance, rotation);
        }

        private void ApplyMouseDelta(Vector2 delta)
        {
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        private bool IsSelf(Collider collider) =>
            collider != null && target != null &&
            (collider.transform == target || collider.transform.IsChildOf(target));

        private float SafeDistance(Vector3 origin, Vector3 direction)
        {
            float radius = Mathf.Max(collisionRadius, GetComponent<UnityEngine.Camera>().nearClipPlane);
            float safe = distance;
            // SphereCast cannot detect geometry already enclosing its origin.
            Collider[] overlaps = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in overlaps)
            {
                if (IsSelf(collider)) continue;
                Vector3 nearest = collider.ClosestPoint(origin);
                if ((nearest - origin).sqrMagnitude < radius * radius)
                    return 0f;
            }

            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (!IsSelf(hit.collider))
                    safe = Mathf.Min(safe, Mathf.Max(0f, hit.distance - wallPadding));
            }

            return safe;
        }
    }
}
