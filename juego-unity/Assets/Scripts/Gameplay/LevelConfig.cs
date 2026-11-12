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
        /// <summary>Enemigos que siempre tiene que haber a la vez.</summary>
        [Tooltip("Enemigos que siempre tiene que haber a la vez: si hay menos, aparece uno al momento.")]
        [Min(0)] public int minEnemies = 1;
        /// <summary>Máximo de enemigos a la vez; también es el número de filas de mensajes.</summary>
        [Tooltip("Máximo de enemigos a la vez. También es el número de filas de mensajes.")]
        [Min(1)] public int maxEnemies = 2;
        /// <summary>Segundos entre la aparición de un enemigo y la del siguiente.</summary>
        [Tooltip("Segundos entre la aparición de un enemigo y la del siguiente, mientras no se llegue al máximo.")]
        [Min(0.1f)] public float spawnInterval = 6f;
        /// <summary>Segundos entre golpe y golpe de un enemigo que está en la puerta.</summary>
        [Tooltip("Segundos entre golpe y golpe de cualquier enemigo que esté en la puerta.")]
        [Min(0.1f)] public float secondsBetweenHits = 2f;
        /// <summary>Segundos que hay que resistir para superar el nivel.</summary>
        [Tooltip("Segundos que hay que resistir para superar el nivel.")]
        [Min(1f)] public float durationSeconds = 60f;
        /// <summary>Desplazamiento César de los mensajes del nivel (1 a 26).</summary>
        [Tooltip("Desplazamiento del cifrado César de los mensajes de este nivel.")]
        [Range(1, 26)] public int caesarShift = 3;

        /// <summary>Tipos de enemigo que pueden aparecer, cada uno con su velocidad y su daño.</summary>
        [Tooltip("Tipos de enemigo que pueden aparecer en este nivel, cada uno con su velocidad y su daño.")]
        public EnemyStats[] enemies = Array.Empty<EnemyStats>();

        /// <summary>Jefe que aparece al empezar. Sin prefab, el nivel no tiene jefe.</summary>
        [Tooltip("Jefe que aparece al empezar el nivel, con su velocidad y su daño. Sin prefab, el nivel no tiene jefe.")]
        public EnemyStats boss = new EnemyStats();

        /// <summary>Clave de la pista en <see cref="PuertaCastillo.UI.Localization"/>.</summary>
        [Tooltip("Clave de la pista en los textos traducidos, como hint.level1.")]
        public string hintKey;

        /// <summary>Mensajes posibles, en mayúsculas y sin tildes ni Ñ. Debe haber más que <see cref="maxEnemies"/>.</summary>
        [Tooltip("En mayúsculas, sin tildes ni Ñ. Debe haber más que maxEnemies.")]
        public string[] messages;

        /// <summary>True si el nivel tiene jefe (su prefab está asignado).</summary>
        public bool HasBoss => boss != null && boss.prefab != null;
    }

    /// <summary>Un tipo de enemigo dentro de un nivel: qué prefab es, a qué velocidad va y cuánto daña.</summary>
    [Serializable]
    public class EnemyStats
    {
        /// <summary>Prefab del enemigo (Enemy, EnemyFast o EnemyBoss).</summary>
        [Tooltip("Prefab del enemigo (Enemy, EnemyFast o EnemyBoss).")]
        public Enemy prefab;

        /// <summary>Velocidad hacia la puerta, en unidades por segundo.</summary>
        [Tooltip("Velocidad a la que avanza hacia la puerta, en unidades por segundo.")]
        [Min(0.01f)] public float speed = 0.4f;

        /// <summary>Vida que quita a la puerta en cada golpe.</summary>
        [Tooltip("Vida que quita a la puerta en cada golpe.")]
        [Min(1)] public int damagePerHit = 2;
    }
}
