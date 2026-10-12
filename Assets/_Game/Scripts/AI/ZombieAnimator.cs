using UnityEngine;
using UnityEngine.AI;

namespace ProtocoloZero.AI
{
    /// <summary>Observes navigation velocity without taking ownership of movement.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public sealed class ZombieAnimator : MonoBehaviour
    {
        [SerializeField] private Animator visualAnimator;
        [SerializeField, Min(0f)] private float dampingTime = 0.1f;
        private NavMeshAgent agent;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        public Animator VisualAnimator => visualAnimator;
        public NavMeshAgent Agent => agent;
        public float ObservedSpeed { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (visualAnimator == null)
                visualAnimator = GetComponentInChildren<Animator>(true);
            if (visualAnimator != null)
                visualAnimator.applyRootMotion = false;
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                return;
            ObservedSpeed = 0f;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                Vector3 velocity = agent.velocity;
                ObservedSpeed = new Vector2(velocity.x, velocity.z).magnitude;
                if (ObservedSpeed <= 0.1f)
                    ObservedSpeed = 0f;
            }
            if (visualAnimator != null && visualAnimator.runtimeAnimatorController != null)
                visualAnimator.SetFloat(SpeedHash, ObservedSpeed, dampingTime, deltaTime);
        }
    }
}
