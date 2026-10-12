using UnityEngine;
using UnityEngine.AI;
using ProtocoloZero.Gameplay;

namespace ProtocoloZero.AI
{
    /// <summary>
    /// Owned companion obstacle that mirrors the Phase 05 ExitGate transform
    /// and carves the baked NavMesh while the gate is locked, so zombies path
    /// around the closed exit instead of through it. Listens to the existing
    /// LevelProgress.PanelActivated event and releases the carve on unlock.
    /// Reads Phase 05 state only; no Phase 05 source or prefab is modified.
    /// </summary>
    [RequireComponent(typeof(NavMeshObstacle))]
    [AddComponentMenu("ProtocoloZero/AI/Gate Navigation Obstacle")]
    public sealed class GateNavigationObstacle : MonoBehaviour
    {
        [SerializeField] private LevelProgress progress;
        [SerializeField] private Transform gateTransform;
        [SerializeField] private Vector3 fallbackSize = new Vector3(4f, 3f, 0.5f);

        public bool IsCarving => obstacle != null && obstacle.enabled && obstacle.carving;

        private NavMeshObstacle obstacle;

        private void Awake()
        {
            if (obstacle == null)
                obstacle = GetComponent<NavMeshObstacle>();
            MirrorGate();
        }

        private void OnEnable()
        {
            if (progress != null)
            {
                progress.PanelActivated += HandlePanelActivated;
                if (progress.IsPanelActivated)
                    ReleaseCarve();
            }
        }

        private void OnDisable()
        {
            if (progress != null)
                progress.PanelActivated -= HandlePanelActivated;
        }

        private void HandlePanelActivated()
        {
            ReleaseCarve();
        }

        private void ReleaseCarve()
        {
            if (obstacle == null)
                return;
            obstacle.carving = false;
            obstacle.enabled = false;
        }

        private void MirrorGate()
        {
            if (gateTransform != null)
                transform.SetPositionAndRotation(gateTransform.position, gateTransform.rotation);
            if (obstacle == null)
                return;

            obstacle.shape = NavMeshObstacleShape.Box;
            BoxCollider box = gateTransform != null
                ? gateTransform.GetComponentInChildren<BoxCollider>()
                : null;
            if (box != null)
            {
                obstacle.center = box.center;
                obstacle.size = Vector3.Scale(box.size, gateTransform.lossyScale);
            }
            else
            {
                obstacle.center = Vector3.zero;
                obstacle.size = fallbackSize;
            }
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }
    }
}
