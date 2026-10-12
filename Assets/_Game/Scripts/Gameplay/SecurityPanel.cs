using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// Security panel. Activates only when <see cref="LevelProgress"/>
    /// holds the required cards; otherwise reports the missing cards.
    /// Repeat interactions after activation stay silent (no extra events).
    /// </summary>
    public sealed class SecurityPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private LevelProgress progress;

        public string DisplayName => "Panel de seguridad";

        public bool CanInteract => progress != null && !progress.IsPanelActivated;

        public void Interact(PlayerInteractor interactor)
        {
            if (progress == null || progress.IsPanelActivated)
                return;
            if (progress.TryActivatePanel())
                Debug.Log("Panel activado");
            else
                Debug.Log("Faltan tarjetas");
        }
    }
}
