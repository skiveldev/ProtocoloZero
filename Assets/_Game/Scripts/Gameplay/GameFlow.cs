using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtocoloZero.Gameplay
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameFlow : MonoBehaviour
    {
        public enum GameState { Menu, Playing, Victory, Defeat }

        [SerializeField] private LevelProgress progress;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Behaviour[] controlledBehaviours;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip damageClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        public GameState State { get; private set; } = GameState.Menu;
        public LevelProgress Progress => progress;
        public PlayerHealth Health => health;
        public GameHud Hud { get; private set; }
        public GameAudioFeedback Feedback { get; private set; }
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            SetControls(false);
            Time.timeScale = 0f;
            SetCursor(false);
            progress.VictoryAchieved += OnVictory;
            health.Died += OnDefeat;
        }

        private void Start()
        {
            Hud = gameObject.AddComponent<GameHud>();
            Hud.Initialize(this);
            Feedback = gameObject.AddComponent<GameAudioFeedback>();
            Feedback.Initialize(this, pickupClip, damageClip, victoryClip, defeatClip);
        }

        public void BeginGame()
        {
            if (State != GameState.Menu)
                return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            SetControls(true);
            SetCursor(true);
            StateChanged?.Invoke(State);
        }

        private void OnVictory()
        {
            if (!health.IsDead)
                Finish(GameState.Victory);
        }

        private void OnDefeat()
        {
            Finish(GameState.Defeat);
        }

        private void Finish(GameState outcome)
        {
            if (State != GameState.Playing)
                return;
            State = outcome;
            SetControls(false);
            Time.timeScale = 0f;
            SetCursor(false);
            StateChanged?.Invoke(State);
        }

        public AsyncOperation Restart()
        {
            if (State != GameState.Victory && State != GameState.Defeat)
                return null;
            Time.timeScale = 1f;
            return SceneManager.LoadSceneAsync(gameObject.scene.name, LoadSceneMode.Single);
        }

        private void SetControls(bool enabled)
        {
            if (controlledBehaviours == null)
                return;
            foreach (Behaviour behaviour in controlledBehaviours)
                if (behaviour != null)
                    behaviour.enabled = enabled;
        }

        private static void SetCursor(bool playing)
        {
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }

        private void OnDestroy()
        {
            if (progress != null) progress.VictoryAchieved -= OnVictory;
            if (health != null) health.Died -= OnDefeat;
            Time.timeScale = 1f;
            SetCursor(false);
        }
    }
}
