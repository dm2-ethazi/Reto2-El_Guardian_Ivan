using System;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Valores de un nivel, para reutilizar las mismas mecánicas en los cinco niveles.
    /// Es un ScriptableObject: cada nivel es un fichero de Assets/Levels que se edita en el
    /// Inspector, así que cambiar la dificultad no obliga a tocar el código.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Puerta del Castillo/Nivel")]
    public class LevelConfig : ScriptableObject
    {
        [Min(0)] public int minEnemies = 1;
        [Min(1)] public int maxEnemies = 2;
        [Min(0.1f)] public float spawnInterval = 6f;
        [Tooltip("Segundos entre golpe y golpe de cualquier enemigo que esté en la puerta.")]
        [Min(0.1f)] public float secondsBetweenHits = 2f;
        [Min(1f)] public float durationSeconds = 60f;
        [Range(1, 26)] public int caesarShift = 3;

        [Tooltip("Tipos de enemigo que pueden aparecer en este nivel, cada uno con su velocidad y su daño.")]
        public EnemyStats[] enemies = Array.Empty<EnemyStats>();

        [Tooltip("Jefe que aparece al empezar el nivel, con su velocidad y su daño. Sin prefab, el nivel no tiene jefe.")]
        public EnemyStats boss = new EnemyStats();

        [Tooltip("Clave de la pista en los textos traducidos, como hint.level1.")]
        public string hintKey;

        [Tooltip("En mayúsculas, sin tildes ni Ñ. Debe haber más que maxEnemies.")]
        public string[] messages;

        public bool HasBoss => boss != null && boss.prefab != null;
    }

    /// <summary>Un tipo de enemigo dentro de un nivel: qué prefab es, a qué velocidad va y cuánto daña.</summary>
    [Serializable]
    public class EnemyStats
    {
        [Tooltip("Prefab del enemigo (Enemy, EnemyFast o EnemyBoss).")]
        public Enemy prefab;

        [Tooltip("Velocidad a la que avanza hacia la puerta, en unidades por segundo.")]
        [Min(0.01f)] public float speed = 0.4f;

        [Tooltip("Vida que quita a la puerta en cada golpe.")]
        [Min(1)] public int damagePerHit = 2;
    }
}
