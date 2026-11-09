using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>Cómo se ve un nivel en el mapa.</summary>
    public enum LevelState
    {
        /// <summary>Aún no se puede jugar: falta superar el anterior.</summary>
        Locked,
        /// <summary>Se puede jugar y todavía no se ha superado.</summary>
        Available,
        /// <summary>Ya se ha superado; se puede repetir.</summary>
        Completed
    }

    /// <summary>
    /// Progreso del jugador: cuántos niveles ha superado, en PlayerPrefs para que se conserve
    /// aunque se cierre el juego. Superar el nivel N desbloquea el N+1, y repetir un nivel
    /// anterior nunca quita progreso. Es independiente de <see cref="SaveGame"/>, que solo
    /// recuerda el último nivel jugado para el botón Continuar.
    /// </summary>
    public static class LevelProgress
    {
        /// <summary>Clave de PlayerPrefs con el número de niveles superados.</summary>
        public const string CompletedKey = "completedLevels";

        /// <summary>
        /// Niveles superados (0 si no se ha superado ninguno). Las partidas guardadas antes de
        /// existir el mapa también cuentan: si se iba por el nivel N, los anteriores están superados.
        /// </summary>
        public static int CompletedCount
        {
            get
            {
                int stored = PlayerPrefs.GetInt(CompletedKey, 0);
                int fromSave = SaveGame.HasSavedLevel ? SaveGame.SavedLevelIndex : 0;
                return Mathf.Max(0, Mathf.Max(stored, fromSave));
            }
        }

        /// <summary>Apunta que se ha superado un nivel (0 es el nivel 1) y lo escribe ya en disco.</summary>
        public static void MarkCompleted(int levelIndex)
        {
            PlayerPrefs.SetInt(CompletedKey, AfterCompleting(CompletedCount, levelIndex));
            PlayerPrefs.Save();
        }

        /// <summary>Borra el progreso: solo queda disponible el nivel 1.</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save();
        }

        /// <summary>Estado de un nivel (0 es el nivel 1) según el progreso guardado.</summary>
        public static LevelState StateOf(int levelIndex)
        {
            return StateOf(levelIndex, CompletedCount);
        }

        /// <summary>True si el nivel se puede jugar (disponible o ya superado).</summary>
        public static bool IsUnlocked(int levelIndex)
        {
            return StateOf(levelIndex) != LevelState.Locked;
        }

        /// <summary>
        /// Niveles superados después de superar <paramref name="levelIndex"/>: nunca baja,
        /// así que repetir un nivel anterior no cambia nada.
        /// </summary>
        public static int AfterCompleting(int completedCount, int levelIndex)
        {
            return Mathf.Max(completedCount, levelIndex + 1);
        }

        /// <summary>
        /// Estado de un nivel con <paramref name="completedCount"/> niveles superados: los anteriores
        /// están superados, el siguiente disponible y el resto bloqueados.
        /// </summary>
        public static LevelState StateOf(int levelIndex, int completedCount)
        {
            if (levelIndex < completedCount)
            {
                return LevelState.Completed;
            }
            return levelIndex == completedCount ? LevelState.Available : LevelState.Locked;
        }
    }
}
