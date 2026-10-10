using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoloZero.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkSpeed = 3f;
        [SerializeField, Min(0f)] private float runSpeed = 6f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float groundedDownwardSpeed = -2f;
        // Null preserves world-axis controls until a camera-relative reference is assigned.
        [SerializeField] private Transform movementReference;

        private CharacterController controller;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();

            Keyboard keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            bool sprinting = false;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                sprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }

            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 right = Vector3.right;
            Vector3 forward = Vector3.forward;
            if (movementReference != null)
            {
                forward = Vector3.ProjectOnPlane(movementReference.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(movementReference.right, Vector3.up).normalized;
            }

            Vector3 direction = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
            if (direction.sqrMagnitude > 0f)
            {
                Quaternion facing = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * deltaTime);
            }

            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = groundedDownwardSpeed;
            verticalSpeed += gravity * deltaTime;

            float speed = sprinting ? runSpeed : walkSpeed;
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * deltaTime);
        }
    }
}
