package el_guardian_ivan.desktop.files;

import el_guardian_ivan.desktop.i18n.I18n;

import java.util.Locale;
import java.util.Set;

/**
 * Comprueba que un nombre de fichero de salida sea válido en Windows y en Linux.
 * La aplicación es la misma para los dos sistemas, así que aplicamos las reglas de Windows,
 * que son las más estrictas: un fichero creado en Linux debe poder abrirse luego en Windows.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public final class FileNameValidator {

    /** Caracteres que Windows no admite en un nombre de fichero. */
    private static final String FORBIDDEN_CHARACTERS = "<>:\"/\\|?*";
    /** Longitud máxima de un nombre en Windows y en la mayoría de sistemas de ficheros de Linux. */
    private static final int MAX_LENGTH = 255;
    /** Nombres de dispositivos de Windows: no se pueden usar ni con extensión ("CON.txt" tampoco). */
    private static final Set<String> RESERVED_NAMES = Set.of(
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9");

    /** Clase de utilidades: no se crean objetos. */
    private FileNameValidator() {
    }

    /**
     * Devuelve un mensaje (en el idioma actual) explicando el problema, o {@code null} si el nombre es válido.
     *
     * @param name nombre que ha escrito el usuario
     * @return el motivo por el que no vale, o {@code null} si es válido
     */
    public static String validate(String name) {
        if (name == null || name.isBlank()) {
            return I18n.get("name.empty");
        }
        if (name.length() > MAX_LENGTH) {
            return I18n.get("name.tooLong", MAX_LENGTH);
        }
        for (char c : name.toCharArray()) {
            if (c < 32) { // Caracteres de control (salto de línea, tabulador...)
                return I18n.get("name.control");
            }
            if (FORBIDDEN_CHARACTERS.indexOf(c) >= 0) {
                return I18n.get("name.forbidden", FORBIDDEN_CHARACTERS);
            }
        }
        // Windows quita en silencio el punto o espacio final: el fichero acabaría con otro nombre
        if (name.endsWith(".") || name.endsWith(" ")) {
            return I18n.get("name.trailing");
        }
        if (name.equals(".") || name.equals("..")) {
            return I18n.get("name.invalid");
        }
        // Se compara la parte anterior al primer punto, sin distinguir mayúsculas
        int dot = name.indexOf('.');
        String base = (dot >= 0 ? name.substring(0, dot) : name).trim().toUpperCase(Locale.ROOT);
        if (RESERVED_NAMES.contains(base)) {
            return I18n.get("name.reserved", base);
        }
        return null;
    }
}
