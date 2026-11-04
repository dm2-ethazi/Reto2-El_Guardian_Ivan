using System.Collections;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PuertaCastillo.Tests
{
    public class EnemyAttackTests
    {
        private Door door;
        private Enemy enemy;

        // Deja un único enemigo y acelera el tiempo hasta que choca con la puerta.
        [UnitySetUp]
        public IEnumerator EnemyReachesTheDoor()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;

            door = Object.FindAnyObjectByType<Door>();
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            enemy = spawner.ActiveEnemies[0];
            spawner.enabled = false;

            Time.timeScale = 5f;
            yield return new WaitUntil(() => enemy.IsAttacking);
            Time.timeScale = 1f;
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator EnemyStopsWithoutCrossingTheDoor()
        {
            Vector2 contact = enemy.transform.position;

            yield return new WaitForSeconds(2f);

            Assert.That(Vector2.Distance(contact, enemy.transform.position), Is.LessThan(0.05f));
            Assert.Greater(enemy.transform.position.y, door.transform.position.y);
            Assert.IsTrue(enemy.IsAttacking);
        }

        [UnityTest]
        public IEnumerator DoorLosesHealthOncePerSecondWhileInContact()
        {
            Assert.AreEqual(100, door.CurrentHealth);

            yield return new WaitForSeconds(3.5f);

            Assert.AreEqual(94, door.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator DamageStopsWhenTheEnemyIsDestroyed()
        {
            yield return new WaitForSeconds(1.5f);
            Object.Destroy(enemy.gameObject);
            int health = door.CurrentHealth;

            yield return new WaitForSeconds(2f);

            Assert.AreEqual(98, health);
            Assert.AreEqual(health, door.CurrentHealth);
        }
    }
}
