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
    /// <summary>Pruebas de la regla de ayuda: casillas, letras cifradas y cambio de desplazamiento.</summary>
    public class CaesarRulerTests
    {
        private CaesarRuler ruler;

        /// <summary>Carga la escena de la partida antes de cada prueba.</summary>
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("PuertaDelCastillo");
            yield return null;
            ruler = Object.FindAnyObjectByType<CaesarRuler>();
        }

        /// <summary>La regla empieza con desplazamiento 0: cada letra igual a su cifrada.</summary>
        [Test]
        public void StartsAlignedWithoutShift()
        {
            Assert.AreEqual(0, ruler.Shift);
            Assert.AreEqual(ruler.PlainLetters, ruler.CipherLetters);
        }

        /// <summary>El botón + desplaza una posición las letras cifradas.</summary>
        [Test]
        public void IncreaseButtonSlidesTheLowerAlphabet()
        {
            Press("BotonSubir", 3);

            Assert.AreEqual(3, ruler.Shift);
            Assert.AreEqual("ABCDEFGHIJKLMNÑOPQRSTUVWXYZ", ruler.PlainLetters);
            Assert.AreEqual("DEFGHIJKLMNÑOPQRSTUVWXYZABC", ruler.CipherLetters);
        }

        /// <summary>El botón - desde 0 da la vuelta al abecedario (26).</summary>
        [Test]
        public void DecreaseButtonWrapsAroundTheAlphabet()
        {
            Press("BotonBajar", 1);

            Assert.AreEqual(26, ruler.Shift);
            Assert.AreEqual("ZABCDEFGHIJKLMNÑOPQRSTUVWXY", ruler.CipherLetters);
        }

        /// <summary>Cada casilla muestra la letra original encima de su cifrada.</summary>
        [Test]
        public void EachCellShowsTheOriginalLetterAboveItsCipher()
        {
            Press("BotonSubir", 3);

            TMP_Text[] labels = ruler.Cells[0].GetComponentsInChildren<TMP_Text>();

            Assert.AreEqual(27, ruler.Cells.Count);
            Assert.AreEqual("A", labels[0].text);
            Assert.AreEqual("D", labels[1].text);
            Assert.Greater(labels[0].rectTransform.anchorMin.y, labels[1].rectTransform.anchorMin.y);
        }

        private void Press(string button, int times)
        {
            Button target = ruler.GetComponentsInChildren<Button>().First(b => b.name == button);
            for (int i = 0; i < times; i++)
            {
                target.onClick.Invoke();
            }
        }
    }
}
