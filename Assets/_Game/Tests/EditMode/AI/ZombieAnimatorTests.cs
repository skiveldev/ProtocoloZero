using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace ProtocoloZero.AI.Tests
{
    public class ZombieAnimatorTests
    {
        private const string Folder = "Assets/_Game/Animations/Zombi/";
        private const string PrefabPath = "Assets/_Game/Prefabs/Enemies/Zombie_Shirtless.prefab";
        private const string VendorPath = "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab";

        [TestCase("Idle", "cbca680a06cc859478395fb444d2bbd0")]
        [TestCase("Walk", "8f5d6ac0f2dc9994e898386ea436bd08")]
        [TestCase("Run", "22e48e5f2213c5c4daaa315fe25d9c32")]
        public void SourceClip_RetainsHumanIdentity_AndExplicitLoopRootSettings(string name, string guid)
        {
            string path = Folder + "Zombi " + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
            Assert.That(importer.sourceAvatar, Is.Null);
            var settings = importer.clipAnimations.Single();
            Assert.That(settings.loopTime && settings.loopPose, Is.True);
            Assert.That(settings.lockRootRotation && settings.keepOriginalOrientation, Is.True);
            Assert.That(settings.lockRootHeightY && settings.heightFromFeet, Is.True);
            Assert.That(settings.keepOriginalPositionY, Is.False);
            Assert.That(settings.lockRootPositionXZ || settings.keepOriginalPositionXZ, Is.False);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var clip = assets.OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
            var avatar = assets.OfType<Avatar>().Single();
            Assert.That(avatar.isValid && avatar.isHuman, Is.True);
            Assert.That(clip.humanMotion && clip.isLooping && !clip.empty, Is.True);
            Assert.That(AnimationUtility.GetCurveBindings(clip).Length, Is.GreaterThan(0));
            Assert.That(clip.name, Is.EqualTo("mixamo.com"));
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string actualGuid, out long id);
            Assert.That(actualGuid, Is.EqualTo(guid));
            Assert.That(id, Is.EqualTo(-203655887218126122L));
        }

        [Test]
        public void Controller_HasThreeExactMotions_AndSixBoundedTransitions()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "ZombieLocomotion.controller");
            Assert.That(controller.layers.Length, Is.EqualTo(1));
            Assert.That(controller.parameters.Length, Is.EqualTo(1));
            Assert.That(controller.parameters[0].name, Is.EqualTo("Speed"));
            Assert.That(controller.parameters[0].type, Is.EqualTo(AnimatorControllerParameterType.Float));
            var machine = controller.layers[0].stateMachine;
            Assert.That(machine.defaultState.name, Is.EqualTo("Idle"));
            Assert.That(machine.anyStateTransitions, Is.Empty);
            Assert.That(machine.states.Select(s => s.state.name), Is.EquivalentTo(new[] { "Idle", "Walk", "Run" }));
            foreach (var child in machine.states)
            {
                var state = child.state;
                Assert.That(AssetDatabase.GetAssetPath(state.motion), Is.EqualTo(Folder + "Zombi " + state.name + ".fbx"));
                Assert.That(state.motion.name, Is.EqualTo("mixamo.com"));
                Assert.That(state.speed, Is.EqualTo(1f));
                Assert.That(state.transitions.Length, Is.EqualTo(2));
                foreach (var transition in state.transitions)
                {
                    Assert.That(transition.hasExitTime, Is.False);
                    Assert.That(transition.hasFixedDuration, Is.True);
                    Assert.That(transition.duration, Is.EqualTo(0.18f));
                    Assert.That(transition.destinationState, Is.Not.EqualTo(state));
                    foreach (var condition in transition.conditions)
                        Assert.That(condition.parameter, Is.EqualTo("Speed"));
                    string target = transition.destinationState.name;
                    if (target == "Idle") Check(transition, AnimatorConditionMode.Less, 0.1f);
                    else if (target == "Run") Check(transition, AnimatorConditionMode.Greater, 2f);
                    else
                    {
                        Assert.That(transition.conditions.Length, Is.EqualTo(2));
                        Check(transition, AnimatorConditionMode.Greater, 0.1f);
                        Check(transition, AnimatorConditionMode.Less, 2f);
                    }
                }
            }
        }

        private static void Check(AnimatorStateTransition transition, AnimatorConditionMode mode, float threshold)
        {
            Assert.That(transition.conditions.Count(c => c.mode == mode && c.threshold == threshold), Is.EqualTo(1));
        }

        [Test]
        public void OnlyOwnedShirtless_UsesExistingChildAnimator_AndOriginalAvatar()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var animator = root.GetComponentInChildren<Animator>(true);
            var adapter = root.GetComponent<ZombieAnimator>();
            Assert.That(root.GetComponent<Animator>(), Is.Null);
            Assert.That(root.GetComponentsInChildren<Animator>(true).Length, Is.EqualTo(1));
            Assert.That(adapter, Is.Not.Null);
            Assert.That(adapter.VisualAnimator, Is.EqualTo(animator));
            Assert.That(root.GetComponent<NavMeshAgent>(), Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.runtimeAnimatorController, Is.EqualTo(
                AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "ZombieLocomotion.controller")));
            Assert.That(AssetDatabase.GetAssetPath(animator.avatar), Is.EqualTo(
                "Assets/NewPunch/ShirtlessZombieFree/Models/ShirtlessZombie_FREE.fbx"));
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(VendorPath).GetComponentInChildren<Animator>(true);
            Assert.That(original.runtimeAnimatorController, Is.Null);
            Assert.That(original.avatar, Is.EqualTo(animator.avatar));
            var male = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Zombie_Male.prefab");
            Assert.That(male.GetComponent<ZombieAnimator>(), Is.Null);
            Assert.That(male.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.Null);
        }

        [TestCase(0f, "Idle")]
        [TestCase(1.5f, "Walk")]
        [TestCase(2.5f, "Run")]
        public void RealAvatar_ControllerEvaluatesState_WithoutMovingVisualRoot(float speed, string state)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var animator = root.GetComponentInChildren<Animator>(true);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Vector3 position = animator.transform.localPosition;
                Quaternion rotation = animator.transform.localRotation;
                animator.Rebind();
                animator.SetFloat("Speed", speed);
                for (int i = 0; i < 120; i++) animator.Update(1f / 30f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(state), Is.True);
                Assert.That(animator.transform.localPosition, Is.EqualTo(position));
                Assert.That(Quaternion.Angle(animator.transform.localRotation, rotation), Is.LessThan(0.01f));
                Assert.That(animator.applyRootMotion, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator MainWarehouse_ActualVelocityDrivesIdleWalkRun_AndStoppedIdle()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity", OpenSceneMode.Single);
            yield return new EnterPlayMode();
            Object.FindFirstObjectByType<ProtocoloZero.Gameplay.GameFlow>().BeginGame();
            var brain = Object.FindAnyObjectByType<SimpleZombieAI>();
            Assert.That(brain, Is.Not.Null);
            var adapter = brain.GetComponent<ZombieAnimator>();
            var agent = brain.GetComponent<NavMeshAgent>();
            var animator = adapter.VisualAnimator;
            Transform player = brain.PlayerTarget;
            Vector3 originalPlayer = player.position;
            var behaviours = player.GetComponentsInChildren<MonoBehaviour>();
            var enabled = new bool[behaviours.Length];
            var character = player.GetComponent<CharacterController>();
            bool characterEnabled = character != null && character.enabled;
            float scale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                for (int i = 0; i < behaviours.Length; i++)
                {
                    enabled[i] = behaviours[i].enabled;
                    behaviours[i].enabled = false;
                }
                if (character != null) character.enabled = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Vector3 localPosition = animator.transform.localPosition;
                yield return WaitForState(adapter, "Idle", 0f, 1f);
                yield return WaitForState(adapter, "Walk", 1.5f, 8f);
                Assert.That(adapter.Agent, Is.EqualTo(agent));
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Patrol));
                player.position = new Vector3(15, 0.08f, 9);
                Physics.SyncTransforms();
                yield return WaitForState(adapter, "Run", 2.5f, 3f);
                Assert.That(brain.CurrentState, Is.EqualTo(SimpleZombieAI.ZombieState.Chase));
                Assert.That(animator.transform.localPosition, Is.EqualTo(localPosition));
                Assert.That(Vector3.Distance(brain.transform.position, agent.nextPosition), Is.LessThan(0.02f));
                // Pause only in memory: requested limit remains 2.5, actual motion stops.
                brain.enabled = false;
                agent.isStopped = true;
                yield return WaitForState(adapter, "Idle", 0f, 2f);
                Assert.That(agent.speed, Is.EqualTo(2.5f));
                Assert.That(adapter.ObservedSpeed, Is.Zero);
                agent.enabled = false;
                adapter.Tick(0.1f);
                Assert.That(adapter.ObservedSpeed, Is.Zero);
                float value = animator.GetFloat("Speed");
                adapter.Tick(float.NaN);
                adapter.Tick(-1f);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(value));
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(animator.transform.localPosition, Is.EqualTo(localPosition));
                Assert.That(Object.FindAnyObjectByType<SceneNavMeshBootstrap>().UsedBakedData, Is.True);
                Assert.That(SceneNavMeshBootstrap.StartupBuildCount, Is.Zero);
            }
            finally
            {
                player.position = originalPlayer;
                if (character != null) character.enabled = characterEnabled;
                for (int i = 0; i < behaviours.Length; i++)
                    if (behaviours[i] != null) behaviours[i].enabled = enabled[i];
                if (agent != null) agent.enabled = true;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                if (brain != null) brain.enabled = true;
                Time.timeScale = scale;
                Physics.SyncTransforms();
            }
            yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        private static IEnumerator WaitForState(ZombieAnimator adapter, string state, float speed, float limit)
        {
            float simulation = Time.time;
            float wall = Time.realtimeSinceStartup;
            while (!Matches(adapter, state, speed) && Time.time - simulation < limit)
            {
                Assert.That(Time.realtimeSinceStartup - wall, Is.LessThan(60f), "animation wall watchdog");
                yield return null;
            }
            Assert.That(Matches(adapter, state, speed), Is.True,
                $"{state}: observed={adapter.ObservedSpeed} parameter={adapter.VisualAnimator.GetFloat("Speed")}");
            Debug.Log($"[P06B-MOTION] {state} velocity={adapter.ObservedSpeed:F4} Speed={adapter.VisualAnimator.GetFloat("Speed"):F4} rootMotion={adapter.VisualAnimator.applyRootMotion}");
        }

        private static bool Matches(ZombieAnimator adapter, string state, float speed)
        {
            return Mathf.Abs(adapter.ObservedSpeed - speed) < 0.1f
                && Mathf.Abs(adapter.VisualAnimator.GetFloat("Speed") - speed) < 0.1f
                && !adapter.VisualAnimator.IsInTransition(0)
                && adapter.VisualAnimator.GetCurrentAnimatorStateInfo(0).IsName(state);
        }
    }
}
