using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// Lets the player activate the nearest valid <see cref="IInteractable"/>
    /// with the E key. Distance is enforced even for an explicitly selected
    /// target, and an optional line-of-sight check avoids interacting
    /// through walls. The scan only runs on key press, never per frame.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float interactRange = 2.5f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private Transform eye;

        public float InteractRange => interactRange;
        public bool RequireLineOfSight => requireLineOfSight;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.eKey.wasPressedThisFrame)
                TryInteract();
        }

        /// <summary>Interact with the nearest valid target in range.</summary>
        public bool TryInteract()
        {
            IInteractable nearest = FindNearestValidTarget();
            if (nearest == null)
                return false;
            nearest.Interact(this);
            return true;
        }

        /// <summary>
        /// Interact with an explicitly selected target. Range, availability
        /// and line of sight are still enforced.
        /// </summary>
        public bool TryInteract(Component selectedTarget)
        {
            if (selectedTarget == null)
                return TryInteract();
            if (!(selectedTarget is IInteractable interactable))
                return false;
            if (!IsTargetValid(interactable))
                return false;
            interactable.Interact(this);
            return true;
        }

        private IInteractable FindNearestValidTarget()
        {
            // Runs only on key press, so a scene-wide scan here is acceptable.
            MonoBehaviour[] candidates =
                Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            IInteractable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!(candidates[i] is IInteractable interactable))
                    continue;
                if (!IsTargetValid(interactable))
                    continue;
                float distance = DistanceTo(interactable);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = interactable;
                }
            }
            return best;
        }

        private bool IsTargetValid(IInteractable interactable)
        {
            if (interactable == null || !interactable.CanInteract)
                return false;
            if (DistanceTo(interactable) > interactRange)
                return false;
            if (requireLineOfSight && !HasLineOfSight(interactable))
                return false;
            return true;
        }

        private float DistanceTo(IInteractable interactable)
        {
            return Vector3.Distance(
                transform.position, ((Component)interactable).transform.position);
        }

        private Vector3 EyePosition =>
            eye != null ? eye.position : transform.position + Vector3.up * 1.4f;

        private bool HasLineOfSight(IInteractable interactable)
        {
            var target = (Component)interactable;
            Vector3 origin = EyePosition;
            Vector3 destination = target.transform.position;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
                return true;
            Physics.SyncTransforms();
            // Triggers never obstruct (unrelated volumes and the target's own
            // trigger are skipped by the query itself). Own body colliders and
            // the target's own solid colliders are skipped explicitly; any
            // other solid collider (e.g. a wall) denies the interaction.
            RaycastHit[] hits = Physics.RaycastAll(
                origin, direction / distance, distance, obstacleMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].transform;
                if (hit == transform || hit.IsChildOf(transform))
                    continue;
                if (hit == target.transform || hit.IsChildOf(target.transform))
                    continue;
                return false;
            }
            return true;
        }
    }
}
