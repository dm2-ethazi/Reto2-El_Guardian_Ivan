using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using PuertaCastillo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas de la respuesta: escribir con el abecedario en pantalla, acertar, fallar y los avisos.</summary>
    public class AnswerTests
    {
        private EnemySpawner spawner;
        private EnemyTargeting targeting;
        private AnswerPanel panel;
        private TMP_InputField field;
        private Door door;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            yield return null;
            spawner = Object.FindAnyObjectByType<EnemySpawner>();
            targeting = Object.FindAnyObjectByType<EnemyTargeting>();
            panel = Object.FindAnyObjectByType<AnswerPanel>();
            field = panel.GetComponentInChildren<TMP_InputField>();
            door = Object.FindAnyObjectByType<Door>();
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void SceneCanReceiveUiInput()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>());
        }

        [Test]
        public void TargetIsHighlightedAndShownOnTheTopBar()
        {
            Enemy target = targeting.Target;

            Assert.IsTrue(target.IsTargeted);
            StringAssert.Contains(target.EncryptedMessage, TopBarText());
        }

        [UnityTest]
        public IEnumerator TargetIsAlwaysTheClosestToTheDoor()
        {
            Time.timeScale = 10f;
            yield return new WaitUntil(() => spawner.ActiveEnemies.Count == 2);
            yield return null;

            Enemy closest = spawner.ActiveEnemies
                .OrderBy(enemy => Vector2.Distance(enemy.transform.position, door.transform.position))
                .First();

            Assert.AreSame(closest, targeting.Target);
            Assert.AreEqual(1, spawner.ActiveEnemies.Count(enemy => enemy.IsTargeted));
        }

        [UnityTest]
        public IEnumerator CorrectAnswerDestroysTheTargetIgnoringCaseAndSpaces()
        {
            Enemy target = targeting.Target;
            field.text = $"  {target.Message.ToLowerInvariant()}  ";

            field.onSubmit.Invoke(field.text);
            yield return null;

            Assert.IsTrue(target == null);
            Assert.AreEqual(string.Empty, field.text);
            Assert.AreEqual(Localization.Get("answer.correct"), FeedbackText());
        }

        [UnityTest]
        public IEnumerator DecryptButtonAlsoSubmitsTheAnswer()
        {
            Enemy target = targeting.Target;
            field.text = target.Message;

            panel.GetComponentInChildren<Button>().onClick.Invoke();
            yield return null;

            Assert.IsTrue(target == null);
        }

        [UnityTest]
        public IEnumerator WrongAnswerKeepsTheEnemyAndMarksTheFieldInRed()
        {
            Enemy target = targeting.Target;
            Image background = field.GetComponent<Image>();
            Color normal = background.color;
            field.text = "RESPUESTA MALA";

            field.onSubmit.Invoke(field.text);
            yield return null;

            Assert.IsTrue(target != null);
            Assert.AreEqual(string.Empty, field.text);
            Assert.AreNotEqual(normal, background.color);
            Assert.AreEqual(Localization.Get("answer.wrong"), FeedbackText());

            field.text = "A";
            Assert.AreEqual(normal, background.color);
            Assert.AreEqual(string.Empty, FeedbackText());
        }

        [UnityTest]
        public IEnumerator FeedbackDisappearsAfterAFewSeconds()
        {
            field.text = "RESPUESTA MALA";
            field.onSubmit.Invoke(field.text);
            yield return null;
            Assert.AreEqual(Localization.Get("answer.wrong"), FeedbackText());

            yield return new WaitForSeconds(2.5f);

            Assert.AreEqual(string.Empty, FeedbackText());
        }

        [UnityTest]
        public IEnumerator AnswerOfAnotherEnemyDoesNotCount()
        {
            Time.timeScale = 10f;
            yield return new WaitUntil(() => spawner.ActiveEnemies.Count == 2);
            Time.timeScale = 1f;
            yield return null;

            Enemy other = spawner.ActiveEnemies.First(enemy => enemy != targeting.Target);

            Assert.IsFalse(targeting.TrySolve(other.Message));
            Assert.IsTrue(other != null);
        }

        [UnityTest]
        public IEnumerator NextClosestEnemyBecomesTheTargetAfterSolving()
        {
            Time.timeScale = 10f;
            yield return new WaitUntil(() => spawner.ActiveEnemies.Count == 2);
            Time.timeScale = 1f;
            yield return null;

            Enemy first = targeting.Target;
            Enemy second = spawner.ActiveEnemies.First(enemy => enemy != first);

            Assert.IsTrue(targeting.TrySolve(first.Message));
            yield return null;
            yield return null;

            Assert.AreSame(second, targeting.Target);
            Assert.IsTrue(second.IsTargeted);
        }

        [UnityTest]
        public IEnumerator SolvingAnAttackerStopsTheDamage()
        {
            Enemy target = targeting.Target;
            spawner.enabled = false;
            Time.timeScale = 5f;
            yield return new WaitUntil(() => target.IsAttacking);
            Time.timeScale = 1f;
            yield return new WaitForSeconds(2.5f);

            Assert.IsTrue(targeting.TrySolve(target.Message));
            int health = door.CurrentHealth;
            yield return new WaitForSeconds(2f);

            Assert.Less(health, 100);
            Assert.AreEqual(health, door.CurrentHealth);
        }

        [Test]
        public void KeyboardFollowsQwertyWithEveryLetterIncludingEnye()
        {
            string letters = string.Concat(panel.OnScreenKeyboard.LetterKeys.Select(key => key.GetComponentInChildren<TMP_Text>().text));

            Assert.AreEqual("QWERTYUIOPASDFGHJKLÑZXCVBNM", letters);
            CollectionAssert.AreEquivalent(PuertaCastillo.Crypto.CaesarCipher.Alphabet, letters);
        }

        [Test]
        public void AnswerFieldNeverOpensTheSystemKeyboard()
        {
            Assert.IsTrue(field.readOnly);
            Assert.IsTrue(field.shouldHideSoftKeyboard);
        }

        [UnityTest]
        public IEnumerator TappingLettersWritesTheAnswerAndSolvesTheTarget()
        {
            Enemy target = targeting.Target;
            LetterKeyboard keyboard = panel.OnScreenKeyboard;

            foreach (char letter in target.Message)
            {
                if (letter == ' ')
                {
                    keyboard.SpaceKey.onClick.Invoke();
                }
                else
                {
                    keyboard.KeyFor(letter).onClick.Invoke();
                }
            }

            Assert.AreEqual(target.Message, field.text);

            panel.GetComponentInChildren<Button>().onClick.Invoke();
            yield return null;

            Assert.IsTrue(target == null);
        }

        [Test]
        public void DeleteKeyRemovesTheLastLetter()
        {
            LetterKeyboard keyboard = panel.OnScreenKeyboard;
            keyboard.KeyFor('A').onClick.Invoke();
            keyboard.KeyFor('Ñ').onClick.Invoke();

            Assert.AreEqual("AÑ", field.text);

            keyboard.DeleteKey.onClick.Invoke();
            Assert.AreEqual("A", field.text);
        }

        [Test]
        public void KeysDoNothingWhileALevelPanelIsShown()
        {
            panel.SetInteractable(false);
            panel.OnScreenKeyboard.LetterKeys[0].onClick.Invoke();

            Assert.AreEqual(string.Empty, field.text);
            Assert.IsFalse(panel.OnScreenKeyboard.LetterKeys[0].interactable);
        }

        private string FeedbackText()
        {
            return panel.GetComponentsInChildren<TMP_Text>().First(label => label.name == "TextoRespuesta").text;
        }

        private string TopBarText()
        {
            return panel.GetComponentsInChildren<TMP_Text>().First(label => label.name == "TextoObjetivo").text;
        }
    }
}
