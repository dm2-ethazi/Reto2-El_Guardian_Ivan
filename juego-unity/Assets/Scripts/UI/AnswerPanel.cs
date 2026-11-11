using PuertaCastillo.Crypto;
using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using PhysicalKeyboard = UnityEngine.InputSystem.Keyboard;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Respuesta del jugador y barra superior con el mensaje del enemigo objetivo.
    /// La respuesta se escribe pulsando las letras del abecedario en pantalla, así en Android no
    /// sale el teclado del sistema. En el ordenador también se puede escribir con el teclado físico.
    /// </summary>
    public class AnswerPanel : MonoBehaviour
    {
        [SerializeField] private EnemyTargeting targeting;
        [SerializeField] private TMP_InputField answerField;
        [SerializeField] private Button decryptButton;
        [SerializeField] private TMP_Text targetLabel;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Image fieldBackground;
        [SerializeField] private Color normalColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        [SerializeField] private Color errorColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Color correctTextColor = new Color32(0x3C, 0x8D, 0x5A, 0xFF);
        [SerializeField] private Color wrongTextColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);
        [SerializeField, Min(0.1f)] private float feedbackSeconds = 2f;

        [Header("Abecedario en pantalla")]
        [Tooltip("Botón que se copia para crear cada tecla.")]
        [SerializeField] private Button keyTemplate;
        [SerializeField] private Sprite keySprite;
        [SerializeField] private Color keyTextColor = new Color32(0x22, 0x22, 0x3B, 0xFF);
        [SerializeField, Min(1)] private int maxLength = 40;

        private float feedbackTimer;

        /// <summary>Teclado de letras. Solo existe mientras se juega.</summary>
        public LetterKeyboard OnScreenKeyboard { get; private set; }

        private void Awake()
        {
            // El campo solo muestra lo escrito: si se pudiera editar, Android abriría su teclado.
            answerField.readOnly = true;
            answerField.shouldHideSoftKeyboard = true;
            answerField.shouldHideMobileInput = true;

            if (keyTemplate != null)
            {
                OnScreenKeyboard = LetterKeyboard.Create(transform, keyTemplate, keySprite, keyTextColor);
            }
        }

        private void OnEnable()
        {
            answerField.onSubmit.AddListener(Submit);
            answerField.onValueChanged.AddListener(ClearError);
            decryptButton.onClick.AddListener(SubmitField);

            if (OnScreenKeyboard != null)
            {
                OnScreenKeyboard.CharacterPressed += Type;
                OnScreenKeyboard.DeletePressed += DeleteLast;
            }

            if (PhysicalKeyboard.current != null)
            {
                PhysicalKeyboard.current.onTextInput += OnPhysicalText;
            }
        }

        private void OnDisable()
        {
            answerField.onSubmit.RemoveListener(Submit);
            answerField.onValueChanged.RemoveListener(ClearError);
            decryptButton.onClick.RemoveListener(SubmitField);

            if (OnScreenKeyboard != null)
            {
                OnScreenKeyboard.CharacterPressed -= Type;
                OnScreenKeyboard.DeletePressed -= DeleteLast;
            }

            if (PhysicalKeyboard.current != null)
            {
                PhysicalKeyboard.current.onTextInput -= OnPhysicalText;
            }
        }

        // Actualiza el mensaje del objetivo y la pista, borra el aviso de acierto/fallo al pasar su tiempo y lee el teclado físico.
        private void Update()
        {
            targetLabel.text = Localization.Format("game.target", targeting.Target != null ? targeting.Target.EncryptedMessage : "-");
            hintLabel.text = Localization.Get(spawner.Config.hintKey);

            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;

                if (feedbackTimer <= 0f)
                {
                    feedbackLabel.text = string.Empty;
                }
            }

            ReadPhysicalKeys();
        }

        /// <summary>Bloquea la respuesta mientras se muestra un panel de nivel.</summary>
        public void SetInteractable(bool interactable)
        {
            answerField.interactable = interactable;
            decryptButton.interactable = interactable;

            if (OnScreenKeyboard != null)
            {
                OnScreenKeyboard.SetInteractable(interactable);
            }

            if (!interactable)
            {
                HideFeedback();
            }
        }

        /// <summary>Añade una letra o un espacio a la respuesta, como al pulsar su tecla.</summary>
        public void Type(char character)
        {
            ReleaseFocus();
            if (!answerField.interactable || answerField.text.Length >= maxLength)
            {
                return;
            }

            answerField.text += character;
        }

        /// <summary>Borra la última letra de la respuesta.</summary>
        public void DeleteLast()
        {
            ReleaseFocus();
            if (!answerField.interactable || answerField.text.Length == 0)
            {
                return;
            }

            answerField.text = answerField.text.Substring(0, answerField.text.Length - 1);
        }

        // Envía lo escrito. Lo usan el botón de descifrar y la tecla Intro.
        private void SubmitField()
        {
            ReleaseFocus();
            Submit(answerField.text);
        }

        // Lo llaman el botón de descifrar e Intro. Un campo vacío no cuenta como fallo.
        private void Submit(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return;
            }

            bool solved = targeting.TrySolve(answer);
            MusicPlayer.PlayEffect(solved ? SoundCue.Correct : SoundCue.Incorrect);
            // Vaciar sin avisar para que el color de error se mantenga hasta que vuelva a escribir.
            answerField.SetTextWithoutNotify(string.Empty);
            fieldBackground.color = solved ? normalColor : errorColor;
            ShowFeedback(solved);
        }

        // Teclado físico: letras y espacio llegan como texto; borrar e Intro, como teclas.
        private void OnPhysicalText(char character)
        {
            char upper = char.ToUpperInvariant(character);
            if (upper == ' ' || CaesarCipher.Alphabet.IndexOf(upper) >= 0)
            {
                Type(upper);
            }
        }

        // Borrar e Intro del teclado físico (las letras llegan por OnPhysicalText).
        private void ReadPhysicalKeys()
        {
            PhysicalKeyboard keyboard = PhysicalKeyboard.current;
            if (keyboard == null || !answerField.interactable)
            {
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                DeleteLast();
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitField();
            }
        }

        // Enseña "¡Correcto!" o "Incorrecto" durante feedbackSeconds.
        private void ShowFeedback(bool solved)
        {
            feedbackLabel.text = Localization.Get(solved ? "answer.correct" : "answer.wrong");
            feedbackLabel.color = solved ? correctTextColor : wrongTextColor;
            feedbackTimer = feedbackSeconds;
        }

        // Quita el aviso de acierto o fallo.
        private void HideFeedback()
        {
            feedbackLabel.text = string.Empty;
            feedbackTimer = 0f;
        }

        // Al volver a escribir desaparecen el color de error y el mensaje.
        private void ClearError(string _)
        {
            fieldBackground.color = normalColor;
            HideFeedback();
        }

        // Un botón pulsado se queda con el foco y el Intro lo volvería a pulsar: se le quita.
        private static void ReleaseFocus()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
