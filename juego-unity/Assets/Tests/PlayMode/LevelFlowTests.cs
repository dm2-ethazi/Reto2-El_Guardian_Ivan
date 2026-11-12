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
    /// <summary>Pruebas del flujo de la partida: pasar de nivel, victoria, derrota, reiniciar y partida guardada.</summary>
    public class LevelFlowTests
    {
        private LevelManager manager;
        private EnemySpawner spawner;
        private Door door;
        private LevelHud hud;

        /// <summary>Borra la partida guardada y carga la partida en el nivel 1.</summary>
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            SaveGame.Clear();
            LevelProgress.Reset();
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            FindObjects();
        }

        /// <summary>Devuelve la escala de tiempo a 1 y limpia la partida guardada.</summary>
        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
            SaveGame.Clear();
            LevelProgress.Reset();
        }

        /// <summary>Cada nivel se guarda al empezar.</summary>
        [UnityTest]
        public IEnumerator EachLevelIsSavedWhenItStarts()
        {
            Assert.IsTrue(SaveGame.HasSavedLevel);
            Assert.AreEqual(0, SaveGame.SavedLevelIndex);

            yield return FinishLevel();
            Time.timeScale = 1f;
            Press("BotonSiguiente");
            yield return null;

            Assert.AreEqual(1, SaveGame.SavedLevelIndex);
        }

        /// <summary>La partida empieza en el nivel pedido desde el menú o el mapa.</summary>
        [UnityTest]
        public IEnumerator RequestedLevelIsWhereTheGameStarts()
        {
            SaveGame.RequestStartLevel(2);
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            FindObjects();

            Assert.AreEqual(3, manager.LevelNumber);
            Assert.AreEqual(2, SaveGame.SavedLevelIndex);
        }

        /// <summary>Ganar el juego borra la partida guardada.</summary>
        [UnityTest]
        public IEnumerator WinningTheGameClearsTheSavedLevel()
        {
            yield return GoToLevelFive();
            for (int i = 0; i < 4; i++)
            {
                spawner.ResolveDecrypt(spawner.Boss);
            }
            yield return null;

            Assert.IsFalse(SaveGame.HasSavedLevel);
        }

        /// <summary>Empieza en el nivel 1 mostrando su tiempo.</summary>
        [Test]
        public void StartsOnLevelOneShowingItsTime()
        {
            Assert.AreEqual(1, manager.LevelNumber);
            Assert.AreEqual(Localization.Format("game.status", 1, "01:00"), Text("TextoEstado"));
            Assert.IsFalse(Panel().activeSelf);
        }

        /// <summary>Al acabar el tiempo el nivel termina y espera al botón.</summary>
        [UnityTest]
        public IEnumerator TimerEndsTheLevelAndWaitsForTheButton()
        {
            yield return FinishLevel();

            Assert.IsTrue(Panel().activeSelf);
            Assert.AreEqual(Localization.Format("level.completed", 1), Text("TituloPanel"));
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
            Assert.AreEqual(1, manager.LevelNumber);
        }

        /// <summary>«Siguiente nivel» carga el siguiente con la puerta a toda su vida.</summary>
        [UnityTest]
        public IEnumerator NextLevelButtonLoadsTheNextLevelWithTheDoorAtFullHealth()
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
            Assert.AreEqual(100, door.CurrentHealth);
            Assert.IsFalse(Panel().activeSelf);
            Assert.GreaterOrEqual(spawner.ActiveEnemies.Count, 1);
        }

        /// <summary>Si cae la puerta sale la derrota y «Reiniciar» repite el mismo nivel.</summary>
        [UnityTest]
        public IEnumerator FallenDoorShowsDefeatAndRestartRepeatsTheLevel()
        {
            door.TakeDamage(100);
            yield return null;

            Assert.IsTrue(Panel().activeSelf);
            Assert.AreEqual(Localization.Get("level.defeat"), Text("TituloPanel"));
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

        /// <summary>Vencer al jefe da la victoria antes de que acabe el tiempo.</summary>
        [UnityTest]
        public IEnumerator DefeatingTheBossShowsVictoryBeforeTheTimeRunsOut()
        {
            yield return GoToLevelFive();
            Assert.That(manager.TimeRemaining, Is.GreaterThan(90f));

            for (int i = 0; i < 4; i++)
            {
                spawner.ResolveDecrypt(spawner.Boss);
            }
            yield return null;

            Assert.IsTrue(Panel().activeSelf);
            Assert.AreEqual(Localization.Get("level.victory"), Text("TituloPanel"));
        }

        /// <summary>Resistir el nivel 5 da la victoria, sin botón de siguiente nivel.</summary>
        [UnityTest]
        public IEnumerator ResistingLevelFiveShowsVictoryWithoutNextButton()
        {
            yield return GoToLevelFive();

            yield return FinishLevel();

            Assert.IsTrue(Panel().activeSelf);
            Assert.AreEqual(Localization.Get("level.victory"), Text("TituloPanel"));
            Assert.IsFalse(hud.GetComponentsInChildren<Button>(true).First(b => b.name == "BotonSiguiente").gameObject.activeSelf);
            Assert.IsTrue(hud.MapButton.gameObject.activeInHierarchy);
            Assert.AreEqual(LevelState.Completed, LevelProgress.StateOf(4));
        }

        /// <summary>El botón Menú del panel vuelve al menú principal.</summary>
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

        /// <summary>Superar un nivel lo desbloquea en el mapa.</summary>
        [UnityTest]
        public IEnumerator CompletingALevelUnlocksTheNextOnTheMap()
        {
            Assert.AreEqual(LevelState.Locked, LevelProgress.StateOf(1));

            yield return FinishLevel();

            Assert.AreEqual(LevelState.Completed, LevelProgress.StateOf(0));
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(1));
        }

        /// <summary>Repetir un nivel anterior conserva el progreso.</summary>
        [UnityTest]
        public IEnumerator ReplayingAnEarlierLevelKeepsTheProgress()
        {
            LevelProgress.MarkCompleted(0);
            LevelProgress.MarkCompleted(1);
            LevelProgress.MarkCompleted(2);
            SaveGame.RequestStartLevel(0);
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            FindObjects();

            yield return FinishLevel();

            Assert.AreEqual(3, LevelProgress.CompletedCount);
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(3));
        }

        /// <summary>Todos los paneles de fin de nivel tienen el botón «Volver al mapa».</summary>
        [UnityTest]
        public IEnumerator EveryEndPanelHasTheMapButton()
        {
            yield return FinishLevel();
            Assert.IsTrue(hud.MapButton.gameObject.activeInHierarchy);
            Assert.AreEqual(Localization.Get("game.map"), hud.MapButton.GetComponentInChildren<TMP_Text>(true).text);
            Time.timeScale = 1f;

            Press("BotonSiguiente");
            yield return null;
            door.TakeDamage(100);
            yield return null;

            Assert.AreEqual(Localization.Get("level.defeat"), Text("TituloPanel"));
            Assert.IsTrue(hud.MapButton.gameObject.activeInHierarchy);
        }

        /// <summary>El botón «Volver al mapa» abre el mapa de niveles.</summary>
        [UnityTest]
        public IEnumerator MapButtonOpensTheMap()
        {
            door.TakeDamage(100);
            yield return null;

            Press("BotonMapa");
            yield return null;
            yield return null;

            Assert.AreEqual(LevelMap.SceneName, SceneManager.GetActiveScene().name);
        }

        /// <summary>El botón Reiniciar de la partida repite el nivel 1.</summary>
        [UnityTest]
        public IEnumerator InGameRestartButtonRepeatsLevelOne()
        {
            door.TakeDamage(50);

            Press("BotonPartidaReiniciar");
            yield return null;
            yield return null;
            yield return null;
            FindObjects();

            Assert.AreEqual(1, manager.LevelNumber);
            Assert.AreEqual(100, door.CurrentHealth);
        }

        /// <summary>El botón Reiniciar de la partida repite el nivel actual.</summary>
        [UnityTest]
        public IEnumerator InGameRestartButtonRepeatsTheCurrentLevel()
        {
            yield return FinishLevel();
            Time.timeScale = 1f;
            Press("BotonSiguiente");
            yield return null;
            door.TakeDamage(50);

            Press("BotonPartidaReiniciar");
            yield return null;
            yield return null;
            yield return null;
            FindObjects();

            Assert.AreEqual(2, manager.LevelNumber);
            Assert.AreEqual(100, door.CurrentHealth);
            Assert.That(manager.TimeRemaining, Is.GreaterThan(69f));
        }

        /// <summary>El botón Menú de la partida vuelve al menú principal.</summary>
        [UnityTest]
        public IEnumerator InGameMenuButtonGoesBackToTheMainMenu()
        {
            Press("BotonPartidaMenu");
            yield return null;
            yield return null;

            Assert.AreEqual(MainMenu.SceneName, SceneManager.GetActiveScene().name);
        }

        /// <summary>Los botones de la partida se ocultan mientras se ve un panel.</summary>
        [UnityTest]
        public IEnumerator InGameButtonsHideWhileThePanelIsShown()
        {
            Assert.IsTrue(hud.InGameMenuButton.gameObject.activeSelf);
            Assert.IsTrue(hud.InGameRestartButton.gameObject.activeSelf);

            door.TakeDamage(100);
            yield return null;

            Assert.IsFalse(hud.InGameMenuButton.gameObject.activeSelf);
            Assert.IsFalse(hud.InGameRestartButton.gameObject.activeSelf);
        }

        private IEnumerator FinishLevel()
        {
            spawner.StopLevel();
            Time.timeScale = 20f;
            int level = manager.LevelNumber;
            yield return new WaitUntil(() => manager.TimeRemaining <= 0f && manager.LevelNumber == level);
            yield return null;
        }

        private IEnumerator GoToLevelFive()
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
            Assert.AreEqual(13, spawner.Config.caesarShift);
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
