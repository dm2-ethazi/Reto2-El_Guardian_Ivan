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

            field.text = "A";
            Assert.AreEqual(normal, background.color);
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

        private string TopBarText()
        {
            return panel.GetComponentsInChildren<TMP_Text>().First(label => label.name == "TextoObjetivo").text;
        }
    }
}
