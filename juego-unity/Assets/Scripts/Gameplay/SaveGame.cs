using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Partida guardada: el nivel en el que va el jugador, en PlayerPrefs para que se conserve
    /// aunque se cierre el juego. También pasa al juego el nivel con el que debe empezar.
    /// </summary>
    public static class SaveGame
    {
        public const string SavedLevelKey = "savedLevel";

        private static int requestedLevel = -1;

        public static bool HasSavedLevel => PlayerPrefs.HasKey(SavedLevelKey);

        /// <summary>Nivel guardado, empezando en 0 para el nivel 1.</summary>
        public static int SavedLevelIndex => Mathf.Max(0, PlayerPrefs.GetInt(SavedLevelKey, 0));

        public static void Save(int levelIndex)
        {
            PlayerPrefs.SetInt(SavedLevelKey, levelIndex);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SavedLevelKey);
            PlayerPrefs.Save();
        }

        /// <summary>Pide que la próxima partida empiece en este nivel. Lo usa el menú antes de cargar el juego.</summary>
        public static void RequestStartLevel(int levelIndex)
        {
            requestedLevel = levelIndex;
        }

        /// <summary>Devuelve el nivel pedido y olvida la petición; sin petición, el valor por defecto.</summary>
        public static int TakeStartLevel(int defaultIndex)
        {
            int level = requestedLevel >= 0 ? requestedLevel : defaultIndex;
            requestedLevel = -1;
            return level;
        }
    }
}
