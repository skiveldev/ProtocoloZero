using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using ProtocoloZero.Gameplay;

namespace ProtocoloZero.AI.Tests
{
    /// <summary>
    /// Static validation of the Phase 06 integration: both wrapper prefabs
    /// carry a matching agent + trigger capsule + shared SimpleZombieAI over a
    /// distinct nested vendor visual, and the main scene holds exactly one
    /// placed zombie with player/waypoint wiring plus the NavMesh bootstrap
    /// and gate carve. No PlayMode, no scene saves, no vendor rewrites.
    /// </summary>
    public class ZombieAIValidationTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";
        private const string ShirtlessPrefabPath = "Assets/_Game/Prefabs/Enemies/Zombie_Shirtless.prefab";
        private const string MalePrefabPath = "Assets/_Game/Prefabs/Enemies/Zombie_Male.prefab";

        private static void AssertSharedAgentConfig(GameObject root, string label)
        {
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            Assert.That(agent, Is.Not.Null, $"{label} carries a NavMeshAgent");
            Assert.That(agent.enabled, Is.False,
                $"{label} agent ships disabled; the brain enables it exactly once after a real NavMesh sample");
            Assert.That(agent.radius, Is.EqualTo(0.35f), $"{label} agent radius");
            Assert.That(agent.height, Is.EqualTo(1.8f), $"{label} agent height");
            Assert.That(agent.speed, Is.EqualTo(1.5f), $"{label} patrol speed");
            Assert.That(agent.agentTypeID, Is.EqualTo(0), $"{label} default agent type");
            Assert.That(agent.areaMask, Is.EqualTo(-1), $"{label} all areas");
            Assert.That(agent.stoppingDistance, Is.EqualTo(0.8f), $"{label} stopping distance");

            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            Assert.That(capsule, Is.Not.Null, $"{label} carries a CapsuleCollider");
            Assert.That(capsule.isTrigger, Is.True, $"{label} capsule never blocks the player");
            Assert.That(capsule.radius, Is.EqualTo(0.35f));
            Assert.That(capsule.height, Is.EqualTo(1.8f));

            SimpleZombieAI brain = root.GetComponent<SimpleZombieAI>();
            Assert.That(brain, Is.Not.Null, $"{label} shares SimpleZombieAI");
            Assert.That(brain.DetectRange, Is.EqualTo(8f));
            Assert.That(brain.LoseRange, Is.EqualTo(12f));
            Assert.That(brain.DecisionInterval, Is.EqualTo(0.25f));
            Assert.That(brain.PatrolSpeed, Is.EqualTo(1.5f));
            Assert.That(brain.ChaseSpeed, Is.EqualTo(2.5f));
            Assert.That(brain.WaypointWaitTime, Is.EqualTo(1.5f));
        }

        [Test]
        public void BothPrefabs_ShareLogic_OverDistinctVendorVisuals()
        {
            GameObject shirtless = PrefabUtility.LoadPrefabContents(ShirtlessPrefabPath);
            GameObject male = PrefabUtility.LoadPrefabContents(MalePrefabPath);
            try
            {
                Assert.That(shirtless, Is.Not.Null, "shirtless wrapper prefab loads");
                Assert.That(male, Is.Not.Null, "male wrapper prefab loads");
                AssertSharedAgentConfig(shirtless, "Zombie_Shirtless");
                AssertSharedAgentConfig(male, "Zombie_Male");

                Animator shirtlessAnimator = shirtless.GetComponentInChildren<Animator>(true);
                Animator maleAnimator = male.GetComponentInChildren<Animator>(true);
                Assert.That(shirtlessAnimator, Is.Not.Null, "shirtless keeps its vendor Animator");
                Assert.That(maleAnimator, Is.Not.Null, "male keeps its vendor Animator");
                Assert.That(shirtlessAnimator.applyRootMotion, Is.False,
                    "root motion overridden off on the wrapper, vendor untouched");
                Assert.That(maleAnimator.applyRootMotion, Is.False);
                Assert.That(shirtlessAnimator.avatar, Is.Not.Null, "shirtless avatar present");
                Assert.That(maleAnimator.avatar, Is.Not.Null, "male avatar present");
                Assert.That(shirtlessAnimator.avatar, Is.Not.SameAs(maleAnimator.avatar),
                    "distinct vendor models behind shared logic");

                Object shirtlessSource = PrefabUtility.GetCorrespondingObjectFromSource(
                    shirtlessAnimator.gameObject);
                Object maleSource = PrefabUtility.GetCorrespondingObjectFromSource(
                    maleAnimator.gameObject);
                Assert.That(shirtlessSource, Is.Not.Null, "shirtless visual stays a nested instance");
                Assert.That(maleSource, Is.Not.Null, "male visual stays a nested instance");
                Assert.That(AssetDatabase.GetAssetPath(shirtlessSource),
                    Does.Contain("ShirtlessZombie"));
                Assert.That(AssetDatabase.GetAssetPath(maleSource),
                    Does.Contain("ZombieMale_AAB"));

                Assert.That(shirtless.GetComponentsInChildren<NavMeshAgent>(true).Length,
                    Is.EqualTo(1), "exactly one agent per wrapper, none on the vendor child");
                Assert.That(male.GetComponentsInChildren<NavMeshAgent>(true).Length,
                    Is.EqualTo(1));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(shirtless);
                PrefabUtility.UnloadPrefabContents(male);
            }
        }

