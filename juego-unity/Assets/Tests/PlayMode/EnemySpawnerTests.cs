using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Crypto;
using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas del generador: mínimo y máximo de enemigos, velocidad, física y mensajes sin repetir.</summary>
    public class EnemySpawnerTests
    {
        private EnemySpawner spawner;

        /// <summary>Carga la escena de la partida antes de cada prueba.</summary>
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            spawner = Object.FindAnyObjectByType<EnemySpawner>();
        }

        /// <summary>Devuelve la escala de tiempo a 1.</summary>
        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        /// <summary>Al empezar aparece enseguida el mínimo de enemigos.</summary>
        [Test]
        public void SpawnsTheMinimumImmediately()
        {
            Assert.AreEqual(1, spawner.ActiveEnemies.Count);
        }

        /// <summary>El enemigo tiene Rigidbody2D Dynamic sin gravedad ni rotación y CircleCollider2D sólido.</summary>
        [Test]
        public void EnemyPhysicsMatchesTheProposal()
        {
            Enemy enemy = spawner.ActiveEnemies[0];
            var body = enemy.GetComponent<Rigidbody2D>();

            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
            Assert.AreEqual(0f, body.gravityScale);
            Assert.IsTrue(body.freezeRotation);
            Assert.IsFalse(enemy.GetComponent<CircleCollider2D>().isTrigger);
        }

        /// <summary>Los enemigos están en la capa Enemy, que no choca consigo misma.</summary>
        [Test]
        public void EnemiesDoNotCollideWithEachOther()
        {
            int layer = spawner.ActiveEnemies[0].gameObject.layer;

            Assert.AreEqual(LayerMask.NameToLayer("Enemy"), layer);
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(layer, layer));
        }

        /// <summary>El enemigo avanza hacia la puerta a la velocidad del nivel.</summary>
        [UnityTest]
        public IEnumerator EnemyMovesTowardsTheDoorAtLevelSpeed()
        {
            Enemy enemy = spawner.ActiveEnemies[0];
            Vector2 door = Object.FindAnyObjectByType<Door>().transform.position;
            float startDistance = Vector2.Distance(enemy.transform.position, door);

            yield return new WaitForSeconds(1f);

            float distance = Vector2.Distance(enemy.transform.position, door);
            Assert.That(startDistance - distance, Is.EqualTo(0.4f).Within(0.1f));
        }

        /// <summary>Nunca hay más enemigos que el máximo del nivel.</summary>
        [UnityTest]
        public IEnumerator NeverExceedsTheMaximum()
        {
            Time.timeScale = 10f;

            yield return new WaitForSecondsRealtime(2f);

            Assert.AreEqual(2, spawner.ActiveEnemies.Count);
        }

        /// <summary>El enemigo muestra su mensaje cifrado con el desplazamiento del nivel.</summary>
        [Test]
        public void EnemyShowsItsMessageEncrypted()
        {
            Enemy enemy = spawner.ActiveEnemies[0];
            TMP_Text label = enemy.GetComponentInChildren<TMP_Text>();

            Assert.IsNotNull(label.font);
            Assert.AreNotEqual(enemy.Message, label.text);
            Assert.AreEqual(enemy.Message, CaesarCipher.Decrypt(label.text, 3));
        }

        /// <summary>Los mensajes no se solapan cuando varios enemigos golpean la puerta.</summary>
        [UnityTest]
        public IEnumerator MessagesDoNotOverlapWhileAttacking()
        {
            Time.timeScale = 10f;

            yield return new WaitUntil(() =>
                spawner.ActiveEnemies.Count == 2 && spawner.ActiveEnemies.All(enemy => enemy.IsAttacking));

            float first = spawner.ActiveEnemies[0].GetComponentInChildren<TMP_Text>().transform.position.y;
            float second = spawner.ActiveEnemies[1].GetComponentInChildren<TMP_Text>().transform.position.y;
            Assert.That(Mathf.Abs(first - second), Is.GreaterThan(0.25f));
        }

        /// <summary>Dos enemigos activos nunca tienen el mismo mensaje.</summary>
        [UnityTest]
        public IEnumerator ActiveEnemiesNeverShareAMessage()
        {
            Time.timeScale = 10f;

            yield return new WaitForSecondsRealtime(1f);

            Assert.AreEqual(2, spawner.ActiveEnemies.Count);
            Assert.AreNotEqual(spawner.ActiveEnemies[0].Message, spawner.ActiveEnemies[1].Message);
        }
    }
}
