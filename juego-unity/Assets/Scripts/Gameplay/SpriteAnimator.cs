using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Repite en bucle los fotogramas de un sprite. Cada tipo de enemigo tiene los suyos.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            spriteRenderer.sprite = frames[(int)(Time.time * framesPerSecond) % frames.Length];
        }
    }
}
