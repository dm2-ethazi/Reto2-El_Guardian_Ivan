using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Instancia enemigos en los puntos de aparición respetando el mínimo y el máximo del nivel.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private LevelConfig config;
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private Door door;
        [SerializeField] private Transform[] spawnPoints;

        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly List<Transform> freePoints = new List<Transform>();
        private float timer;

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;

        private void Update()
        {
            // Los enemigos destruidos quedan como null en la lista.
            activeEnemies.RemoveAll(enemy => enemy == null);
            timer += Time.deltaTime;

            bool belowMinimum = activeEnemies.Count < config.minEnemies;
            bool intervalDone = timer >= config.spawnInterval && activeEnemies.Count < config.maxEnemies;

            if (belowMinimum || intervalDone)
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            Enemy enemy = Instantiate(enemyPrefab, NextSpawnPoint().position, Quaternion.identity);
            enemy.Init(door.transform.position, config, PickMessage(), FreeLabelRow());
            activeEnemies.Add(enemy);
            timer = 0f;
        }

        private int FreeLabelRow()
        {
            return Enumerable.Range(0, config.maxEnemies)
                .First(row => activeEnemies.All(enemy => enemy.LabelRow != row));
        }

        // Elige un mensaje que no tenga ningún enemigo activo.
        private string PickMessage()
        {
            string[] free = config.messages
                .Where(message => activeEnemies.All(enemy => enemy.Message != message))
                .ToArray();

            return free[Random.Range(0, free.Length)];
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
    }
}
