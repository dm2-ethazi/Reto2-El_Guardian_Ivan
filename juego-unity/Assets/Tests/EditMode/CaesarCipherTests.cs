using NUnit.Framework;
using PuertaCastillo.Crypto;

namespace PuertaCastillo.Tests
{
    /// <summary>Pruebas del cifrado César con el abecedario español.</summary>
    public class CaesarCipherTests
    {
        /// <summary>Cifrar avanza cada letra el desplazamiento indicado.</summary>
        [Test]
        public void EncryptsWithTheShift()
        {
            Assert.AreEqual("DEUH", CaesarCipher.Encrypt("ABRE", 3));
        }

        /// <summary>Al pasar de la Z se vuelve al principio del abecedario.</summary>
        [Test]
        public void WrapsAroundTheAlphabet()
        {
            Assert.AreEqual("ABC", CaesarCipher.Encrypt("XYZ", 3));
        }

        /// <summary>Los espacios y los caracteres que no son letras no cambian.</summary>
        [Test]
        public void KeepsSpacesAndOtherCharacters()
        {
            Assert.AreEqual("D E, 1!", CaesarCipher.Encrypt("A B, 1!", 3));
        }

        /// <summary>Descifrar devuelve el texto original con el desplazamiento de cada nivel.</summary>
        /// <param name="shift">Desplazamiento de un nivel.</param>
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(11)]
        [TestCase(13)]
        public void DecryptRecoversTheOriginalInEveryLevelShift(int shift)
        {
            const string message = "ABRE LA PUERTA";

            Assert.AreEqual(message, CaesarCipher.Decrypt(CaesarCipher.Encrypt(message, shift), shift));
        }

        /// <summary>La Ñ forma parte del abecedario: va entre la N y la O.</summary>
        [Test]
        public void TheEñeIsPartOfTheAlphabet()
        {
            Assert.AreEqual("Ñ", CaesarCipher.Encrypt("N", 1));
            Assert.AreEqual("O", CaesarCipher.Encrypt("Ñ", 1));
            Assert.AreEqual("N", CaesarCipher.Decrypt("Ñ", 1));
            Assert.AreEqual("Ñ", CaesarCipher.Decrypt(CaesarCipher.Encrypt("Ñ", 13), 13));
        }

        /// <summary>Un desplazamiento de vueltas enteras (0, 27, -27) deja el texto igual.</summary>
        /// <param name="shift">Desplazamiento múltiplo de 27.</param>
        [TestCase(0)]
        [TestCase(27)]
        [TestCase(-27)]
        public void WholeTurnsLeaveTheTextUnchanged(int shift)
        {
            Assert.AreEqual("PUERTA", CaesarCipher.Encrypt("PUERTA", shift));
        }
    }
}
