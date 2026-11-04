using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEditor;

namespace PuertaCastillo.Tests
{
    public class LevelConfigTests
    {
        private static LevelConfig Level(int number)
        {
            return AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Levels/Level{number}.asset");
        }

        [TestCase(1, 3, 1, 2, 6f, 2)]
        [TestCase(2, 5, 2, 3, 5f, 3)]
        [TestCase(3, 7, 3, 3, 4.5f, 4)]
        [TestCase(4, 11, 3, 4, 4f, 5)]
        [TestCase(5, 13, 4, 5, 3.5f, 6)]
        public void MatchesTheProposalTables(int number, int shift, int min, int max, float interval, int damage)
        {
            LevelConfig level = Level(number);

            Assert.AreEqual(shift, level.caesarShift);
            Assert.AreEqual(min, level.minEnemies);
            Assert.AreEqual(max, level.maxEnemies);
            Assert.AreEqual(interval, level.spawnInterval);
            Assert.AreEqual(damage, level.damagePerHit);
        }

        [Test]
        public void EnemiesGetFasterEveryLevel()
        {
            for (int number = 2; number <= 5; number++)
            {
                Assert.Greater(Level(number).enemySpeed, Level(number - 1).enemySpeed);
            }
        }

        [Test]
        public void MessagesGetLongerEveryLevel()
        {
            for (int number = 2; number <= 5; number++)
            {
                Assert.Greater(Level(number).messages.Average(m => m.Length), Level(number - 1).messages.Average(m => m.Length));
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void MessagesAreValidAndEnough(int number)
        {
            LevelConfig level = Level(number);

            Assert.Greater(level.messages.Distinct().Count(), level.maxEnemies);
            Assert.IsTrue(level.messages.All(m => m.All(c => c == ' ' || (c >= 'A' && c <= 'Z'))));
            Assert.IsNotEmpty(level.hint);
        }

        [Test]
        public void SecondEnemyTypeAppearsFromLevelThree()
        {
            Assert.AreEqual(1, Level(1).enemyPrefabs.Length);
            Assert.AreEqual(1, Level(2).enemyPrefabs.Length);
            Assert.AreEqual(2, Level(3).enemyPrefabs.Length);
            Assert.AreEqual(2, Level(4).enemyPrefabs.Length);
            Assert.AreEqual(2, Level(5).enemyPrefabs.Length);
            Assert.Greater(Level(3).enemyPrefabs[1].SpeedMultiplier, Level(3).enemyPrefabs[0].SpeedMultiplier);
        }

        [Test]
        public void OnlyLevelFiveHasABoss()
        {
            Assert.IsTrue(Enumerable.Range(1, 4).All(number => Level(number).bossPrefab == null));
            Assert.IsNotNull(Level(5).bossPrefab);
        }
    }
}
