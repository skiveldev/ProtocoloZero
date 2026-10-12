using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProtocoloZero.AI.Tests
{
    /// <summary>
    /// Honest rig/clip audit for both zombie vendors. Empty .meta clip lists
    /// do NOT prove the absence of embedded FBX clips, so this test inventories
    /// the real imported assets: it asserts only authored facts (humanoid
    /// avatar present, no AnimatorController assigned anywhere) and writes the
    /// actual AnimationClip inventory to the log for the manual report instead
    /// of flagging a false negative.
    /// </summary>
    public class ZombieAnimationAuditTests
    {
        private const string ShirtlessFbx =
            "Assets/NewPunch/ShirtlessZombieFree/Models/ShirtlessZombie_FREE.fbx";
        private const string MaleFbx = "Assets/ZombieMale_AAB/Model/ZombieMale_AAB.fbx";
        private const string ShirtlessPrefab =
            "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab";
        private const string MalePrefab = "Assets/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_URP.prefab";

        private static void AuditModel(string fbxPath, string label)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            Assert.That(assets, Is.Not.Null.And.Not.Empty, $"{label} FBX imports");

            Avatar humanAvatar = null;
            int clipCount = 0;
            var clipNames = new System.Text.StringBuilder();
            foreach (Object asset in assets)
            {
                if (asset is Avatar avatar && avatar.isHuman)
                    humanAvatar = avatar;
                if (asset is AnimationClip clip)
                {
                    clipCount++;
                    clipNames.Append(clip.name).Append("; ");
                }
            }

            TestContext.WriteLine(
                $"[RigAudit] {label}: assets={assets.Length} clips={clipCount} [{clipNames}] " +
                $"humanAvatar={(humanAvatar != null ? humanAvatar.name : "<none>")}");
            Assert.That(humanAvatar, Is.Not.Null, $"{label} provides a humanoid avatar");
            // Deliberately no clip-count assert: an empty import list is not
            // proof of anything, and a default take would need manual review.
        }

        [Test]
        public void VendorModels_HumanoidAvatars_ClipInventoryLogged()
        {
            AuditModel(ShirtlessFbx, "ShirtlessZombie_FREE");
            AuditModel(MaleFbx, "ZombieMale_AAB");
        }

        [Test]
        public void VendorPrefabs_HaveNoAnimatorControllerAssigned()
        {
            foreach (string path in new[] { ShirtlessPrefab, MalePrefab })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, $"{path} loads");
                foreach (Animator animator in prefab.GetComponentsInChildren<Animator>(true))
                    Assert.That(animator.runtimeAnimatorController, Is.Null,
                        $"{path}: no controller assigned, bind pose until manual animation work");
            }
        }
    }
}
