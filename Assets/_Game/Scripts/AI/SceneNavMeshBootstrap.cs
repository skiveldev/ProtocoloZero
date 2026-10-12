using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using ProtocoloZero.Gameplay;

namespace ProtocoloZero.AI
{
    /// <summary>
    /// Builds the main-scene NavMesh once at startup from real static collider
    /// geometry (floor tiles, walls, props carry colliders in the vendor
    /// prefabs). When the surface already references a baked NavMeshData asset
    /// (Editor bake from the same real geometry), the baked data is added to
    /// the NavMesh system and no rebuild runs at PlayMode startup. Otherwise
    /// the surface builds synchronously in Awake (one-time hitch, disclosed)
    /// before any zombie AI runs. Dynamic gameplay objects (player, cards,
    /// panel, zone, gate) are excluded via runtime NavMeshModifier.ignoreFromBuild
    /// so no original prefab is modified; agents and carve obstacles are
    /// auto-excluded by the package (ignoreNavMeshAgent / ignoreNavMeshObstacle
    /// stay enabled). Runs at order -200, ahead of NavMeshSurface (-102) and
    /// zombie AI. The surface's own OnEnable repeats AddData safely: the
    /// package ignores a second AddData while the instance is valid.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("ProtocoloZero/AI/Scene NavMesh Bootstrap")]
    public sealed class SceneNavMeshBootstrap : MonoBehaviour
    {
        [SerializeField] private NavMeshSurface surface;

        public NavMeshSurface Surface => surface;
        public bool HasBuilt { get; private set; }

        /// <summary>
        /// True when startup reused the baked NavMeshData asset instead of
        /// rebuilding geometry. Tests assert this on the main scene.
        /// </summary>
        public bool UsedBakedData { get; private set; }

        /// <summary>
        /// Counts runtime geometry rebuilds process-wide. The main scene with
        /// a baked asset must stay at zero; other levels without an asset may
        /// build once. Tests reset via <see cref="ResetStartupBuildCount"/>.
        /// </summary>
        public static int StartupBuildCount { get; private set; }

        public static void ResetStartupBuildCount() => StartupBuildCount = 0;

        private void Awake()
        {
            if (HasBuilt)
                return;
            if (surface == null)
                surface = GetComponent<NavMeshSurface>();
            if (surface == null)
                surface = gameObject.AddComponent<NavMeshSurface>();

            // Baked-data fast path: reuse the Editor-baked asset built from
            // the same real main-scene geometry. AddData before any agent
            // Awake; the surface OnEnable repeats it harmlessly (guarded by
            // the package while the instance is valid). Never clears global
            // NavMesh data owned by other objects.
            if (surface.navMeshData != null)
            {
                surface.AddData();
                HasBuilt = true;
                UsedBakedData = true;
                return;
            }

            ConfigureSurface(surface);
            ExcludeDynamicSources();
            try
            {
                surface.BuildNavMesh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[SceneNavMeshBootstrap] Runtime NavMesh build failed on " +
                    $"'{gameObject.name}': {ex.Message}. Bake a NavMeshData asset " +
                    $"in the Editor instead of removing geometry.", this);
                HasBuilt = false;
                return;
            }
            StartupBuildCount++;
            HasBuilt = surface.navMeshData != null;
            UsedBakedData = false;
        }

        private static void ConfigureSurface(NavMeshSurface target)
        {
            target.agentTypeID = 0;
            target.collectObjects = CollectObjects.All;
            target.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            target.defaultArea = 0;
            target.ignoreNavMeshAgent = true;
            target.ignoreNavMeshObstacle = true;
        }

        /// <summary>
        /// Adds runtime-only ignore modifiers (never saved into prefabs) so
        /// the player CharacterController, card/panel/zone trigger colliders
        /// and the locked gate blocker do not bake into walkable geometry.
        /// Startup-only discovery: no per-frame Find calls anywhere in AI.
        /// </summary>
        private static void ExcludeDynamicSources()
        {
            GameObject player = GameObject.Find("Player_Renegade");
            if (player != null)
                IgnoreFromBuild(player.transform);

            foreach (AccessCard card in FindObjectsByType<AccessCard>(FindObjectsSortMode.None))
                IgnoreFromBuild(card.transform);
            foreach (SecurityPanel panel in FindObjectsByType<SecurityPanel>(FindObjectsSortMode.None))
                IgnoreFromBuild(panel.transform);
            foreach (VictoryZone zone in FindObjectsByType<VictoryZone>(FindObjectsSortMode.None))
                IgnoreFromBuild(zone.transform);
            foreach (ExitGate gate in FindObjectsByType<ExitGate>(FindObjectsSortMode.None))
                IgnoreFromBuild(gate.transform);
        }

        private static void IgnoreFromBuild(Transform root)
        {
            if (root == null)
                return;
            NavMeshModifier modifier = root.GetComponent<NavMeshModifier>();
            if (modifier == null)
                modifier = root.gameObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
        }
    }
}
