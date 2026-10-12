using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProtocoloZero.Gameplay.Tests
{
    /// <summary>
    /// EditMode coverage for the Phase 05 access flow: unique cards, E pickup
    /// with enforced range, panel gating at 3/3, single unlock, player-only
    /// single victory, and session reset behaviour. No scene or asset is mutated.
    /// </summary>
    public class Phase05GameplayTests : InputTestFixture
    {
        private Keyboard keyboard;
        private readonly List<GameObject> owned = new List<GameObject>();
        private readonly List<string> logs = new List<string>();

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            Application.logMessageReceived += CaptureLog;
        }

        [TearDown]
        public override void TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
            foreach (GameObject go in owned)
                Object.DestroyImmediate(go);
            owned.Clear();
            logs.Clear();
            base.TearDown();
        }

        private void CaptureLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                logs.Add(message);
        }

        private GameObject Track(GameObject go)
        {
            owned.Add(go);
            return go;
        }

        private LevelProgress CreateProgress()
        {
            var go = Track(new GameObject("Progress"));
            return go.AddComponent<LevelProgress>();
        }

        private PlayerInteractor CreateInteractor(Vector3 position, bool lineOfSight)
        {
            var go = Track(new GameObject("Player"));
            go.transform.position = position;
            PlayerInteractor interactor = go.AddComponent<PlayerInteractor>();
            var serialized = new UnityEditor.SerializedObject(interactor);
            serialized.FindProperty("interactRange").floatValue = 2.5f;
            serialized.FindProperty("requireLineOfSight").boolValue = lineOfSight;
            serialized.ApplyModifiedProperties();
            return interactor;
        }

        private AccessCard CreateCard(string id, Vector3 position, LevelProgress progress)
        {
            var go = Track(new GameObject("Card_" + id));
            go.transform.position = position;
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            AccessCard card = go.AddComponent<AccessCard>();
            var serialized = new UnityEditor.SerializedObject(card);
            serialized.FindProperty("cardId").stringValue = id;
            serialized.FindProperty("progress").objectReferenceValue = progress;
            serialized.ApplyModifiedProperties();
            return card;
        }

        private SecurityPanel CreatePanel(LevelProgress progress)
        {
            var go = Track(new GameObject("Panel"));
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            SecurityPanel panel = go.AddComponent<SecurityPanel>();
            var serialized = new UnityEditor.SerializedObject(panel);
            serialized.FindProperty("progress").objectReferenceValue = progress;
            serialized.ApplyModifiedProperties();
            return panel;
        }

        private ExitGate CreateGate(LevelProgress progress)
        {
            // Inactive-first wiring: ExitGate.OnEnable subscribes to
            // LevelProgress.PanelActivated only when progress is already set.
            // Adding the component on an active object runs OnEnable with a
            // null progress, so the gate would never observe the unlock.
            var go = Track(new GameObject("Gate"));
            go.SetActive(false);
            BoxCollider blocker = go.AddComponent<BoxCollider>();
            ExitGate gate = go.AddComponent<ExitGate>();
            var serialized = new UnityEditor.SerializedObject(gate);
            serialized.FindProperty("progress").objectReferenceValue = progress;
            serialized.FindProperty("blocker").objectReferenceValue = blocker;
            serialized.ApplyModifiedProperties();
            go.SetActive(true);
            // EditMode never runs Awake/OnEnable for plain AddComponent, so
            // drive the known lifecycle explicitly after wiring is complete.
            InvokeLifecycle(gate, "OnDisable");
            InvokeLifecycle(gate, "Awake");
            InvokeLifecycle(gate, "OnEnable");
            return gate;
        }

        private static void InvokeLifecycle(Component component, string methodName)
        {
            component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(component, null);
        }

        private VictoryZone CreateZone(Transform playerRoot, LevelProgress progress, Vector3 position)
        {
            var go = Track(new GameObject("VictoryZone"));
            go.transform.position = position;
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            VictoryZone zone = go.AddComponent<VictoryZone>();
            var serialized = new UnityEditor.SerializedObject(zone);
            serialized.FindProperty("playerRoot").objectReferenceValue = playerRoot;
            serialized.FindProperty("progress").objectReferenceValue = progress;
            serialized.ApplyModifiedProperties();
            return zone;
        }

        private static void InvokeUpdate(PlayerInteractor interactor)
        {
            typeof(PlayerInteractor).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(interactor, null);
        }

        private void CollectAll(LevelProgress progress, params string[] ids)
        {
            foreach (string id in ids)
            {
                AccessCard card = CreateCard(id, Vector3.zero, progress);
                card.Interact(null);
            }
            logs.Clear();
        }

        [Test]
        public void CardCollectsOnce_LogsOneOfThree_ThenHides()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, false);
            AccessCard card = CreateCard("Card_A", Vector3.zero, progress);

            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));
            Assert.That(logs, Contains.Item("Tarjeta recogida 1/3"));
            Assert.That(card.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DuplicateCardId_DoesNotCount_StaysVisible()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A");

            AccessCard duplicate = CreateCard("Card_A", Vector3.zero, progress);
            duplicate.Interact(null);

            Assert.That(progress.CollectedCount, Is.EqualTo(1));
            Assert.That(duplicate.gameObject.activeSelf, Is.True);
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void FourthDistinctId_BeyondRequired_IsRejected()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A", "Card_B", "Card_C");

            AccessCard extra = CreateCard("Card_D", Vector3.zero, progress);
            extra.Interact(null);

            Assert.That(progress.CollectedCount, Is.EqualTo(3));
            Assert.That(extra.gameObject.activeSelf, Is.True);
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void EmptyCardId_IsRejected()
        {
            LevelProgress progress = CreateProgress();
            AccessCard card = CreateCard("Card_A", Vector3.zero, progress);
            var idProperty = new UnityEditor.SerializedObject(card);
            idProperty.FindProperty("cardId").stringValue = string.Empty;
            idProperty.ApplyModifiedProperties();

            Assert.That(card.CanInteract, Is.False);
            card.Interact(null);

            Assert.That(progress.CollectedCount, Is.EqualTo(0));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void OutOfRange_SelectedTarget_IsDenied()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, false);
            AccessCard far = CreateCard("Card_A", new Vector3(10f, 0f, 0f), progress);

            // Distance is enforced even for an explicitly selected target.
            Assert.That(interactor.TryInteract(far), Is.False);
            Assert.That(interactor.TryInteract(), Is.False);
            Assert.That(progress.CollectedCount, Is.EqualTo(0));
        }

        [Test]
        public void NearestInRangeTarget_IsPicked()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, false);
            AccessCard near = CreateCard("Card_A", new Vector3(1f, 0f, 0f), progress);
            CreateCard("Card_B", new Vector3(2f, 0f, 0f), progress);

            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));
            Assert.That(near.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void PressingE_CollectsNearbyCard()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, false);
            CreateCard("Card_A", new Vector3(1f, 0f, 0f), progress);

            Press(keyboard.eKey);
            InvokeUpdate(interactor);

            Assert.That(progress.CollectedCount, Is.EqualTo(1));
            Assert.That(logs, Contains.Item("Tarjeta recogida 1/3"));
        }

        [Test]
        public void WallBetweenPlayerAndCard_BlocksInteraction()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, true);
            CreateCard("Card_A", new Vector3(2f, 1f, 0f), progress);
            var wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector3(1f, 1f, 0f);
            var wallCollider = wall.AddComponent<BoxCollider>();
            wallCollider.size = new Vector3(0.5f, 3f, 3f);
            Physics.SyncTransforms();

            Assert.That(interactor.TryInteract(), Is.False);
            Assert.That(progress.CollectedCount, Is.EqualTo(0));
        }

        [Test]
        public void OwnBodyCollider_DoesNotBlockInteraction()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, true);
            var body = interactor.gameObject.AddComponent<BoxCollider>();
            body.size = new Vector3(0.6f, 1.8f, 0.6f);
            body.center = new Vector3(0f, 0.9f, 0f);
            CreateCard("Card_A", new Vector3(2f, 0f, 0f), progress);
            Physics.SyncTransforms();

            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));
        }

        [Test]
        public void UnrelatedTrigger_BetweenPlayerAndCard_IsIgnored()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, true);
            CreateCard("Card_A", new Vector3(2f, 1f, 0f), progress);
            var curtain = Track(new GameObject("Curtain"));
            curtain.transform.position = new Vector3(1f, 1f, 0f);
            var curtainCollider = curtain.AddComponent<BoxCollider>();
            curtainCollider.size = new Vector3(0.5f, 3f, 3f);
            curtainCollider.isTrigger = true;
            Physics.SyncTransforms();

            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));
        }

        [Test]
        public void ClearPath_WithLineOfSight_AllowsInteraction()
        {
            LevelProgress progress = CreateProgress();
            PlayerInteractor interactor = CreateInteractor(Vector3.zero, true);
            CreateCard("Card_A", new Vector3(2f, 0f, 0f), progress);
            Physics.SyncTransforms();

            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));
        }

        [Test]
        public void PanelDenies_BelowThreeCards_LogsMissing()
        {
            foreach (int collected in new[] { 0, 1, 2 })
            {
                LevelProgress progress = CreateProgress();
                string[] ids = { "Card_A", "Card_B", "Card_C" };
                for (int i = 0; i < collected; i++)
                    CreateCard(ids[i], Vector3.zero, progress).Interact(null);
                logs.Clear();

                SecurityPanel panel = CreatePanel(progress);
                int events = 0;
                progress.PanelActivated += () => events++;

                panel.Interact(null);

                Assert.That(progress.IsPanelActivated, Is.False, $"with {collected} cards");
                Assert.That(logs, Contains.Item("Faltan tarjetas"), $"with {collected} cards");
                Assert.That(events, Is.EqualTo(0));
            }
        }

        [Test]
        public void PanelActivates_AtThreeCards_ThenRepeatsStaySilent()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A", "Card_B", "Card_C");
            SecurityPanel panel = CreatePanel(progress);
            int events = 0;
            progress.PanelActivated += () => events++;

            panel.Interact(null);

            Assert.That(progress.IsPanelActivated, Is.True);
            Assert.That(logs, Contains.Item("Panel activado"));
            Assert.That(events, Is.EqualTo(1));

            logs.Clear();
            panel.Interact(null);
            panel.Interact(null);

            Assert.That(events, Is.EqualTo(1));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void GateLocked_UntilPanelUnlocks_LogsOnce()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A", "Card_B", "Card_C");
            SecurityPanel panel = CreatePanel(progress);
            ExitGate gate = CreateGate(progress);
            int events = 0;
            progress.PanelActivated += () => events++;

            Assert.That(gate.IsLocked, Is.True);

            gate.Unlock();
            Assert.That(gate.IsLocked, Is.True, "direct unlock without panel stays locked");

            panel.Interact(null);

            Assert.That(gate.IsLocked, Is.False, "panel activation releases the blocker");
            Assert.That(events, Is.EqualTo(1), "panel fires exactly once");
            Assert.That(logs, Contains.Item("Salida desbloqueada"));
            Assert.That(logs.Count(log => log == "Salida desbloqueada"), Is.EqualTo(1),
                "unlock logs exactly once");

            logs.Clear();
            gate.Unlock();
            Assert.That(logs, Is.Empty, "repeat unlock stays silent");
        }

        [Test]
        public void VictoryZone_IgnoresForeignColliders()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A", "Card_B", "Card_C");
            CreatePanel(progress).Interact(null);
            Assert.That(progress.IsPanelActivated, Is.True, "setup: panel activated");
            var player = Track(new GameObject("Player"));
            VictoryZone zone = CreateZone(player.transform, progress, Vector3.zero);
            int events = 0;
            progress.VictoryAchieved += () => events++;
            // Arrange/Act scoping: the setup legitimately logs
            // "Panel activado"; only the foreign-collider action below must
            // stay fully silent.
            logs.Clear();

            var stranger = Track(new GameObject("Stranger"));
            stranger.AddComponent<BoxCollider>();
            stranger.AddComponent<CharacterController>();

            zone.NotifyTriggerEntered(stranger.GetComponent<BoxCollider>());

            Assert.That(progress.IsVictoryAchieved, Is.False);
            Assert.That(events, Is.EqualTo(0));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void VictoryZone_ForeignInteractor_UnrelatedToPlayerRoot_IsRejected()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A", "Card_B", "Card_C");
            CreatePanel(progress).Interact(null);
            var player = Track(new GameObject("Player"));
            VictoryZone zone = CreateZone(player.transform, progress, Vector3.zero);
            int events = 0;
            progress.VictoryAchieved += () => events++;
            logs.Clear();

            // A PlayerInteractor on an object unrelated to the serialized
            // player root must not count as the player, even after 3/3 + panel.
            var impostor = Track(new GameObject("Impostor"));
            impostor.AddComponent<PlayerInteractor>();
            var impostorCollider = impostor.AddComponent<BoxCollider>();

            Assert.That(zone.IsPlayer(impostorCollider), Is.False);
            zone.NotifyTriggerEntered(impostorCollider);

            Assert.That(progress.IsVictoryAchieved, Is.False);
            Assert.That(events, Is.EqualTo(0));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void VictoryZone_RequiresPanel_ThenWinsOnceForExactPlayer()
        {
            LevelProgress progress = CreateProgress();
            var player = Track(new GameObject("Player"));
            var playerCollider = player.AddComponent<BoxCollider>();
            VictoryZone zone = CreateZone(player.transform, progress, Vector3.zero);
            int events = 0;
            progress.VictoryAchieved += () => events++;

            zone.NotifyTriggerEntered(playerCollider);
            Assert.That(progress.IsVictoryAchieved, Is.False, "no victory before panel");

            CollectAll(progress, "Card_A", "Card_B", "Card_C");
            CreatePanel(progress).Interact(null);
            logs.Clear();

            // A child collider of the exact player root counts as the player.
            var rifle = Track(new GameObject("RifleSocket"));
            rifle.transform.SetParent(player.transform);
            var rifleCollider = rifle.AddComponent<BoxCollider>();

            zone.NotifyTriggerEntered(rifleCollider);

            Assert.That(progress.IsVictoryAchieved, Is.True);
            Assert.That(logs, Contains.Item("Victoria"));
            Assert.That(events, Is.EqualTo(1));

            logs.Clear();
            zone.NotifyTriggerEntered(playerCollider);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void PostVictory_CardsAreRejected()
        {
            LevelProgress progress = CreateProgress();
            CollectAll(progress, "Card_A", "Card_B", "Card_C");
            CreatePanel(progress).Interact(null);
            var player = Track(new GameObject("Player"));
            var playerCollider = player.AddComponent<BoxCollider>();
            CreateZone(player.transform, progress, Vector3.zero)
                .NotifyTriggerEntered(playerCollider);
            Assert.That(progress.IsVictoryAchieved, Is.True);
            logs.Clear();

            CreateCard("Card_D", Vector3.zero, progress).Interact(null);

            Assert.That(progress.CollectedCount, Is.EqualTo(3));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void FreshProgress_StartsEmpty_ReenableKeepsCollected()
        {
            LevelProgress progress = CreateProgress();
            Assert.That(progress.CollectedCount, Is.EqualTo(0));

            CreateCard("Card_A", Vector3.zero, progress).Interact(null);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));

            // Simulates a mid-game disable/enable with domain reload disabled:
            // progress must survive, never be wiped by OnEnable.
            progress.gameObject.SetActive(false);
            progress.gameObject.SetActive(true);
            Assert.That(progress.CollectedCount, Is.EqualTo(1));

            // A fresh session (scene reload) starts empty again.
            LevelProgress fresh = CreateProgress();
            Assert.That(fresh.CollectedCount, Is.EqualTo(0));
            Assert.That(fresh.IsPanelActivated, Is.False);
            Assert.That(fresh.IsVictoryAchieved, Is.False);
        }
    }
}
