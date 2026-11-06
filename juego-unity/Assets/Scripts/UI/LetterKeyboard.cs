using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Teclado QWERTY en pantalla para escribir la respuesta pulsando letras, pensado para móviles:
    /// así no aparece el teclado del sistema. Tiene las 27 letras (con la Ñ), espacio y borrar.
    /// Las teclas se crean al empezar copiando un botón de la escena, para que tengan su estilo,
    /// y <see cref="GameLayout"/> las coloca con <see cref="Arrange"/>.
    /// </summary>
    public class LetterKeyboard : MonoBehaviour
    {
        public const int Columns = 10;
        public const int Rows = 4;

        /// <summary>Letras en el orden de las teclas, fila a fila, como en un teclado español.</summary>
        public const string QwertyLetters = "QWERTYUIOPASDFGHJKLÑZXCVBNM";

        private const float Gap = 8f;

        // La última fila lleva el botón de enviar y mide más que las de letras para pulsarlo bien.
        private const float LastRowHeight = 112f;
        private const int DeleteColumns = 3;
        private const int SpaceColumns = 6;

        private readonly List<Button> letterKeys = new List<Button>();
        private Button spaceKey;
        private Button deleteKey;
        private TMP_Text spaceLabel;
        private TMP_Text deleteLabel;

        /// <summary>Se lanza con la letra pulsada, o con un espacio.</summary>
        public event Action<char> CharacterPressed;

        /// <summary>Se lanza al pulsar borrar.</summary>
        public event Action DeletePressed;

        public IReadOnlyList<Button> LetterKeys => letterKeys;

        /// <summary>Tecla de esa letra.</summary>
        public Button KeyFor(char letter)
        {
            return letterKeys[QwertyLetters.IndexOf(letter)];
        }
        public Button SpaceKey => spaceKey;
        public Button DeleteKey => deleteKey;

        /// <summary>
        /// Crea el teclado dentro de <paramref name="parent"/> copiando <paramref name="template"/>,
        /// un botón con un texto TMP como hijo.
        /// </summary>
        public static LetterKeyboard Create(Transform parent, Button template, Sprite keySprite, Color textColor)
        {
            var root = new GameObject("Teclado", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            LetterKeyboard keyboard = root.AddComponent<LetterKeyboard>();
            foreach (char letter in QwertyLetters)
            {
                char typed = letter;
                Button key = keyboard.CreateKey(template, keySprite, textColor, letter.ToString(), out _);
                key.onClick.AddListener(() => keyboard.CharacterPressed?.Invoke(typed));
                keyboard.letterKeys.Add(key);
            }

            keyboard.spaceKey = keyboard.CreateKey(template, keySprite, textColor, string.Empty, out keyboard.spaceLabel);
            keyboard.spaceKey.onClick.AddListener(() => keyboard.CharacterPressed?.Invoke(' '));
            keyboard.deleteKey = keyboard.CreateKey(template, keySprite, textColor, string.Empty, out keyboard.deleteLabel);
            keyboard.deleteKey.onClick.AddListener(() => keyboard.DeletePressed?.Invoke());
            keyboard.RefreshLabels();
            return keyboard;
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += RefreshLabels;
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= RefreshLabels;
        }

        public void SetInteractable(bool interactable)
        {
            foreach (Button key in letterKeys)
            {
                key.interactable = interactable;
            }

            spaceKey.interactable = interactable;
            deleteKey.interactable = interactable;
        }

        /// <summary>
        /// Coloca las teclas en un rectángulo de la interfaz, en unidades lógicas desde la esquina
        /// superior izquierda. Filas: Q-P, A-Ñ, Z-M y borrar, y espacio con el botón de enviar.
        /// </summary>
        public void Arrange(float x, float y, float width, float height, RectTransform submitKey)
        {
            float keyWidth = (width - (Columns - 1) * Gap) / Columns;
            float keyHeight = LetterRowHeight(height);
            float fontSize = Mathf.Clamp(Mathf.Min(keyWidth, keyHeight) * 0.5f, 22f, 48f);

            for (int i = 0; i < letterKeys.Count; i++)
            {
                int row = i / Columns;
                int column = i % Columns;
                PlaceKey(letterKeys[i], x, y, column, row, 1, keyWidth, keyHeight, fontSize);
            }

            int lastLetterColumn = letterKeys.Count % Columns;
            PlaceKey(deleteKey, x, y, lastLetterColumn, Rows - 2, DeleteColumns, keyWidth, keyHeight, fontSize * 0.7f);
            float lastRowY = y + (Rows - 1) * (keyHeight + Gap);
            PlaceKey(spaceKey, x, lastRowY, 0, 0, SpaceColumns, keyWidth, LastRowHeight, fontSize * 0.7f);
            Rect submit = SubmitKeyRect(x, y, width, height);
            GameLayoutPlace(submitKey, submit.x, submit.y, submit.width, submit.height);
        }

        /// <summary>Hueco del botón de enviar: la última fila, a la derecha del espacio.</summary>
        public static Rect SubmitKeyRect(float x, float y, float width, float height)
        {
            float keyWidth = (width - (Columns - 1) * Gap) / Columns;
            float left = SpaceColumns * (keyWidth + Gap);
            return new Rect(x + left, y + (Rows - 1) * (LetterRowHeight(height) + Gap), width - left, LastRowHeight);
        }

        /// <summary>Alto que ocupan las cuatro filas con teclas de letra de ese alto.</summary>
        public static float HeightFor(float keyHeight)
        {
            return (Rows - 1) * keyHeight + LastRowHeight + (Rows - 1) * Gap;
        }

        private static float LetterRowHeight(float height)
        {
            return (height - LastRowHeight - (Rows - 1) * Gap) / (Rows - 1);
        }

        private void PlaceKey(Button key, float x, float y, int column, int row, int span, float keyWidth, float keyHeight, float fontSize)
        {
            float width = span * keyWidth + (span - 1) * Gap;
            GameLayoutPlace((RectTransform)key.transform, x + column * (keyWidth + Gap), y + row * (keyHeight + Gap), width, keyHeight);
            TMP_Text label = key.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing = false;
            label.fontSize = fontSize;
        }

        // Mismo criterio que GameLayout: esquina superior izquierda y unidades lógicas.
        private static void GameLayoutPlace(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private Button CreateKey(Button template, Sprite keySprite, Color textColor, string text, out TMP_Text label)
        {
            Button key = Instantiate(template, transform, false);
            key.name = text.Length > 0 ? $"Tecla{text}" : "Tecla";
            key.onClick.RemoveAllListeners();

            // La copia no debe traer animaciones ni textos traducidos del botón original.
            foreach (MonoBehaviour behaviour in key.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ButtonAnimation || behaviour is LocalizedText)
                {
                    DestroyImmediate(behaviour);
                }
            }

            key.transform.localScale = Vector3.one;
            if (keySprite != null)
            {
                key.image.sprite = keySprite;
                key.image.type = Image.Type.Sliced;
            }

            // Que el teclado no se quede con el foco: así las flechas y el Intro siguen siendo del juego.
            Navigation navigation = key.navigation;
            navigation.mode = Navigation.Mode.None;
            key.navigation = navigation;

            label = key.GetComponentInChildren<TMP_Text>();
            label.text = text;
            label.color = textColor;
            label.alignment = TextAlignmentOptions.Center;
            return key;
        }

        private void RefreshLabels()
        {
            if (spaceLabel != null)
            {
                spaceLabel.text = Localization.Get("game.space");
                deleteLabel.text = Localization.Get("game.delete");
            }
        }
    }
}
