using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Menú inicial: empieza la partida, elige el idioma o sale del juego tras pedir confirmación.
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
        [SerializeField] private Button spanishButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button basqueButton;

        private void OnEnable()
        {
            spanishButton.onClick.AddListener(ChooseSpanish);
            englishButton.onClick.AddListener(ChooseEnglish);
            basqueButton.onClick.AddListener(ChooseBasque);
            ShowCurrentLanguage();
            startButton.onClick.AddListener(StartGame);
            exitButton.onClick.AddListener(AskToExit);
            confirmExitButton.onClick.AddListener(Exit);
            cancelExitButton.onClick.AddListener(CancelExit);
        }

        private void OnDisable()
        {
            spanishButton.onClick.RemoveListener(ChooseSpanish);
            englishButton.onClick.RemoveListener(ChooseEnglish);
            basqueButton.onClick.RemoveListener(ChooseBasque);
            startButton.onClick.RemoveListener(StartGame);
            exitButton.onClick.RemoveListener(AskToExit);
            confirmExitButton.onClick.RemoveListener(Exit);
            cancelExitButton.onClick.RemoveListener(CancelExit);
        }

        private void ChooseSpanish()
        {
            ChooseLanguage(Language.Spanish);
        }

        private void ChooseEnglish()
        {
            ChooseLanguage(Language.English);
        }

        private void ChooseBasque()
        {
            ChooseLanguage(Language.Basque);
        }

        private void ChooseLanguage(Language language)
        {
            Localization.SetLanguage(language);
            ShowCurrentLanguage();
        }

        // El botón del idioma actual queda desactivado, y así se ve cuál está elegido.
        private void ShowCurrentLanguage()
        {
            spanishButton.interactable = Localization.Current != Language.Spanish;
            englishButton.interactable = Localization.Current != Language.English;
            basqueButton.interactable = Localization.Current != Language.Basque;
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

        // Application.Quit no hace nada dentro del editor: allí se detiene el modo Play.
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
