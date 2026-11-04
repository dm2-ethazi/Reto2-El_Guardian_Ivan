using System.Text;

namespace PuertaCastillo.Crypto
{
    /// <summary>
    /// Cifrado César sobre el abecedario español. El resto de caracteres se conserva sin cambios.
    /// </summary>
    public static class CaesarCipher
    {
        public const string Alphabet = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

        public static string Encrypt(string text, int shift)
        {
            int normalizedShift = ((shift % Alphabet.Length) + Alphabet.Length) % Alphabet.Length;
            var result = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                int index = Alphabet.IndexOf(c);
                result.Append(index >= 0 ? Alphabet[(index + normalizedShift) % Alphabet.Length] : c);
            }

            return result.ToString();
        }

        public static string Decrypt(string text, int shift)
        {
            return Encrypt(text, -shift);
        }
    }
}
