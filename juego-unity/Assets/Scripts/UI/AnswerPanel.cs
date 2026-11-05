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
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Image fieldBackground;
        [SerializeField] private Color normalColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        [SerializeField] private Color errorColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Color correctTextColor = new Color32(0x3C, 0x8D, 0x5A, 0xFF);
        [SerializeField] private Color wrongTextColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);
        [SerializeField, Min(0.1f)] private float feedbackSeconds = 2f;

        private float feedbackTimer;

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
        }

        /// <summary>Bloquea la respuesta mientras se muestra un panel de nivel.</summary>
        public void SetInteractable(bool interactable)
        {
            answerField.interactable = interactable;
            decryptButton.interactable = interactable;

            if (interactable)
            {
                answerField.ActivateInputField();
            }
            else
            {
                HideFeedback();
            }
        }

        private void SubmitField()
        {
            Submit(answerField.text);
        }

        // Lo llaman tanto Intro como el botón. Un campo vacío no cuenta como fallo.
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
            ShowFeedback(solved);
            StartCoroutine(RefocusField());
        }

        private void ShowFeedback(bool solved)
        {
            feedbackLabel.text = Localization.Get(solved ? "answer.correct" : "answer.wrong");
            feedbackLabel.color = solved ? correctTextColor : wrongTextColor;
            feedbackTimer = feedbackSeconds;
        }

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

        // Intro y el botón le quitan el foco al campo: se devuelve un frame después para seguir escribiendo.
        private IEnumerator RefocusField()
        {
            yield return null;
            answerField.ActivateInputField();
        }
    }
}
