using UnityEngine;

namespace ProtocoloZero.Player
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");

        [SerializeField, Min(0f)] private float idleDeadzone = 0.05f;
        [SerializeField, Min(0f)] private float speedDamping = 0.1f;

        private CharacterController controller;
        private Animator animator;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
        }

        private void LateUpdate()
        {
            if (controller == null || animator == null || animator.runtimeAnimatorController == null)
                return;

            Vector3 velocity = controller.velocity;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            if (horizontalSpeed <= idleDeadzone || speedDamping <= 0f)
                animator.SetFloat(Speed, horizontalSpeed <= idleDeadzone ? 0f : horizontalSpeed);
            else
                animator.SetFloat(Speed, horizontalSpeed, speedDamping, Time.deltaTime);
        }
    }
}
