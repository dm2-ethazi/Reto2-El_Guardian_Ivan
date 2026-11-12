using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Muestra en pantalla la resistencia de la puerta.
    /// </summary>
    public class DoorHealthBar : MonoBehaviour
    {
        /// <summary>Puerta cuya vida se muestra.</summary>
        [SerializeField] private Door door;
        /// <summary>Barra de vida.</summary>
        [SerializeField] private Slider slider;
        /// <summary>Texto «vida / máxima».</summary>
        [SerializeField] private TMP_Text label;

        // Se suscribe al evento de la puerta para actualizarse solo cuando cambia la vida.
        private void OnEnable()
        {
            door.HealthChanged += Show;
        }

        private void OnDisable()
        {
            door.HealthChanged -= Show;
        }

        private void Start()
        {
            Show(door.CurrentHealth, door.MaxHealth);
        }

        // Pone la barra y el texto "vida / máxima".
        private void Show(int current, int max)
        {
            slider.maxValue = max;
            slider.value = current;
            label.text = $"{current} / {max}";
        }
    }
}
