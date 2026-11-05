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
        [SerializeField] private Color victoryColor = new Color32(0x3C, 0x8D, 0x5A, 0xFF);
        [SerializeField] private Color defeatColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);

        // Conecta los eventos de la partida con los paneles y los botones con sus acciones.
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
            int seconds = Mathf.CeilToInt(levelManager.TimeRemaining);
            statusLabel.text = Localization.Format("game.status", levelManager.LevelNumber, $"{seconds / 60:00}:{seconds % 60:00}");
        }

        private void HidePanel()
        {
            panel.SetActive(false);
            answerPanel.SetInteractable(true);
        }

        private void ShowLevelCompleted()
        {
            panelTitle.color = victoryColor;
            ShowPanel(Localization.Format("level.completed", levelManager.LevelNumber), true);
        }

        private void ShowVictory()
        {
            panelTitle.color = victoryColor;
            ShowPanel(Localization.Get("level.victory"), false);
        }

        private void ShowDefeat()
        {
            panelTitle.color = defeatColor;
            ShowPanel(Localization.Get("level.defeat"), false);
        }

        // El mismo panel sirve para todo: solo cambian el título y los botones visibles.
        private void ShowPanel(string title, bool canContinue)
        {
            panelTitle.text = title;
            nextLevelButton.gameObject.SetActive(canContinue);
            restartButton.gameObject.SetActive(!canContinue);
            menuButton.gameObject.SetActive(!canContinue);
            panel.SetActive(true);
            answerPanel.SetInteractable(false);
        }

        // Recargar la escena deja la puerta a 100 y la partida en el nivel 1.
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
