using System;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Controla la partida: el nivel actual, su temporizador y las transiciones de victoria y derrota.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelConfig[] levels;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private Door door;
        [SerializeField, Min(0)] private int repairPerLevel = 20;
        [Tooltip("Nivel con el que empieza la partida. 0 es el nivel 1.")]
        [SerializeField, Min(0)] private int startLevelIndex;

        private int levelIndex;
        private bool playing;

        /// <summary>Se lanza al empezar cada nivel.</summary>
        public event Action LevelStarted;

        /// <summary>Se lanza cuando la puerta resiste todo el tiempo del nivel.</summary>
        public event Action LevelCompleted;

        /// <summary>Se lanza al derrotar al jefe final.</summary>
        public event Action Won;

        /// <summary>Se lanza cuando cae la puerta.</summary>
        public event Action Lost;

        public int LevelNumber => levelIndex + 1;
        public float TimeRemaining { get; private set; }

        /// <summary>El nivel del jefe no tiene tiempo: se supera derrotándolo.</summary>
        public bool HasTimer => levels[levelIndex].bossPrefab == null;

        private void OnEnable()
        {
            door.Fell += Lose;
            spawner.BossDefeated += Win;
        }

        private void OnDisable()
        {
            door.Fell -= Lose;
            spawner.BossDefeated -= Win;
        }

        private void Start()
        {
            StartLevel(startLevelIndex);
        }

        private void Update()
        {
            if (!playing || !HasTimer)
            {
                return;
            }

            TimeRemaining = Mathf.Max(TimeRemaining - Time.deltaTime, 0f);

            if (TimeRemaining <= 0f)
            {
                CompleteLevel();
            }
        }

        public void NextLevel()
        {
            StartLevel(levelIndex + 1);
        }

        private void StartLevel(int index)
        {
            levelIndex = index;
            TimeRemaining = levels[index].durationSeconds;
            playing = true;
            spawner.StartLevel(levels[index]);
            LevelStarted?.Invoke();
        }

        private void CompleteLevel()
        {
            playing = false;
            spawner.StopLevel();
            door.Repair(repairPerLevel);
            LevelCompleted?.Invoke();
        }

        private void Win()
        {
            playing = false;
            Won?.Invoke();
        }

        private void Lose()
        {
            playing = false;
            spawner.StopLevel();
            Lost?.Invoke();
        }
    }
}
