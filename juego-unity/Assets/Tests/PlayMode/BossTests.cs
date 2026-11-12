using System.Collections;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas del jefe del nivel 5: es el objetivo, pierde vida con cada acierto y muere a los 4.</summary>
    public class BossTests
    {
        private const int BossDecrypts = 4;

        private EnemySpawner spawner;
        private EnemyTargeting targeting;
        private Enemy boss;

        // Sin el generador activo solo queda el jefe en escena.
        /// <summary>Carga la partida directamente en el nivel 5.</summary>
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

        /// <summary>El jefe es el objetivo y empieza con toda su vida.</summary>
        [Test]
        public void BossIsTheTargetAndStartsWithFullHealth()
        {
            Assert.AreSame(boss, targeting.Target);
            Assert.AreEqual(BossDecrypts, boss.RemainingDecrypts);
        }

        /// <summary>Un acierto le quita vida al jefe y le cambia el mensaje.</summary>
        [Test]
        public void CorrectAnswerHurtsTheBossAndChangesItsMessage()
        {
            string previous = boss.Message;

            Assert.IsTrue(targeting.TrySolve(previous));

            Assert.AreEqual(BossDecrypts - 1, boss.RemainingDecrypts);
            Assert.AreNotEqual(previous, boss.Message);
            Assert.AreSame(boss, targeting.Target);
        }

        /// <summary>Un fallo no le quita vida al jefe.</summary>
        [Test]
        public void WrongAnswerDoesNotHurtTheBoss()
        {
            Assert.IsFalse(targeting.TrySolve("RESPUESTA MALA"));
            Assert.AreEqual(BossDecrypts, boss.RemainingDecrypts);
        }

        /// <summary>El jefe muere con todos sus aciertos y el generador se detiene.</summary>
        [UnityTest]
        public IEnumerator BossDiesAfterAllItsDecryptsAndTheSpawnerStops()
        {
            int defeats = 0;
            spawner.BossDefeated += () => defeats++;

            for (int i = 0; i < BossDecrypts; i++)
            {
                Assert.IsTrue(targeting.TrySolve(boss.Message));
                yield return null;
            }

            yield return null;
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(boss == null);
            Assert.AreEqual(1, defeats);
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
        }

        /// <summary>El jefe golpea la puerta con el daño del nivel.</summary>
        [UnityTest]
        public IEnumerator BossHitsWithTheLevelDamage()
        {
            Door door = Object.FindAnyObjectByType<Door>();
            Time.timeScale = 10f;
            yield return new WaitUntil(() => boss.IsAttacking);
            Time.timeScale = 1f;

            yield return new WaitForSeconds(2.5f);

            Assert.AreEqual(100 - 6, door.CurrentHealth);
        }

        /// <summary>En el nivel 5 solo aparece el jefe.</summary>
        [UnityTest]
        public IEnumerator OnlyTheBossAppearsInLevelFive()
        {
            spawner.enabled = true;
            Time.timeScale = 5f;

            yield return new WaitForSeconds(20f);

            Assert.AreEqual(1, spawner.ActiveEnemies.Count);
            Assert.AreSame(boss, spawner.ActiveEnemies[0]);
        }

        /// <summary>Devuelve la escala de tiempo a 1.</summary>
        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }
    }
}
