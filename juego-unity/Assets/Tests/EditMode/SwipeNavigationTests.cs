using NUnit.Framework;
using PuertaCastillo.Gameplay;
using PuertaCastillo.UI;
using UnityEngine;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas de los gestos de deslizar: qué cuenta como gesto y en qué nivel entra el menú.</summary>
    public class SwipeNavigationTests
    {
        private const float Width = 1080f;

        [Test]
        public void LongSwipeToTheLeftCounts()
        {
            Assert.AreEqual(Swipe.Left, SwipeNavigation.Classify(new Vector2(-500f, -40f), 0.3f, Width));
        }

        [Test]
        public void LongSwipeToTheRightCounts()
        {
            Assert.AreEqual(Swipe.Right, SwipeNavigation.Classify(new Vector2(500f, 40f), 0.3f, Width));
        }

        // Con la pantalla siguiendo al dedo, un arrastre lento también vale si pasa el umbral.
        [Test]
        public void SlowLongSwipeCounts()
        {
            Assert.AreEqual(Swipe.Right, SwipeNavigation.Classify(new Vector2(600f, 0f), 1.5f, Width));
        }

        [Test]
        public void QuickShortFlickCounts()
        {
            Assert.AreEqual(Swipe.Left, SwipeNavigation.Classify(new Vector2(-200f, 0f), 0.2f, Width));
        }

        [Test]
        public void SlowShortSwipeDoesNothing()
        {
            Assert.AreEqual(Swipe.None, SwipeNavigation.Classify(new Vector2(300f, 0f), 0.9f, Width));
        }

        [Test]
        public void TinySwipeDoesNothing()
        {
            Assert.AreEqual(Swipe.None, SwipeNavigation.Classify(new Vector2(100f, 0f), 0.1f, Width));
        }

        [Test]
        public void MostlyVerticalSwipeDoesNothing()
        {
            Assert.AreEqual(Swipe.None, SwipeNavigation.Classify(new Vector2(400f, 500f), 0.3f, Width));
        }

        [Test]
        public void TapDoesNothing()
        {
            Assert.AreEqual(Swipe.None, SwipeNavigation.Classify(Vector2.zero, 0.1f, Width));
        }

        // La respuesta visual solo avanza en la dirección válida y llega a 1 en el umbral (35 % del ancho).
        [Test]
        public void ProgressOnlyGrowsInTheValidDirection()
        {
            float threshold = Width * SwipeNavigation.MinWidthFraction;
            Assert.AreEqual(1f, SwipeNavigation.Progress(new Vector2(-threshold, 0f), Swipe.Left, Width), 0.001f);
            Assert.AreEqual(0.5f, SwipeNavigation.Progress(new Vector2(-threshold / 2f, 0f), Swipe.Left, Width), 0.001f);
            Assert.AreEqual(0f, SwipeNavigation.Progress(new Vector2(threshold, 0f), Swipe.Left, Width));
            Assert.AreEqual(0f, SwipeNavigation.Progress(new Vector2(-100f, 300f), Swipe.Left, Width));
            Assert.AreEqual(0f, SwipeNavigation.Progress(new Vector2(-threshold, 0f), Swipe.None, Width));
        }

        // En el menú, deslizar a la izquierda entra en el nivel guardado o, si no hay, en el nivel 1.
        [Test]
        public void MenuSwipeEntersTheSavedLevel()
        {
            bool had = SaveGame.HasSavedLevel;
            int saved = SaveGame.SavedLevelIndex;
            try
            {
                SaveGame.Save(2);
                Assert.AreEqual(2, MainMenu.SwipeStartLevel);
                SaveGame.Clear();
                Assert.AreEqual(0, MainMenu.SwipeStartLevel);
            }
            finally
            {
                if (had) SaveGame.Save(saved); else SaveGame.Clear();
            }
        }
    }
}
