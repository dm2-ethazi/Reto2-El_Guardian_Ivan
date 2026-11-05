using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    public class MainMenuTests
    {
        private MainMenu menu;

        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync(MainMenu.SceneName);
            yield return null;
            menu = Object.FindAnyObjectByType<MainMenu>();
        }

        [Test]
        public void MenuIsTheFirstSceneOfTheBuild()
        {
            Assert.AreEqual(0, SceneManager.GetActiveScene().buildIndex);
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>());
        }

        [Test]
        public void ConfirmationIsHiddenAtTheStart()
        {
            Assert.IsFalse(ConfirmPanel().activeSelf);
        }

        [UnityTest]
        public IEnumerator StartButtonLoadsTheGame()
        {
            Press("BotonIniciar");
            yield return null;
            yield return null;

            Assert.AreEqual("PuertaDelCastillo", SceneManager.GetActiveScene().name);
        }

        [Test]
        public void ExitAsksForConfirmationAndCanBeCancelled()
        {
            Press("BotonSalir");
            Assert.IsTrue(ConfirmPanel().activeSelf);

            Press("BotonNoSalir");
            Assert.IsFalse(ConfirmPanel().activeSelf);
        }

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
