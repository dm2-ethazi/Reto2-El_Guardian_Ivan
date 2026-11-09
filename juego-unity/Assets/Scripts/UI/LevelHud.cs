using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Muestra el nivel y el tiempo restante, y los paneles de nivel completado, victoria y derrota.
    /// También crea los botones de reiniciar y volver al menú que se ven durante la partida, y los
    /// gestos del móvil que hacen lo mismo (deslizar a la derecha reinicia y a la izquierda va al menú).
    /// Los paneles de fin de nivel tienen además el botón "Volver al mapa" para elegir otro nivel.
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

        // Debajo de los otros botones del panel, que están a y = -100 con 130 de alto.
        private static readonly Vector2 MapButtonPosition = new Vector2(0f, -260f);
        private static readonly Vector2 MapButtonSize = new Vector2(560f, 130f);

        private Button inGameRestartButton;
        private Button inGameMenuButton;
        private Button mapButton;

        /// <summary>Botón "Volver al mapa" de los paneles de nivel completado, victoria y derrota.</summary>
        public Button MapButton => mapButton;

        public RectTransform InGameRestartButton => (RectTransform)inGameRestartButton.transform;
        public RectTransform InGameMenuButton => (RectTransform)inGameMenuButton.transform;

        private void Awake()
        {
            inGameRestartButton = CreateInGameButton(restartButton, "BotonPartidaReiniciar");
            inGameMenuButton = CreateInGameButton(menuButton, "BotonPartidaMenu");
            mapButton = CreateMapButton();

            SwipeNavigation swipe = gameObject.AddComponent<SwipeNavigation>();
            // En la partida se desliza a la derecha para volver al menú; a la izquierda no hace nada.
            swipe.Direction = Swipe.Right;
            swipe.Swiped = GoToMenu;
            swipe.DestinationLabel = () => Localization.Get("swipe.menu");
            swipe.LabelFont = statusLabel.font;
            swipe.IsAllowed = () => !panel.activeSelf;
        }

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
            inGameRestartButton.onClick.AddListener(Restart);
            inGameMenuButton.onClick.AddListener(GoToMenu);
            mapButton.onClick.AddListener(GoToMap);
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
            inGameRestartButton.onClick.RemoveListener(Restart);
            inGameMenuButton.onClick.RemoveListener(GoToMenu);
            mapButton.onClick.RemoveListener(GoToMap);
        }

        // Texto de estado: número de nivel y tiempo restante en mm:ss.
        private void Update()
        {
            int seconds = Mathf.CeilToInt(levelManager.TimeRemaining);
            statusLabel.text = Localization.Format("game.status", levelManager.LevelNumber, $"{seconds / 60:00}:{seconds % 60:00}");
        }

        // Al empezar un nivel: oculta el panel y deja jugar.
        private void HidePanel()
        {
            panel.SetActive(false);
            SetInGameButtonsVisible(true);
            answerPanel.SetInteractable(true);
        }

        // Nivel superado: panel verde con "Siguiente nivel".
        private void ShowLevelCompleted()
        {
            panelTitle.color = victoryColor;
            ShowPanel(Localization.Format("level.completed", levelManager.LevelNumber), true);
        }

        // Victoria final: panel verde con reiniciar y menú.
        private void ShowVictory()
        {
            panelTitle.color = victoryColor;
            ShowPanel(Localization.Get("level.victory"), false);
        }

        // Derrota: panel rojo con reiniciar y menú.
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
            SetInGameButtonsVisible(false);
            answerPanel.SetInteractable(false);
        }

        // Los botones de reiniciar y menú de la partida se ocultan mientras hay un panel.
        private void SetInGameButtonsVisible(bool visible)
        {
            inGameRestartButton.gameObject.SetActive(visible);
            inGameMenuButton.gameObject.SetActive(visible);
        }

        // "Volver al mapa": copia del botón de menú del panel, centrada debajo de los demás botones.
        // Se ve en los tres paneles porque es hijo del panel y no se oculta nunca.
        private Button CreateMapButton()
        {
            Button button = Instantiate(menuButton, menuButton.transform.parent, false);
            button.name = "BotonMapa";
            button.gameObject.SetActive(true);
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = MapButtonPosition;
            rect.sizeDelta = MapButtonSize;

            LocalizedText localized = button.GetComponentInChildren<LocalizedText>(true);
            if (localized != null)
            {
                localized.SetKey("game.map");
            }
            else
            {
                button.GetComponentInChildren<TMP_Text>(true).text = Localization.Get("game.map");
            }
            return button;
        }

        // Copia un botón del panel, sin animaciones, y lo deja por debajo del panel. Lo coloca GameLayout.
        private Button CreateInGameButton(Button template, string buttonName)
        {
            Button button = Instantiate(template, transform, false);
            button.name = buttonName;
            button.gameObject.SetActive(true);
            button.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            foreach (ButtonAnimation animation in button.GetComponentsInChildren<ButtonAnimation>(true))
            {
                DestroyImmediate(animation);
            }

            button.transform.localScale = Vector3.one;

            // El texto se reduce lo que haga falta para no tocar el borde en ningún idioma.
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 30f;
            label.margin = new Vector4(10f, 0f, 10f, 0f);

            // Que el botón no se quede con el foco: así las flechas y el Intro siguen siendo del juego.
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        // Reinicia solo el nivel actual: recarga la escena empezando en este nivel, con la puerta a 100.
        private void Restart()
        {
            SaveGame.RequestStartLevel(levelManager.LevelNumber - 1);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Vuelve al mapa de niveles, donde se puede repetir cualquier nivel ya desbloqueado.
        private void GoToMap()
        {
            SceneManager.LoadScene(LevelMap.SceneName);
        }

        // Vuelve al menú principal. Lo usan el botón de menú y el gesto de deslizar a la derecha.
        private void GoToMenu()
        {
            SceneManager.LoadScene(MainMenu.SceneName);
        }
    }
}
