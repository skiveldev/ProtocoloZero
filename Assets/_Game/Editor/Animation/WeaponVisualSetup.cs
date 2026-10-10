using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProtocoloZero.Editor.Animation
{
    public static class WeaponVisualSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";
        private const string RifleGuid = "8e932e37952f66549b83c02c624baceb";

        public static void AttachRifle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(RifleGuid));
            if (prefab == null || prefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new InvalidOperationException("The visual rifle prefab is absent or contains physics components");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = scene.GetRootGameObjects().Single(root => root.name == "Player_Renegade");
            var animator = player.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
                throw new InvalidOperationException("Scene player lacks a valid Humanoid Animator");
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null || hand.name != "hand_r")
                throw new InvalidOperationException("Expected Renegade right-hand bone is missing");

            var sockets = hand.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name == "RifleSocket").ToArray();
            if (sockets.Length != 0)
            {
                if (sockets.Length != 1 || sockets[0].parent != hand || sockets[0].childCount != 1 ||
                    PrefabUtility.GetCorrespondingObjectFromSource(sockets[0].GetChild(0).gameObject) != prefab)
                    throw new InvalidOperationException("Existing RifleSocket differs from expected visual attachment");
                Debug.Log("Visual rifle already attached; leaving scene unchanged");
                return;
            }
            if (hand.GetComponentsInChildren<Transform>(true).Any(child =>
                    child != hand && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab))
                throw new InvalidOperationException("Rifle already exists under right hand without a socket");

            var socket = new GameObject("RifleSocket").transform;
            socket.SetParent(hand, false);
            // Initial inspector-adjustable grip pose. Confirm barrel and grip against the model in Scene view.
            socket.localPosition = new Vector3(0.02f, 0f, 0.06f);
            socket.localRotation = Quaternion.Euler(0f, 90f, 0f);
            socket.localScale = Vector3.one;
            var rifle = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            rifle.transform.SetParent(socket, false);
            rifle.transform.localPosition = Vector3.zero;
            rifle.transform.localRotation = Quaternion.identity;
            rifle.transform.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(hand);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rifle.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save visual rifle scene");
            Debug.Log("Attached scene-only visual rifle to Renegade right hand");
        }
    }
}
