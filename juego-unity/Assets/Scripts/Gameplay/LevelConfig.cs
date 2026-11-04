using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Valores de un nivel, para reutilizar las mismas mecánicas en los cinco niveles.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Puerta del Castillo/Nivel")]
    public class LevelConfig : ScriptableObject
    {
        [Min(0)] public int minEnemies = 1;
        [Min(1)] public int maxEnemies = 2;
        [Min(0.1f)] public float spawnInterval = 6f;
        [Min(0.1f)] public float enemySpeed = 0.4f;
        [Min(1)] public int damagePerHit = 2;
        [Min(0.1f)] public float secondsBetweenHits = 2f;
        [Min(1f)] public float durationSeconds = 60f;
        [Range(1, 26)] public int caesarShift = 3;

        [Tooltip("Tipos de enemigo que pueden aparecer en este nivel.")]
        public Enemy[] enemyPrefabs;

        [Tooltip("Jefe que aparece al empezar el nivel. Vacío si el nivel no tiene jefe.")]
        public Enemy bossPrefab;

        [TextArea] public string hint;

        [Tooltip("En mayúsculas, sin tildes ni Ñ. Debe haber más que maxEnemies.")]
        public string[] messages;
    }
}
