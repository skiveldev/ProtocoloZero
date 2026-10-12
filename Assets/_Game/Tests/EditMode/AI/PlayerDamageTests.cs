using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ProtocoloZero.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProtocoloZero.AI.Tests
{
    public sealed class PlayerDamageTests
    {
        private GameObject player;
        private GameObject zombie;
        private GameObject wall;
        private PlayerHealth health;
        private SimpleZombieAI brain;
        private ZombieProximityDamage attack;
        private static int runtimeDeaths;

        private static void CountRuntimeDeath()
        {
            runtimeDeaths++;
        }

        [SetUp]
        public void SetUp()
        {
            // UnityTest setup runs again after EnterPlayMode/domain reload.
            // Never introduce fixture actors into the real-scene harness.
            if (TestContext.CurrentContext.Test.Name == nameof(MainSceneRuntime_ChaseAppliesDamageAndDeathOnce))
                return;
            player = new GameObject("Health test player");
            player.SetActive(false);
            health = player.AddComponent<PlayerHealth>();
            InvokeAwake(health);
            player.SetActive(true);
            zombie = new GameObject("Damage test zombie");
            zombie.SetActive(false);
            brain = zombie.AddComponent<SimpleZombieAI>();
            zombie.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            var serialized = new SerializedObject(brain);
            serialized.FindProperty("playerTarget").objectReferenceValue = player.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            attack = zombie.AddComponent<ZombieProximityDamage>();
            InvokeAwake(brain);
            InvokeAwake(attack);
            zombie.SetActive(true);
            SetBrainState(SimpleZombieAI.ZombieState.Chase, true);
            player.transform.position = Vector3.right;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(zombie);
            Object.DestroyImmediate(player);
        }

        private static void InvokeAwake(Component component)
        {
            component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(component, null);
        }

        // Damage policy tests isolate the already-tested navigation producer.
        private void SetBrainState(SimpleZombieAI.ZombieState state, bool ready)
        {
            typeof(SimpleZombieAI).GetField("<CurrentState>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(brain, state);
            typeof(SimpleZombieAI).GetField("<IsNavigationReady>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(brain, ready);
        }

        [Test]
        public void HealthStartsFull_RejectsNonPositiveDamage()
        {
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Assert.That(health.MaxHealth, Is.EqualTo(100));
            Assert.That(health.TakeDamage(0), Is.False);
            Assert.That(health.TakeDamage(int.MinValue), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void DamageClampsAndNotifies_DeathOccursOnce()
        {
            int changes = 0;
            int deaths = 0;
            health.HealthChanged += (current, maximum) =>
            {
                changes++;
                Assert.That(current, Is.InRange(0, maximum));
            };
            health.Died += () => deaths++;
            Assert.That(health.TakeDamage(20), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(80));
            Assert.That(health.TakeDamage(int.MaxValue), Is.True);
            Assert.That(health.TakeDamage(20), Is.False);
            Assert.That(health.IsDead, Is.True);
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void ReenableAndRepeatedAwake_DoNotHeal()
        {
            health.TakeDamage(20);
            player.SetActive(false);
            player.SetActive(true);
            InvokeAwake(health);
            Assert.That(health.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void ChaseDamageHasCooldown_AndNoLongFrameBurst()
        {
            Assert.That(attack.Tick(0.01f), Is.True);
            Assert.That(attack.Tick(0.5f), Is.False);
            Assert.That(attack.Tick(0.5f), Is.True);
            Assert.That(attack.Tick(100f), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(40));
        }

        [Test]
        public void PatrolOffMeshOutOfRangeAndDifferentFloor_DoNotDamage()
        {
            SetBrainState(SimpleZombieAI.ZombieState.Patrol, true);
            Assert.That(attack.Tick(1f), Is.False);
            SetBrainState(SimpleZombieAI.ZombieState.Chase, false);
            Assert.That(attack.Tick(1f), Is.False);
            SetBrainState(SimpleZombieAI.ZombieState.Chase, true);
            player.transform.position = Vector3.right * 2f;
            Assert.That(attack.Tick(1f), Is.False);
            player.transform.position = Vector3.up * 3f;
            Assert.That(attack.Tick(1f), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void WallBlocksDamage_TriggerAndOwnBodiesDoNot()
        {
            wall = new GameObject("Damage blocking wall");
            wall.transform.position = new Vector3(0.5f, 1f, 0f);
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.1f, 2f, 2f);
            Physics.SyncTransforms();
            Assert.That(attack.Tick(1f), Is.False);
            collider.isTrigger = true;
            player.AddComponent<BoxCollider>();
            zombie.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.That(attack.Tick(1f), Is.True);
        }

        [Test]
        public void DisabledMissingDeadAndPausedTargets_DoNotDamage()
        {
            Assert.That(attack.Tick(0f), Is.False);
            Assert.That(attack.Tick(float.NaN), Is.False);
            player.SetActive(false);
            Assert.That(attack.Tick(1f), Is.False);
            player.SetActive(true);
            health.TakeDamage(100);
            Assert.That(attack.Tick(1f), Is.False);
            Object.DestroyImmediate(health);
            Assert.That(attack.Tick(1f), Is.False);
        }

        [Test]
        public void MainSceneBindsHealthAndDamageWithoutChangingVendorPrefabs()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity", OpenSceneMode.Additive);
            try
            {
                PlayerHealth sceneHealth = null;
                ZombieProximityDamage sceneAttack = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponent<PlayerHealth>() != null)
                        sceneHealth = root.GetComponent<PlayerHealth>();
                    if (root.GetComponent<ZombieProximityDamage>() != null)
                        sceneAttack = root.GetComponent<ZombieProximityDamage>();
                }
                Assert.That(sceneHealth, Is.Not.Null);
                Assert.That(sceneAttack, Is.Not.Null);
                Assert.That(sceneAttack.GetComponent<SimpleZombieAI>().PlayerTarget,
                    Is.EqualTo(sceneHealth.transform));
                Assert.That(sceneHealth.MaxHealth, Is.EqualTo(100));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [UnityTest]
        public IEnumerator MainSceneRuntime_ChaseAppliesDamageAndDeathOnce()
        {
            Object.DestroyImmediate(zombie);
            Object.DestroyImmediate(player);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity",
                OpenSceneMode.Single);
            yield return new EnterPlayMode();
            Object.FindFirstObjectByType<GameFlow>().BeginGame();
            var sceneBrain = Object.FindFirstObjectByType<SimpleZombieAI>();
            var sceneHealth = sceneBrain.PlayerTarget.GetComponent<PlayerHealth>();
            var movement = sceneHealth.GetComponent<ProtocoloZero.Gameplay.PlayerInteractor>();
            // Disable movement components by type name without a Gameplay -> Player dependency.
            foreach (MonoBehaviour behaviour in sceneHealth.GetComponents<MonoBehaviour>())
                if (behaviour.GetType().Name == "PlayerMovement")
                    behaviour.enabled = false;
            if (movement != null)
                movement.enabled = false;
            sceneHealth.GetComponent<CharacterController>().enabled = false;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!sceneBrain.IsNavigationReady && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(sceneBrain.IsNavigationReady, Is.True);
            sceneBrain.PlayerTarget.position = sceneBrain.transform.position + Vector3.right * 0.9f;
            Physics.SyncTransforms();
            runtimeDeaths = 0;
            sceneHealth.Died += CountRuntimeDeath;
            deadline = Time.realtimeSinceStartup + 60f;
            bool observedChase = false;
            while (!sceneHealth.IsDead && Time.realtimeSinceStartup < deadline)
            {
                observedChase |= sceneBrain.CurrentState == SimpleZombieAI.ZombieState.Chase;
                yield return null;
            }
            Assert.That(observedChase, Is.True, "damage occurs during chase; defeat then disables the brain");
            Assert.That(sceneHealth.CurrentHealth, Is.Zero);
            Assert.That(runtimeDeaths, Is.EqualTo(1));
            Assert.That(sceneHealth.TakeDamage(20), Is.False);
            Debug.Log("[PA4-DAMAGE] Real scene chase reduced 100 health to zero; death event once.");
            yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
