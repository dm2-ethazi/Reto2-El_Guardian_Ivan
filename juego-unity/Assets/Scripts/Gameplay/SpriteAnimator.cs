using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Repite en bucle los fotogramas de un sprite. Cada tipo de enemigo tiene los suyos.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        /// <summary>Fotogramas de la animación, en orden.</summary>
        [SerializeField] private Sprite[] frames;
        /// <summary>Velocidad de la animación, en fotogramas por segundo.</summary>
        [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>Anima otro SpriteRenderer (el hijo visual del enemigo) en lugar del propio.</summary>
        /// <param name="target">SpriteRenderer que se animará.</param>
        public void SetTarget(SpriteRenderer target)
        {
            spriteRenderer = target;
        }

        // Elige el fotograma según el tiempo de juego, así todos los enemigos del mismo tipo van sincronizados.
        private void Update()
        {
            spriteRenderer.sprite = frames[(int)(Time.time * framesPerSecond) % frames.Length];
        }
    }
}
