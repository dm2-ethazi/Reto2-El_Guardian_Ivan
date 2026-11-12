package el_guardian_ivan.desktop.files;

import java.util.Locale;

/**
 * Convierte un tamaño en bytes a un texto legible: "512 B", "1,5 KB", "20,0 MB", "4,2 GB"...
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public final class FileSizeFormatter {

    /** Unidades de menor a mayor; cada una es 1024 veces la anterior. */
    private static final String[] UNITS = {"B", "KB", "MB", "GB", "TB"};
    /** Formato español: coma decimal. */
    private static final Locale SPANISH = Locale.forLanguageTag("es-ES");

    /** Clase de utilidades: no se crean objetos. */
    private FileSizeFormatter() {
    }

    /**
     * Usa múltiplos de 1024, como el Explorador de Windows, con la coma decimal española.
     *
     * @param bytes tamaño en bytes
     * @return el tamaño con su unidad, por ejemplo "3,2 MB"
     */
    public static String format(long bytes) {
        return format(bytes, SPANISH);
    }

    /**
     * Igual, pero con el separador decimal del idioma indicado ("1,5 MB" o "1.5 MB").
     *
     * @param bytes tamaño en bytes (no negativo)
     * @param locale idioma que decide el separador decimal
     * @return el tamaño con su unidad
     * @throws IllegalArgumentException si {@code bytes} es negativo
     */
    public static String format(long bytes, Locale locale) {
        if (bytes < 0) {
            throw new IllegalArgumentException("El tamaño no puede ser negativo.");
        }
        if (bytes < 1024) {
            return bytes + " B"; // Los bytes se muestran sin decimales
        }
        // Se divide entre 1024 hasta que el número quede por debajo de 1024
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < UNITS.length - 1) {
            value /= 1024;
            unit++;
        }
        return String.format(locale, "%.1f %s", value, UNITS[unit]);
    }
}
