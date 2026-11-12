using PuertaCastillo.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Coloca la interfaz de la partida según la pantalla y encuadra el escenario en el hueco libre.
    /// El escenario ocupa toda la pantalla, con la interfaz encima. Los botones no se mueven solos:
    /// solo cambian de sitio y de tamaño al cambiar la pantalla o girarla.
    /// En vertical los datos y la regla van arriba, y la respuesta con el abecedario abajo.
    /// En horizontal el escenario queda a la izquierda, con la vida y el objetivo encima, y la
    /// pista, la regla y el abecedario en una columna a la derecha, al alcance del pulgar.
    /// </summary>
    [ExecuteAlways]
    public class GameLayout : MonoBehaviour
    {
        private const float Margin = 16f;
        private const float Padding = 14f;
        private const float Gap = 10f;
        private const float RowHeight = 64f;
        private const float HintHeight = 76f;
        private const float TouchSize = 110f;
        private const float InGameButtonWidth = 150f;
        private const float PortraitCellHeight = 112f;
        private const float LandscapeCellHeight = 100f;
        private const float FieldHeight = 96f;
        private const float FeedbackHeight = 46f;
        private const float KeyHeight = 96f;
        private const float ColumnRatio = 0.46f;
        private const float MinColumnWidth = 760f;
        private const float MaxColumnWidth = 960f;
        private const float ShiftWidth = 120f;

        // Parte del mundo que debe verse siempre en el hueco libre.
        private const float WorldWidth = 10f;
        private const float WorldHeight = 12f;
        private const float WorldBottom = -7f;
        private const float SpawnEdgeGap = 2f;
        private const float EnemyEdgeGap = 1f;
        private const float WidestSpawn = 8f;
        private static readonly float[] SpawnPositions = { -1f, -0.5f, 0f, 0.5f, 1f };

        /// <summary>Escalador del Canvas de la partida.</summary>
        [Header("Pantalla")]
        [SerializeField] private CanvasScaler scaler;
        /// <summary>Cámara ortográfica del escenario, que se encuadra en el hueco libre.</summary>
        [SerializeField] private Camera worldCamera;
        /// <summary>Puntos de aparición, que se reparten según el ancho visible.</summary>
        [SerializeField] private Transform[] spawnPoints;

        /// <summary>Fondo del panel de datos de arriba.</summary>
        [Header("Fondos de los paneles")]
        [SerializeField] private RectTransform topPanel;
        /// <summary>Fondo del panel de respuesta (abajo en vertical, columna derecha en horizontal).</summary>
        [SerializeField] private RectTransform bottomPanel;

        /// <summary>Icono de la vida de la puerta.</summary>
        [Header("Datos")]
        [SerializeField] private RectTransform healthIcon;
        /// <summary>Barra de vida de la puerta.</summary>
        [SerializeField] private RectTransform healthBar;
        /// <summary>Placa del nivel y el tiempo.</summary>
        [SerializeField] private RectTransform statusPlate;
        /// <summary>Icono del reloj.</summary>
        [SerializeField] private RectTransform timeIcon;
        /// <summary>Texto «Nivel N mm:ss».</summary>
        [SerializeField] private RectTransform statusText;
        /// <summary>Barra con el mensaje del objetivo.</summary>
        [SerializeField] private RectTransform targetBar;
        /// <summary>Placa de la pista.</summary>
        [SerializeField] private RectTransform hintPlate;
        /// <summary>Icono de la pista.</summary>
        [SerializeField] private RectTransform hintIcon;
        /// <summary>Texto de la pista.</summary>
        [SerializeField] private RectTransform hintText;

        /// <summary>Fondo de las casillas de la regla.</summary>
        [Header("Regla")]
        [SerializeField] private RectTransform rulerPlate;
        /// <summary>Rótulo «Original».</summary>
        [SerializeField] private RectTransform plainCaption;
        /// <summary>Rótulo «Cifrada».</summary>
        [SerializeField] private RectTransform cipherCaption;
        /// <summary>Número del desplazamiento de la regla.</summary>
        [SerializeField] private RectTransform shiftText;
        /// <summary>Fila modelo de letras originales (solo se ve en el editor).</summary>
        [SerializeField] private RectTransform plainRow;
        /// <summary>Fila modelo de letras cifradas (solo se ve en el editor).</summary>
        [SerializeField] private RectTransform cipherRow;
        /// <summary>Botón + de la regla.</summary>
        [SerializeField] private RectTransform increaseButton;
        /// <summary>Botón - de la regla.</summary>
        [SerializeField] private RectTransform decreaseButton;

        /// <summary>Aviso de acierto o fallo.</summary>
        [Header("Respuesta")]
        [SerializeField] private RectTransform feedbackText;
        /// <summary>Campo de respuesta.</summary>
        [SerializeField] private RectTransform answerField;
        /// <summary>Botón «Descifrar», que va en la última fila del teclado.</summary>
        [SerializeField] private RectTransform decryptButton;
        /// <summary>Panel de respuesta, que crea el abecedario en pantalla al empezar a jugar.</summary>
        [Tooltip("Crea el abecedario en pantalla al empezar a jugar.")]
        [SerializeField] private AnswerPanel answerPanel;

        private Vector2 lastScreen;
        private Rect lastSafeArea;

        /// <summary>Tamaño de la pantalla en unidades lógicas tras la última colocación.</summary>
        public Vector2 LogicalSize { get; private set; }

        // Solo recoloca cuando cambia el tamaño de la pantalla o el área segura (al girar el móvil).
        private void Update()
        {
            Vector2 screen = ScreenFit.CurrentSize;
            Rect safeArea = Application.isPlaying ? Screen.safeArea : new Rect(Vector2.zero, screen);

            if (screen != lastScreen || safeArea != lastSafeArea)
            {
                lastScreen = screen;
                lastSafeArea = safeArea;
                Apply(screen.x, screen.y, safeArea);
            }
        }

        /// <summary>Recoloca todo para una pantalla de esos píxeles, respetando muescas y bordes.</summary>
        /// <param name="width">Ancho de la pantalla en píxeles.</param>
        /// <param name="height">Alto de la pantalla en píxeles.</param>
        /// <param name="safeArea">Área segura en píxeles (sin muescas ni bordes redondeados).</param>
        public void Apply(float width, float height, Rect safeArea)
        {
            ScreenFit.Configure(scaler, width, height);
            LogicalSize = ScreenFit.LogicalSize(width, height);
            float scale = LogicalSize.x / width;

            float left = safeArea.xMin * scale + Margin;
            float right = (width - safeArea.xMax) * scale + Margin;
            float top = (height - safeArea.yMax) * scale + Margin;
            float bottom = safeArea.yMin * scale + Margin;

            // El hueco libre se mide desde la esquina inferior izquierda.
            Rect free = ScreenFit.IsPortrait(width, height)
                ? ApplyPortrait(left, right, top, bottom)
                : ApplyLandscape(left, right, top, bottom);

            FitCamera(free);
        }

        // Datos y regla arriba; respuesta y abecedario abajo, a todo el ancho.
        private Rect ApplyPortrait(float left, float right, float top, float bottom)
        {
            float cardWidth = LogicalSize.x - left - right;
            float x = left + Padding;
            float width = cardWidth - 2f * Padding;

            float y = PlaceStatus(x, top + Padding, width);
            y = PlaceHint(x, y, width);
            y = PlaceRuler(x, y, width, PortraitCellHeight);
            Place(topPanel, left, top, cardWidth, y - Gap + Padding - top);
            float topEnd = y - Gap + Padding;

            float keysHeight = LetterKeyboard.HeightFor(KeyHeight);
            float cardHeight = Padding + FieldHeight + Gap + keysHeight + Padding;
            float cardY = LogicalSize.y - bottom - cardHeight;
            Place(bottomPanel, left, cardY, cardWidth, cardHeight);
            Place(feedbackText, x, cardY - FeedbackHeight - 8f, width, FeedbackHeight);
            Place(answerField, x, cardY + Padding, width, FieldHeight);
            PlaceKeys(x, cardY + Padding + FieldHeight + Gap, width, keysHeight);

            float freeBottom = LogicalSize.y - cardY;
            return new Rect(0f, freeBottom, LogicalSize.x, cardY - topEnd);
        }

        // Escenario a la izquierda con la vida y el objetivo encima; regla y abecedario a la derecha.
        private Rect ApplyLandscape(float left, float right, float top, float bottom)
        {
            float columnWidth = Mathf.Clamp(LogicalSize.x * ColumnRatio, MinColumnWidth, MaxColumnWidth);
            float columnX = LogicalSize.x - right - columnWidth;
            float x = columnX + Padding;
            float width = columnWidth - 2f * Padding;

            float keysHeight = LetterKeyboard.HeightFor(KeyHeight);
            float content = HintHeight + Gap + TouchSize + Gap + CaesarRuler.GridHeight(LandscapeCellHeight) + Gap
                + FeedbackHeight + FieldHeight + Gap + keysHeight;
            float cardHeight = content + 2f * Padding;
            float available = LogicalSize.y - top - bottom;
            float cardY = top + Mathf.Max(0f, (available - cardHeight) / 2f);

            float y = PlaceHint(x, cardY + Padding, width);
            y = PlaceRuler(x, y, width, LandscapeCellHeight);
            Place(feedbackText, x, y, width, FeedbackHeight);
            y += FeedbackHeight;
            Place(answerField, x, y, width, FieldHeight);
            y += FieldHeight + Gap;
            PlaceKeys(x, y, width, keysHeight);
            Place(bottomPanel, columnX, cardY, columnWidth, cardHeight);

            // La vida, el tiempo y el objetivo van en una franja sobre el escenario.
            float gameRight = columnX - Margin;
            float stripWidth = gameRight - left;
            float stripEnd = PlaceStatus(left + Padding, top + Padding, stripWidth - 2f * Padding) - Gap + Padding;
            Place(topPanel, left, top, stripWidth, stripEnd - top);

            return new Rect(0f, 0f, gameRight, LogicalSize.y - stripEnd);
        }

        // Vida, tiempo y botones de la partida en una fila, y el objetivo debajo. Devuelve dónde acaban.
        private float PlaceStatus(float left, float y, float width)
        {
            PlaceInGameButtons(left + width, y);

            float dataWidth = width - 2f * (InGameButtonWidth + Gap);
            float statusWidth = Mathf.Min(300f, dataWidth * 0.42f);
            float rowY = y + (TouchSize - RowHeight) / 2f;
            Place(healthIcon, left, rowY + 4f, RowHeight - 8f, RowHeight - 8f);
            Place(healthBar, left + RowHeight, rowY, dataWidth - statusWidth - RowHeight - Gap, RowHeight);
            Place(statusPlate, left + dataWidth - statusWidth, rowY, statusWidth, RowHeight);
            Place(timeIcon, left + dataWidth - statusWidth + 10f, rowY + 12f, 40f, 40f);
            Place(statusText, left + dataWidth - statusWidth + 54f, rowY, statusWidth - 62f, RowHeight);
            y += TouchSize + Gap;

            Place(targetBar, left, y, width, RowHeight + 8f);
            return y + RowHeight + 8f + Gap;
        }

        // Los botones solo existen jugando: los crea LevelHud al empezar.
        private void PlaceInGameButtons(float right, float y)
        {
            LevelHud hud = statusText.GetComponentInParent<LevelHud>(true);
            if (!Application.isPlaying || hud == null)
            {
                return;
            }

            Place(hud.InGameRestartButton, right - InGameButtonWidth, y, InGameButtonWidth, TouchSize);
            Place(hud.InGameMenuButton, right - 2f * InGameButtonWidth - Gap, y, InGameButtonWidth, TouchSize);
        }

        // Placa de la pista con su icono. Devuelve dónde empieza lo siguiente.
        private float PlaceHint(float left, float y, float width)
        {
            Place(hintPlate, left, y, width, HintHeight);
            Place(hintIcon, left + 12f, y + 18f, 40f, 40f);
            Place(hintText, left + 60f, y + 4f, width - 72f, HintHeight - 8f);
            return y + HintHeight + Gap;
        }

        // Botones - y + con los rótulos y el desplazamiento entre ellos y, debajo, las casillas.
        private float PlaceRuler(float x, float y, float width, float cellHeight)
        {
            float captionsX = x + TouchSize + Gap;
            float captionWidth = (width - 2f * (TouchSize + Gap) - ShiftWidth) / 2f;
            Place(decreaseButton, x, y, TouchSize, TouchSize);
            Place(plainCaption, captionsX, y, captionWidth, TouchSize);
            Place(shiftText, captionsX + captionWidth, y, ShiftWidth, TouchSize);
            Place(cipherCaption, captionsX + captionWidth + ShiftWidth, y, captionWidth, TouchSize);
            Place(increaseButton, x + width - TouchSize, y, TouchSize, TouchSize);
            y += TouchSize + Gap;

            float gridHeight = CaesarRuler.GridHeight(cellHeight);
            Place(rulerPlate, x, y, width, gridHeight);
            PlaceRows(x, y, width, gridHeight);
            if (Application.isPlaying)
            {
                plainCaption.GetComponentInParent<CaesarRuler>(true).Arrange(x, y, width, cellHeight);
            }

            return y + gridHeight + Gap;
        }

        // El abecedario solo existe jugando; en el editor se coloca al menos el botón de descifrar.
        private void PlaceKeys(float x, float y, float width, float height)
        {
            LetterKeyboard keyboard = answerPanel != null ? answerPanel.OnScreenKeyboard : null;
            if (keyboard != null)
            {
                keyboard.Arrange(x, y, width, height, decryptButton);
                return;
            }

            Rect submit = LetterKeyboard.SubmitKeyRect(x, y, width, height);
            Place(decryptButton, submit.x, submit.y, submit.width, submit.height);
        }

        // Las filas de la escena solo se ven en el editor: al jugar sirven de modelo para las casillas.
        private void PlaceRows(float x, float y, float width, float height)
        {
            float rowHeight = height / 2f - 6f;
            Place(plainRow, x + 10f, y + 6f, width - 20f, rowHeight);
            Place(cipherRow, x + 10f, y + height / 2f, width - 20f, rowHeight);
        }

        /// <summary>Coloca un elemento midiendo desde la esquina superior izquierda, en unidades lógicas.</summary>
        /// <param name="rect">Elemento que se coloca.</param>
        /// <param name="x">Borde izquierdo.</param>
        /// <param name="y">Borde superior, medido hacia abajo.</param>
        /// <param name="width">Ancho.</param>
        /// <param name="height">Alto.</param>
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // La cámara ocupa toda la pantalla: el zoom y la posición encuadran el mundo en el hueco libre.
        private void FitCamera(Rect free)
        {
            float aspect = free.width / free.height;
            float freeSize = Mathf.Max(WorldHeight / 2f, WorldWidth / (2f * aspect));
            float unitsPerLogical = 2f * freeSize / free.height;

            float size = LogicalSize.y / 2f * unitsPerLogical;
            float cameraX = (LogicalSize.x / 2f - free.center.x) * unitsPerLogical;
            float cameraY = WorldBottom + (LogicalSize.y / 2f - free.y) * unitsPerLogical;

            worldCamera.rect = new Rect(0f, 0f, 1f, 1f);
            worldCamera.orthographicSize = size;
            worldCamera.transform.position = new Vector3(cameraX, cameraY, worldCamera.transform.position.z);

            PlaceSpawnPoints(freeSize * aspect);
        }

        // Los puntos de aparición se abren o se juntan para que los enemigos salgan dentro de la vista.
        private void PlaceSpawnPoints(float halfWidth)
        {
            float widest = Mathf.Min(WidestSpawn, halfWidth - SpawnEdgeGap);
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Vector3 position = spawnPoints[i].position;
                position.x = SpawnPositions[i] * widest;
                spawnPoints[i].position = position;
            }

            // Al girar la pantalla, los enemigos que ya están en juego vuelven a quedar a la vista.
            if (!Application.isPlaying)
            {
                return;
            }

            float limit = halfWidth - EnemyEdgeGap;
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                Vector3 position = enemy.transform.position;
                position.x = Mathf.Clamp(position.x, -limit, limit);
                enemy.transform.position = position;
            }
        }
    }
}
