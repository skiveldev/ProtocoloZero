using System;
using UnityEngine;

namespace ProtocoloZero.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth == 0;
        public event Action<int, int> HealthChanged;
        public event Action Died;

        private bool initialized;

        private void Awake()
        {
            if (initialized)
                return;
            maxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = maxHealth;
            initialized = true;
        }

        public bool TakeDamage(int amount)
        {
            if (!initialized || IsDead || amount <= 0)
                return false;

            CurrentHealth -= Mathf.Min(amount, CurrentHealth);
            bool died = IsDead;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            if (died)
                Died?.Invoke();
            return true;
        }
    }
}
