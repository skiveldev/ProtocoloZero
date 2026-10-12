using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// Contract for anything the player can activate with the interact key.
    /// Kept minimal so cards, panels and future pickups stay reusable.
    /// </summary>
    public interface IInteractable
    {
        string DisplayName { get; }
        bool CanInteract { get; }
        void Interact(PlayerInteractor interactor);
    }
}
