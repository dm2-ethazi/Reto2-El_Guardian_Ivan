using System;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Controla la partida: el nivel actual, su temporizador y las transiciones de victoria y derrota.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Tooltip("Los niveles en orden (Assets/Levels/Level1..5). El último es el del jefe.")]
        [SerializeField] private LevelConfig[] levels;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private Door door;
        [Tooltip("Nivel con el que empieza la partida si el menú no pide otro (Continuar). 0 es el nivel 1.")]
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

        /// <summary>Número del nivel actual tal como lo ve el jugador (empieza en 1).</summary>
        public int LevelNumber => levelIndex + 1;
        /// <summary>Segundos que quedan para superar el nivel.</summary>
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
            StartLevel(Mathf.Clamp(SaveGame.TakeStartLevel(startLevelIndex), 0, levels.Length - 1));
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

        /// <summary>Empieza el nivel siguiente. Lo llama el botón "Siguiente nivel".</summary>
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
            // Se guarda al empezar: si se cierra el juego a mitad, Continuar retoma este nivel desde el principio.
            SaveGame.Save(index);
            LevelStarted?.Invoke();
        }

        // Resistir el último nivel es la victoria final; en los demás se espera al botón de siguiente nivel.
        private void CompleteLevel()
        {
            // Superar el nivel desbloquea el siguiente en el mapa; repetir uno anterior no quita progreso.
            LevelProgress.MarkCompleted(levelIndex);

            if (levelIndex == levels.Length - 1)
            {
                Win();
                return;
            }

            playing = false;
            spawner.StopLevel();
            LevelCompleted?.Invoke();
        }

        // Victoria final: se para todo y se borra la partida guardada.
        private void Win()
        {
            playing = false;
            spawner.StopLevel();
            LevelProgress.MarkCompleted(levelIndex);
            // Partida terminada: ya no hay nada que continuar.
            SaveGame.Clear();
            Won?.Invoke();
        }

        // Derrota: la puerta ha caído. La partida guardada se mantiene para reintentar el nivel.
        private void Lose()
        {
            playing = false;
            spawner.StopLevel();
            Lost?.Invoke();
        }
    }
}
