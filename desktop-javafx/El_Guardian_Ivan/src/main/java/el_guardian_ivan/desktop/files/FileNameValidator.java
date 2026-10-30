package el_guardian_ivan.desktop.files;

import java.util.Locale;
import java.util.Set;

/**
 * Comprueba que un nombre de fichero de salida sea válido en Windows y en Linux.
 * La aplicación es la misma para los dos sistemas, así que aplicamos las reglas de Windows,
 * que son las más estrictas: un fichero creado en Linux debe poder abrirse luego en Windows.
 */
public final class FileNameValidator {

    private static final String FORBIDDEN_CHARACTERS = "<>:\"/\\|?*";
    private static final int MAX_LENGTH = 255;
    private static final Set<String> RESERVED_NAMES = Set.of(
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9");

    private FileNameValidator() {
    }

    /** Devuelve un mensaje explicando el problema, o {@code null} si el nombre es válido. */
    public static String validate(String name) {
        if (name == null || name.isBlank()) {
            return "El nombre del fichero no puede estar vacío.";
        }
        if (name.length() > MAX_LENGTH) {
            return "El nombre del fichero es demasiado largo (máximo " + MAX_LENGTH + " caracteres).";
        }
        for (char c : name.toCharArray()) {
            if (c < 32) {
                return "El nombre del fichero contiene caracteres de control.";
            }
            if (FORBIDDEN_CHARACTERS.indexOf(c) >= 0) {
                return "El nombre del fichero no puede contener ninguno de estos caracteres: " + FORBIDDEN_CHARACTERS;
            }
        }
        if (name.endsWith(".") || name.endsWith(" ")) {
            return "El nombre del fichero no puede terminar en punto ni en espacio.";
        }
        if (name.equals(".") || name.equals("..")) {
            return "El nombre del fichero no es válido.";
        }
        int dot = name.indexOf('.');
        String base = (dot >= 0 ? name.substring(0, dot) : name).trim().toUpperCase(Locale.ROOT);
        if (RESERVED_NAMES.contains(base)) {
            return "\"" + base + "\" es un nombre reservado de Windows y no se puede usar.";
        }
        return null;
    }
}
