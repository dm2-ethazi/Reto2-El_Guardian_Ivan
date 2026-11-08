using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Menú inicial: empieza la partida o continúa la guardada, elige el idioma o sale del juego tras pedir confirmación.
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

        private Button continueButton;

        /// <summary>Botón de continuar la partida guardada. Es null si no hay partida guardada.</summary>
        public Button ContinueButton => continueButton;

        // El botón de continuar solo existe si hay partida guardada. Es una copia de Iniciar partida,
        // colocada justo antes en la columna; MenuLayout lo sitúa encima.
        /// <summary>
        /// Nivel en el que entra el gesto de deslizar a la derecha: el guardado si hay partida
        /// (como Continuar) o el primero (como Iniciar partida).
        /// </summary>
        public static int SwipeStartLevel => SaveGame.HasSavedLevel ? SaveGame.SavedLevelIndex : 0;

        private void Awake()
        {
            SwipeNavigation swipe = gameObject.AddComponent<SwipeNavigation>();
            swipe.OnlyControlsBlock = true;
            // El menú está a la izquierda del nivel: se desliza a la izquierda para entrar.
            swipe.Direction = Swipe.Left;
            swipe.Swiped = EnterGameBySwipe;
            swipe.DestinationLabel = () => Localization.Format("swipe.level", SwipeStartLevel + 1);
            swipe.LabelFont = startButton.GetComponentInChildren<TMP_Text>(true).font;
            swipe.IsAllowed = () => !confirmPanel.activeSelf;

            if (!SaveGame.HasSavedLevel)
            {
                return;
            }

            continueButton = Instantiate(startButton, startButton.transform.parent, false);
            continueButton.name = "BotonContinuar";
            continueButton.transform.SetSiblingIndex(startButton.transform.GetSiblingIndex());
            foreach (LocalizedText localized in continueButton.GetComponentsInChildren<LocalizedText>(true))
            {
                DestroyImmediate(localized);
            }
        }

        private void OnEnable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(ContinueGame);
                Localization.LanguageChanged += ShowContinueLabel;
                ShowContinueLabel();
            }

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
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ContinueGame);
                Localization.LanguageChanged -= ShowContinueLabel;
            }

            spanishButton.onClick.RemoveListener(ChooseSpanish);
            englishButton.onClick.RemoveListener(ChooseEnglish);
            basqueButton.onClick.RemoveListener(ChooseBasque);
            startButton.onClick.RemoveListener(StartGame);
            exitButton.onClick.RemoveListener(AskToExit);
            confirmExitButton.onClick.RemoveListener(Exit);
            cancelExitButton.onClick.RemoveListener(CancelExit);
        }

        // Botones de idioma: cada uno elige el suyo.
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

        // Cambia el idioma de todo el juego y marca el botón elegido.
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

        // Rótulo "Continuar (nivel N)" en el idioma actual.
        private void ShowContinueLabel()
        {
            continueButton.GetComponentInChildren<TMP_Text>(true).text = Localization.Format("menu.continue", SaveGame.SavedLevelIndex + 1);
        }

        // Iniciar partida siempre empieza en el nivel 1; al empezar, el nivel 1 sustituye a la partida guardada.
        private void StartGame()
        {
            SaveGame.RequestStartLevel(0);
            SceneManager.LoadScene(GameSceneName);
        }

        // Continuar: carga el juego en el nivel guardado.
        private void ContinueGame()
        {
            SaveGame.RequestStartLevel(SaveGame.SavedLevelIndex);
            SceneManager.LoadScene(GameSceneName);
        }

        // Gesto de deslizar a la izquierda: entra en el nivel guardado o en el 1.
        private void EnterGameBySwipe()
        {
            SaveGame.RequestStartLevel(SwipeStartLevel);
            SceneManager.LoadScene(GameSceneName);
        }

        // Salir pide confirmación antes de cerrar el juego.
        private void AskToExit()
        {
            confirmPanel.SetActive(true);
        }

        // "No": cierra la confirmación y sigue en el menú.
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
