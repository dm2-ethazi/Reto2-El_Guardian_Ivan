using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Instancia enemigos en los puntos de aparición respetando el mínimo y el máximo del nivel.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private LevelConfig config;
        [SerializeField] private Door door;
        [SerializeField] private Transform[] spawnPoints;

        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly List<Transform> freePoints = new List<Transform>();
        private Enemy boss;
        private bool stopped = true;
        private float timer;

        /// <summary>Se lanza cuando se derrota al jefe del nivel.</summary>
        public event Action BossDefeated;

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
        public LevelConfig Config => config;
        public Enemy Boss => boss;

        /// <summary>Vacía la escena de enemigos y empieza a generar los del nivel indicado.</summary>
        public void StartLevel(LevelConfig level)
        {
            config = level;
            DestroyActiveEnemies();
            freePoints.Clear();
            timer = 0f;
            stopped = false;

            if (config.HasBoss)
            {
                boss = Spawn(config.boss, spawnPoints[spawnPoints.Length / 2]);
            }
        }

        /// <summary>Deja de generar y destruye los enemigos que queden.</summary>
        public void StopLevel()
        {
            stopped = true;
            DestroyActiveEnemies();
        }

        private void Update()
        {
            if (stopped)
            {
                return;
            }

            // Los enemigos destruidos quedan como null en la lista.
            activeEnemies.RemoveAll(enemy => enemy == null);
            timer += Time.deltaTime;

            // Crea uno al momento si falta el mínimo y, si no, cada intervalo mientras quepa en el máximo.
            bool belowMinimum = activeEnemies.Count < config.minEnemies;
            bool intervalDone = timer >= config.spawnInterval && activeEnemies.Count < config.maxEnemies;

            if ((belowMinimum || intervalDone) && config.enemies.Length > 0)
            {
                EnemyStats stats = config.enemies[Random.Range(0, config.enemies.Length)];
                Spawn(stats, NextSpawnPoint());
            }
        }

        /// <summary>Aplica un mensaje descifrado: el enemigo muere o, si es el jefe, recibe un mensaje nuevo.</summary>
        public void ResolveDecrypt(Enemy enemy)
        {
            if (!enemy.TakeDecrypt())
            {
                enemy.SetMessage(PickMessage());
                return;
            }

            Destroy(enemy.gameObject);

            if (enemy == boss)
            {
                boss = null;
                StopLevel();
                BossDefeated?.Invoke();
            }
        }

        // Crea el enemigo a partir del prefab y le da los valores del nivel y un mensaje libre.
        private Enemy Spawn(EnemyStats stats, Transform point)
        {
            Enemy enemy = Instantiate(stats.prefab, point.position, Quaternion.identity);
            enemy.Init(door.transform.position, stats, config, PickMessage(), FreeLabelRow());
            activeEnemies.Add(enemy);
            timer = 0f;
            return enemy;
        }

        private void DestroyActiveEnemies()
        {
            foreach (Enemy enemy in activeEnemies.Where(enemy => enemy != null))
            {
                Destroy(enemy.gameObject);
            }

            activeEnemies.Clear();
        }

        // Reparte los puntos sin repetir hasta agotarlos, para que no aparezcan enemigos solapados.
        private Transform NextSpawnPoint()
        {
            if (freePoints.Count == 0)
            {
                freePoints.AddRange(spawnPoints);
            }

            int index = Random.Range(0, freePoints.Count);
            Transform point = freePoints[index];
            freePoints.RemoveAt(index);
            return point;
        }

        // Busca la primera fila de mensaje que no use ningún enemigo activo.
        private int FreeLabelRow()
        {
            return Enumerable.Range(0, config.maxEnemies)
                .First(row => activeEnemies.All(enemy => enemy == null || enemy.LabelRow != row));
        }

        // Elige un mensaje que no tenga ningún enemigo activo.
        private string PickMessage()
        {
            string[] free = config.messages
                .Where(message => activeEnemies.All(enemy => enemy == null || enemy.Message != message))
                .ToArray();

            return free[Random.Range(0, free.Length)];
        }
    }
}
