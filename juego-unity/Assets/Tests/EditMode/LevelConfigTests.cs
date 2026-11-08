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
        [TestCase(5, 13, 1, 1, 3.5f, 6)]
        public void MatchesTheProposalTables(int number, int shift, int min, int max, float interval, int damage)
        {
            LevelConfig level = Level(number);

            Assert.AreEqual(shift, level.caesarShift);
            Assert.AreEqual(min, level.minEnemies);
            Assert.AreEqual(max, level.maxEnemies);
            Assert.AreEqual(interval, level.spawnInterval);
            Assert.IsTrue(level.enemies.Concat(new[] { level.boss }).Where(e => e.prefab != null).All(e => e.damagePerHit == damage));
        }

        [Test]
        public void EnemiesGetFasterEveryLevel()
        {
            for (int number = 2; number <= 4; number++)
            {
                Assert.Greater(Level(number).enemies.Max(e => e.speed), Level(number - 1).enemies.Max(e => e.speed));
            }
        }

        [Test]
        public void MessagesGetShorterUntilLevelFour()
        {
            for (int number = 2; number <= 4; number++)
            {
                Assert.Less(Level(number).messages.Max(m => m.Length), Level(number - 1).messages.Min(m => m.Length));
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
            Assert.That(UI.Localization.Keys, Does.Contain(level.hintKey));
        }

        [Test]
        public void SecondEnemyTypeAppearsFromLevelThree()
        {
            Assert.AreEqual(1, Level(1).enemies.Length);
            Assert.AreEqual(1, Level(2).enemies.Length);
            Assert.AreEqual(2, Level(3).enemies.Length);
            Assert.AreEqual(2, Level(4).enemies.Length);
            Assert.AreEqual(0, Level(5).enemies.Length);
            Assert.Greater(Level(3).enemies[1].speed, Level(3).enemies[0].speed);
        }

        [Test]
        public void OnlyLevelFiveHasABoss()
        {
            Assert.IsTrue(Enumerable.Range(1, 4).All(number => !Level(number).HasBoss));
            Assert.IsTrue(Level(5).HasBoss);
        }
    }
}
