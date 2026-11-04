using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Menú inicial: empieza la partida o sale del juego tras pedir confirmación.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public const string SceneName = "MenuPrincipal";
        private const string GameSceneName = "PuertaDelCastillo";

        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button confirmExitButton;
        [SerializeField] private Button cancelExitButton;

        private void OnEnable()
        {
            startButton.onClick.AddListener(StartGame);
            exitButton.onClick.AddListener(AskToExit);
            confirmExitButton.onClick.AddListener(Exit);
            cancelExitButton.onClick.AddListener(CancelExit);
        }

        private void OnDisable()
        {
            startButton.onClick.RemoveListener(StartGame);
            exitButton.onClick.RemoveListener(AskToExit);
            confirmExitButton.onClick.RemoveListener(Exit);
            cancelExitButton.onClick.RemoveListener(CancelExit);
        }

        private void StartGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }

        private void AskToExit()
        {
            confirmPanel.SetActive(true);
        }

        private void CancelExit()
        {
            confirmPanel.SetActive(false);
        }

        private void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
