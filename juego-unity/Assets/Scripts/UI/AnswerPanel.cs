using System.Collections;
using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Campo de respuesta del jugador y barra superior con el mensaje del enemigo objetivo.
    /// </summary>
    public class AnswerPanel : MonoBehaviour
    {
        [SerializeField] private EnemyTargeting targeting;
        [SerializeField] private TMP_InputField answerField;
        [SerializeField] private Button decryptButton;
        [SerializeField] private TMP_Text targetLabel;
        [SerializeField] private Image fieldBackground;
        [SerializeField] private Color normalColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        [SerializeField] private Color errorColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);

        private void OnEnable()
        {
            answerField.onSubmit.AddListener(Submit);
            answerField.onValueChanged.AddListener(ClearError);
            decryptButton.onClick.AddListener(SubmitField);
        }

        private void OnDisable()
        {
            answerField.onSubmit.RemoveListener(Submit);
            answerField.onValueChanged.RemoveListener(ClearError);
            decryptButton.onClick.RemoveListener(SubmitField);
        }

        private void Start()
        {
            answerField.ActivateInputField();
        }

        private void Update()
        {
            targetLabel.text = targeting.Target != null ? $"Objetivo: {targeting.Target.EncryptedMessage}" : "Objetivo: -";
        }

        private void SubmitField()
        {
            Submit(answerField.text);
        }

        private void Submit(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                StartCoroutine(RefocusField());
                return;
            }

            bool solved = targeting.TrySolve(answer);
            // Vaciar sin avisar para que el color de error se mantenga hasta que vuelva a escribir.
            answerField.SetTextWithoutNotify(string.Empty);
            fieldBackground.color = solved ? normalColor : errorColor;
            StartCoroutine(RefocusField());
        }

        private void ClearError(string _)
        {
            fieldBackground.color = normalColor;
        }

        // Intro y el botón le quitan el foco al campo: se devuelve un frame después para seguir escribiendo.
        private IEnumerator RefocusField()
        {
            yield return null;
            answerField.ActivateInputField();
        }
    }
}
