using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuertaCastillo.UI
{
    /// <summary>Idiomas de la interfaz, en el mismo orden que las traducciones de <see cref="Localization"/>.</summary>
    public enum Language
    {
        /// <summary>Español (idioma por defecto).</summary>
        Spanish,
        /// <summary>Inglés.</summary>
        English,
        /// <summary>Euskera.</summary>
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
            ["menu.title"] = new[] { "El Guardián Iván", "El Guardián Iván", "El Guardián Iván" },
            ["menu.start"] = new[] { "Iniciar partida", "Start game", "Hasi partida" },
            ["menu.continue"] = new[] { "Continuar (nivel {0})", "Continue (level {0})", "Jarraitu ({0}. maila)" },
            ["menu.exit"] = new[] { "Salir", "Exit", "Irten" },
            ["menu.confirm"] = new[] { "¿Seguro que quieres salir?", "Are you sure you want to exit?", "Ziur irten nahi duzula?" },
            ["menu.yes"] = new[] { "Sí, salir", "Yes, exit", "Bai, irten" },
            ["menu.no"] = new[] { "No", "No", "Ez" },

            ["game.restart"] = new[] { "Reiniciar", "Restart", "Berrabiarazi" },
            ["game.menu"] = new[] { "Menú", "Menu", "Menua" },
            ["game.next"] = new[] { "Siguiente nivel", "Next level", "Hurrengo maila" },
            ["game.map"] = new[] { "Volver al mapa", "Back to map", "Itzuli mapara" },
            ["game.placeholder"] = new[] { "Pulsa las letras del mensaje", "Tap the letters of the message", "Sakatu mezuaren letrak" },
            ["game.decrypt"] = new[] { "Descifrar", "Decrypt", "Deszifratu" },
            ["game.space"] = new[] { "Espacio", "Space", "Zuriunea" },
            ["game.delete"] = new[] { "Borrar", "Delete", "Ezabatu" },
            ["game.status"] = new[] { "Nivel {0}  {1}", "Level {0}  {1}", "{0}. maila  {1}" },
            ["game.target"] = new[] { "Objetivo: {0}", "Target: {0}", "Helburua: {0}" },

            ["map.title"] = new[] { "Mapa del reino", "Kingdom map", "Erresumaren mapa" },

            ["swipe.menu"] = new[] { "Menú", "Menu", "Menua" },
            ["swipe.level"] = new[] { "Nivel {0}", "Level {0}", "{0}. maila" },

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
            ["hint.level4"] = new[] { "Pista: la A se cifra como una L.", "Hint: A is encrypted as L.", "Pista: A letra L gisa zifratzen da." },
            ["hint.level5"] = new[] { "Pista: la A se cifra como una N.", "Hint: A is encrypted as N.", "Pista: A letra N gisa zifratzen da." }
        };

        /// <summary>Se lanza al cambiar de idioma, para que los textos visibles se actualicen.</summary>
        public static event Action LanguageChanged;

        /// <summary>Todas las claves de texto que existen (lo usan los tests).</summary>
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

        /// <summary>Cambia el idioma, lo guarda y avisa a los textos visibles para que se traduzcan.</summary>
        /// <param name="language">Idioma elegido.</param>
        public static void SetLanguage(Language language)
        {
            PlayerPrefs.SetInt(LanguagePref, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        /// <summary>Texto de una clave en el idioma actual. La clave debe existir en la tabla.</summary>
        /// <param name="key">Clave del texto, como menu.start.</param>
        /// <returns>El texto traducido.</returns>
        public static string Get(string key)
        {
            return Texts[key][(int)Current];
        }

        /// <summary>Como <see cref="Get"/>, pero sustituye {0}, {1}... por los valores dados.</summary>
        /// <param name="key">Clave del texto.</param>
        /// <param name="args">Valores que sustituyen a {0}, {1}...</param>
        /// <returns>El texto traducido con los valores puestos.</returns>
        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }
}
