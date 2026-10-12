using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ProtocoloZero.Gameplay.Tests
{
    /// <summary>
    /// Validates the Phase 05 scene wiring in ProtocoloZero_Nivel01 only:
    /// exactly three distinctly-identified cards linked to the manager,
    /// panel/gate/zone references, the player interactor, pre-existing
    /// phase invariants and floor clearance under every gameplay point.
    /// The scene is opened additively and always closed in finally.
    /// </summary>
    public class Phase05SceneValidationTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity";

        private static T Field<T>(object target, string name)
        {
            return (T)typeof(ProtocoloZero.Player.PlayerMovement)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        [Test]
        public void Scene_HasThreeDistinctCards_PlayerPanelGateZoneWired()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var cards = new List<AccessCard>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    cards.AddRange(root.GetComponentsInChildren<AccessCard>(true));
                cards.RemoveAll(c => !c.gameObject.scene.IsValid());

                Assert.That(cards.Count, Is.EqualTo(3), "exactly 3 access cards");

                var ids = new HashSet<string>();
                foreach (AccessCard card in cards)
                    ids.Add(card.CardId);
                Assert.That(ids, Is.EquivalentTo(new[] { "Card_A", "Card_B", "Card_C" }));

                LevelProgress progress = Object.FindFirstObjectByType<LevelProgress>();
                Assert.That(progress, Is.Not.Null, "scene has a LevelProgress manager");
                Assert.That(progress.RequiredCards, Is.EqualTo(3));
                foreach (AccessCard card in cards)
                {
                    var serialized = new UnityEditor.SerializedObject(card);
                    Assert.That(
                        serialized.FindProperty("progress").objectReferenceValue,
                        Is.EqualTo(progress),
                        $"{card.CardId} links the scene manager");
                }

                SecurityPanel panel =
                    Object.FindFirstObjectByType<SecurityPanel>();
                Assert.That(panel, Is.Not.Null);
                Assert.That(
                    new UnityEditor.SerializedObject(panel).FindProperty("progress").objectReferenceValue,
                    Is.EqualTo(progress));

                ExitGate gate = Object.FindFirstObjectByType<ExitGate>();
                Assert.That(gate, Is.Not.Null);
                Assert.That(gate.IsLocked, Is.True, "exit starts physically solid");
                Assert.That(
                    new UnityEditor.SerializedObject(gate).FindProperty("progress").objectReferenceValue,
                    Is.EqualTo(progress));

                VictoryZone zone = Object.FindFirstObjectByType<VictoryZone>();
                Assert.That(zone, Is.Not.Null);
                var zoneSerialized = new UnityEditor.SerializedObject(zone);
                Assert.That(
                    zoneSerialized.FindProperty("progress").objectReferenceValue,
                    Is.EqualTo(progress));
                Transform playerRoot =
                    (Transform)zoneSerialized.FindProperty("playerRoot").objectReferenceValue;
                Assert.That(playerRoot, Is.Not.Null, "zone pins the exact player identity");

                PlayerInteractor interactor =
                    playerRoot.GetComponentInChildren<PlayerInteractor>();
                if (interactor == null)
                    interactor = playerRoot.GetComponent<PlayerInteractor>();
                Assert.That(interactor, Is.Not.Null, "player carries a PlayerInteractor");
                Assert.That(playerRoot.GetComponent<ProtocoloZero.Player.PlayerMovement>(),
                    Is.Not.Null, "pre-existing movement stays on the player");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ExistingPhases_StayInvariant()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject player = GameObject.Find("Player_Renegade");
                Assert.That(player, Is.Not.Null, "player prefab instance preserved");
                var movement = player.GetComponent<ProtocoloZero.Player.PlayerMovement>();
                Assert.That(movement, Is.Not.Null);
                Assert.That(Field<float>(movement, "walkSpeed"), Is.EqualTo(3f));
                Assert.That(Field<float>(movement, "runSpeed"), Is.EqualTo(6f));

                var camera = Object.FindFirstObjectByType<ProtocoloZero.Camera.OrbitCameraController>();
                Assert.That(camera, Is.Not.Null, "orbit camera controller preserved");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GameplayPoints_HaveFloorBeneath()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Physics.SyncTransforms();
                var points = new List<Transform>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (AccessCard card in root.GetComponentsInChildren<AccessCard>(true))
                        points.Add(card.transform);
                    foreach (SecurityPanel panel in root.GetComponentsInChildren<SecurityPanel>(true))
                        points.Add(panel.transform);
                    foreach (ExitGate gate in root.GetComponentsInChildren<ExitGate>(true))
                        points.Add(gate.transform);
                    foreach (VictoryZone zone in root.GetComponentsInChildren<VictoryZone>(true))
                        points.Add(zone.transform);
                }
                Assert.That(points.Count, Is.EqualTo(6), "3 cards + panel + gate + zone");

                foreach (Transform point in points)
                {
                    Vector3 origin = point.position + Vector3.up * 5f;
                    bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit info, 7f);
                    Assert.That(hit, Is.True, $"{point.name} has floor beneath");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
