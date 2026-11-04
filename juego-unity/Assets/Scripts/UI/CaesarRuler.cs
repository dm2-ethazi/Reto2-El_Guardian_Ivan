using PuertaCastillo.Crypto;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Regla de ayuda: dos abecedarios alineados, con el de abajo desplazable por el jugador.
    /// Con el desplazamiento correcto, cada letra cifrada queda justo debajo de su original.
    /// Se usa con los botones + y - o con las flechas arriba y abajo.
    /// </summary>
    public class CaesarRuler : MonoBehaviour
    {
        [SerializeField] private TMP_Text cipherRow;
        [SerializeField] private TMP_Text plainRow;
        [SerializeField] private TMP_Text shiftLabel;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private TMP_InputField answerField;

        public int Shift { get; private set; }

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
            answerField.ActivateInputField();
        }

        private void Decrease()
        {
            ChangeShift(-1);
            answerField.ActivateInputField();
        }

        private void ChangeShift(int delta)
        {
            int size = CaesarCipher.Alphabet.Length;
            Shift = (Shift + delta + size) % size;
            Show();
        }

        private void Show()
        {
            plainRow.text = Monospaced(CaesarCipher.Alphabet);
            cipherRow.text = Monospaced(CaesarCipher.Encrypt(CaesarCipher.Alphabet, Shift));
            shiftLabel.text = Shift.ToString();
        }

        // Todas las letras ocupan el mismo ancho para que las dos filas queden alineadas.
        private static string Monospaced(string letters)
        {
            return $"<mspace=1em>{letters}</mspace>";
        }
    }
}
