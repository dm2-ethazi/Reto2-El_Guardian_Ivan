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
    /// <summary>Pruebas del menú principal: botones, idiomas, continuar y salir.</summary>
    public class MainMenuTests
    {
        private MainMenu menu;

        /// <summary>Carga el menú principal antes de cada prueba.</summary>
        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            SaveGame.Clear();
            yield return SceneManager.LoadSceneAsync(MainMenu.SceneName);
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenu>();
        }

        /// <summary>Borra la partida guardada después de cada prueba.</summary>
        [TearDown]
        public void ClearSavedGame()
        {
            SaveGame.Clear();
            LevelProgress.Reset();
        }

        /// <summary>Sin partida guardada no hay botón Continuar.</summary>
        [Test]
        public void WithoutSavedGameThereIsNoContinueButton()
        {
            Assert.IsNull(menu.ContinueButton);
        }

        /// <summary>Con partida guardada, Continuar sale encima de Iniciar y carga ese nivel.</summary>
        [UnityTest]
        public IEnumerator SavedGameShowsContinueAboveStartAndLoadsThatLevel()
        {
            SaveGame.Save(2);
            yield return SceneManager.LoadSceneAsync(MainMenu.SceneName);
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenu>();

            Button continueButton = menu.ContinueButton;
            Assert.IsNotNull(continueButton);
            Assert.AreEqual(Localization.Format("menu.continue", 3), continueButton.GetComponentInChildren<TMPro.TMP_Text>(true).text);
            var start = (RectTransform)LanguageButton("BotonIniciar").transform;
            Assert.Greater(((RectTransform)continueButton.transform).anchoredPosition.y, start.anchoredPosition.y);

            continueButton.onClick.Invoke();
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(3, Object.FindAnyObjectByType<LevelManager>().LevelNumber);
        }

        /// <summary>Iniciar partida abre el mapa sin tocar la partida guardada.</summary>
        [UnityTest]
        public IEnumerator StartButtonOpensTheMapWithoutTouchingTheSavedGame()
        {
            SaveGame.Save(3);
            yield return SceneManager.LoadSceneAsync(MainMenu.SceneName);
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenu>();

            Press("BotonIniciar");
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(LevelMap.SceneName, SceneManager.GetActiveScene().name);
            Assert.AreEqual(3, SaveGame.SavedLevelIndex);
            Assert.IsTrue(Object.FindAnyObjectByType<LevelMap>().Nodes[3].interactable);
        }

        /// <summary>El menú es la primera escena de la compilación.</summary>
        [Test]
        public void MenuIsTheFirstSceneOfTheBuild()
        {
            Assert.AreEqual(0, SceneManager.GetActiveScene().buildIndex);
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>());
        }

        /// <summary>La confirmación de salir está oculta al empezar.</summary>
        [Test]
        public void ConfirmationIsHiddenAtTheStart()
        {
            Assert.IsFalse(ConfirmPanel().activeSelf);
        }

        /// <summary>Iniciar partida carga el mapa de niveles.</summary>
        [UnityTest]
        public IEnumerator StartButtonLoadsTheMap()
        {
            Press("BotonIniciar");
            yield return null;
            yield return null;

            Assert.AreEqual(LevelMap.SceneName, SceneManager.GetActiveScene().name);
        }

        /// <summary>Salir pide confirmación y se puede cancelar.</summary>
        [Test]
        public void ExitAsksForConfirmationAndCanBeCancelled()
        {
            Press("BotonSalir");
            Assert.IsTrue(ConfirmPanel().activeSelf);

            Press("BotonNoSalir");
            Assert.IsFalse(ConfirmPanel().activeSelf);
        }

        /// <summary>Los botones de idioma traducen el menú y el idioma se mantiene al recargar.</summary>
        [UnityTest]
        public IEnumerator LanguageButtonTranslatesTheMenuAndIsKeptAfterReloading()
        {
            Language previous = Localization.Current;

            Press("BotonEs");
            Press("BotonEu");
            Assert.AreEqual(Localization.Get("menu.exit"), ExitLabel());
            Assert.AreEqual("Irten", ExitLabel());
            Assert.IsFalse(LanguageButton("BotonEu").interactable);
            Assert.IsTrue(LanguageButton("BotonEs").interactable);

            yield return SceneManager.LoadSceneAsync(MainMenu.SceneName);
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenu>();

            Assert.AreEqual("Irten", ExitLabel());
            Assert.IsFalse(LanguageButton("BotonEu").interactable);

            Localization.SetLanguage(previous);
        }

        /// <summary>El menú queda centrado y sus botones no tienen animación.</summary>
        /// <param name="width">Ancho de la pantalla en píxeles.</param>
        /// <param name="height">Alto de la pantalla en píxeles.</param>
        [TestCase(1080, 1920)]
        [TestCase(1080, 2400)]
        [TestCase(1920, 1080)]
        [TestCase(1024, 768)]
        [TestCase(3440, 1440)]
        public void MenuStaysCenteredAndButtonsDoNotAnimate(int width, int height)
        {
            var layout = menu.GetComponent<MenuLayout>();
            var safe = new Rect(24, 32, width - 64, height - 80);
            layout.Apply(width, height, safe);
            var column = (RectTransform)menu.transform.Find("Columna");
            var logical = ScreenFit.LogicalSize(width, height);
            var expected = (safe.center - new Vector2(width, height) / 2f) * (logical.x / width);
            Assert.Less(Vector2.Distance(expected, column.anchoredPosition), 0.01f);
            Assert.IsEmpty(menu.GetComponentsInChildren<ButtonAnimation>(true));
            foreach (Button button in menu.GetComponentsInChildren<Button>(true))
            {
                Assert.AreEqual(0f, button.colors.fadeDuration);
                Assert.IsNotNull(button.image.sprite);
            }
            layout.Apply(width, height);
            Assert.Less(column.anchoredPosition.magnitude, 0.01f);
        }
        private string ExitLabel()
        {
            return LanguageButton("BotonSalir").GetComponentInChildren<TMPro.TMP_Text>(true).text;
        }

        private Button LanguageButton(string name)
        {
            return menu.GetComponentsInChildren<Button>(true).First(b => b.name == name);
        }

        private GameObject ConfirmPanel()
        {
            return menu.transform.Cast<Transform>().First(child => child.name == "PanelConfirmacion").gameObject;
        }

        private void Press(string button)
        {
            menu.GetComponentsInChildren<Button>(true).First(b => b.name == button).onClick.Invoke();
        }
    }
}