        [Test]
        public void MainScene_HoldsExactlyOneWiredZombie()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var brains = new System.Collections.Generic.List<SimpleZombieAI>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    brains.AddRange(root.GetComponentsInChildren<SimpleZombieAI>(true));
                brains.RemoveAll(b => !b.gameObject.scene.IsValid());
                Assert.That(brains.Count, Is.EqualTo(1), "exactly one zombie placed");
                SimpleZombieAI brain = brains[0];

                var serialized = new SerializedObject(brain);
                Transform player = (Transform)serialized.FindProperty("playerTarget")
                    .objectReferenceValue;
                Assert.That(player, Is.Not.Null, "player reference assigned in scene");
                Assert.That(player.name, Is.EqualTo("Player_Renegade"));

                Assert.That(brain.Waypoints, Is.Not.Null);
                Assert.That(brain.Waypoints.Length, Is.EqualTo(4), "four patrol waypoints");
                foreach (Transform point in brain.Waypoints)
                    Assert.That(point, Is.Not.Null, "every waypoint reference assigned");

                NavMeshAgent agent = brain.GetComponent<NavMeshAgent>();
                Assert.That(agent.radius, Is.EqualTo(0.35f));
                Assert.That(agent.height, Is.EqualTo(1.8f));
                Assert.That(agent.enabled, Is.False,
                    "placed zombie inherits the disabled-until-real-NavMesh contract");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void MainScene_NavBootstrap_And_GateCarve_Wired()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneNavMeshBootstrap bootstrap =
                    Object.FindFirstObjectByType<SceneNavMeshBootstrap>();
                Assert.That(bootstrap, Is.Not.Null, "scene owns the NavMesh bootstrap");
                Assert.That(bootstrap.Surface, Is.Not.Null, "bootstrap references its surface");
                Assert.That(bootstrap.Surface.useGeometry,
                    Is.EqualTo(NavMeshCollectGeometry.PhysicsColliders),
                    "bake uses real static collider geometry");
                Assert.That(bootstrap.Surface.collectObjects,
                    Is.EqualTo(Unity.AI.Navigation.CollectObjects.All));
                Assert.That(bootstrap.Surface.ignoreNavMeshAgent, Is.True,
                    "agents auto-excluded from the bake");
                Assert.That(bootstrap.Surface.ignoreNavMeshObstacle, Is.True);

                GateNavigationObstacle gateObstacle =
                    Object.FindFirstObjectByType<GateNavigationObstacle>();
                Assert.That(gateObstacle, Is.Not.Null, "owned gate carve obstacle exists");
                var obstacle = gateObstacle.GetComponent<NavMeshObstacle>();
                Assert.That(obstacle, Is.Not.Null);
                Assert.That(obstacle.shape, Is.EqualTo(NavMeshObstacleShape.Box),
                    "gate carve matches the box gate, never a capsule");
                Assert.That(obstacle.size, Is.EqualTo(new Vector3(4f, 3f, 0.5f)),
                    "gate carve covers the full gate blocker");
                Assert.That(obstacle.carving, Is.True, "gate carves while locked");
                var serialized = new SerializedObject(gateObstacle);
                Assert.That(serialized.FindProperty("progress").objectReferenceValue,
                    Is.EqualTo(Object.FindFirstObjectByType<LevelProgress>()),
                    "carve listens to the existing Phase 05 manager");
                Assert.That(serialized.FindProperty("gateTransform").objectReferenceValue,
                    Is.Not.Null, "carve mirrors the actual gate transform");

                GameObject navigation = GameObject.Find("Phase06_Navigation");
                Assert.That(navigation, Is.Not.Null);
                GameObject points = GameObject.Find("ZombiePatrol_Points");
                Assert.That(points, Is.Not.Null);
                Assert.That(points.transform.childCount, Is.EqualTo(4));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void MainScene_Phase05_PreservationSnapshot()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var cards = new System.Collections.Generic.List<AccessCard>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    cards.AddRange(root.GetComponentsInChildren<AccessCard>(true));
                cards.RemoveAll(c => !c.gameObject.scene.IsValid());
                Assert.That(cards.Count, Is.EqualTo(3), "Phase 05 cards preserved");

                Assert.That(Object.FindFirstObjectByType<SecurityPanel>(), Is.Not.Null);
                Assert.That(Object.FindFirstObjectByType<ExitGate>(), Is.Not.Null);
                Assert.That(Object.FindFirstObjectByType<VictoryZone>(), Is.Not.Null);
                Assert.That(Object.FindFirstObjectByType<LevelProgress>(), Is.Not.Null);

                GameObject player = GameObject.Find("Player_Renegade");
                Assert.That(player, Is.Not.Null, "player instance preserved");
                Assert.That(player.transform.position.x, Is.EqualTo(8.263f).Within(0.001f),
                    "player start untouched");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
