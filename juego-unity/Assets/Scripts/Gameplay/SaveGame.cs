using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Partida guardada: el nivel en el que va el jugador, en PlayerPrefs para que se conserve
    /// aunque se cierre el juego. También pasa al juego el nivel con el que debe empezar.
    /// </summary>
    public static class SaveGame
    {
        /// <summary>Clave de PlayerPrefs donde se guarda el nivel. Borrarla equivale a no tener partida.</summary>
        public const string SavedLevelKey = "savedLevel";

        private static int requestedLevel = -1;

        /// <summary>True si hay una partida guardada que se pueda continuar.</summary>
        public static bool HasSavedLevel => PlayerPrefs.HasKey(SavedLevelKey);

        /// <summary>Nivel guardado, empezando en 0 para el nivel 1.</summary>
        public static int SavedLevelIndex => Mathf.Max(0, PlayerPrefs.GetInt(SavedLevelKey, 0));

        /// <summary>Guarda el nivel actual (0 es el nivel 1) y lo escribe ya en disco.</summary>
        /// <param name="levelIndex">Nivel actual (0 es el nivel 1).</param>
        public static void Save(int levelIndex)
        {
            PlayerPrefs.SetInt(SavedLevelKey, levelIndex);
            PlayerPrefs.Save();
        }

        /// <summary>Borra la partida guardada, por ejemplo al ganar el juego.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SavedLevelKey);
            PlayerPrefs.Save();
        }

        /// <summary>Pide que la próxima partida empiece en este nivel. Lo usa el menú antes de cargar el juego.</summary>
        /// <param name="levelIndex">Nivel con el que empezar (0 es el nivel 1).</param>
        public static void RequestStartLevel(int levelIndex)
        {
            requestedLevel = levelIndex;
        }

        /// <summary>Devuelve el nivel pedido y olvida la petición; sin petición, el valor por defecto.</summary>
        /// <param name="defaultIndex">Nivel que se usa si nadie ha pedido otro.</param>
        /// <returns>El nivel con el que debe empezar la partida.</returns>
        public static int TakeStartLevel(int defaultIndex)
        {
            int level = requestedLevel >= 0 ? requestedLevel : defaultIndex;
            requestedLevel = -1;
            return level;
        }
    }
}
