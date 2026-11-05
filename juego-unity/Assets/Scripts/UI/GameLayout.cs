using PuertaCastillo.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Coloca la interfaz de la partida según la pantalla y encuadra el escenario en el hueco libre.
    /// El escenario ocupa toda la pantalla, con la interfaz flotando encima. En vertical los datos
    /// van arriba y la respuesta abajo; en horizontal, todo en una columna a la izquierda.
    /// </summary>
    [ExecuteAlways]
    public class GameLayout : MonoBehaviour
    {
        private const float Margin = 16f;
        private const float Padding = 14f;
        private const float Gap = 10f;
        private const float RowHeight = 64f;
        private const float HintHeight = 76f;
        private const float CaptionHeight = 36f;
        private const float TouchSize = 124f;
        private const float MaxColumnWidth = 720f;
        private const float ColumnRatio = 0.4f;

        // Parte del mundo que debe verse siempre en el hueco libre.
        private const float WorldWidth = 10f;
        private const float WorldHeight = 12f;
        private const float WorldBottom = -7f;
        private const float SpawnEdgeGap = 2f;
        private const float EnemyEdgeGap = 1f;
        private const float WidestSpawn = 8f;
        private static readonly float[] SpawnPositions = { -1f, -0.5f, 0f, 0.5f, 1f };

        [Header("Pantalla")]
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Fondos de los paneles")]
        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform bottomPanel;

        [Header("Datos")]
        [SerializeField] private RectTransform healthIcon;
        [SerializeField] private RectTransform healthBar;
        [SerializeField] private RectTransform statusPlate;
        [SerializeField] private RectTransform timeIcon;
        [SerializeField] private RectTransform statusText;
        [SerializeField] private RectTransform targetBar;
        [SerializeField] private RectTransform hintPlate;
        [SerializeField] private RectTransform hintIcon;
        [SerializeField] private RectTransform hintText;

        [Header("Regla")]
        [SerializeField] private RectTransform rulerPlate;
        [SerializeField] private RectTransform plainCaption;
        [SerializeField] private RectTransform cipherCaption;
        [SerializeField] private RectTransform shiftText;
        [SerializeField] private RectTransform plainRow;
        [SerializeField] private RectTransform cipherRow;
        [SerializeField] private RectTransform increaseButton;
        [SerializeField] private RectTransform decreaseButton;

        [Header("Respuesta")]
        [SerializeField] private RectTransform feedbackText;
        [SerializeField] private RectTransform answerField;
        [SerializeField] private RectTransform decryptButton;

        private Vector2 lastScreen;
        private Rect lastSafeArea;

        public Vector2 LogicalSize { get; private set; }

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
                : ApplyLandscape(left, top, bottom);

            FitCamera(free);
        }

        // Datos arriba y respuesta abajo, con los botones de la regla a los lados de los abecedarios.
        private Rect ApplyPortrait(float left, float right, float top, float bottom)
        {
            float cardWidth = LogicalSize.x - left - right;
            float x = left + Padding;
            float width = cardWidth - 2f * Padding;
            float y = PlaceData(x, top + Padding, width);

            Place(plainCaption, x, y, width / 2f - 40f, CaptionHeight);
            Place(shiftText, x + width / 2f - 40f, y, 80f, CaptionHeight);
            Place(cipherCaption, x + width / 2f + 40f, y, width / 2f - 40f, CaptionHeight);
            y += CaptionHeight + Gap;

            float rowsWidth = width - 2f * (TouchSize + Gap);
            Place(decreaseButton, x, y, TouchSize, TouchSize);
            Place(rulerPlate, x + TouchSize + Gap, y, rowsWidth, TouchSize);
            PlaceRows(x + TouchSize + Gap, y, rowsWidth, TouchSize);
            Place(increaseButton, x + width - TouchSize, y, TouchSize, TouchSize);
            y += TouchSize + Padding;

            Place(topPanel, left, top, cardWidth, y - top);

            float buttonWidth = 300f;
            float cardHeight = TouchSize + 2f * Padding;
            float cardY = LogicalSize.y - bottom - cardHeight;
            float rowY = cardY + Padding;
            Place(bottomPanel, left, cardY, cardWidth, cardHeight);
            Place(answerField, x, rowY, width - buttonWidth - Gap, TouchSize);
            Place(decryptButton, x + width - buttonWidth, rowY, buttonWidth, TouchSize);
            Place(feedbackText, x, cardY - 58f, width, 50f);

            float freeBottom = LogicalSize.y - cardY;
            return new Rect(0f, freeBottom, LogicalSize.x, cardY - y);
        }

        // Todo en una columna a la izquierda, centrada y sin más alto del que necesita.
        private Rect ApplyLandscape(float left, float top, float bottom)
        {
            float cardWidth = Mathf.Min(MaxColumnWidth, LogicalSize.x * ColumnRatio);
            float x = left + Padding;
            float width = cardWidth - 2f * Padding;

            float content = RowHeight + Gap + RowHeight + 8f + Gap + HintHeight + Gap
                + CaptionHeight + Gap + TouchSize + Gap + TouchSize + Gap
                + 46f + TouchSize + Gap + TouchSize;
            float cardHeight = content + 2f * Padding;
            float cardY = Mathf.Max(top, (LogicalSize.y - cardHeight) / 2f);
            float y = PlaceData(x, cardY + Padding, width);

            Place(plainCaption, x, y, width / 2f, CaptionHeight);
            Place(cipherCaption, x + width / 2f, y, width / 2f, CaptionHeight);
            y += CaptionHeight + Gap;

            Place(rulerPlate, x, y, width, TouchSize);
            PlaceRows(x, y, width, TouchSize);
            y += TouchSize + Gap;

            Place(decreaseButton, x, y, TouchSize, TouchSize);
            Place(shiftText, x + TouchSize, y, width - 2f * TouchSize, TouchSize);
            Place(increaseButton, x + width - TouchSize, y, TouchSize, TouchSize);
            y += TouchSize + Gap;

            Place(feedbackText, x, y, width, 46f);
            y += 46f;
            Place(answerField, x, y, width, TouchSize);
            y += TouchSize + Gap;
            Place(decryptButton, x, y, width, TouchSize);

            Place(topPanel, left, cardY, cardWidth, cardHeight);
            Place(bottomPanel, 0f, 0f, 0f, 0f);

            float freeX = left + cardWidth + Margin;
            return new Rect(freeX, 0f, LogicalSize.x - freeX, LogicalSize.y);
        }

        // Vida, tiempo, objetivo y pista, iguales en las dos orientaciones. Devuelve dónde acaban.
        private float PlaceData(float left, float y, float width)
        {
            float statusWidth = Mathf.Min(300f, width * 0.42f);
            Place(healthIcon, left, y + 4f, RowHeight - 8f, RowHeight - 8f);
            Place(healthBar, left + RowHeight, y, width - statusWidth - RowHeight - Gap, RowHeight);
            Place(statusPlate, left + width - statusWidth, y, statusWidth, RowHeight);
            Place(timeIcon, left + width - statusWidth + 10f, y + 12f, 40f, 40f);
            Place(statusText, left + width - statusWidth + 54f, y, statusWidth - 62f, RowHeight);
            y += RowHeight + Gap;

            Place(targetBar, left, y, width, RowHeight + 8f);
            y += RowHeight + 8f + Gap;

            Place(hintPlate, left, y, width, HintHeight);
            Place(hintIcon, left + 12f, y + 18f, 40f, 40f);
            Place(hintText, left + 60f, y + 4f, width - 72f, HintHeight - 8f);
            return y + HintHeight + Gap;
        }

        private void PlaceRows(float x, float y, float width, float height)
        {
            float rowHeight = height / 2f - 6f;
            Place(plainRow, x + 10f, y + 6f, width - 20f, rowHeight);
            Place(cipherRow, x + 10f, y + height / 2f, width - 20f, rowHeight);
        }

        // Todo se mide desde la esquina superior izquierda, en unidades lógicas.
        private static void Place(RectTransform rect, float x, float y, float width, float height)
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
