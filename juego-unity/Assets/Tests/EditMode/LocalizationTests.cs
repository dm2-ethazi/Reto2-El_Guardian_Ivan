using NUnit.Framework;
using PuertaCastillo.UI;
using UnityEngine;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas de las traducciones: todas las claves existen en los tres idiomas.</summary>
    public class LocalizationTests
    {
        private Language previous;

        /// <summary>Guarda el idioma elegido antes de la prueba.</summary>
        [SetUp]
        public void RememberLanguage()
        {
            previous = Localization.Current;
        }

        /// <summary>Vuelve a poner el idioma que había.</summary>
        [TearDown]
        public void RestoreLanguage()
        {
            Localization.SetLanguage(previous);
        }

        /// <summary>Los textos cambian con el idioma.</summary>
        /// <param name="language">Idioma elegido.</param>
        /// <param name="expected">Texto esperado del botón Descifrar.</param>
        [TestCase(Language.Spanish, "Descifrar")]
        [TestCase(Language.English, "Decrypt")]
        [TestCase(Language.Basque, "Deszifratu")]
        public void TextsChangeWithTheLanguage(Language language, string expected)
        {
            Localization.SetLanguage(language);

            Assert.AreEqual(expected, Localization.Get("game.decrypt"));
        }

        /// <summary>Todas las claves tienen texto en los tres idiomas.</summary>
        [Test]
        public void EveryTextIsTranslatedIntoEveryLanguage()
        {
            foreach (Language language in System.Enum.GetValues(typeof(Language)))
            {
                Localization.SetLanguage(language);

                foreach (string key in Localization.Keys)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(Localization.Get(key)), key);
                }
            }
        }

        /// <summary>El idioma elegido se guarda en PlayerPrefs.</summary>
        [Test]
        public void ChosenLanguageIsSavedInThePreferences()
        {
            Localization.SetLanguage(Language.Basque);

            Assert.AreEqual((int)Language.Basque, PlayerPrefs.GetInt("language"));
            Assert.AreEqual(Language.Basque, Localization.Current);
        }

        /// <summary>Un idioma guardado no válido se trata como español.</summary>
        [Test]
        public void InvalidSavedLanguageFallsBackToSpanish()
        {
            PlayerPrefs.SetInt("language", 99);

            Assert.AreEqual(Language.Spanish, Localization.Current);
        }

        /// <summary>Cambiar de idioma avisa a los textos para que se traduzcan.</summary>
        [Test]
        public void ChangingTheLanguageNotifiesTheTexts()
        {
            int changes = 0;
            void Count() => changes++;
            Localization.LanguageChanged += Count;

            Localization.SetLanguage(Language.English);
            Localization.LanguageChanged -= Count;

            Assert.AreEqual(1, changes);
        }
    }
}
