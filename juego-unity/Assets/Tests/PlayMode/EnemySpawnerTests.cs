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

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            yield return null;
            spawner = Object.FindAnyObjectByType<EnemySpawner>();
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void SpawnsTheMinimumImmediately()
        {
            Assert.AreEqual(1, spawner.ActiveEnemies.Count);
        }

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

        [Test]
        public void EnemiesDoNotCollideWithEachOther()
        {
            int layer = spawner.ActiveEnemies[0].gameObject.layer;

            Assert.AreEqual(LayerMask.NameToLayer("Enemy"), layer);
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(layer, layer));
        }

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

        [UnityTest]
        public IEnumerator NeverExceedsTheMaximum()
        {
            Time.timeScale = 10f;

            yield return new WaitForSecondsRealtime(2f);

            Assert.AreEqual(2, spawner.ActiveEnemies.Count);
        }

        [Test]
        public void EnemyShowsItsMessageEncrypted()
        {
            Enemy enemy = spawner.ActiveEnemies[0];
            TMP_Text label = enemy.GetComponentInChildren<TMP_Text>();

            Assert.IsNotNull(label.font);
            Assert.AreNotEqual(enemy.Message, label.text);
            Assert.AreEqual(enemy.Message, CaesarCipher.Decrypt(label.text, 3));
        }

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
