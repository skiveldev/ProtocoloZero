#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProtocoloZero.AI;
using ProtocoloZero.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ProtocoloZero.Diagnostics
{
    // Diagnostics only: no instance exists in ordinary play or Editor tests.
    public sealed class StandaloneSmoke : MonoBehaviour
    {
        [Serializable]
        private sealed class Receipt
        {
            public bool success;
            public string error;
            public string[] completed;
            public string input = "Virtual InputSystem keyboard; no desktop input. Camera mouse manual.";
            public string setup = "In-memory player relocation; zombie/damage disabled during cards. No healing. VictoryZone notification API, not physical traversal.";
        }

        private readonly List<string> completed = new List<string>();
        private Keyboard keyboard;
        private string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--pa4-smoke") < 0
                || FindFirstObjectByType<StandaloneSmoke>() != null) return;
            var go = new GameObject("PA4 development smoke");
            DontDestroyOnLoad(go);
            go.AddComponent<StandaloneSmoke>();
        }

        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "--pa4-smoke-json");
            output = index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
            if (string.IsNullOrEmpty(output) || !Directory.Exists(Path.GetDirectoryName(output)))
            {
                Debug.LogError("[PA4-SMOKE] Existing explicit JSON parent path required.");
                Application.Quit(2);
                yield break;
            }
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            IEnumerator sequence = Run();
            float deadline = Time.realtimeSinceStartup + 150f;
            while (true)
            {
                object next = null;
                bool more = false;
                Exception failure = null;
                try
                {
                    Require(Time.realtimeSinceStartup < deadline, "Overall smoke watchdog");
                    more = sequence.MoveNext();
                    if (more) next = sequence.Current;
                }
                catch (Exception error) { failure = error; }
                if (failure != null) { Finish(false, failure.ToString()); yield break; }
                if (!more) { Finish(true, null); yield break; }
                yield return next;
            }
        }

        private IEnumerator Run()
        {
            yield return null;
            GameFlow flow = FindFirstObjectByType<GameFlow>();
            Require(flow != null && flow.gameObject.scene.buildIndex == 0, "Main scene0");
            RequireFresh(flow);
            Mark("main-menu-100hp-0cards-locked-gate");
            flow.Hud.StartButton.onClick.Invoke();
            Require(flow.State == GameFlow.GameState.Playing && !flow.Hud.Overlay.activeSelf, "Start button Playing");
            Require(flow.Feedback.HasAssignedClips, "Installed audio assignments");
            SimpleZombieAI brain = FindFirstObjectByType<SimpleZombieAI>();
            ZombieProximityDamage damage = brain.GetComponent<ZombieProximityDamage>();
            brain.enabled = false;
            damage.enabled = false;
            Transform player = flow.Health.transform;
            Vector3 origin = player.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            float until = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < until) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            float walked = Vector3.Distance(origin, player.position);
            Require(walked > 0.2f, "Virtual W moves actual player");
            origin = player.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            until = Time.realtimeSinceStartup + 0.4f;
            while (Time.realtimeSinceStartup < until) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Require(Vector3.Distance(origin, player.position) > walked * 1.2f, "Shift runs faster than W");
            Mark("virtual-W-and-Shift-movement");
            yield return null;

            AccessCard[] cards = FindObjectsByType<AccessCard>(FindObjectsSortMode.None);
            Require(cards.Length == 3, "Three scene cards");
            foreach (AccessCard card in cards)
            {
                Place(player, card.transform.position);
                yield return null;
                int before = flow.Progress.CollectedCount;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Require(flow.Progress.CollectedCount == before + 1, "Actual E collects " + card.CardId);
            }
            Require(flow.Hud.CardsLabel.text == "Tarjetas 3/3", "Card HUD");
            Mark("three-distinct-cards-via-E-range-LOS");
            SecurityPanel panel = FindFirstObjectByType<SecurityPanel>();
            Place(player, panel.transform.position);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Require(flow.Progress.IsPanelActivated && !FindFirstObjectByType<ExitGate>().IsLocked, "E panel/gate");
            Mark("panel-E-and-gate-unlock");
            VictoryZone zone = FindFirstObjectByType<VictoryZone>();
            Place(player, zone.transform.position);
            zone.NotifyTriggerEntered(player.GetComponent<CharacterController>());
            Require(flow.State == GameFlow.GameState.Victory && flow.Hud.OutcomeLabel.text.Contains("Victoria"), "Victory zone/UI");
            Require(flow.Feedback.CueCount == 4, "Three pickup and one victory cues");
            Mark("player-zone-notification-victory-UI-audio-cues");
            flow.Hud.RestartButton.onClick.Invoke();
            until = Time.realtimeSinceStartup + 20f;
            while (flow != null && Time.realtimeSinceStartup < until) yield return null;
            Require(flow == null, "Old victory scene destroyed");
            yield return null;
            flow = FindFirstObjectByType<GameFlow>();
            RequireFresh(flow);
            Mark("actual-victory-button-reload-reset");
            flow.Hud.StartButton.onClick.Invoke();
            brain = FindFirstObjectByType<SimpleZombieAI>();
            until = Time.realtimeSinceStartup + 10f;
            while (!brain.IsNavigationReady && Time.realtimeSinceStartup < until) yield return null;
            Require(brain.IsNavigationReady && FindFirstObjectByType<SceneNavMeshBootstrap>().UsedBakedData,
                "Standalone baked navigation");
            player = flow.Health.transform;
            foreach (MonoBehaviour behaviour in player.GetComponents<MonoBehaviour>())
                if (behaviour.GetType().Name == "PlayerMovement") behaviour.enabled = false;
            player.GetComponent<CharacterController>().enabled = false;
            player.position = brain.transform.position + Vector3.right * 0.9f;
            Physics.SyncTransforms();
            int last = flow.Health.CurrentHealth;
            float previousHit = -1f;
            bool flash = false;
            bool faded = false;
            until = Time.realtimeSinceStartup + 20f;
            while (!flow.Health.IsDead && Time.realtimeSinceStartup < until)
            {
                flash |= flow.Hud.DamageFlash.gameObject.activeInHierarchy && flow.Hud.DamageFlash.color.a > 0f;
                if (flash && flow.Hud.DamageFlash.color.a == 0f) faded = true;
                if (flow.Health.CurrentHealth != last)
                {
                    Require(last - flow.Health.CurrentHealth == 20, "Actual20 damage");
                    if (previousHit >= 0) Require(Time.realtimeSinceStartup - previousHit >= 0.85f, "Damage cooldown");
                    previousHit = Time.realtimeSinceStartup;
                    last = flow.Health.CurrentHealth;
                    Require(flow.Hud.HealthLabel.text == $"Salud {last}/100", "Damage HUD");
                }
                yield return null;
            }
            Require(flow.Health.IsDead && flow.State == GameFlow.GameState.Defeat, "Real proximity defeat");
            Require(flash && faded, "Actual damage flash and fade");
            Require(flow.Hud.OutcomeLabel.text.Contains("Derrota") && flow.Feedback.CueCount == 5,
                "Defeat screen and four damage/one defeat cues");
            Mark("real-zombie-cooldown-damage-HUD-flash-fade-defeat-audio");
            flow.Hud.RestartButton.onClick.Invoke();
            until = Time.realtimeSinceStartup + 20f;
            while (flow != null && Time.realtimeSinceStartup < until) yield return null;
            Require(flow == null, "Old defeat scene destroyed");
            yield return null;
            RequireFresh(FindFirstObjectByType<GameFlow>());
            Mark("actual-defeat-button-reload-reset");
        }

        private static void Place(Transform player, Vector3 target)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.position = new Vector3(target.x, 0.08f, target.z);
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static void RequireFresh(GameFlow flow)
        {
            Require(flow != null && flow.Hud != null && flow.State == GameFlow.GameState.Menu
                && flow.Health.CurrentHealth == 100 && flow.Progress.CollectedCount == 0
                && !flow.Progress.IsPanelActivated && FindFirstObjectByType<ExitGate>().IsLocked, "Fresh scene reset");
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
        }

        private void Mark(string label) { completed.Add(label); Debug.Log("[PA4-SMOKE] " + label); }

        private void Finish(bool success, string error)
        {
            InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(output, JsonUtility.ToJson(new Receipt
                { success = success, error = error, completed = completed.ToArray() }, true));
            Debug.Log($"[PA4-SMOKE] complete success={success} error={error}");
            Application.Quit(success ? 0 : 1);
        }
    }
}
#endif
