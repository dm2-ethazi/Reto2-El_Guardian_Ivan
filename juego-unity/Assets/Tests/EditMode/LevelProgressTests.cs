using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEngine;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas del progreso de niveles: desbloquear, repetir sin perder progreso y guardarlo.</summary>
    public class LevelProgressTests
    {
        // Las pruebas tocan PlayerPrefs: se guarda lo que hubiera para no borrar la partida del editor.
        private int? previousProgress;
        private int? previousSave;

        /// <summary>Guarda las preferencias del jugador y empieza cada prueba sin progreso.</summary>
        [SetUp]
        public void BackUpAndClear()
        {
            previousProgress = PlayerPrefs.HasKey(LevelProgress.CompletedKey) ? PlayerPrefs.GetInt(LevelProgress.CompletedKey) : (int?)null;
            previousSave = PlayerPrefs.HasKey(SaveGame.SavedLevelKey) ? PlayerPrefs.GetInt(SaveGame.SavedLevelKey) : (int?)null;
            LevelProgress.Reset();
            SaveGame.Clear();
        }

        /// <summary>Devuelve las preferencias del jugador a como estaban.</summary>
        [TearDown]
        public void Restore()
        {
            Restore(LevelProgress.CompletedKey, previousProgress);
            Restore(SaveGame.SavedLevelKey, previousSave);
            PlayerPrefs.Save();
        }

        /// <summary>Sin progreso solo se puede jugar el nivel 1.</summary>
        [Test]
        public void WithoutProgressOnlyLevelOneIsAvailable()
        {
            Assert.AreEqual(0, LevelProgress.CompletedCount);
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(0));
            for (int level = 1; level < 5; level++)
            {
                Assert.AreEqual(LevelState.Locked, LevelProgress.StateOf(level), $"Nivel {level + 1}");
                Assert.IsFalse(LevelProgress.IsUnlocked(level));
            }
        }

        /// <summary>Superar un nivel desbloquea el siguiente.</summary>
        [Test]
        public void CompletingALevelUnlocksTheNextOne()
        {
            LevelProgress.MarkCompleted(0);

            Assert.AreEqual(LevelState.Completed, LevelProgress.StateOf(0));
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(1));
            Assert.AreEqual(LevelState.Locked, LevelProgress.StateOf(2));
        }

        /// <summary>Repetir un nivel anterior no quita progreso.</summary>
        [Test]
        public void ReplayingAnEarlierLevelNeverLosesProgress()
        {
            for (int level = 0; level < 3; level++)
            {
                LevelProgress.MarkCompleted(level);
            }
            Assert.AreEqual(3, LevelProgress.CompletedCount);

            // Estando en el nivel 4, se repiten el 1 y el 2: el 4 sigue disponible.
            LevelProgress.MarkCompleted(0);
            LevelProgress.MarkCompleted(1);
            SaveGame.Save(0);

            Assert.AreEqual(3, LevelProgress.CompletedCount);
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(3));
            Assert.AreEqual(LevelState.Completed, LevelProgress.StateOf(2));
        }

        /// <summary>El progreso se guarda en PlayerPrefs.</summary>
        [Test]
        public void ProgressIsSavedInThePreferences()
        {
            LevelProgress.MarkCompleted(1);

            Assert.AreEqual(2, PlayerPrefs.GetInt(LevelProgress.CompletedKey));
        }

        /// <summary>Una partida guardada de antes del mapa cuenta como progreso.</summary>
        [Test]
        public void SavedGameFromBeforeTheMapCountsAsProgress()
        {
            SaveGame.Save(3);

            Assert.AreEqual(3, LevelProgress.CompletedCount);
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(3));
        }

        /// <summary>Vencer al jefe deja todos los niveles como superados.</summary>
        [Test]
        public void BeatingTheBossMarksEveryLevelAsCompleted()
        {
            for (int level = 0; level < 5; level++)
            {
                LevelProgress.MarkCompleted(level);
            }

            for (int level = 0; level < 5; level++)
            {
                Assert.AreEqual(LevelState.Completed, LevelProgress.StateOf(level));
            }
        }

        /// <summary>Borrar el progreso deja disponible solo el nivel 1.</summary>
        [Test]
        public void ResetLeavesOnlyLevelOne()
        {
            LevelProgress.MarkCompleted(2);
            LevelProgress.Reset();

            Assert.AreEqual(0, LevelProgress.CompletedCount);
            Assert.IsFalse(PlayerPrefs.HasKey(LevelProgress.CompletedKey));
        }

        /// <summary>El número de niveles superados nunca baja.</summary>
        /// <param name="completed">Niveles superados antes.</param>
        /// <param name="levelIndex">Nivel que se supera (0 es el nivel 1).</param>
        /// <returns>Niveles superados después.</returns>
        [TestCase(0, 0, ExpectedResult = 1)]
        [TestCase(3, 0, ExpectedResult = 3)]
        [TestCase(3, 2, ExpectedResult = 3)]
        [TestCase(3, 3, ExpectedResult = 4)]
        [TestCase(1, 4, ExpectedResult = 5)]
        public int CompletedCountNeverGoesDown(int completed, int levelIndex)
        {
            return LevelProgress.AfterCompleting(completed, levelIndex);
        }

        /// <summary>El estado de un nivel depende de cuántos se han superado.</summary>
        /// <param name="levelIndex">Nivel (0 es el nivel 1).</param>
        /// <param name="completed">Niveles superados.</param>
        /// <returns>Estado del nivel.</returns>
        [TestCase(0, 2, ExpectedResult = LevelState.Completed)]
        [TestCase(1, 2, ExpectedResult = LevelState.Completed)]
        [TestCase(2, 2, ExpectedResult = LevelState.Available)]
        [TestCase(3, 2, ExpectedResult = LevelState.Locked)]
        [TestCase(4, 5, ExpectedResult = LevelState.Completed)]
        public LevelState StateDependsOnHowManyLevelsAreCompleted(int levelIndex, int completed)
        {
            return LevelProgress.StateOf(levelIndex, completed);
        }

        private static void Restore(string key, int? value)
        {
            if (value.HasValue)
            {
                PlayerPrefs.SetInt(key, value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
