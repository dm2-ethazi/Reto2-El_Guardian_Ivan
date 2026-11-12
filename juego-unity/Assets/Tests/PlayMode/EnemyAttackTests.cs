using System.Collections;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas del ataque: los enemigos llegan a la puerta y la golpean cada cierto tiempo.</summary>
    public class EnemyAttackTests
    {
        private Door door;
        private Enemy enemy;

        // Deja un único enemigo y acelera el tiempo hasta que choca con la puerta.
        /// <summary>Carga la partida y acelera el tiempo hasta que un enemigo llega a la puerta.</summary>
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

        /// <summary>Devuelve la escala de tiempo a 1.</summary>
        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        /// <summary>El enemigo se detiene ante la puerta sin atravesarla.</summary>
        [UnityTest]
        public IEnumerator EnemyStopsWithoutCrossingTheDoor()
        {
            Vector2 contact = enemy.transform.position;

            yield return new WaitForSeconds(2f);

            Assert.That(Vector2.Distance(contact, enemy.transform.position), Is.LessThan(0.05f));
            Assert.Greater(enemy.transform.position.y, door.transform.position.y);
            Assert.IsTrue(enemy.IsAttacking);
        }

        /// <summary>La puerta pierde vida cada 2 segundos mientras hay contacto.</summary>
        [UnityTest]
        public IEnumerator DoorLosesHealthEveryTwoSecondsWhileInContact()
        {
            Assert.AreEqual(100, door.CurrentHealth);

            yield return new WaitForSeconds(4.5f);

            Assert.AreEqual(96, door.CurrentHealth);
        }

        /// <summary>El daño se detiene al destruir al enemigo.</summary>
        [UnityTest]
        public IEnumerator DamageStopsWhenTheEnemyIsDestroyed()
        {
            yield return new WaitForSeconds(2.5f);
            Object.Destroy(enemy.gameObject);
            int health = door.CurrentHealth;

            yield return new WaitForSeconds(2f);

            Assert.AreEqual(98, health);
            Assert.AreEqual(health, door.CurrentHealth);
        }
    }
}
