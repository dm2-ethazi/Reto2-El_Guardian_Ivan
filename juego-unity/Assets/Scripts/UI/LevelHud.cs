using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Muestra el nivel y el tiempo restante, y los paneles de nivel completado, victoria y derrota.
    /// </summary>
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private AnswerPanel answerPanel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text panelTitle;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void OnEnable()
        {
            levelManager.LevelStarted += HidePanel;
            levelManager.LevelCompleted += ShowLevelCompleted;
            levelManager.Won += ShowVictory;
            levelManager.Lost += ShowDefeat;
            nextLevelButton.onClick.AddListener(levelManager.NextLevel);
            restartButton.onClick.AddListener(Restart);
            menuButton.onClick.AddListener(GoToMenu);
        }

        private void OnDisable()
        {
            levelManager.LevelStarted -= HidePanel;
            levelManager.LevelCompleted -= ShowLevelCompleted;
            levelManager.Won -= ShowVictory;
            levelManager.Lost -= ShowDefeat;
            nextLevelButton.onClick.RemoveListener(levelManager.NextLevel);
            restartButton.onClick.RemoveListener(Restart);
            menuButton.onClick.RemoveListener(GoToMenu);
        }

        private void Update()
        {
            string time = levelManager.HasTimer
                ? $"{Mathf.CeilToInt(levelManager.TimeRemaining) / 60:00}:{Mathf.CeilToInt(levelManager.TimeRemaining) % 60:00}"
                : "¡Derrota al jefe!";
            statusLabel.text = $"Nivel {levelManager.LevelNumber}  {time}";
        }

        private void HidePanel()
        {
            panel.SetActive(false);
            answerPanel.SetInteractable(true);
        }

        private void ShowLevelCompleted()
        {
            ShowPanel($"Nivel {levelManager.LevelNumber} completado", true);
        }

        private void ShowVictory()
        {
            ShowPanel("¡Victoria! El castillo está a salvo", false);
        }

        private void ShowDefeat()
        {
            ShowPanel("Derrota: la puerta ha caído", false);
        }

        private void ShowPanel(string title, bool canContinue)
        {
            panelTitle.text = title;
            nextLevelButton.gameObject.SetActive(canContinue);
            restartButton.gameObject.SetActive(!canContinue);
            menuButton.gameObject.SetActive(!canContinue);
            panel.SetActive(true);
            answerPanel.SetInteractable(false);
        }

        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void GoToMenu()
        {
            SceneManager.LoadScene(MainMenu.SceneName);
        }
    }
}
