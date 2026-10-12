using System.Collections;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace ProtocoloZero.AI.Tests
{
    /// <summary>
    /// Real-navigation integration on an isolated temporary surface: a floor
    /// plus a wall with a gap, baked with the actual NavMesh package (no
    /// mocks). Reachable points complete, the wall blocks a straight raycast,
    /// and a cross-wall route detours instead of passing through. The static
    /// path test needs no frames and stays a plain EditMode test; the brain
    /// tick tests are UnityTests that enter PlayMode, because a NavMeshAgent
    /// only reports isOnNavMesh and computes paths on real player-loop
    /// frames. PlayMode here is an isolated boot scene, never the warehouse
    /// acceptance walk. All temporary objects and NavMesh data are destroyed
    /// in finally.
    /// </summary>
    public class ZombieNavIntegrationTests
    {
        private GameObject tempRoot;

        [TearDown]
        public void CleanupNavData()
        {
            if (tempRoot != null)
                Object.DestroyImmediate(tempRoot);
            tempRoot = null;
            NavMesh.RemoveAllNavMeshData();
        }

        private static GameObject AddBox(string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = size;
            go.transform.position = center;
            return go;
        }

        private NavMeshSurface BuildTempSurface()
        {
            tempRoot = new GameObject("Phase06_TempNav");
            GameObject floor = AddBox("TempFloor", new Vector3(0f, -0.5f, 0f),
                new Vector3(20f, 1f, 20f));
            floor.transform.SetParent(tempRoot.transform, true);
            // Wall along x at z = 2 with a 4 m gap around x = 0.
            GameObject wallLeft = AddBox("TempWallLeft", new Vector3(-6f, 1.5f, 2f),
                new Vector3(8f, 3f, 1f));
            wallLeft.transform.SetParent(tempRoot.transform, true);
            GameObject wallRight = AddBox("TempWallRight", new Vector3(6f, 1.5f, 2f),
                new Vector3(8f, 3f, 1f));
            wallRight.transform.SetParent(tempRoot.transform, true);

            NavMeshSurface surface = tempRoot.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            return surface;
        }

        [Test]
        public void RealNavMesh_ReachablePoints_Complete_WallForcesDetour()
        {
            BuildTempSurface();

            Assert.That(
                NavMesh.SamplePosition(new Vector3(0f, 0f, -4f), out NavMeshHit floorHit,
                    2f, NavMesh.AllAreas),
                Is.True, "floor sample lands on the baked mesh");

            var path = new NavMeshPath();
            Assert.That(
                NavMesh.CalculatePath(new Vector3(0f, 0f, -4f), new Vector3(0f, 0f, 8f),
                    NavMesh.AllAreas, path),
                Is.True, "route through the gap calculates");
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));

            // Straight line through solid wall masonry must stay blocked.
            Vector3 blockedFrom = new Vector3(-6f, 1f, -4f);
            Vector3 blockedTo = new Vector3(-6f, 1f, 8f);
            Assert.That(
                NavMesh.Raycast(blockedFrom, blockedTo, out NavMeshHit wallHit, NavMesh.AllAreas),
                Is.True, "wall bakes as a hole, not walkable");

            // The routed path must detour via the gap instead of crossing.
            var detour = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(blockedFrom, blockedTo, NavMesh.AllAreas, detour),
                Is.True);
            Assert.That(detour.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            float straight = Vector3.Distance(blockedFrom, blockedTo);
            float routed = 0f;
            for (int i = 1; i < detour.corners.Length; i++)
                routed += Vector3.Distance(detour.corners[i - 1], detour.corners[i]);
            Assert.That(routed, Is.GreaterThan(straight + 1f),
                $"detour ({routed:F2} m) must exceed the blocked straight line ({straight:F2} m)");
        }

        private SimpleZombieAI SpawnZombieOnMesh(Vector3 position)
        {
            Assert.That(
                NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas),
                Is.True, "spawn point lands on the temp mesh");
            var go = new GameObject("TickProbe");
            go.transform.SetParent(tempRoot.transform, true);
            go.transform.position = hit.position;
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.stoppingDistance = 0.8f;
            agent.enabled = false;
            SimpleZombieAI brain = go.AddComponent<SimpleZombieAI>();
            // The brain is driven by explicit Tick calls below; its Update
            // stays off so destination counts are fully deterministic.
            brain.enabled = false;
            return brain;
        }

        /// <summary>
        /// Boots PlayMode from an empty scene so the warehouse bootstrap never
        /// runs and no gameplay object interferes. Must run on the EditMode
        /// side, before EnterPlayMode; it is idempotent across the domain
        /// reload that entering PlayMode triggers.
        /// </summary>
        private static void EnsureIsolatedPlayBoot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        private static void SetWaypoints(SimpleZombieAI brain, params Transform[] points)
        {
            var serialized = new SerializedObject(brain);
            SerializedProperty waypoints = serialized.FindProperty("waypoints");
            waypoints.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
                waypoints.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            serialized.ApplyModifiedProperties();
        }

        private static GameObject AddWaypoint(string name, Vector3 position, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            return go;
        }

        [UnityTest]
        public IEnumerator Tick_AllNullWaypoints_StaysInPatrol_WithoutOverflowOrErrors()
        {
            EnsureIsolatedPlayBoot();
            yield return new EnterPlayMode();
            BuildTempSurface();
            try
            {
                SimpleZombieAI brain = SpawnZombieOnMesh(new Vector3(0f, 0f, -4f));
                SetWaypoints(brain, null, null);

                int frames = 0;
                while (!brain.IsNavigationReady && frames < 30)
                {
                    brain.Tick(0.25f);
                    yield return null;
                    frames++;
                }
                Assert.That(brain.IsNavigationReady, Is.True,
                    "agent reaches the baked mesh on real frames");

                Assert.DoesNotThrow(() =>
                {
                    for (int i = 0; i < 30; i++)
                        brain.Tick(0.25f);
                }, "bounded scan over all-null waypoints must never recurse or throw");
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Patrol));
                Assert.That(brain.HasValidDestination, Is.False);
                Assert.That(brain.DestinationRequestCount, Is.EqualTo(0),
                    "no destination is ever issued for unreachable points");
            }
            finally
            {
                CleanupNavData();
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Tick_UnsampleableWaypoints_RetriesBounded_WithoutOverflowOrErrors()
        {
            EnsureIsolatedPlayBoot();
            yield return new EnterPlayMode();
            BuildTempSurface();
            try
            {
                SimpleZombieAI brain = SpawnZombieOnMesh(new Vector3(0f, 0f, -4f));
                GameObject farA = AddWaypoint("FarA", new Vector3(500f, 0f, 500f), tempRoot.transform);
                GameObject farB = AddWaypoint("FarB", new Vector3(501f, 0f, 501f), tempRoot.transform);
                SetWaypoints(brain, farA.transform, farB.transform);

                int frames = 0;
                while (!brain.IsNavigationReady && frames < 30)
                {
                    brain.Tick(0.25f);
                    yield return null;
                    frames++;
                }
                Assert.That(brain.IsNavigationReady, Is.True,
                    "agent reaches the baked mesh on real frames");

                Assert.DoesNotThrow(() =>
                {
                    for (int i = 0; i < 30; i++)
                        brain.Tick(0.25f);
                }, "bounded scan over off-mesh waypoints must never recurse or throw");
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Patrol));
                Assert.That(brain.HasValidDestination, Is.False);
                Assert.That(brain.DestinationRequestCount, Is.EqualTo(0));
            }
            finally
            {
                CleanupNavData();
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Tick_ReachableWaypoint_KeepsIndexZero_WithoutPerFrameRepath()
        {
            EnsureIsolatedPlayBoot();
            yield return new EnterPlayMode();
            BuildTempSurface();
            try
            {
                SimpleZombieAI brain = SpawnZombieOnMesh(new Vector3(0f, 0f, -4f));
                GameObject near = AddWaypoint("Near", new Vector3(2f, 0f, -4f), tempRoot.transform);
                SetWaypoints(brain, near.transform);

                NavMeshAgent agent = brain.GetComponent<NavMeshAgent>();
                int frames = 0;
                while ((!brain.IsNavigationReady || !brain.HasValidDestination) && frames < 30)
                {
                    brain.Tick(0.25f);
                    yield return null;
                    frames++;
                }
                Assert.That(agent.enabled, Is.True,
                    "agent ships disabled and is enabled exactly once after a real NavMesh sample");
                Assert.That(brain.IsNavigationReady, Is.True,
                    "navigation reports ready only after real frames, never assumed");
                Assert.That(brain.HasValidDestination, Is.True,
                    "reachable waypoint routes on the baked mesh");

                for (int i = 0; i < 4; i++)
                {
                    brain.Tick(0.25f);
                    yield return null;
                }

                Assert.That(brain.CurrentWaypointIndex, Is.EqualTo(0),
                    "index 0 is kept until a true arrival; a missing path never counts as visited");
                Assert.That(brain.DestinationRequestCount, Is.LessThanOrEqualTo(2),
                    "no per-frame SetDestination reset while the path is pending");
            }
            finally
            {
                CleanupNavData();
            }
            yield return new ExitPlayMode();
        }
    }
}
