using System.Text;

namespace PuertaCastillo.Crypto
{
    /// <summary>
    /// Cifrado César sobre las letras A-Z. El resto de caracteres se conserva sin cambios.
    /// </summary>
    public static class CaesarCipher
    {
        private const int AlphabetSize = 26;

        public static string Encrypt(string text, int shift)
        {
            int normalizedShift = ((shift % AlphabetSize) + AlphabetSize) % AlphabetSize;
            var result = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                bool isLetter = c >= 'A' && c <= 'Z';
                result.Append(isLetter ? (char)('A' + (c - 'A' + normalizedShift) % AlphabetSize) : c);
            }

            return result.ToString();
        }

        public static string Decrypt(string text, int shift)
        {
            return Encrypt(text, -shift);
        }
    }
}
