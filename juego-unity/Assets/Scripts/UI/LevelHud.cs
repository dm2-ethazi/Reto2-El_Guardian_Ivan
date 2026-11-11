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

        // Medidas del panel de fin de nivel, en unidades lógicas (ver LayoutPanel). La caja mide
        // 980 x 564 y siempre cabe: la pantalla lógica mide al menos 1080 x 1080 (ScreenFit).
        private const float PanelWidth = 980f;
        private const float PanelPadding = 48f;
        private const float PanelGap = 26f;
        private const float PanelTitleHeight = 150f;
        private const float PanelButtonHeight = 130f;
        private const float PanelWideButton = 560f;
        private const float PanelHalfButton = 430f;

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
            LayoutPanel(canContinue);
            panel.SetActive(true);
            SetInGameButtonsVisible(false);
            answerPanel.SetInteractable(false);
        }

        // Coloca la caja, el título y los botones del panel de arriba abajo, con la caja del alto
        // justo para que todo quede dentro con margen: título, una fila con "Siguiente nivel" (o
        // "Reiniciar" y "Menú" lado a lado) y debajo "Volver al mapa". Todo va centrado en pantalla.
        private void LayoutPanel(bool canContinue)
        {
            float height = 2f * PanelPadding + PanelTitleHeight + 2f * PanelGap + 2f * PanelButtonHeight;
            Transform box = panel.transform.Find("Caja");
            if (box != null)
            {
                PlaceInPanel(box, 0f, PanelWidth, height);
            }

            float y = height / 2f - PanelPadding - PanelTitleHeight / 2f;
            PlaceInPanel(panelTitle.transform, y, PanelWidth - 2f * PanelPadding, PanelTitleHeight);

            y -= PanelTitleHeight / 2f + PanelGap + PanelButtonHeight / 2f;
            if (canContinue)
            {
                PlaceInPanel(nextLevelButton.transform, y, PanelWideButton, PanelButtonHeight);
            }
            else
            {
                float half = Mathf.Min(PanelHalfButton, (PanelWidth - 2f * PanelPadding - PanelGap) / 2f);
                float offset = (half + PanelGap) / 2f;
                PlaceInPanel(restartButton.transform, y, half, PanelButtonHeight, -offset);
                PlaceInPanel(menuButton.transform, y, half, PanelButtonHeight, offset);
            }

            y -= PanelButtonHeight + PanelGap;
            PlaceInPanel(mapButton.transform, y, PanelWideButton, PanelButtonHeight);
        }

        // Centra un elemento del panel en horizontal (o lo desplaza x) a la altura y, medida desde el centro.
        private static void PlaceInPanel(Transform element, float y, float width, float height, float x = 0f)
        {
            var rect = (RectTransform)element;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
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
            rect.sizeDelta = new Vector2(PanelWideButton, PanelButtonHeight);

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
