using TMPro;
using UnityEngine;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Pone en un texto fijo de la interfaz su traducción, y la cambia si cambia el idioma.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Clave del texto en Localization.cs, por ejemplo menu.start.")]
        [SerializeField] private string key;

        private void OnEnable()
        {
            Localization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= Refresh;
        }

        // Pone el texto de la clave en el idioma actual.
        private void Refresh()
        {
            GetComponent<TMP_Text>().text = Localization.Get(key);
        }
    }
}
