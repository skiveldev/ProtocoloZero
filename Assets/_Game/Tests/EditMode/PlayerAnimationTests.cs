using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProtocoloZero.Player.Tests
{
    public class PlayerAnimationTests
    {
        private const string ClipsDirectory = "Assets/_Game/Animations/Renegade/";
        private const string TargetGuid = "26489012c8ee8af4d84156e5e5ac1035";
        private const string ControllerPath = ClipsDirectory + "Renegade.controller";
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";

        [Test]
        public void AnimationFolderContainsOnlyFinalRifleSetAndController()
        {
            var expected = new[]
            {
                "Renegade.controller", "Renegade.controller.meta",
                "Rifle Idle.fbx", "Rifle Idle.fbx.meta",
                "Rifle Walk.fbx", "Rifle Walk.fbx.meta",
                "Rifle Run.fbx", "Rifle Run.fbx.meta"
            };
            var actual = System.IO.Directory.GetFiles(ClipsDirectory)
                .Select(System.IO.Path.GetFileName).OrderBy(name => name).ToArray();
            Assert.That(actual, Is.EqualTo(expected.OrderBy(name => name).ToArray()));
        }

        [TestCase("Rifle Idle")]
        [TestCase("Rifle Walk")]
        [TestCase("Rifle Run")]
        public void SourceClipAndAvatarsAreHumanoidAndLooping(string name)
        {
            var targetPath = AssetDatabase.GUIDToAssetPath(TargetGuid);
            Assert.That(targetPath, Is.Not.Empty, "Target Avatar asset is missing");
            var target = AssetDatabase.LoadAllAssetsAtPath(targetPath).OfType<Avatar>()
                .FirstOrDefault(avatar => avatar.isHuman && avatar.isValid);
            Assert.That(target, Is.Not.Null, "Target Avatar is not valid Humanoid");

            string path = ClipsDirectory + name + ".fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var clip = assets.OfType<AnimationClip>()
                .SingleOrDefault(asset => !asset.name.Contains("__preview"));
            Assert.That(clip, Is.Not.Null, name + " imported clip missing");
            Assert.That(clip.humanMotion, Is.True, name + " must be Humanoid motion");
            Assert.That(assets.OfType<Avatar>(), Is.Not.Empty, name + " source Avatar missing");
            foreach (var source in assets.OfType<Avatar>())
            {
                Assert.That(source.isValid && source.isHuman, Is.True, name + " source Avatar invalid");
            }
            Assert.That(clip.isLooping, Is.True, name + " must loop");
        }

        [Test]
        public void LocomotionUsesActualClipsAtMovementSpeeds()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters.Select(parameter => parameter.name), Does.Contain("Speed"));
            Assert.That(controller.parameters.Single(parameter => parameter.name == "Speed").type,
                Is.EqualTo(AnimatorControllerParameterType.Float));
            var layer = controller.layers.Single();
            var state = layer.stateMachine.defaultState;
            Assert.That(state.name, Is.EqualTo("Locomotion"));
            Assert.That(layer.stateMachine.anyStateTransitions, Is.Empty);
            var tree = state.motion as BlendTree;
            Assert.That(tree, Is.Not.Null);
            Assert.That(tree.blendType, Is.EqualTo(BlendTreeType.Simple1D));
            Assert.That(tree.blendParameter, Is.EqualTo("Speed"));
            Assert.That(tree.useAutomaticThresholds, Is.False);
            var children = tree.children;
            Assert.That(children.Select(child => child.threshold), Is.EqualTo(new[] { 0f, 3f, 6f }));
            string[] names = { "Rifle Idle", "Rifle Walk", "Rifle Run" };
            for (int i = 0; i < names.Length; i++)
            {
                var imported = AssetDatabase.LoadAllAssetsAtPath(ClipsDirectory + names[i] + ".fbx")
                    .OfType<AnimationClip>().Single(clip => !clip.name.Contains("__preview"));
                Assert.That(children[i].motion, Is.SameAs(imported));
            }
        }

        [Test]
        public void MainSceneUsesSceneOnlyDriverAndDisablesRootMotion()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().Single(root => root.name == "Player_Renegade");
                Assert.That(player.GetComponent<PlayerAnimation>(), Is.Not.Null);
                var animator = player.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
                Assert.That(animator.runtimeAnimatorController,
                    Is.SameAs(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)));
                var prefabPath = AssetDatabase.GUIDToAssetPath("bc0db8f04b753bd49b7f06ae6ab40513");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab.GetComponent<PlayerAnimation>(), Is.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void DriverHandlesCharacterVelocityAndReturnsToIdle()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().Single(root => root.name == "Player_Renegade");
                var character = player.GetComponent<CharacterController>();
                var animator = player.GetComponent<Animator>();
                var driver = player.GetComponent<PlayerAnimation>();
                // EditMode has no advancing frame delta; isolate measured speed from runtime damping.
                typeof(PlayerAnimation).GetField("speedDamping", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(driver, 0f);
                var tick = typeof(PlayerAnimation).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(tick, Is.Not.Null);
                tick.Invoke(driver, null);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(0f).Within(0.01f));
                character.Move(Vector3.right * 0.1f);
                Assert.That(character.velocity.x, Is.GreaterThan(0f));
                Assert.DoesNotThrow(() => tick.Invoke(driver, null));
                character.Move(Vector3.up * 0.1f);
                tick.Invoke(driver, null);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TargetAvatarEvaluatesLocomotionAtMultipleSpeedsAndTimes()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().Single(root => root.name == "Player_Renegade");
                var animator = player.GetComponent<Animator>();
                Assert.That(animator.isHuman, Is.True);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                Assert.That(hips, Is.Not.Null);
                Assert.That(head, Is.Not.Null);
                foreach (float speed in new[] { 0f, 3f, 6f })
                {
                    animator.SetFloat("Speed", speed);
                    foreach (float time in new[] { 0.1f, 0.5f, 0.9f })
                    {
                        animator.Play("Locomotion", 0, time);
                        animator.Update(0.02f);
                        Assert.That(IsFinite(hips.position) && IsFinite(head.position), Is.True,
                            $"Nonfinite Humanoid bone at speed {speed}, time {time}");
                        Assert.That(Vector3.Distance(hips.position, head.position), Is.GreaterThan(0.1f));
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool IsFinite(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
                   !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z);
        }
    }
}
