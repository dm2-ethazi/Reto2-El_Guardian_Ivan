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

        [SetUp]
        public void BackUpAndClear()
        {
            previousProgress = PlayerPrefs.HasKey(LevelProgress.CompletedKey) ? PlayerPrefs.GetInt(LevelProgress.CompletedKey) : (int?)null;
            previousSave = PlayerPrefs.HasKey(SaveGame.SavedLevelKey) ? PlayerPrefs.GetInt(SaveGame.SavedLevelKey) : (int?)null;
            LevelProgress.Reset();
            SaveGame.Clear();
        }

        [TearDown]
        public void Restore()
        {
            Restore(LevelProgress.CompletedKey, previousProgress);
            Restore(SaveGame.SavedLevelKey, previousSave);
            PlayerPrefs.Save();
        }

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

        [Test]
        public void CompletingALevelUnlocksTheNextOne()
        {
            LevelProgress.MarkCompleted(0);

            Assert.AreEqual(LevelState.Completed, LevelProgress.StateOf(0));
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(1));
            Assert.AreEqual(LevelState.Locked, LevelProgress.StateOf(2));
        }

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

        [Test]
        public void ProgressIsSavedInThePreferences()
        {
            LevelProgress.MarkCompleted(1);

            Assert.AreEqual(2, PlayerPrefs.GetInt(LevelProgress.CompletedKey));
        }

        [Test]
        public void SavedGameFromBeforeTheMapCountsAsProgress()
        {
            SaveGame.Save(3);

            Assert.AreEqual(3, LevelProgress.CompletedCount);
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(3));
        }

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

        [Test]
        public void ResetLeavesOnlyLevelOne()
        {
            LevelProgress.MarkCompleted(2);
            LevelProgress.Reset();

            Assert.AreEqual(0, LevelProgress.CompletedCount);
            Assert.IsFalse(PlayerPrefs.HasKey(LevelProgress.CompletedKey));
        }

        [TestCase(0, 0, ExpectedResult = 1)]
        [TestCase(3, 0, ExpectedResult = 3)]
        [TestCase(3, 2, ExpectedResult = 3)]
        [TestCase(3, 3, ExpectedResult = 4)]
        [TestCase(1, 4, ExpectedResult = 5)]
        public int CompletedCountNeverGoesDown(int completed, int levelIndex)
        {
            return LevelProgress.AfterCompleting(completed, levelIndex);
        }

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
