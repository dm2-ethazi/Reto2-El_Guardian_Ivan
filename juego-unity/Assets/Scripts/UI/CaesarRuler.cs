using System.Collections.Generic;
using System.Linq;
using PuertaCastillo.Crypto;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Regla de ayuda: una casilla por letra, con la original arriba y su cifrada justo debajo.
    /// Las casillas van en dos bloques (A-N y, centrado debajo, Ñ-Z) para que las letras se lean bien
    /// en un móvil. Cada casilla es una tecla independiente con una línea fina entre las dos letras.
    /// Se crean al empezar copiando los textos de la escena y las coloca <see cref="GameLayout"/>.
    /// El desplazamiento se cambia con los botones + y - o con las flechas arriba y abajo.
    /// </summary>
    public class CaesarRuler : MonoBehaviour
    {
        private const int BlockColumns = 14;
        private const float CellGap = 6f;
        private const float BlockGap = 12f;
        private const float FontToCell = 0.36f;
        private const float BorderScale = 1.6f;
        private const float ShiftFontSize = 48f;

        [SerializeField] private TMP_Text cipherRow;
        [SerializeField] private TMP_Text plainRow;
        [SerializeField] private TMP_Text shiftLabel;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;

        [Header("Casillas")]
        [Tooltip("Sprite de cada casilla, en Sliced (el de las teclas).")]
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Color dividerColor = new Color32(0x4A, 0x4E, 0x69, 0x80);
        [SerializeField] private Color plainColor = new Color32(0x22, 0x22, 0x3B, 0xFF);
        [SerializeField] private Color cipherColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);

        private readonly List<RectTransform> cells = new List<RectTransform>();
        private readonly List<TMP_Text> plainLabels = new List<TMP_Text>();
        private readonly List<TMP_Text> cipherLabels = new List<TMP_Text>();

        public int Shift { get; private set; }
        public IReadOnlyList<RectTransform> Cells => cells;
        public string PlainLetters => string.Concat(plainLabels.Select(label => label.text));
        public string CipherLetters => string.Concat(cipherLabels.Select(label => label.text));

        /// <summary>Alto de los dos bloques con casillas de ese alto.</summary>
        public static float GridHeight(float cellHeight)
        {
            return 2f * cellHeight + BlockGap;
        }

        private void Awake()
        {
            shiftLabel.enableAutoSizing = false;
            shiftLabel.fontSize = ShiftFontSize;

            string alphabet = CaesarCipher.Alphabet;
            for (int i = 0; i < alphabet.Length; i++)
            {
                CreateCell(alphabet[i], i);
            }

            plainRow.gameObject.SetActive(false);
            cipherRow.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            increaseButton.onClick.AddListener(Increase);
            decreaseButton.onClick.AddListener(Decrease);
        }

        private void OnDisable()
        {
            increaseButton.onClick.RemoveListener(Increase);
            decreaseButton.onClick.RemoveListener(Decrease);
        }

        private void Start()
        {
            Show();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                ChangeShift(1);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                ChangeShift(-1);
            }
        }

        private void Increase()
        {
            ChangeShift(1);
        }

        private void Decrease()
        {
            ChangeShift(-1);
        }

        private void ChangeShift(int delta)
        {
            int size = CaesarCipher.Alphabet.Length;
            Shift = (Shift + delta + size) % size;
            Show();
        }

        /// <summary>Coloca las casillas en un rectángulo de la interfaz, en unidades lógicas.</summary>
        public void Arrange(float x, float y, float width, float cellHeight)
        {
            float cellWidth = (width - (BlockColumns - 1) * CellGap) / BlockColumns;
            float fontSize = Mathf.Min(cellHeight * FontToCell, cellWidth * 0.62f);

            for (int i = 0; i < cells.Count; i++)
            {
                int row = i / BlockColumns;
                int column = i % BlockColumns;
                // El segundo bloque tiene una casilla menos: se centra bajo el primero.
                int rowCount = Mathf.Min(BlockColumns, cells.Count - row * BlockColumns);
                float rowOffset = (BlockColumns - rowCount) * (cellWidth + CellGap) / 2f;
                float cellX = x + rowOffset + column * (cellWidth + CellGap);
                float cellY = y + row * (cellHeight + BlockGap);
                GameLayout.Place(cells[i], cellX, cellY, cellWidth, cellHeight);
                plainLabels[i].fontSize = fontSize;
                cipherLabels[i].fontSize = fontSize;
            }
        }

        private void Show()
        {
            string cipher = CaesarCipher.Encrypt(CaesarCipher.Alphabet, Shift);
            for (int i = 0; i < cipherLabels.Count; i++)
            {
                cipherLabels[i].text = cipher[i].ToString();
            }

            shiftLabel.text = Shift.ToString();
        }

        // Una casilla con la letra original en su mitad de arriba y la cifrada en la de abajo.
        private void CreateCell(char letter, int index)
        {
            var cell = new GameObject($"Casilla{letter}", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)cell.transform;
            rect.SetParent(transform, false);

            Image background = cell.GetComponent<Image>();
            background.sprite = cellSprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = BorderScale;
            background.raycastTarget = false;

            // Línea fina entre la letra original y la cifrada, sin llegar al borde de la casilla.
            var divider = new GameObject("Separador", typeof(RectTransform), typeof(Image));
            var dividerRect = (RectTransform)divider.transform;
            dividerRect.SetParent(rect, false);
            dividerRect.anchorMin = new Vector2(0.2f, 0.5f);
            dividerRect.anchorMax = new Vector2(0.8f, 0.5f);
            dividerRect.sizeDelta = new Vector2(0f, 2f);
            Image dividerImage = divider.GetComponent<Image>();
            dividerImage.color = dividerColor;
            dividerImage.raycastTarget = false;

            cells.Add(rect);
            plainLabels.Add(CreateLabel(plainRow, rect, letter.ToString(), plainColor, 0.5f, 1f));
            cipherLabels.Add(CreateLabel(cipherRow, rect, string.Empty, cipherColor, 0f, 0.5f));
        }

        private static TMP_Text CreateLabel(TMP_Text template, Transform parent, string text, Color color, float bottom, float top)
        {
            TMP_Text label = Instantiate(template, parent, false);
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.color = color;
            label.text = text;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            label.margin = Vector4.zero;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            return label;
        }
    }
}
