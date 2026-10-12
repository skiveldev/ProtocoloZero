using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace ProtocoloZero.AI.Tests
{
    public class MainLevelZombieNavigationTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";
        private const string DataPath = "Assets/_Game/Navigation/ProtocoloZero_Nivel01_NavMesh.asset";
        private static readonly NavMeshQueryFilter Filter = new NavMeshQueryFilter
        {
            agentTypeID = 0, areaMask = -1
        };

        [UnityTest]
        public IEnumerator MainWarehouse_PatrolMoves_ChaseMoves_LossReturns_WithoutRuntimeBuild()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            Object.FindFirstObjectByType<ProtocoloZero.Gameplay.GameFlow>().BeginGame();
            var brain = Object.FindFirstObjectByType<SimpleZombieAI>();
            Assert.That(brain, Is.Not.Null, "main scene must contain SimpleZombieAI");
            var bootstrap = Object.FindFirstObjectByType<SceneNavMeshBootstrap>();
            Assert.That(bootstrap, Is.Not.Null, "main scene must contain SceneNavMeshBootstrap");
            var agent = brain.GetComponent<NavMeshAgent>();
            Transform player = brain.PlayerTarget;
            Assert.That(player, Is.Not.Null, "main scene must wire the player target");
            Vector3 originalPlayer = player.position;
            var behaviours = player.GetComponentsInChildren<MonoBehaviour>();
            var enabled = new bool[behaviours.Length];
            var controller = player.GetComponent<CharacterController>();
            bool controllerEnabled = controller != null && controller.enabled;
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                for (int i = 0; i < behaviours.Length; i++)
                {
                    enabled[i] = behaviours[i].enabled;
                    behaviours[i].enabled = false;
                }
                if (controller != null) controller.enabled = false;
                Assert.That(Object.FindObjectsByType<SimpleZombieAI>(FindObjectsSortMode.None).Length,
                    Is.EqualTo(1));
                yield return WaitForSimulation(brain, CompletionCriterion.Ready, Vector3.zero, 5f, 20f, "ready");
                Assert.That(brain.IsNavigationReady, Is.True);
                Assert.That(bootstrap.HasBuilt && bootstrap.UsedBakedData, Is.True);
                Assert.That(SceneNavMeshBootstrap.StartupBuildCount, Is.Zero);
                Assert.That(brain.transform.position.y, Is.EqualTo(0.08f).Within(0.02f));
                Vector3 start = brain.transform.position;
                yield return WaitForSimulation(brain,
                    CompletionCriterion.Displacement, start, 8f, 60f, "patrol");
                float patrol = Vector3.Distance(start, brain.transform.position);
                Assert.That(patrol, Is.GreaterThan(0.5f), "real authored spawn patrol must move");
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Patrol));
                Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
                Debug.Log($"[P06-MOTION] patrol start={start} end={brain.transform.position} displacement={patrol:F4}");

                // Existing WP_02 is measured ground, reachable and inside detection range.
                player.position = ProveGround(brain.Waypoints[1].position, brain, "chase target WP_02");
                Physics.SyncTransforms();
                yield return WaitForSimulation(brain,
                    CompletionCriterion.Chase, Vector3.zero, 2f, 60f, "detect");
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Chase));
                Assert.That(agent.speed, Is.EqualTo(2.5f));
                Debug.Log($"[P06-MOTION] detection distance={brain.EvaluateDistanceToPlayer():F4} " +
                    $"threshold={brain.DetectRange} state={brain.CurrentState} speed={agent.speed}");
                start = brain.transform.position;
                yield return WaitForSimulation(brain,
                    CompletionCriterion.Displacement, start, 3f, 60f, "chase");
                float chase = Vector3.Distance(start, brain.transform.position);
                Assert.That(chase, Is.GreaterThan(0.5f));
                Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete));
                Debug.Log($"[P06-MOTION] chase displacement={chase:F4} speed={agent.speed}");

                // State-only setup: warp to proven WP_03, not a claim that patrol walked there.
                Vector3 returnAnchor = ProveGround(brain.Waypoints[2].position, brain, "return WP_03");
                Assert.That(agent.Warp(returnAnchor), Is.True);
                player.position = originalPlayer;
                Physics.SyncTransforms();
                Assert.That(brain.EvaluateDistanceToPlayer(), Is.GreaterThan(12f));
                brain.Tick(0.25f);
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Returning));
                Assert.That(agent.speed, Is.EqualTo(1.5f));
                Debug.Log($"[P06-MOTION] loss distance={brain.EvaluateDistanceToPlayer():F4} " +
                    $"threshold={brain.LoseRange} state={brain.CurrentState} speed={agent.speed}");
                yield return WaitForSimulation(brain,
                    CompletionCriterion.Patrol, Vector3.zero, 3f, 60f, "return");
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Patrol));
                Debug.Log($"[P06-MOTION] Chase->Returning->Patrol lossDistance={brain.EvaluateDistanceToPlayer():F4} " +
                    $"baked={bootstrap.UsedBakedData} runtimeBuilds={SceneNavMeshBootstrap.StartupBuildCount}");
            }
            finally
            {
                player.position = originalPlayer;
                if (controller != null) controller.enabled = controllerEnabled;
                for (int i = 0; i < behaviours.Length; i++)
                    if (behaviours[i] != null) behaviours[i].enabled = enabled[i];
                Time.timeScale = previousScale;
                Physics.SyncTransforms();
            }
            yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        private enum CompletionCriterion { Ready, Displacement, Chase, Patrol }

        private static bool IsCompleted(SimpleZombieAI brain, CompletionCriterion criterion,
            Vector3 displacementOrigin)
        {
            switch (criterion)
            {
                case CompletionCriterion.Ready: return brain.IsNavigationReady;
                case CompletionCriterion.Displacement:
                    return Vector3.Distance(displacementOrigin, brain.transform.position) > 0.5f;
                case CompletionCriterion.Chase:
                    return brain.CurrentState == SimpleZombieAI.ZombieState.Chase;
                case CompletionCriterion.Patrol:
                    return brain.CurrentState == SimpleZombieAI.ZombieState.Patrol;
                default: throw new System.ArgumentOutOfRangeException(nameof(criterion));
            }
        }

        private static IEnumerator WaitForSimulation(SimpleZombieAI brain, CompletionCriterion criterion,
            Vector3 displacementOrigin, float simulationLimit, float wallLimit, string label)
        {
            float startSimulation = Time.time;
            float startWall = Time.realtimeSinceStartup;
            int frames = 0;
            // Frame duration varies in batch mode: frame count is evidence, not a motion deadline.
            while (Time.time - startSimulation < simulationLimit && !IsCompleted(brain, criterion, displacementOrigin))
            {
                if (Time.realtimeSinceStartup - startWall >= wallLimit)
                    Assert.Fail($"{label} wall-clock watchdog expired: " +
                        TimingStatus(brain, startSimulation, startWall, frames));
                frames++;
                yield return null;
            }
            Debug.Log($"[P06-TIMING] {label} completed={IsCompleted(brain, criterion, displacementOrigin)} simulationLimit={simulationLimit} " +
                TimingStatus(brain, startSimulation, startWall, frames));
            Assert.That(IsCompleted(brain, criterion, displacementOrigin), Is.True,
                label + " simulation deadline expired");
        }

        private static string TimingStatus(SimpleZombieAI brain, float startSimulation, float startWall, int frames)
        {
            var agent = brain.GetComponent<NavMeshAgent>();
            bool usable = agent.enabled && agent.isOnNavMesh;
            string navigation = usable
                ? $"path={agent.pathStatus} pending={agent.pathPending} hasPath={agent.hasPath} " +
                  $"stopped={agent.isStopped} remaining={agent.remainingDistance:F4} velocity={agent.velocity}"
                : $"agentEnabled={agent.enabled} onMesh={agent.isOnNavMesh}";
            return $"sim={Time.time - startSimulation:F6} wall={Time.realtimeSinceStartup - startWall:F6} " +
                $"frames={frames} timeScale={Time.timeScale} state={brain.CurrentState} " +
                $"wp={brain.CurrentWaypointIndex} ready={brain.IsNavigationReady} valid={brain.HasValidDestination} " +
                $"requests={brain.DestinationRequestCount} position={brain.transform.position} " +
                $"playerDistance={brain.EvaluateDistanceToPlayer():F4} speed={agent.speed} {navigation}";
        }

        [Test]
        public void OldShelfSpawn_RejectsUpwardStartupWarp_LeavesAgentDisabled()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var brain = Object.FindFirstObjectByType<SimpleZombieAI>();
                var agent = brain.GetComponent<NavMeshAgent>();
                var surface = Object.FindFirstObjectByType<SceneNavMeshBootstrap>().Surface;
                Assert.That(surface.navMeshData, Is.Not.Null);
                surface.AddData();
                brain.transform.position = new Vector3(15, 0, 12);
                Physics.SyncTransforms();
                Assert.That(NavMesh.SamplePosition(brain.transform.position, out NavMeshHit hit, 5f, Filter), Is.True);
                Assert.That(hit.position.y, Is.GreaterThan(4f));
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[SimpleZombieAI\].*startup NavMesh hit.*staying off-mesh, no upward warp\."));
                for (int i = 0; i < 20; i++) brain.Tick(0.25f);
                Assert.That(agent.enabled, Is.False);
                Assert.That(brain.IsNavigationReady, Is.False);
                Assert.That(brain.transform.position, Is.EqualTo(new Vector3(15, 0, 12)));
                Assert.That(brain.DestinationRequestCount, Is.Zero);
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); }
        }

        [Test]
        public void BakedWarehouse_ExistingWaypointsAreClearGround_ConnectedToPlayer()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Unity.AI.Navigation.NavMeshSurface surface = null;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var brain = Object.FindFirstObjectByType<SimpleZombieAI>();
                Assert.That(Object.FindObjectsByType<SimpleZombieAI>(FindObjectsSortMode.None).Length,
                    Is.EqualTo(1));
                Assert.That(brain, Is.Not.Null);
                Assert.That(brain.transform.position, Is.EqualTo(new Vector3(9, 0, 9)),
                    "authored root on proven WP_01 ground, not shelf");
                surface = Object.FindFirstObjectByType<SceneNavMeshBootstrap>().Surface;
                var data = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
                Assert.That(data, Is.Not.Null, "reuse the completed engine bake");
                surface.RemoveData();
                surface.navMeshData = data;
                surface.AddData();
                Physics.SyncTransforms();
                Assert.That(NavMesh.CalculateTriangulation().vertices.Length, Is.GreaterThan(0));
                Assert.That(brain.Waypoints.Length, Is.EqualTo(4));
                Assert.That(brain.Waypoints[0].position, Is.EqualTo(new Vector3(9, 0, 9)));
                var hits = new Vector3[4];
                for (int i = 0; i < hits.Length; i++)
                    hits[i] = ProveGround(brain.Waypoints[i].position, brain, brain.Waypoints[i].name);
                Vector3 player = ProveGround(brain.PlayerTarget.position, brain, "player");
                ProvePath(player, hits[0], "player->WP_01 candidate");
                for (int i = 0; i < hits.Length; i++)
                {
                    ProvePath(hits[0], hits[i], $"candidate->WP_{i + 1:00}");
                    ProvePath(hits[i], hits[(i + 1) % hits.Length], $"loop {i + 1}->{(i + 1) % 4 + 1}");
                }
            }
            finally
            {
                if (surface != null) surface.RemoveData();
                if (System.Array.Exists(setup, s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
        }

        private static Vector3 ProveGround(Vector3 position, SimpleZombieAI brain, string label)
        {
            var agent = brain.GetComponent<NavMeshAgent>();
            RaycastHit[] rays = Physics.RaycastAll(position + Vector3.up * 2f, Vector3.down,
                3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(rays, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit floor = default;
            bool found = false;
            foreach (var ray in rays)
            {
                if (ray.transform.IsChildOf(brain.transform)
                    || ray.transform.IsChildOf(brain.PlayerTarget)) continue;
                floor = ray;
                found = true;
                break;
            }
            Assert.That(found, Is.True, label + " real collider below");
            Assert.That(floor.collider.name, Does.Contain("Floor"), label + " floor, not shelf");
            Assert.That(floor.point.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(Vector3.Dot(floor.normal, Vector3.up), Is.GreaterThan(0.99f));
            Assert.That(NavMesh.SamplePosition(position, out NavMeshHit nav, 0.5f, Filter), Is.True);
            Assert.That(nav.position.y, Is.EqualTo(0.08f).Within(0.02f));
            var overlaps = Physics.OverlapCapsule(nav.position + Vector3.up * (agent.radius + 0.05f),
                nav.position + Vector3.up * (agent.height - agent.radius), agent.radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
                Assert.That(overlap.transform.IsChildOf(brain.transform)
                    || overlap.transform.IsChildOf(brain.PlayerTarget), Is.True,
                    label + " blocked by " + Hierarchy(overlap.transform));
            Debug.Log($"[P06-GROUND] {label} collider={Hierarchy(floor.transform)} " +
                $"bounds={floor.collider.bounds} top={floor.point.y:F4} normal={floor.normal} " +
                $"nav={nav.position} radius={agent.radius} height={agent.height} clear=True");
            return nav.position;
        }

        private static string Hierarchy(Transform node)
        {
            string path = node.name;
            while (node.parent != null) { node = node.parent; path = node.name + "/" + path; }
            return path;
        }

        private static void ProvePath(Vector3 from, Vector3 to, string label)
        {
            var path = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(from, to, Filter, path), Is.True, label);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label);
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            Debug.Log($"[P06-GROUND] {label} PathComplete length={length:F4} corners={path.corners.Length}");
        }
    }
}
