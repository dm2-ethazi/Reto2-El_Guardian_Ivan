using System;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Puerta del castillo: guarda su resistencia y avisa cuando cambia o llega a 0.
    /// Solo lleva un BoxCollider2D sólido: el Rigidbody2D del enemigo basta para que Unity
    /// detecte el choque y el enemigo se quede parado contra ella.
    /// Otros scripts se enteran de los cambios mediante eventos, sin que la puerta los conozca.
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

        /// <summary>Devuelve la puerta a su vida máxima. Se usa al empezar cada nivel.</summary>
        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        /// <summary>Resta vida sin bajar de 0. Al llegar a 0 lanza <see cref="Fell"/> una sola vez.</summary>
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
