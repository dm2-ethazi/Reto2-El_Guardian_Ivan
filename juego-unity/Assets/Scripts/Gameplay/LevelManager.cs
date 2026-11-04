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
        [Tooltip("Nivel con el que empieza la partida. 0 es el nivel 1.")]
        [SerializeField, Min(0)] private int startLevelIndex;

        private int levelIndex;
        private bool playing;

        /// <summary>Se lanza al empezar cada nivel.</summary>
        public event Action LevelStarted;

        /// <summary>Se lanza cuando la puerta resiste todo el tiempo de un nivel que no es el último.</summary>
        public event Action LevelCompleted;

        /// <summary>Se lanza al resistir el último nivel o al derrotar antes al jefe final.</summary>
        public event Action Won;

        /// <summary>Se lanza cuando cae la puerta.</summary>
        public event Action Lost;

        public int LevelNumber => levelIndex + 1;
        public float TimeRemaining { get; private set; }

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

        // Cuenta atrás del nivel: si la puerta sigue en pie al llegar a 0, el nivel está superado.
        private void Update()
        {
            if (!playing)
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

        // Cada nivel empieza con la puerta a toda su vida.
        private void StartLevel(int index)
        {
            levelIndex = index;
            TimeRemaining = levels[index].durationSeconds;
            playing = true;
            door.ResetHealth();
            spawner.StartLevel(levels[index]);
            LevelStarted?.Invoke();
        }

        // Resistir el último nivel es la victoria final; en los demás se espera al botón de siguiente nivel.
        private void CompleteLevel()
        {
            if (levelIndex == levels.Length - 1)
            {
                Win();
                return;
            }

            playing = false;
            spawner.StopLevel();
            LevelCompleted?.Invoke();
        }

        private void Win()
        {
            playing = false;
            spawner.StopLevel();
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
