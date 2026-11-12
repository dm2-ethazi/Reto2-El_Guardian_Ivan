using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEditor;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas de los cinco niveles: valores coherentes y acordes con la propuesta técnica.</summary>
    public class LevelConfigTests
    {
        private static LevelConfig Level(int number)
        {
            return AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Levels/Level{number}.asset");
        }

        /// <summary>Cada nivel tiene el desplazamiento, mínimo, máximo, intervalo y daño de las tablas de la propuesta.</summary>
        /// <param name="number">Número del nivel.</param>
        /// <param name="shift">Desplazamiento esperado.</param>
        /// <param name="min">Mínimo de enemigos esperado.</param>
        /// <param name="max">Máximo de enemigos esperado.</param>
        /// <param name="interval">Intervalo de aparición esperado.</param>
        /// <param name="damage">Daño por golpe esperado.</param>
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

        /// <summary>El enemigo más rápido de cada nivel (2 a 4) es más rápido que el del anterior.</summary>
        [Test]
        public void EnemiesGetFasterEveryLevel()
        {
            for (int number = 2; number <= 4; number++)
            {
                Assert.Greater(Level(number).enemies.Max(e => e.speed), Level(number - 1).enemies.Max(e => e.speed));
            }
        }

        /// <summary>Del nivel 1 al 4, los mensajes de cada nivel son más cortos que los del anterior.</summary>
        [Test]
        public void MessagesGetShorterUntilLevelFour()
        {
            for (int number = 2; number <= 4; number++)
            {
                Assert.Less(Level(number).messages.Max(m => m.Length), Level(number - 1).messages.Min(m => m.Length));
            }
        }

        /// <summary>Hay más mensajes que el máximo de enemigos, solo con letras A-Z y espacios, y la pista existe.</summary>
        /// <param name="number">Número del nivel.</param>
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

        /// <summary>El segundo tipo de enemigo (más rápido) aparece en los niveles 3 y 4; el 5 solo tiene al jefe.</summary>
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

        /// <summary>Solo el nivel 5 tiene jefe.</summary>
        [Test]
        public void OnlyLevelFiveHasABoss()
        {
            Assert.IsTrue(Enumerable.Range(1, 4).All(number => !Level(number).HasBoss));
            Assert.IsTrue(Level(5).HasBoss);
        }
    }
}
