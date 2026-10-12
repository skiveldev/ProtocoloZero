using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// Physically solid exit blocker. Only a panel activation unlocks it:
    /// the blocker collider is disabled and the visual is hidden, exactly
    /// once. Starts locked on every fresh scene load.
    /// </summary>
    public sealed class ExitGate : MonoBehaviour
    {
        [SerializeField] private LevelProgress progress;
        [SerializeField] private Collider blocker;
        [SerializeField] private GameObject visualRoot;

        public bool IsLocked =>
            blocker != null && blocker.enabled && blocker.gameObject.activeInHierarchy;

        private bool unlockLogged;

        private void Awake()
        {
            ApplyInitialLock();
        }

        private void OnEnable()
        {
            if (progress != null)
            {
                progress.PanelActivated += HandlePanelActivated;
                if (progress.IsPanelActivated)
                    Unlock();
            }
        }

        private void OnDisable()
        {
            if (progress != null)
                progress.PanelActivated -= HandlePanelActivated;
        }

        private void OnValidate()
        {
            if (blocker == null)
                blocker = GetComponent<Collider>();
            if (visualRoot == null)
                visualRoot = gameObject;
        }

        /// <summary>
        /// Unlocks only when the panel is activated. Hides the visual
        /// (a separate visual object, or this object's own renderers when
        /// the visual is the gate itself) and disables the solid blocker.
        /// Safe to call twice; logs exactly once.
        /// </summary>
        public void Unlock()
        {
            if (progress == null || !progress.IsPanelActivated)
                return;
            if (blocker != null)
                blocker.enabled = false;
            SetVisualVisible(false);
            if (!unlockLogged)
            {
                unlockLogged = true;
                Debug.Log("Salida desbloqueada");
            }
        }

        private void HandlePanelActivated()
        {
            Unlock();
        }

        private void SetVisualVisible(bool visible)
        {
            if (visualRoot != null && visualRoot != gameObject)
            {
                visualRoot.SetActive(visible);
                return;
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = visible;
        }

        private void ApplyInitialLock()
        {
            if (progress != null && progress.IsPanelActivated)
                return;
            if (blocker != null)
                blocker.enabled = true;
            if (visualRoot != null && visualRoot != gameObject)
            {
                if (!visualRoot.activeSelf)
                    visualRoot.SetActive(true);
            }
            else
            {
                SetVisualVisible(true);
            }
        }
    }
}
