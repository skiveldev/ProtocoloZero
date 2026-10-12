using System;
using System.Linq;
using ProtocoloZero.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProtocoloZero.Editor.Animation
{
    public static class RenegadeAnimationSetup
    {
        private const string Directory = "Assets/_Game/Animations/Renegade/";
        private const string ControllerPath = Directory + "Renegade.controller";
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";
        private const string TargetAvatarGuid = "26489012c8ee8af4d84156e5e5ac1035";

        public static void Build()
        {
            // Validate every imported dependency before changing the controller or scene.
            string avatarPath = AssetDatabase.GUIDToAssetPath(TargetAvatarGuid);
            var target = AssetDatabase.LoadAllAssetsAtPath(avatarPath).OfType<Avatar>()
                .FirstOrDefault(avatar => avatar.isValid && avatar.isHuman);
            if (target == null)
                throw new InvalidOperationException("Renegade target Humanoid Avatar is invalid");

            var clips = RifleNames.Select(LoadClip).ToArray();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = scene.GetRootGameObjects().Single(root => root.name == "Player_Renegade");
            var animator = player.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid ||
                !animator.avatar.isHuman || player.GetComponent<CharacterController>() == null ||
                animator.avatar != target)
                throw new InvalidOperationException("Scene Renegade root is missing the expected Humanoid Animator/CharacterController");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                var tree = new BlendTree
                {
                    name = "Idle Walk Run",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = "Speed",
                    useAutomaticThresholds = false
                };
                AssetDatabase.AddObjectToAsset(tree, controller);
                tree.AddChild(clips[0], 0f);
                tree.AddChild(clips[1], 3f);
                tree.AddChild(clips[2], 6f);
                tree.useAutomaticThresholds = false;
                var state = controller.layers[0].stateMachine.AddState("Locomotion");
                state.motion = tree;
                controller.layers[0].stateMachine.defaultState = state;
                EditorUtility.SetDirty(tree);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
            if (player.GetComponent<PlayerAnimation>() == null)
                player.AddComponent<PlayerAnimation>();
            if (animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Renegade main scene");
            Debug.Log("Phase04 Renegade locomotion authored from validated imported clips");
        }

        private static readonly string[] RifleNames = { "Rifle Idle", "Rifle Walk", "Rifle Run" };

        public static void EnableRifleLoops()
        {
            // Validate all imports before changing even the first importer.
            foreach (string name in RifleNames)
            {
                string path = Directory + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                var clips = assets.OfType<AnimationClip>()
                    .Where(clip => !clip.name.Contains("__preview")).ToArray();
                if (importer == null || importer.animationType != ModelImporterAnimationType.Human ||
                    clips.Length != 1 || !clips[0].humanMotion ||
                    !assets.OfType<Avatar>().Any() ||
                    assets.OfType<Avatar>().Any(avatar => !avatar.isValid || !avatar.isHuman))
                    throw new InvalidOperationException(name + " lacks a valid imported Humanoid clip/Avatar");
                var settings = importer.clipAnimations;
                if (settings.Length == 0)
                    settings = importer.defaultClipAnimations;
                if (settings.Length != 1)
                    throw new InvalidOperationException(name + " must have exactly one importer clip setting");
                Debug.Log(name + " imported clip " + clips[0].name + " averageSpeed " + clips[0].averageSpeed);
            }

            foreach (string name in RifleNames)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(Directory + name + ".fbx");
                var settings = importer.clipAnimations;
                if (settings.Length == 0)
                    settings = importer.defaultClipAnimations;
                if (!settings[0].loopTime)
                {
                    settings[0].loopTime = true;
                    importer.clipAnimations = settings;
                    importer.SaveAndReimport();
                }
                LoadClip(name);
            }
        }

        public static void ReplaceLocomotionWithRifle()
        {
            string avatarPath = AssetDatabase.GUIDToAssetPath(TargetAvatarGuid);
            if (!AssetDatabase.LoadAllAssetsAtPath(avatarPath).OfType<Avatar>()
                .Any(avatar => avatar.isValid && avatar.isHuman))
                throw new InvalidOperationException("Renegade target Humanoid Avatar is invalid");
            var rifle = RifleNames.Select(LoadClip).ToArray();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null || controller.layers.Length != 1 ||
                controller.parameters.Count(parameter => parameter.name == "Speed" &&
                    parameter.type == AnimatorControllerParameterType.Float) != 1 ||
                controller.layers[0].stateMachine.states.Length != 1 ||
                controller.layers[0].stateMachine.defaultState.name != "Locomotion" ||
                !(controller.layers[0].stateMachine.defaultState.motion is BlendTree tree) ||
                tree.blendType != BlendTreeType.Simple1D || tree.blendParameter != "Speed" ||
                tree.useAutomaticThresholds || tree.children.Length != 3 ||
                tree.children[0].threshold != 0f || tree.children[1].threshold != 3f ||
                tree.children[2].threshold != 6f ||
                 tree.children[0].motion != rifle[0] ||
                 tree.children[1].motion != rifle[1] ||
                 tree.children[2].motion != rifle[2])
                throw new InvalidOperationException("Existing locomotion layout differs from expected Rifle layout");

            var children = tree.children;
            if (children.Where((child, index) => child.motion != rifle[index]).Any())
            {
                for (int i = 0; i < children.Length; i++)
                    children[i].motion = rifle[i];
                tree.children = children;
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
            }
        }

        public static void RemoveDiscardedAnimations()
        {
            // Never delete source files until the final controller and all retained imports are valid.
            ReplaceLocomotionWithRifle();
            string[] discarded = { "Idle", "Walk", "Run", "Runner", "Runner01", "Stand Run" };
            foreach (string name in discarded)
            {
                string path = Directory + name + ".fbx";
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Could not delete discarded animation: " + path);
                Debug.Log("Deleted discarded animation and meta: " + path);
            }
        }

        private static AnimationClip LoadClip(string name)
        {
            string path = Directory + name + ".fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var clips = assets.OfType<AnimationClip>()
                .Where(clip => !clip.name.Contains("__preview")).ToArray();
            if (clips.Length != 1 || !clips[0].humanMotion || !clips[0].isLooping ||
                !assets.OfType<Avatar>().Any() ||
                assets.OfType<Avatar>().Any(avatar => !avatar.isValid || !avatar.isHuman))
                throw new InvalidOperationException(name + " does not contain one valid looping Humanoid clip/Avatar");
            return clips[0];
        }
    }
}
