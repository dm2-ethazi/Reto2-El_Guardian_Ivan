using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuertaCastillo.UI
{
    /// <summary>Idiomas de la interfaz, en el mismo orden que las traducciones de <see cref="Localization"/>.</summary>
    public enum Language
    {
        Spanish,
        English,
        Basque
    }

    /// <summary>
    /// Textos de la interfaz en cada idioma. El idioma elegido se guarda en PlayerPrefs, así que
    /// se conserva entre ejecuciones. Los mensajes cifrados no se traducen: son parte del juego.
    /// </summary>
    public static class Localization
    {
        private const string LanguagePref = "language";

        // Cada entrada: clave y sus textos en español, inglés y euskera.
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["menu.title"] = new[] { "El guardián Iván", "El guardián Iván", "El guardián Iván" },
            ["menu.start"] = new[] { "Iniciar partida", "Start game", "Hasi partida" },
            ["menu.exit"] = new[] { "Salir", "Exit", "Irten" },
            ["menu.confirm"] = new[] { "¿Seguro que quieres salir?", "Are you sure you want to exit?", "Ziur irten nahi duzula?" },
            ["menu.yes"] = new[] { "Sí, salir", "Yes, exit", "Bai, irten" },
            ["menu.no"] = new[] { "No", "No", "Ez" },

            ["game.restart"] = new[] { "Reiniciar", "Restart", "Berrabiarazi" },
            ["game.menu"] = new[] { "Menú", "Menu", "Menua" },
            ["game.next"] = new[] { "Siguiente nivel", "Next level", "Hurrengo maila" },
            ["game.placeholder"] = new[] { "Pulsa las letras del mensaje", "Tap the letters of the message", "Sakatu mezuaren letrak" },
            ["game.decrypt"] = new[] { "Descifrar", "Decrypt", "Deszifratu" },
            ["game.space"] = new[] { "Espacio", "Space", "Zuriunea" },
            ["game.delete"] = new[] { "Borrar", "Delete", "Ezabatu" },
            ["game.status"] = new[] { "Nivel {0}  {1}", "Level {0}  {1}", "{0}. maila  {1}" },
            ["game.target"] = new[] { "Objetivo: {0}", "Target: {0}", "Helburua: {0}" },

            ["ruler.plain"] = new[] { "Original", "Plain", "Jatorrizkoa" },
            ["ruler.cipher"] = new[] { "Cifrada", "Cipher", "Zifratua" },

            ["answer.correct"] = new[] { "¡Correcto!", "Correct!", "Zuzen!" },
            ["answer.wrong"] = new[] { "Incorrecto, inténtalo de nuevo", "Wrong, try again", "Okerra, saiatu berriro" },

            ["level.completed"] = new[] { "Nivel {0} completado", "Level {0} complete", "{0}. maila gaindituta" },
            ["level.victory"] = new[] { "¡Victoria! El castillo está a salvo", "Victory! The castle is safe", "Garaipena! Gaztelua salbu dago" },
            ["level.defeat"] = new[] { "Derrota: la puerta ha caído", "Defeat: the gate has fallen", "Porrota: atea erori da" },

            ["hint.level1"] = new[] { "Pista: suma 3 a cada letra. La A pasa a ser la D.", "Hint: add 3 to each letter. A becomes D.", "Pista: gehitu 3 letra bakoitzari. A letra D bihurtzen da." },
            ["hint.level2"] = new[] { "Pista: suma 5 a cada letra. La A pasa a ser la F.", "Hint: add 5 to each letter. A becomes F.", "Pista: gehitu 5 letra bakoitzari. A letra F bihurtzen da." },
            ["hint.level3"] = new[] { "Pista: la A se cifra como una H.", "Hint: A is encrypted as H.", "Pista: A letra H gisa zifratzen da." },
            ["hint.level4"] = new[] { "Pista: la letra más repetida suele ser la E o la A.", "Hint: the most repeated letter is usually E or A.", "Pista: gehien errepikatzen den letra E edo A izan ohi da." },
            ["hint.level5"] = new[] { "Pista: la A se cifra como una N.", "Hint: A is encrypted as N.", "Pista: A letra N gisa zifratzen da." }
        };

        /// <summary>Se lanza al cambiar de idioma, para que los textos visibles se actualicen.</summary>
        public static event Action LanguageChanged;

        public static IEnumerable<string> Keys => Texts.Keys;

        /// <summary>Idioma guardado. Sin preferencia o con un valor no válido, español.</summary>
        public static Language Current
        {
            get
            {
                int saved = PlayerPrefs.GetInt(LanguagePref, (int)Language.Spanish);
                return Enum.IsDefined(typeof(Language), saved) ? (Language)saved : Language.Spanish;
            }
        }

        public static void SetLanguage(Language language)
        {
            PlayerPrefs.SetInt(LanguagePref, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            return Texts[key][(int)Current];
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }
}
