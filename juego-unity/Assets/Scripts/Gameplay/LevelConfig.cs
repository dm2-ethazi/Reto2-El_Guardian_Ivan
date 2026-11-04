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
        [Min(0.1f)] public float enemySpeed = 0.6f;
        [Min(1)] public int damagePerHit = 2;
    }
}
