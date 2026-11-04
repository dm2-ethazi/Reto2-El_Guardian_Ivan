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
        [SerializeField] private Door door;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;

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

        private void Show(int current, int max)
        {
            slider.maxValue = max;
            slider.value = current;
            label.text = $"{current} / {max}";
        }
    }
}
