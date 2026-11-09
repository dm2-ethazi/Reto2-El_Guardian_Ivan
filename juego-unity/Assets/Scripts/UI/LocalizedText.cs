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

        /// <summary>Cambia la clave del texto y lo traduce ya. Sirve para botones copiados de otro.</summary>
        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

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
