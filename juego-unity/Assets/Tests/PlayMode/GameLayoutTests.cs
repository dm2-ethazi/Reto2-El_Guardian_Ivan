using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using PuertaCastillo.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    public class GameLayoutTests
    {
        private const float MinimumTouchSize = 110f;

        private GameLayout layout;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            layout = Object.FindAnyObjectByType<GameLayout>();
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2400)]
        [TestCase(720, 1280)]
        [TestCase(1920, 1080)]
        [TestCase(2400, 1080)]
        [TestCase(1024, 768)]
        [TestCase(3440, 1440)]
        public void GameButtonsAreEasyToTouchAndStayOnScreen(int width, int height)
        {
            layout.Apply(width, height, new Rect(0, 0, width, height));

            string[] buttons = { "BotonSubir", "BotonBajar", "BotonDescifrar" };
            foreach (Button button in layout.GetComponentsInChildren<Button>(true).Where(b => buttons.Contains(b.name)))
            {
                var rect = (RectTransform)button.transform;
                Assert.GreaterOrEqual(rect.sizeDelta.x, MinimumTouchSize, button.name);
                Assert.GreaterOrEqual(rect.sizeDelta.y, MinimumTouchSize, button.name);
                Assert.GreaterOrEqual(rect.anchoredPosition.x, 0f, button.name);
                Assert.LessOrEqual(rect.anchoredPosition.x + rect.sizeDelta.x, layout.LogicalSize.x, button.name);
                Assert.LessOrEqual(-rect.anchoredPosition.y + rect.sizeDelta.y, layout.LogicalSize.y, button.name);
            }
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2400)]
        [TestCase(1920, 1080)]
        [TestCase(2400, 1080)]
        [TestCase(1024, 768)]
        public void CameraSeesTheDoorAndEveryEnemyStart(int width, int height)
        {
            layout.Apply(width, height, new Rect(0, 0, width, height));

            Camera camera = Camera.main;
            float halfViewWidth = camera.orthographicSize * width / height;
            float top = camera.transform.position.y + camera.orthographicSize;
            float bottom = camera.transform.position.y - camera.orthographicSize;
            Door door = Object.FindAnyObjectByType<Door>();

            Assert.Greater(door.transform.position.y, bottom);
            foreach (Transform point in Object.FindAnyObjectByType<EnemySpawner>().transform)
            {
                Assert.Less(Mathf.Abs(point.position.x - camera.transform.position.x), halfViewWidth, point.name);
                Assert.Less(point.position.y, top, point.name);
            }
        }

        [Test]
        public void SafeAreaKeepsTheInterfaceAwayFromTheNotch()
        {
            layout.Apply(1080, 2400, new Rect(0, 0, 1080, 2400));
            RectTransform health = HealthBar();
            float withoutNotch = -health.anchoredPosition.y;

            layout.Apply(1080, 2400, new Rect(0, 0, 1080, 2300));

            Assert.AreEqual(withoutNotch + 100f, -health.anchoredPosition.y, 0.01f);
        }

        private RectTransform HealthBar()
        {
            return (RectTransform)Object.FindAnyObjectByType<DoorHealthBar>().transform;
        }
    }
}
