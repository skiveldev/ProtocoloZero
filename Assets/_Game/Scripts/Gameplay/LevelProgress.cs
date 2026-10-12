using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    /// <summary>
    /// Scene state manager for the Phase 05 access flow: unique card IDs,
    /// panel activation (only at the required count) and a single victory.
    /// Instance state only (no statics) so a scene reload always resets.
    /// Awake performs the full reset for fresh loads; OnEnable only repairs
    /// a null collection so a mid-game disable/enable never wipes progress.
    /// </summary>
    public sealed class LevelProgress : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredCards = 3;

        public int RequiredCards => requiredCards;
        public int CollectedCount => collectedIds != null ? collectedIds.Count : 0;
        public bool IsPanelActivated => panelActivated;
        public bool IsVictoryAchieved => victoryAchieved;

        public event Action<int> CardCollected;
        public event Action PanelActivated;
        public event Action VictoryAchieved;

        private HashSet<string> collectedIds;
        private bool panelActivated;
        private bool victoryAchieved;

        private void Awake()
        {
            ResetState();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            Debug.Log($"Tarjeta recogida 0/{requiredCards}");
        }

        /// <summary>
        /// Counts a card ID once, up to exactly <see cref="RequiredCards"/>.
        /// Rejects empty IDs, duplicates, anything beyond the required count
        /// and any pickup after victory. Returns true only when the count grows.
        /// </summary>
        public bool TryCollectCard(string cardId)
        {
            EnsureInitialized();
            if (victoryAchieved)
                return false;
            if (string.IsNullOrWhiteSpace(cardId))
                return false;
            if (collectedIds.Count >= requiredCards)
                return false;
            if (!collectedIds.Add(cardId))
                return false;
            CardCollected?.Invoke(collectedIds.Count);
            return true;
        }

        /// <summary>
        /// Activates the panel only once the required cards are collected.
        /// </summary>
        public bool TryActivatePanel()
        {
            EnsureInitialized();
            if (panelActivated)
                return false;
            if (collectedIds.Count < requiredCards)
                return false;
            panelActivated = true;
            PanelActivated?.Invoke();
            return true;
        }

        /// <summary>
        /// Grants victory once, and only after the panel was activated.
        /// </summary>
        public bool TryWin()
        {
            EnsureInitialized();
            if (victoryAchieved || !panelActivated)
                return false;
            victoryAchieved = true;
            VictoryAchieved?.Invoke();
            return true;
        }

        private void EnsureInitialized()
        {
            if (collectedIds == null)
                collectedIds = new HashSet<string>(StringComparer.Ordinal);
        }

        private void ResetState()
        {
            collectedIds = new HashSet<string>(StringComparer.Ordinal);
            panelActivated = false;
            victoryAchieved = false;
        }
    }
}
