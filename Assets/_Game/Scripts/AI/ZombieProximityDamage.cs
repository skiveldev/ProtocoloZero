using ProtocoloZero.Gameplay;
using UnityEngine;

namespace ProtocoloZero.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SimpleZombieAI))]
    public sealed class ZombieProximityDamage : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 20;
        [SerializeField, Min(0.1f)] private float range = 1.4f;
        [SerializeField, Min(0.05f)] private float cooldown = 1f;

        private SimpleZombieAI brain;
        private PlayerHealth health;
        private Transform cachedTarget;
        private float remainingCooldown;

        private void Awake()
        {
            brain = GetComponent<SimpleZombieAI>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // One hit per tick at most: long frames never accumulate burst damage.
        public bool Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime <= 0f
                || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return false;
            remainingCooldown = Mathf.Max(0f, remainingCooldown - deltaTime);
            if (brain == null || !brain.isActiveAndEnabled || !brain.IsNavigationReady
                || brain.CurrentState != SimpleZombieAI.ZombieState.Chase)
                return false;

            Transform target = brain.PlayerTarget;
            if (target != cachedTarget)
            {
                cachedTarget = target;
                health = target != null ? target.GetComponent<PlayerHealth>() : null;
            }
            if (health == null || !health.isActiveAndEnabled || health.IsDead
                || remainingCooldown > 0f
                || Vector3.Distance(transform.position, target.position) > range)
                return false;

            Vector3 origin = transform.position + Vector3.up;
            Vector3 direction = target.position + Vector3.up - origin;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction.normalized,
                direction.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(target))
                    return false;
            }
            if (!health.TakeDamage(Mathf.Max(1, damage)))
                return false;
            remainingCooldown = Mathf.Max(0.05f, cooldown);
            return true;
        }
    }
}
