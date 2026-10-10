using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProtocoloZero.Player.Tests
{
    public class PlayerWeaponVisualTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";
        private const string RifleGuid = "8e932e37952f66549b83c02c624baceb";

        [Test]
        public void RifleIsOneSceneOnlyPrefabInstanceOnHumanoidRightHand()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().Single(root => root.name == "Player_Renegade");
                var animator = player.GetComponent<Animator>();
                Assert.That(animator.isHuman, Is.True);
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Assert.That(hand, Is.Not.Null);
                var sockets = hand.GetComponentsInChildren<Transform>(true)
                    .Where(child => child.name == "RifleSocket").ToArray();
                Assert.That(sockets, Has.Length.EqualTo(1));
                var socket = sockets[0];
                Assert.That(socket.parent, Is.SameAs(hand));
                Assert.That(socket.localScale, Is.EqualTo(Vector3.one));
                Assert.That(socket.childCount, Is.EqualTo(1));
                var rifle = socket.GetChild(0).gameObject;
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(rifle),
                    Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(RifleGuid))));
                Assert.That(rifle.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(rifle.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(rifle.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(rifle.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(rifle.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath("bc0db8f04b753bd49b7f06ae6ab40513"));
                Assert.That(source.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.name == "RifleSocket"), Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
