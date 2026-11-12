package el_guardian_ivan.desktop.logging;

import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Quita de un texto los datos sensibles antes de escribirlo en los registros (logs).
 * Es la última barrera: aunque un mensaje o una excepción lleven una ruta o una contraseña,
 * lo que llega al fichero ya va enmascarado.
 *
 * <ul>
 *     <li>La carpeta del usuario ({@code C:\Users\ivan}) pasa a ser {@code ~}.</li>
 *     <li>El nombre de usuario del sistema pasa a ser {@code <usuario>}.</li>
 *     <li>Lo que siga a "contraseña", "password", "clave", "key", "token"... con {@code =} o {@code :}
 *     pasa a ser {@code ****}.</li>
 *     <li>Las cadenas hexadecimales o Base64 largas (claves, sales, nonces) pasan a ser {@code [oculto]}.</li>
 * </ul>
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public final class SensitiveDataMasker {

    /** Texto que sustituye a las contraseñas y claves. */
    static final String MASK = "****";
    /** Texto que sustituye a los bloques largos de bytes (hexadecimal o Base64). */
    static final String HIDDEN = "[oculto]";
    /** Texto que sustituye al nombre de usuario del sistema. */
    static final String USER = "<usuario>";

    /** "contraseña=1234", "password: abc", "clave = x"... (sin distinguir mayúsculas). */
    private static final Pattern SECRET_VALUE = Pattern.compile(
            "(?iu)\\b(contrase(?:ñ|n)a|password|passwd|pwd|pass|clave|key|secret|token|pasahitza)(\\s*[=:]\\s*)(\"[^\"]*\"|'[^']*'|\\S+)");
    /** 32 o más caracteres hexadecimales seguidos: claves, hashes, sales... */
    private static final Pattern LONG_HEX = Pattern.compile("\\b[0-9A-Fa-f]{32,}\\b");
    /** 20 o más caracteres Base64 seguidos que incluyan al menos un dígito (para no tapar palabras normales). */
    private static final Pattern LONG_BASE64 = Pattern.compile("(?<![\\w/+])(?=[A-Za-z0-9+/]*\\d)[A-Za-z0-9+/]{20,}={0,2}(?![\\w/+])");

    /** Instancia con la carpeta y el usuario del sistema actual. */
    private static final SensitiveDataMasker SYSTEM = new SensitiveDataMasker(
            System.getProperty("user.home"), System.getProperty("user.name"));

    /** Carpeta del usuario (como expresión regular), o {@code null} si no se conoce. */
    private final Pattern homePattern;
    /** Nombre de usuario (como expresión regular), o {@code null} si no se conoce o es muy corto. */
    private final Pattern userPattern;

    /**
     * Para los tests: permite indicar la carpeta y el usuario en lugar de usar los del sistema.
     *
     * @param userHome carpeta personal del usuario, por ejemplo {@code C:\Users\ivan}
     * @param userName nombre de usuario, por ejemplo {@code ivan}
     */
    SensitiveDataMasker(String userHome, String userName) {
        this.homePattern = isBlank(userHome) ? null : pathPattern(userHome);
        // Con nombres de 1-2 letras se taparían trozos de palabras normales
        this.userPattern = isBlank(userName) || userName.length() < 3 ? null
                : Pattern.compile("(?i)(?<![\\p{L}\\p{N}])" + Pattern.quote(userName) + "(?![\\p{L}\\p{N}])");
    }

    /**
     * Enmascara el texto con los datos del usuario que está usando la aplicación.
     *
     * @param text texto original (puede ser {@code null})
     * @return el texto sin datos sensibles
     */
    public static String maskText(String text) {
        return SYSTEM.mask(text);
    }

    /**
     * Enmascara el texto.
     *
     * @param text texto original (puede ser {@code null})
     * @return el texto sin datos sensibles, o {@code null} si era {@code null}
     */
    String mask(String text) {
        if (text == null || text.isEmpty()) {
            return text;
        }
        String result = text;
        if (homePattern != null) {
            result = homePattern.matcher(result).replaceAll("~");
        }
        if (userPattern != null) {
            result = userPattern.matcher(result).replaceAll(Matcher.quoteReplacement(USER));
        }
        result = SECRET_VALUE.matcher(result).replaceAll("$1$2" + MASK);
        result = LONG_HEX.matcher(result).replaceAll(Matcher.quoteReplacement(HIDDEN));
        result = LONG_BASE64.matcher(result).replaceAll(Matcher.quoteReplacement(HIDDEN));
        return result;
    }

    /**
     * Expresión que encuentra la carpeta con barras de Windows o de Linux y sin distinguir mayúsculas,
     * porque en Windows "C:\Users\Ivan" y "c:/users/ivan" son la misma carpeta.
     *
     * @param path carpeta
     * @return la expresión regular
     */
    private static Pattern pathPattern(String path) {
        String trimmed = path.replaceAll("[\\\\/]+$", "");
        StringBuilder regex = new StringBuilder();
        for (String part : trimmed.split("[\\\\/]+", -1)) {
            if (!regex.isEmpty()) {
                regex.append("[\\\\/]+");
            }
            regex.append(Pattern.quote(part));
        }
        boolean windows = System.getProperty("os.name", "").toLowerCase(Locale.ROOT).contains("win")
                || trimmed.matches("^[A-Za-z]:.*");
        return Pattern.compile(regex + "(?![\\p{L}\\p{N}])", windows ? Pattern.CASE_INSENSITIVE | Pattern.UNICODE_CASE : 0);
    }

    /**
     * Indica si un texto no tiene contenido.
     *
     * @param text texto
     * @return {@code true} si es {@code null} o solo espacios
     */
    private static boolean isBlank(String text) {
        return text == null || text.isBlank();
    }
}
