using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// A pickup card with a unique ID. Counts once toward
    /// <see cref="LevelProgress"/>, then hides its visual/object.
    /// OnEnable never clears the collected flag, so a re-enable
    /// cannot resurrect an already picked card.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class AccessCard : MonoBehaviour, IInteractable
    {
        [SerializeField] private string cardId = "Card_A";
        [SerializeField] private LevelProgress progress;
        [SerializeField] private GameObject visualRoot;

        public string CardId => cardId;
        public string DisplayName => $"Tarjeta {cardId}";

        public bool CanInteract =>
            !collected && progress != null && !string.IsNullOrWhiteSpace(cardId);

        private bool collected;

        private void OnValidate()
        {
            if (visualRoot == null)
                visualRoot = gameObject;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract)
                return;
            if (!progress.TryCollectCard(cardId))
                return;
            collected = true;
            Debug.Log($"Tarjeta recogida {progress.CollectedCount}/{progress.RequiredCards}");
            GameObject root = visualRoot != null ? visualRoot : gameObject;
            root.SetActive(false);
        }
    }
}
