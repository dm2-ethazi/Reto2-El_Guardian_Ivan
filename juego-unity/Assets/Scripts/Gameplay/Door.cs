using System;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Puerta del castillo: guarda su resistencia y avisa cuando cambia o llega a 0.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        /// <summary>Se lanza con la vida actual y la vida máxima.</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>Se lanza una sola vez, cuando la vida llega a 0.</summary>
        public event Action Fell;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDown => CurrentHealth <= 0;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Repair(int amount)
        {
            if (amount <= 0 || IsDown)
            {
                return;
            }

            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDown)
            {
                return;
            }

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (IsDown)
            {
                Fell?.Invoke();
            }
        }
    }
}
