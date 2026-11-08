using System.Text;

namespace PuertaCastillo.Crypto
{
    /// <summary>
    /// Cifrado César sobre el abecedario español. El resto de caracteres se conserva sin cambios.
    /// </summary>
    public static class CaesarCipher
    {
        /// <summary>Abecedario español de 27 letras (con Ñ). Su orden define el cifrado.</summary>
        public const string Alphabet = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

        /// <summary>Cifra el texto avanzando cada letra <paramref name="shift"/> posiciones en el abecedario.</summary>
        /// <param name="text">Texto en mayúsculas. Lo que no es letra del abecedario se copia igual.</param>
        /// <param name="shift">Desplazamiento; puede ser negativo o mayor que 27.</param>
        /// <returns>El texto cifrado.</returns>
        public static string Encrypt(string text, int shift)
        {
            // Deja el desplazamiento entre 0 y 26, también si es negativo (así descifra Decrypt).
            int normalizedShift = ((shift % Alphabet.Length) + Alphabet.Length) % Alphabet.Length;
            var result = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                // Cada letra avanza en el abecedario y vuelve al principio al pasar de la Z.
                int index = Alphabet.IndexOf(c);
                result.Append(index >= 0 ? Alphabet[(index + normalizedShift) % Alphabet.Length] : c);
            }

            return result.ToString();
        }

        /// <summary>Descifra: es cifrar con el desplazamiento contrario.</summary>
        public static string Decrypt(string text, int shift)
        {
            return Encrypt(text, -shift);
        }
    }
}
