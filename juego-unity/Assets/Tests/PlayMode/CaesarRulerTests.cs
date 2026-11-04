using System.Collections;
using System.Linq;
using NUnit.Framework;
using PuertaCastillo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuertaCastillo.Tests
{
    public class CaesarRulerTests
    {
        private CaesarRuler ruler;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            ruler = Object.FindAnyObjectByType<CaesarRuler>();
        }

        [Test]
        public void StartsAlignedWithoutShift()
        {
            Assert.AreEqual(0, ruler.Shift);
            Assert.AreEqual(Row("FilaCifrada"), Row("FilaOriginal"));
        }

        [Test]
        public void IncreaseButtonSlidesTheLowerAlphabet()
        {
            Press("BotonSubir", 3);

            Assert.AreEqual(3, ruler.Shift);
            Assert.AreEqual("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Row("FilaOriginal"));
            Assert.AreEqual("DEFGHIJKLMNOPQRSTUVWXYZABC", Row("FilaCifrada"));
        }

        [Test]
        public void DecreaseButtonWrapsAroundTheAlphabet()
        {
            Press("BotonBajar", 1);

            Assert.AreEqual(25, ruler.Shift);
            Assert.AreEqual("ZABCDEFGHIJKLMNOPQRSTUVWXY", Row("FilaCifrada"));
        }

        private void Press(string button, int times)
        {
            Button target = ruler.GetComponentsInChildren<Button>().First(b => b.name == button);
            for (int i = 0; i < times; i++)
            {
                target.onClick.Invoke();
            }
        }

        private string Row(string name)
        {
            TMP_Text row = ruler.GetComponentsInChildren<TMP_Text>().First(label => label.name == name);
            row.ForceMeshUpdate();
            return row.GetParsedText();
        }
    }
}
