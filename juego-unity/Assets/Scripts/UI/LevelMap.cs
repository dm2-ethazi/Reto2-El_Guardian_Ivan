using System.Collections.Generic;
using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Mapa de niveles: una imagen ilustrada (MapaProgresion.png: el camino, el paisaje, el castillo
    /// del jefe y un medallón por nivel) con un botón encima de cada medallón, en las posiciones
    /// configuradas en el inspector. Cada nodo puede estar bloqueado (oscurecido, con candado, no se
    /// puede pulsar), disponible (anillo de chispas doradas) o superado (marca verde).
    /// Al pulsar un nodo se juega ese nivel.
    /// La imagen se ve entera y sin deformarse (con bandas si la pantalla tiene otra proporción);
    /// en vertical se usa la imagen vertical si la hay. Los nodos se crean al darle a Play.
    /// </summary>
    public class LevelMap : MonoBehaviour
    {
        /// <summary>Nombre de la escena del mapa de niveles.</summary>
        public const string SceneName = "MapaNiveles";
        private const string GameSceneName = "PuertaDelCastillo";

        // Paleta del juego.
        private static readonly Color Night = new Color32(0x1B, 0x1B, 0x3A, 0xFF);
        private static readonly Color Gold = new Color32(0xD4, 0xA0, 0x17, 0xFF);
        private static readonly Color Parchment = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        private static readonly Color Ink = new Color32(0x22, 0x22, 0x3B, 0xFF);
        private static readonly Color Green = new Color32(0x3C, 0x8D, 0x5A, 0xFF);

        private const float Margin = 32f;
        private const float TopBarHeight = 110f;
        private const float BackButtonWidth = 260f;
        private const float FrameWidth = 8f;
        private const float MinNodeSize = 100f;

        /// <summary>Escalador del Canvas del mapa.</summary>
        [SerializeField] private CanvasScaler scaler;
        /// <summary>Número de niveles del juego. El último es el castillo del jefe.</summary>
        [Tooltip("Número de niveles del juego. El último es el castillo del jefe.")]
        [SerializeField, Min(1)] private int levelCount = 5;

        /// <summary>Ilustración del mapa en horizontal.</summary>
        [Header("Imagen del mapa")]
        [Tooltip("Ilustración del mapa en horizontal, con el camino y un hueco para cada nivel.")]
        [SerializeField] private Sprite mapImage;
        /// <summary>Centro de cada nivel en la imagen horizontal, de 0 a 1 desde abajo a la izquierda.</summary>
        [Tooltip("Centro de cada nivel en la imagen horizontal: 0..1 desde abajo a la izquierda.")]
        [SerializeField] private Vector2[] nodePositions =
        {
            new Vector2(0.153f, 0.325f), new Vector2(0.302f, 0.431f), new Vector2(0.481f, 0.474f),
            new Vector2(0.634f, 0.527f), new Vector2(0.769f, 0.570f)
        };
        /// <summary>Ilustración opcional para pantallas en vertical.</summary>
        [Tooltip("Opcional: ilustración para pantallas en vertical. Sin ella se usa la horizontal.")]
        [SerializeField] private Sprite portraitMapImage;
        /// <summary>Centro de cada nivel en la imagen vertical.</summary>
        [Tooltip("Centro de cada nivel en la imagen vertical, como en la horizontal.")]
        [SerializeField] private Vector2[] portraitNodePositions = new Vector2[0];
        /// <summary>Diámetro de los medallones de la imagen respecto a su alto.</summary>
        [Tooltip("Diámetro de los medallones de la imagen respecto a su alto.")]
        [SerializeField, Range(0.05f, 0.3f)] private float nodeSizeRatio = 0.096f;

        /// <summary>Fondo que cubre la pantalla detrás del mapa.</summary>
        [Header("Sprites")]
        [SerializeField] private Sprite background;
        /// <summary>Pergamino liso que se usa si no hay ilustración asignada.</summary>
        [SerializeField] private Sprite board;
        /// <summary>Círculo para nodos, chispas e insignias.</summary>
        [SerializeField] private Sprite circle;
        /// <summary>Cuadrado para sombras, marco, candado y marcas.</summary>
        [SerializeField] private Sprite square;
        /// <summary>Sprite del botón de volver al menú.</summary>
        [SerializeField] private Sprite buttonSprite;

        /// <summary>Fuente del título del mapa.</summary>
        [Header("Fuentes")]
        [SerializeField] private TMP_FontAsset titleFont;
        /// <summary>Fuente de los textos de los botones.</summary>
        [SerializeField] private TMP_FontAsset textFont;

        private readonly List<Button> nodes = new List<Button>();
        private readonly List<Rect> nodeAreas = new List<Rect>();
        private RectTransform content;
        private Vector2 lastSize;
        private Rect lastSafeArea;

        /// <summary>Botón de cada nivel, en orden (el primero es el nivel 1).</summary>
        public IReadOnlyList<Button> Nodes => nodes;

        /// <summary>Zona de cada nodo en unidades lógicas, con el origen en el centro de la pantalla.</summary>
        public IReadOnlyList<Rect> NodeAreas => nodeAreas;

        /// <summary>Botón para volver al menú principal.</summary>
        public Button BackButton { get; private set; }

        /// <summary>Zona del botón de volver al menú, como <see cref="NodeAreas"/>.</summary>
        public Rect BackButtonArea { get; private set; }

        /// <summary>Tamaño de la pantalla en unidades lógicas.</summary>
        public Vector2 LogicalSize { get; private set; }

        private void OnEnable()
        {
            Localization.LanguageChanged += Rebuild;
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= Rebuild;
        }

        private void Start()
        {
            Refresh();
        }

        // Solo se redibuja cuando cambia el tamaño de la pantalla o el área segura.
        // Escape (o el botón Atrás de Android) vuelve al menú.
        private void Update()
        {
            Refresh();

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                GoToMenu();
            }
        }

        private void Refresh()
        {
            Vector2 size = ScreenFit.CurrentSize;
            Rect safe = Screen.safeArea;
            if (size != lastSize || safe != lastSafeArea)
            {
                lastSize = size;
                lastSafeArea = safe;
                Apply(size.x, size.y, safe);
            }
        }

        // Al cambiar de idioma se vuelven a escribir los rótulos.
        private void Rebuild()
        {
            if (lastSize.x > 0f)
            {
                Apply(lastSize.x, lastSize.y, lastSafeArea);
            }
        }

        /// <summary>Dibuja el mapa para una pantalla sin muescas (toda ella es área segura).</summary>
        /// <param name="width">Ancho de la pantalla en píxeles.</param>
        /// <param name="height">Alto de la pantalla en píxeles.</param>
        public void Apply(float width, float height)
        {
            Apply(width, height, new Rect(0f, 0f, width, height));
        }

        /// <summary>Dibuja el mapa entero para esa pantalla, con el progreso guardado.</summary>
        /// <param name="width">Ancho de la pantalla en píxeles.</param>
        /// <param name="height">Alto de la pantalla en píxeles.</param>
        /// <param name="safeArea">Área segura en píxeles.</param>
        public void Apply(float width, float height, Rect safeArea)
        {
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            ScreenFit.Configure(scaler, width, height);
            LogicalSize = ScreenFit.LogicalSize(width, height);
            float unitsPerPixel = LogicalSize.x / width;
            bool portrait = ScreenFit.IsPortrait(width, height);

            if (content != null)
            {
                content.gameObject.SetActive(false);
                Destroy(content.gameObject);
            }
            nodes.Clear();
            nodeAreas.Clear();

            content = NewRect("Mapa", transform);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = LogicalSize;
            content.anchoredPosition = Vector2.zero;

            // Área segura en unidades lógicas, con el origen en el centro de la pantalla.
            var safe = new Rect(
                safeArea.x * unitsPerPixel - LogicalSize.x / 2f,
                safeArea.y * unitsPerPixel - LogicalSize.y / 2f,
                safeArea.width * unitsPerPixel,
                safeArea.height * unitsPerPixel);

            DrawBackground();
            Rect top = DrawTopBar(safe);
            DrawBoard(safe, top, portrait);
        }

        /// <summary>Juega un nivel (0 es el nivel 1) si está desbloqueado. Lo usan los nodos del mapa.</summary>
        /// <param name="levelIndex">Nivel que se quiere jugar (0 es el nivel 1).</param>
        public void PlayLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levelCount || !LevelProgress.IsUnlocked(levelIndex))
            {
                return;
            }

            SaveGame.RequestStartLevel(levelIndex);
            SceneManager.LoadScene(GameSceneName);
        }

        private void GoToMenu()
        {
            SceneManager.LoadScene(MainMenu.SceneName);
        }

        // Fondo del menú, oscurecido para que el pergamino destaque. Cubre la pantalla sin deformarse.
        private void DrawBackground()
        {
            Image image = NewImage("Fondo", content, background, new Color(0.55f, 0.55f, 0.65f));
            float aspect = background != null ? background.rect.width / background.rect.height : 16f / 9f;
            float imageHeight = Mathf.Max(LogicalSize.y, LogicalSize.x / aspect);
            Place(image.rectTransform, Vector2.zero, new Vector2(imageHeight * aspect, imageHeight));

            Image shade = NewImage("Sombra", content, square, new Color(Night.r, Night.g, Night.b, 0.45f));
            Place(shade.rectTransform, Vector2.zero, LogicalSize);
        }

        // Botón de volver al menú arriba a la izquierda y el título centrado. Devuelve la franja ocupada.
        private Rect DrawTopBar(Rect safe)
        {
            var bar = new Rect(safe.xMin + Margin, safe.yMax - Margin - TopBarHeight, safe.width - 2f * Margin, TopBarHeight);

            float backWidth = Mathf.Min(BackButtonWidth, bar.width * 0.3f);
            BackButtonArea = new Rect(bar.xMin, bar.yMin, backWidth, bar.height);
            Image backImage = NewImage("BotonVolverMenu", content, buttonSprite, Color.white);
            backImage.type = Image.Type.Sliced;
            backImage.raycastTarget = true;
            Place(backImage.rectTransform, BackButtonArea.center, BackButtonArea.size);
            BackButton = MakeButton(backImage);
            BackButton.onClick.AddListener(GoToMenu);
            TMP_Text backLabel = NewText("Texto", backImage.rectTransform, Localization.Get("game.menu"), textFont, Ink, 44f);
            Stretch(backLabel.rectTransform);
            backLabel.margin = new Vector4(12f, 0f, 12f, 0f);

            // El título se centra en la pantalla pero sin pisar el botón.
            float titleHalf = Mathf.Max(0f, Mathf.Min(bar.center.x - BackButtonArea.xMax - Margin, bar.xMax - bar.center.x));
            TMP_Text shadow = NewText("TituloSombra", content, Localization.Get("map.title"), titleFont, Ink, 72f);
            Place(shadow.rectTransform, bar.center + new Vector2(4f, -4f), new Vector2(titleHalf * 2f, bar.height));
            TMP_Text title = NewText("Titulo", content, Localization.Get("map.title"), titleFont, Gold, 72f);
            Place(title.rectTransform, bar.center, new Vector2(titleHalf * 2f, bar.height));
            return bar;
        }

        // Imagen del mapa entera en el hueco libre, con marco, y los nodos encima de sus huecos.
        private void DrawBoard(Rect safe, Rect top, bool portrait)
        {
            var free = new Rect(safe.xMin + Margin, safe.yMin + Margin, safe.width - 2f * Margin, top.yMin - Margin - (safe.yMin + Margin));

            bool usePortrait = portrait && portraitMapImage != null && portraitNodePositions.Length >= levelCount;
            Sprite image = usePortrait ? portraitMapImage : mapImage;
            Vector2[] positions = usePortrait ? portraitNodePositions : nodePositions;
            float aspect = image != null ? image.rect.width / image.rect.height : 16f / 9f;

            // La imagen cabe entera sin deformarse; lo que sobra se queda con el fondo oscurecido.
            float width = Mathf.Max(1f, Mathf.Min(free.width, free.height * aspect));
            var area = new Rect(free.center.x - width / 2f, free.center.y - width / aspect / 2f, width, width / aspect);

            Image frame = NewImage("Marco", content, square, Ink);
            Place(frame.rectTransform, area.center, area.size + Vector2.one * FrameWidth * 2f);
            Image picture = NewImage("ImagenMapa", content, image != null ? image : board, Color.white);
            if (image == null)
            {
                // Sin ilustración asignada, un pergamino liso para que el mapa siga funcionando.
                picture.type = Image.Type.Sliced;
                picture.pixelsPerUnitMultiplier = 0.5f;
            }
            Place(picture.rectTransform, area.center, area.size);
            RectTransform map = picture.rectTransform;

            // El dibujo del nodo mide lo mismo que el medallón de la imagen; la zona pulsable, al menos MinNodeSize.
            float nodeSize = area.height * nodeSizeRatio;
            float hitSize = Mathf.Max(nodeSize, MinNodeSize);
            int completed = LevelProgress.CompletedCount;
            for (int i = 0; i < levelCount; i++)
            {
                Vector2 normalized = positions.Length > 0 ? positions[Mathf.Min(i, positions.Length - 1)] : new Vector2((i + 0.5f) / levelCount, 0.5f);
                Vector2 center = new Vector2(area.xMin + normalized.x * area.width, area.yMin + normalized.y * area.height);
                DrawNode(i, map, center - area.center, nodeSize, hitSize, LevelProgress.StateOf(i, completed));
                // Las zonas pulsables de los nodos se dan respecto al centro de la pantalla.
                nodeAreas.Add(new Rect(center - Vector2.one * hitSize / 2f, Vector2.one * hitSize));
            }
        }

        // Nodo de un nivel, encima del medallón pintado en la imagen, que se sigue viendo:
        // disponible lleva un anillo de chispas doradas, bloqueado se oscurece con un candado
        // y superado lleva una marca verde. La zona pulsable mide al menos MinNodeSize.
        private void DrawNode(int index, RectTransform map, Vector2 center, float size, float hitSize, LevelState state)
        {
            Image root = NewImage("Nodo" + (index + 1), map, circle, Color.clear);
            root.raycastTarget = true;
            Place(root.rectTransform, center, Vector2.one * hitSize);

            Button button = MakeButton(root);
            button.interactable = state != LevelState.Locked;
            int level = index;
            button.onClick.AddListener(() => PlayLevel(level));
            nodes.Add(button);

            if (state == LevelState.Available)
            {
                const int Sparks = 12;
                for (int i = 0; i < Sparks; i++)
                {
                    float angle = i * Mathf.PI * 2f / Sparks;
                    Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * 0.64f;
                    Place(NewImage("Chispa", root.rectTransform, circle, Gold).rectTransform, position, Vector2.one * size * 0.1f);
                }
            }
            else if (state == LevelState.Locked)
            {
                Place(NewImage("Sombra", root.rectTransform, circle, WithAlpha(Ink, 0.82f)).rectTransform, Vector2.zero, Vector2.one * size);
                DrawLock(root.rectTransform, Vector2.zero, size * 0.5f);
            }
            else
            {
                Vector2 badgeCenter = new Vector2(size * 0.38f, size * 0.38f);
                DrawBadge(root.rectTransform, badgeCenter, size * 0.42f, Parchment);
                DrawCheck(root.rectTransform, badgeCenter, size * 0.32f);
            }
        }

        // Círculo con contorno oscuro donde se ponen el candado o la marca de superado.
        private void DrawBadge(RectTransform parent, Vector2 center, float size, Color color)
        {
            Place(NewImage("InsigniaBorde", parent, circle, Ink).rectTransform, center, Vector2.one * size);
            Place(NewImage("Insignia", parent, circle, color).rectTransform, center, Vector2.one * size * 0.8f);
        }

        // Candado: cuerpo con ojo de cerradura y arco hecho con tres barras.
        private void DrawLock(RectTransform parent, Vector2 center, float size)
        {
            RectTransform padlock = NewRect("Candado", parent);
            Place(padlock, center, Vector2.one * size);
            Color metal = Parchment;
            Place(NewImage("ArcoIzquierdo", padlock, square, metal).rectTransform, new Vector2(-size * 0.21f, size * 0.17f), new Vector2(size * 0.12f, size * 0.36f));
            Place(NewImage("ArcoDerecho", padlock, square, metal).rectTransform, new Vector2(size * 0.21f, size * 0.17f), new Vector2(size * 0.12f, size * 0.36f));
            Place(NewImage("ArcoArriba", padlock, square, metal).rectTransform, new Vector2(0f, size * 0.33f), new Vector2(size * 0.54f, size * 0.12f));
            Place(NewImage("Cuerpo", padlock, square, metal).rectTransform, new Vector2(0f, -size * 0.17f), new Vector2(size * 0.66f, size * 0.48f));
            Place(NewImage("Cerradura", padlock, square, Ink).rectTransform, new Vector2(0f, -size * 0.17f), new Vector2(size * 0.1f, size * 0.2f));
        }

        // Marca de superado: dos trazos verdes.
        private void DrawCheck(RectTransform parent, Vector2 center, float size)
        {
            float thickness = size * 0.2f;
            Vector2 corner = center + new Vector2(-size * 0.1f, -size * 0.25f);
            Segment("MarcaCorta", parent, center + new Vector2(-size * 0.4f, 0f), corner, thickness, Green);
            Segment("MarcaLarga", parent, corner, center + new Vector2(size * 0.4f, size * 0.32f), thickness, Green);
        }

        // Tramo recto entre dos puntos: un rectángulo girado.
        private void Segment(string name, RectTransform parent, Vector2 from, Vector2 to, float width, Color color)
        {
            Image line = NewImage(name, parent, square, color);
            Vector2 delta = to - from;
            Place(line.rectTransform, (from + to) / 2f, new Vector2(delta.magnitude, width));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // Botón sin animaciones ni fundidos, como los del resto del juego: los apagados se ven grises.
        private static Button MakeButton(Image image)
        {
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.fadeDuration = 0f;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            return button;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            Image image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text NewText(string name, Transform parent, string text, TMP_FontAsset font, Color color, float size)
        {
            TextMeshProUGUI label = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                label.font = font;
            }
            label.text = text;
            label.color = color;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        // Coloca un elemento por su centro, con el ancla en el centro del padre.
        private static void Place(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
