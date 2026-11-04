using NUnit.Framework;
using PuertaCastillo.Crypto;

namespace PuertaCastillo.Tests
{
    public class CaesarCipherTests
    {
        [Test]
        public void EncryptsWithTheShift()
        {
            Assert.AreEqual("DEUH", CaesarCipher.Encrypt("ABRE", 3));
        }

        [Test]
        public void WrapsAroundTheAlphabet()
        {
            Assert.AreEqual("ABC", CaesarCipher.Encrypt("XYZ", 3));
        }

        [Test]
        public void KeepsSpacesAndOtherCharacters()
        {
            Assert.AreEqual("D E, 1!", CaesarCipher.Encrypt("A B, 1!", 3));
        }

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

        [TestCase(0)]
        [TestCase(26)]
        [TestCase(-26)]
        public void WholeTurnsLeaveTheTextUnchanged(int shift)
        {
            Assert.AreEqual("PUERTA", CaesarCipher.Encrypt("PUERTA", shift));
        }
    }
}
