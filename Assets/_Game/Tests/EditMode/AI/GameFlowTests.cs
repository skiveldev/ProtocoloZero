using System.Collections;
using NUnit.Framework;
using ProtocoloZero.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace ProtocoloZero.AI.Tests
{
    public sealed class GameFlowTests
    {
        [UnityTest]
        public IEnumerator MainScene_ButtonsHudFeedbackOutcomesAndRealReload()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity", OpenSceneMode.Single);
            yield return new EnterPlayMode();
            var flow = Object.FindFirstObjectByType<GameFlow>();
            yield return null;
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Menu));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(flow.Hud.Overlay.activeSelf, Is.True);
            Assert.That(Cursor.visible, Is.True);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset, Is.Not.Null);
            var brain = Object.FindFirstObjectByType<SimpleZombieAI>(FindObjectsInactive.Include);
            Assert.That(brain.enabled, Is.False);
            foreach (MonoBehaviour behaviour in flow.Health.GetComponents<MonoBehaviour>())
                if (behaviour.GetType().Name == "PlayerMovement" || behaviour is PlayerInteractor)
                    Assert.That(behaviour.enabled, Is.False);
            flow.Hud.StartButton.onClick.Invoke();
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(flow.Hud.Overlay.activeSelf, Is.False);
            Assert.That(brain.enabled, Is.True);
            Assert.That(flow.Hud.HealthLabel.text, Is.EqualTo("Salud 100/100"));
            Assert.That(flow.Feedback.HasAssignedClips, Is.True);
            flow.Health.TakeDamage(20);
            Assert.That(flow.Hud.HealthLabel.text, Is.EqualTo("Salud 80/100"));
            Assert.That(flow.Feedback.CueCount, Is.EqualTo(1));
            Assert.That(flow.Feedback.Source.isPlaying, Is.True);
            Assert.That(flow.Hud.DamageFlash.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.Hud.DamageFlash.enabled, Is.True);
            Assert.That(flow.Hud.DamageFlash.canvas.isActiveAndEnabled, Is.True);
            Assert.That(flow.Hud.DamageFlash.color.r, Is.EqualTo(1f));
            Assert.That(flow.Hud.DamageFlash.color.a, Is.GreaterThan(0f));
            Assert.That(flow.Hud.DamageFlash.raycastTarget, Is.False);
            float fadeDeadline = Time.realtimeSinceStartup + 1f;
            while (flow.Hud.DamageFlash.color.a > 0f && Time.realtimeSinceStartup < fadeDeadline)
                yield return null;
            Assert.That(flow.Hud.DamageFlash.color.a, Is.Zero);
            Debug.Log("[PA4-FX] Active screen-overlay red damage flash verified; unscaled fade reached zero.");
            flow.Progress.TryCollectCard("A");
            flow.Progress.TryCollectCard("B");
            flow.Progress.TryCollectCard("C");
            Assert.That(flow.Hud.CardsLabel.text, Is.EqualTo("Tarjetas 3/3"));
            Assert.That(flow.Hud.ObjectiveLabel.text, Does.Contain("activa el panel"));
            flow.Progress.TryActivatePanel();
            Assert.That(flow.Hud.ObjectiveLabel.text, Does.Contain("alcanza la salida"));
            Assert.That(flow.Progress.TryWin(), Is.True);
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Victory));
            Assert.That(flow.Hud.OutcomeLabel.text, Does.Contain("Victoria"));
            Assert.That(brain.enabled, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(flow.Feedback.CueCount, Is.EqualTo(5));
            flow.Health.TakeDamage(100);
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Victory));
            Assert.That(flow.Feedback.CueCount, Is.EqualTo(5));
            flow.Hud.RestartButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 30f;
            while (flow != null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow == null, Is.True, "restart must destroy the old scene instance");
            yield return null;
            flow = Object.FindFirstObjectByType<GameFlow>();
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Menu));
            Assert.That(flow.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(flow.Progress.CollectedCount, Is.Zero);
            Assert.That(flow.Progress.IsPanelActivated, Is.False);
            Assert.That(Object.FindFirstObjectByType<ExitGate>().IsLocked, Is.True);
            flow.Hud.StartButton.onClick.Invoke();
            flow.Health.TakeDamage(100);
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Defeat));
            Assert.That(flow.Hud.OutcomeLabel.text, Does.Contain("Derrota"));
            Assert.That(flow.Feedback.CueCount, Is.EqualTo(1));
            flow.Progress.TryCollectCard("A");
            flow.Progress.TryCollectCard("B");
            flow.Progress.TryCollectCard("C");
            flow.Progress.TryActivatePanel();
            flow.Progress.TryWin();
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Defeat));
            Assert.That(flow.Feedback.CueCount, Is.EqualTo(1));
            Debug.Log("[PA4-FLOW] Buttons, HUD events, cues, first-terminal-wins and actual reload verified.");
            flow.Hud.RestartButton.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 30f;
            while (flow != null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow == null, Is.True);
            yield return null;
            flow = Object.FindFirstObjectByType<GameFlow>();
            Assert.That(flow.State, Is.EqualTo(GameFlow.GameState.Menu));
            Assert.That(flow.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(flow.Progress.CollectedCount, Is.Zero);
            yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
