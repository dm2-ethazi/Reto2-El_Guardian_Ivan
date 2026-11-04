using System.Collections;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PuertaCastillo.Tests
{
    public class BossTests
    {
        private const int BossDecrypts = 5;

        private EnemySpawner spawner;
        private EnemyTargeting targeting;
        private Enemy boss;

        // Sin el generador activo solo queda el jefe en escena.
        [UnitySetUp]
        public IEnumerator StartLevelFive()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            spawner = Object.FindAnyObjectByType<EnemySpawner>();
            targeting = Object.FindAnyObjectByType<EnemyTargeting>();
            spawner.enabled = false;
            spawner.StartLevel(AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Levels/Level5.asset"));
            boss = spawner.Boss;
            yield return null;
            yield return null;
        }

        [Test]
        public void BossIsTheTargetAndStartsWithFullHealth()
        {
            Assert.AreSame(boss, targeting.Target);
            Assert.AreEqual(BossDecrypts, boss.RemainingDecrypts);
        }

        [Test]
        public void CorrectAnswerHurtsTheBossAndChangesItsMessage()
        {
            string previous = boss.Message;

            Assert.IsTrue(targeting.TrySolve(previous));

            Assert.AreEqual(BossDecrypts - 1, boss.RemainingDecrypts);
            Assert.AreNotEqual(previous, boss.Message);
            Assert.AreSame(boss, targeting.Target);
        }

        [Test]
        public void WrongAnswerDoesNotHurtTheBoss()
        {
            Assert.IsFalse(targeting.TrySolve("RESPUESTA MALA"));
            Assert.AreEqual(BossDecrypts, boss.RemainingDecrypts);
        }

        [UnityTest]
        public IEnumerator BossDiesAfterAllItsDecryptsAndTheSpawnerStops()
        {
            int defeats = 0;
            spawner.BossDefeated += () => defeats++;
            spawner.enabled = true;

            for (int i = 0; i < BossDecrypts; i++)
            {
                yield return new WaitUntil(() => targeting.Target == boss);
                Assert.IsTrue(targeting.TrySolve(boss.Message));
            }

            yield return null;
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(boss == null);
            Assert.AreEqual(1, defeats);
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
        }

        [UnityTest]
        public IEnumerator BossHitsWithTheLevelDamage()
        {
            Door door = Object.FindAnyObjectByType<Door>();
            Time.timeScale = 10f;
            yield return new WaitUntil(() => boss.IsAttacking);
            Time.timeScale = 1f;

            yield return new WaitForSeconds(1.5f);

            Assert.AreEqual(100 - 6, door.CurrentHealth);
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }
    }
}
