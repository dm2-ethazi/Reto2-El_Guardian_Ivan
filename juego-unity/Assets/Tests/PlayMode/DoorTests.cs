using System.Collections;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using PuertaCastillo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas de la puerta: vida, daño y aviso al caer.</summary>
    public class DoorTests
    {
        private Door door;
        private DoorHealthBar bar;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            door = Object.FindAnyObjectByType<Door>();
            bar = Object.FindAnyObjectByType<DoorHealthBar>();
        }

        [Test]
        public void StartsWith100Health()
        {
            Assert.AreEqual(100, door.CurrentHealth);
            Assert.AreEqual("100 / 100", bar.GetComponentInChildren<TMP_Text>().text);
            Assert.IsNotNull(bar.GetComponentInChildren<TMP_Text>().font);
        }

        [UnityTest]
        public IEnumerator EachHitPlaysAWoodenKnockButNotTooManyAtOnce()
        {
            door.PlayHitFeedback(1f);
            Assert.AreEqual(1, door.HitSoundsPlayed);
            StringAssert.StartsWith("GolpePuerta", door.LastHitSound.name);

            // Dos enemigos que golpean en el mismo instante suenan como un solo golpe.
            door.PlayHitFeedback(1f);
            Assert.AreEqual(1, door.HitSoundsPlayed);

            yield return new WaitForSeconds(0.2f);
            door.PlayHitFeedback(2f);
            Assert.AreEqual(2, door.HitSoundsPlayed);
        }

        [Test]
        public void ThereAreSeveralKnockSounds()
        {
            AudioClip[] sounds = Resources.LoadAll<AudioClip>(Door.HitSoundsFolder);
            Assert.GreaterOrEqual(sounds.Length, 3);
            foreach (AudioClip sound in sounds)
            {
                Assert.That(sound.length, Is.InRange(0.2f, 0.6f), sound.name);
            }
        }

        [Test]
        public void HasSolidColliderWithoutRigidbody()
        {
            Assert.IsFalse(door.GetComponent<BoxCollider2D>().isTrigger);
            Assert.IsNull(door.GetComponent<Rigidbody2D>());
        }

        [Test]
        public void DamageUpdatesTheBar()
        {
            door.TakeDamage(30);

            Assert.AreEqual(70, door.CurrentHealth);
            Assert.AreEqual(70f, bar.GetComponentInChildren<Slider>().value);
            Assert.AreEqual("70 / 100", bar.GetComponentInChildren<TMP_Text>().text);
        }

        [Test]
        public void HealthStopsAtZeroAndFallsOnce()
        {
            int falls = 0;
            door.Fell += () => falls++;

            door.TakeDamage(150);
            door.TakeDamage(10);

            Assert.AreEqual(0, door.CurrentHealth);
            Assert.IsTrue(door.IsDown);
            Assert.AreEqual(1, falls);
        }
    }
}
