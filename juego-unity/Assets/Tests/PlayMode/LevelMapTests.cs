using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.Gameplay;
using PuertaCastillo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas del mapa de niveles: estados de los nodos, jugar un nivel y colocación en pantalla.</summary>
    public class LevelMapTests
    {
        private LevelMap map;

        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            LevelProgress.Reset();
            SaveGame.Clear();
            yield return LoadMapScene();
        }

        [TearDown]
        public void ClearProgress()
        {
            LevelProgress.Reset();
            SaveGame.Clear();
        }

        [Test]
        public void MapIsInTheBuildWithAnEventSystem()
        {
            Assert.GreaterOrEqual(SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MapaNiveles.unity"), 0);
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>());
            Assert.AreEqual(5, map.Nodes.Count);
        }

        [Test]
        public void WithoutProgressOnlyLevelOneCanBePressed()
        {
            Assert.IsTrue(map.Nodes[0].interactable);
            for (int i = 1; i < 5; i++)
            {
                Assert.IsFalse(map.Nodes[i].interactable, map.Nodes[i].name);
                Assert.IsNotNull(Find(map.Nodes[i], "Candado"), map.Nodes[i].name);
            }
        }

        [UnityTest]
        public IEnumerator CompletedLevelsShowAMarkAndTheNextIsAvailable()
        {
            LevelProgress.MarkCompleted(0);
            LevelProgress.MarkCompleted(1);
            LevelProgress.MarkCompleted(2);
            yield return LoadMapScene();

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(map.Nodes[i].interactable, map.Nodes[i].name);
                Assert.IsNotNull(Find(map.Nodes[i], "Insignia"), map.Nodes[i].name);
                Assert.IsNull(Find(map.Nodes[i], "Candado"), map.Nodes[i].name);
            }

            Assert.IsTrue(map.Nodes[3].interactable);
            Assert.IsNull(Find(map.Nodes[3], "Candado"));
            Assert.IsFalse(map.Nodes[4].interactable);
        }

        [UnityTest]
        public IEnumerator LockedLevelCannotBePlayed()
        {
            map.PlayLevel(4);
            yield return null;
            yield return null;

            Assert.AreEqual(LevelMap.SceneName, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator PressingAnEarlierLevelPlaysItWithoutLosingProgress()
        {
            for (int i = 0; i < 3; i++)
            {
                LevelProgress.MarkCompleted(i);
            }
            yield return LoadMapScene();

            map.Nodes[1].onClick.Invoke();
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual("PuertaDelCastillo", SceneManager.GetActiveScene().name);
            Assert.AreEqual(2, Object.FindAnyObjectByType<LevelManager>().LevelNumber);
            Assert.AreEqual(3, LevelProgress.CompletedCount);
            Assert.AreEqual(LevelState.Available, LevelProgress.StateOf(3));
        }

        [UnityTest]
        public IEnumerator BackButtonGoesToTheMainMenu()
        {
            map.BackButton.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.AreEqual(MainMenu.SceneName, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator TextsFollowTheLanguage()
        {
            Language previous = Localization.Current;
            Localization.SetLanguage(Language.Basque);
            yield return null;

            var texts = map.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => t.text).ToList();
            Assert.Contains("Erresumaren mapa", texts);
            Assert.AreEqual("Menua", map.BackButton.GetComponentInChildren<TMPro.TMP_Text>().text);

            Localization.SetLanguage(previous);
        }

        [TestCase(1080, 1920)]
        [TestCase(1080, 2400)]
        [TestCase(720, 1280)]
        [TestCase(1920, 1080)]
        [TestCase(2400, 1080)]
        [TestCase(1024, 768)]
        [TestCase(3440, 1440)]
        public void NodesFitTheScreenAndDoNotOverlap(int width, int height)
        {
            map.Apply(width, height);
            Vector2 half = map.LogicalSize / 2f;
            var areas = map.NodeAreas.Append(map.BackButtonArea).ToList();

            for (int i = 0; i < areas.Count; i++)
            {
                Assert.GreaterOrEqual(areas[i].width, 100f, $"{i}");
                Assert.GreaterOrEqual(areas[i].xMin, -half.x, $"{i}");
                Assert.LessOrEqual(areas[i].xMax, half.x, $"{i}");
                Assert.GreaterOrEqual(areas[i].yMin, -half.y, $"{i}");
                Assert.LessOrEqual(areas[i].yMax, half.y, $"{i}");

                for (int j = i + 1; j < areas.Count; j++)
                {
                    Assert.IsFalse(areas[i].Overlaps(areas[j]), $"{i} y {j}");
                }
            }
        }

        [Test]
        public void MapShowsTheIllustrationAndDarkensLockedLevels()
        {
            var picture = map.GetComponentsInChildren<Image>().First(image => image.name == "ImagenMapa");
            Assert.AreEqual("MapaProgresion", picture.sprite.name);
            Assert.IsNotNull(Find(map.Nodes[1], "Sombra"));
            Assert.IsNotNull(Find(map.Nodes[0], "Chispa"));
        }

        [Test]
        public void MapButtonsDoNotAnimate()
        {
            Assert.IsEmpty(map.GetComponentsInChildren<ButtonAnimation>(true));
            foreach (Button button in map.GetComponentsInChildren<Button>(true))
            {
                Assert.AreEqual(0f, button.colors.fadeDuration);
            }
        }

        private IEnumerator LoadMapScene()
        {
            yield return SceneManager.LoadSceneAsync(LevelMap.SceneName);
            yield return null;
            yield return null;
            map = Object.FindAnyObjectByType<LevelMap>();
        }

        private static Transform Find(Button node, string childName)
        {
            return node.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == childName);
        }
    }
}
