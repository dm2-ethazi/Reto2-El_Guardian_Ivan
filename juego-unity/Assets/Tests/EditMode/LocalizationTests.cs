using NUnit.Framework;
using PuertaCastillo.UI;
using UnityEngine;

namespace PuertaCastillo.Tests
{
    public class LocalizationTests
    {
        private Language previous;

        [SetUp]
        public void RememberLanguage()
        {
            previous = Localization.Current;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Localization.SetLanguage(previous);
        }

        [TestCase(Language.Spanish, "Descifrar")]
        [TestCase(Language.English, "Decrypt")]
        [TestCase(Language.Basque, "Deszifratu")]
        public void TextsChangeWithTheLanguage(Language language, string expected)
        {
            Localization.SetLanguage(language);

            Assert.AreEqual(expected, Localization.Get("game.decrypt"));
        }

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

        [Test]
        public void ChosenLanguageIsSavedInThePreferences()
        {
            Localization.SetLanguage(Language.Basque);

            Assert.AreEqual((int)Language.Basque, PlayerPrefs.GetInt("language"));
            Assert.AreEqual(Language.Basque, Localization.Current);
        }

        [Test]
        public void InvalidSavedLanguageFallsBackToSpanish()
        {
            PlayerPrefs.SetInt("language", 99);

            Assert.AreEqual(Language.Spanish, Localization.Current);
        }

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
