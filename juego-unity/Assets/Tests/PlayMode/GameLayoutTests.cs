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
    /// <summary>Pruebas de la colocación de la interfaz: botones y teclas grandes sin solaparse, cámara y área segura.</summary>
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

            string[] buttons = { "BotonSubir", "BotonBajar", "BotonDescifrar", "BotonPartidaReiniciar", "BotonPartidaMenu" };
            Button[] found = layout.GetComponentsInChildren<Button>(true).Where(b => buttons.Contains(b.name)).ToArray();
            Assert.AreEqual(buttons.Length, found.Length);

            foreach (Button button in found)
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
        [TestCase(720, 1280)]
        [TestCase(1920, 1080)]
        [TestCase(2400, 1080)]
        [TestCase(1024, 768)]
        [TestCase(3440, 1440)]
        public void LetterKeysAreBigEnoughAndDoNotOverlap(int width, int height)
        {
            layout.Apply(width, height, new Rect(0, 0, width, height));

            LetterKeyboard keyboard = Object.FindAnyObjectByType<AnswerPanel>().OnScreenKeyboard;
            var keys = keyboard.LetterKeys.Append(keyboard.SpaceKey).Append(keyboard.DeleteKey)
                .Select(key => (RectTransform)key.transform)
                .Append((RectTransform)Object.FindAnyObjectByType<AnswerPanel>().GetComponentInChildren<Button>().transform)
                .ToList();
            var areas = keys.Select(Area).ToList();

            for (int i = 0; i < areas.Count; i++)
            {
                Assert.GreaterOrEqual(areas[i].width, 70f, keys[i].name);
                Assert.GreaterOrEqual(areas[i].height, 70f, keys[i].name);
                Assert.GreaterOrEqual(areas[i].xMin, 0f, keys[i].name);
                Assert.LessOrEqual(areas[i].xMax, layout.LogicalSize.x, keys[i].name);
                Assert.LessOrEqual(areas[i].yMax, layout.LogicalSize.y, keys[i].name);

                for (int j = i + 1; j < areas.Count; j++)
                {
                    Assert.IsFalse(areas[i].Overlaps(areas[j]), $"{keys[i].name} y {keys[j].name}");
                }
            }
        }

        [TestCase(1080, 1920, 50f)]
        [TestCase(1080, 2400, 50f)]
        [TestCase(720, 1280, 50f)]
        [TestCase(1920, 1080, 44f)]
        [TestCase(2400, 1080, 44f)]
        [TestCase(1024, 768, 44f)]
        [TestCase(3440, 1440, 44f)]
        public void RulerLettersAreBigAndDoNotOverlapTheInterface(int width, int height, float minimumFont)
        {
            layout.Apply(width, height, new Rect(0, 0, width, height));

            CaesarRuler ruler = Object.FindAnyObjectByType<CaesarRuler>();
            AnswerPanel answer = Object.FindAnyObjectByType<AnswerPanel>();
            LevelHud hud = Object.FindAnyObjectByType<LevelHud>();
            var others = answer.OnScreenKeyboard.LetterKeys.Select(key => (RectTransform)key.transform)
                .Append(hud.InGameRestartButton)
                .Append(hud.InGameMenuButton)
                .Append((RectTransform)ruler.GetComponentsInChildren<Button>().First(b => b.name == "BotonSubir").transform)
                .Append((RectTransform)ruler.GetComponentsInChildren<Button>().First(b => b.name == "BotonBajar").transform)
                .Select(Area).ToList();
            var cells = ruler.Cells.Select(Area).ToList();

            Assert.GreaterOrEqual(ruler.Cells[0].GetComponentInChildren<TMPro.TMP_Text>().fontSize, minimumFont);

            for (int i = 0; i < cells.Count; i++)
            {
                Assert.GreaterOrEqual(cells[i].xMin, 0f);
                Assert.LessOrEqual(cells[i].xMax, layout.LogicalSize.x);
                Assert.LessOrEqual(cells[i].yMax, layout.LogicalSize.y);
                Assert.IsFalse(others.Any(other => cells[i].Overlaps(other)), ruler.Cells[i].name);

                for (int j = i + 1; j < cells.Count; j++)
                {
                    Assert.IsFalse(cells[i].Overlaps(cells[j]), $"{ruler.Cells[i].name} y {ruler.Cells[j].name}");
                }
            }
        }

        [Test]
        public void GameButtonsDoNotMoveOnTheirOwn()
        {
            Assert.IsEmpty(Object.FindObjectsByType<ButtonAnimation>(FindObjectsSortMode.None));
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

        // Rectángulo en unidades lógicas, medido desde la esquina superior izquierda.
        private static Rect Area(RectTransform rect)
        {
            return new Rect(rect.anchoredPosition.x, -rect.anchoredPosition.y, rect.sizeDelta.x, rect.sizeDelta.y);
        }

        private RectTransform HealthBar()
        {
            return (RectTransform)Object.FindAnyObjectByType<DoorHealthBar>().transform;
        }
    }
}
