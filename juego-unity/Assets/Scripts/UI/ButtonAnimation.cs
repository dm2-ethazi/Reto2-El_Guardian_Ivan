using UnityEngine;
using UnityEngine.EventSystems;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Hace que un botón respire suavemente y crezca al pasar el ratón por encima.
    /// Usa tiempo sin escalar para seguir funcionando con el juego en pausa.
    /// </summary>
    public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float IdleAmount = 0.015f;
        private const float IdleSpeed = 2.2f;
        private const float HoverScale = 1.07f;
        private const float Smoothing = 12f;

        private float hoverTarget = 1f;
        private float hover = 1f;

        /// <summary>El ratón entra en el botón: crece un poco.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            hoverTarget = HoverScale;
        }

        /// <summary>El ratón sale del botón: vuelve a su tamaño.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            hoverTarget = 1f;
        }

        private void OnDisable()
        {
            hover = hoverTarget = 1f;
            transform.localScale = Vector3.one;
        }

        // Suaviza el cambio de tamaño y le suma un "respirar" lento con un seno.
        private void Update()
        {
            hover = Mathf.Lerp(hover, hoverTarget, Smoothing * Time.unscaledDeltaTime);
            float idle = 1f + Mathf.Sin(Time.unscaledTime * IdleSpeed) * IdleAmount;
            transform.localScale = Vector3.one * (hover * idle);
        }
    }
}
