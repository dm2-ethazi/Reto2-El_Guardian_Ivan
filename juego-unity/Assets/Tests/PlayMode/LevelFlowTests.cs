using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using PuertaCastillo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    public class LevelFlowTests
    {
        private LevelManager manager;
        private EnemySpawner spawner;
        private Door door;
        private LevelHud hud;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            FindObjects();
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void StartsOnLevelOneShowingItsTime()
        {
            Assert.AreEqual(1, manager.LevelNumber);
            StringAssert.Contains("Nivel 1", Text("TextoEstado"));
            StringAssert.Contains("01:00", Text("TextoEstado"));
            Assert.IsFalse(Panel().activeSelf);
        }

        [UnityTest]
        public IEnumerator TimerEndsTheLevelAndRepairsTheDoor()
        {
            door.TakeDamage(50);
            yield return FinishLevel();

            Assert.IsTrue(Panel().activeSelf);
            StringAssert.Contains("completado", Text("TituloPanel"));
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
            Assert.AreEqual(70, door.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator RepairNeverExceedsTheMaximum()
        {
            yield return FinishLevel();

            Assert.AreEqual(100, door.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator NextLevelButtonLoadsTheNextLevelKeepingTheDoor()
        {
            door.TakeDamage(50);
            yield return FinishLevel();
            Time.timeScale = 1f;

            Press("BotonSiguiente");
            yield return null;
            yield return null;

            Assert.AreEqual(2, manager.LevelNumber);
            Assert.AreEqual(5, spawner.Config.caesarShift);
            Assert.That(manager.TimeRemaining, Is.GreaterThan(69f));
            Assert.AreEqual(70, door.CurrentHealth);
            Assert.IsFalse(Panel().activeSelf);
            Assert.GreaterOrEqual(spawner.ActiveEnemies.Count, 1);
        }

        [UnityTest]
        public IEnumerator FallenDoorShowsDefeatAndRestartGoesBackToLevelOne()
        {
            door.TakeDamage(100);
            yield return null;

            Assert.IsTrue(Panel().activeSelf);
            StringAssert.Contains("Derrota", Text("TituloPanel"));
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);

            Press("BotonReiniciar");
            yield return null;
            yield return null;
            yield return null;
            FindObjects();

            Assert.AreEqual(1, manager.LevelNumber);
            Assert.AreEqual(100, door.CurrentHealth);
            Assert.IsFalse(Panel().activeSelf);
        }

        [UnityTest]
        public IEnumerator DefeatingTheBossShowsVictory()
        {
            for (int level = 1; level < 5; level++)
            {
                yield return FinishLevel();
                Press("BotonSiguiente");
                yield return null;
            }

            Time.timeScale = 1f;
            yield return null;
            Assert.AreEqual(5, manager.LevelNumber);
            Assert.IsFalse(manager.HasTimer);
            StringAssert.Contains("jefe", Text("TextoEstado"));

            for (int i = 0; i < 5; i++)
            {
                spawner.ResolveDecrypt(spawner.Boss);
            }
            yield return null;

            Assert.IsTrue(Panel().activeSelf);
            StringAssert.Contains("Victoria", Text("TituloPanel"));
        }

        [UnityTest]
        public IEnumerator MenuButtonGoesBackToTheMainMenu()
        {
            door.TakeDamage(100);
            yield return null;

            Press("BotonMenu");
            yield return null;
            yield return null;

            Assert.AreEqual(MainMenu.SceneName, SceneManager.GetActiveScene().name);
        }

        private IEnumerator FinishLevel()
        {
            spawner.StopLevel();
            Time.timeScale = 20f;
            int level = manager.LevelNumber;
            yield return new WaitUntil(() => manager.TimeRemaining <= 0f && manager.LevelNumber == level);
            yield return null;
        }

        private void FindObjects()
        {
            manager = Object.FindAnyObjectByType<LevelManager>();
            spawner = Object.FindAnyObjectByType<EnemySpawner>();
            door = Object.FindAnyObjectByType<Door>();
            hud = Object.FindAnyObjectByType<LevelHud>();
        }

        private GameObject Panel()
        {
            return hud.transform.Cast<Transform>().First(child => child.name == "PanelNivel").gameObject;
        }

        private string Text(string name)
        {
            return hud.GetComponentsInChildren<TMP_Text>(true).First(label => label.name == name).text;
        }

        private void Press(string button)
        {
            hud.GetComponentsInChildren<Button>(true).First(b => b.name == button).onClick.Invoke();
        }
    }
}
