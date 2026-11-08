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
        [Tooltip("Vida de la puerta al empezar cada nivel.")]
        [SerializeField, Min(1)] private int maxHealth = 100;

        /// <summary>Se lanza con la vida actual y la vida máxima.</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>Se lanza una sola vez, cuando la vida llega a 0.</summary>
        public event Action Fell;

        [Header("Golpe recibido")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.55f, 0.45f, 1f);
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.08f;
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.18f;

        // Temblor y destello sobre un hijo visual: el collider de la puerta no se mueve.
        private SpriteRenderer visual;
        private Color visualColor;
        private float shakeTimer = -1f;
        private float shakeStrength;

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

        /// <summary>
        /// Tiembla y destella un instante. Lo llama el enemigo justo al golpear.
        /// <paramref name="strength"/> 1 es un golpe normal; el jefe usa más.
        /// </summary>
        public void PlayHitFeedback(float strength)
        {
            if (visual == null)
            {
                visual = SpriteVisual.MoveToChild(gameObject);
                if (visual == null)
                {
                    return;
                }
                visualColor = visual.color;
            }

            shakeStrength = Mathf.Max(strength, shakeTimer >= 0f ? shakeStrength : 0f);
            shakeTimer = 0f;
        }

        // Temblor que se apaga poco a poco y destello rojo que vuelve al color original.
        private void LateUpdate()
        {
            if (visual == null || shakeTimer < 0f)
            {
                return;
            }

            shakeTimer += Time.deltaTime;
            float duration = shakeDuration * Mathf.Sqrt(shakeStrength);
            float t = shakeTimer / duration;
            if (t >= 1f)
            {
                shakeTimer = -1f;
                visual.transform.localPosition = Vector3.zero;
                visual.color = visualColor;
                return;
            }

            float fade = 1f - t;
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * (shakeAmplitude * shakeStrength * fade);
            visual.transform.localPosition = transform.InverseTransformVector(jitter);
            visual.color = Color.Lerp(visualColor, hitFlashColor, fade * fade);
        }
    }
}
