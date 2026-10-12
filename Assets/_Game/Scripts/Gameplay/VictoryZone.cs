using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// Exit trigger zone. Grants victory once, only after the panel was
    /// activated, and only to the exact serialized player identity:
    /// the player root transform itself or one of its descendants.
    /// Any other collider — including foreign CharacterControllers and
    /// foreign PlayerInteractors — is ignored.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class VictoryZone : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private LevelProgress progress;

        public Transform PlayerRoot => playerRoot;

        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            NotifyTriggerEntered(other);
        }

        /// <summary>
        /// Direct entry point shared by the physics callback and tests.
        /// </summary>
        public void NotifyTriggerEntered(Collider other)
        {
            if (!IsPlayer(other))
                return;
            if (progress == null)
                return;
            if (progress.TryWin())
                Debug.Log("Victoria");
        }

        public bool IsPlayer(Collider other)
        {
            if (other == null || playerRoot == null)
                return false;
            Transform otherTransform = other.transform;
            return otherTransform == playerRoot || otherTransform.IsChildOf(playerRoot);
        }
    }
}
