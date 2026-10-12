using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    public sealed class GameAudioFeedback : MonoBehaviour
    {
        public int CueCount { get; private set; }
        public AudioSource Source { get; private set; }
        public bool HasAssignedClips => pickup != null && damage != null && victory != null && defeat != null;
        private GameFlow flow;
        private AudioClip pickup, damage, victory, defeat;
        private int lastHealth;

        public void Initialize(GameFlow owner, AudioClip pickupClip, AudioClip damageClip,
            AudioClip victoryClip, AudioClip defeatClip)
        {
            flow = owner;
            pickup = pickupClip;
            damage = damageClip;
            victory = victoryClip;
            defeat = defeatClip;
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.volume = 0.35f;
            lastHealth = flow.Health.CurrentHealth;
            flow.Progress.CardCollected += OnCard;
            flow.Health.HealthChanged += OnHealth;
            flow.StateChanged += OnState;
        }

        private void OnCard(int count)
        {
            if (flow.State == GameFlow.GameState.Playing) Play(pickup);
        }

        private void OnHealth(int current, int maximum)
        {
            if (current < lastHealth && current > 0 && flow.State == GameFlow.GameState.Playing)
                Play(damage);
            lastHealth = current;
        }

        private void OnState(GameFlow.GameState state)
        {
            if (state == GameFlow.GameState.Victory) Play(victory);
            if (state == GameFlow.GameState.Defeat) Play(defeat);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            Source.Stop();
            Source.clip = clip;
            Source.Play();
            CueCount++;
        }

        private void OnDestroy()
        {
            if (flow == null) return;
            flow.Progress.CardCollected -= OnCard;
            flow.Health.HealthChanged -= OnHealth;
            flow.StateChanged -= OnState;
        }
    }
}
