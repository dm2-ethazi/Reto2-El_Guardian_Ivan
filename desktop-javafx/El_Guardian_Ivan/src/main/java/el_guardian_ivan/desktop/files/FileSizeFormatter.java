package el_guardian_ivan.desktop.files;

import java.util.Locale;

/** Convierte un tamaño en bytes a un texto legible: "512 B", "1,5 KB", "20,0 MB", "4,2 GB"... */
public final class FileSizeFormatter {

    private static final String[] UNITS = {"B", "KB", "MB", "GB", "TB"};
    private static final Locale SPANISH = Locale.forLanguageTag("es-ES");

    private FileSizeFormatter() {
    }

    /** Usa múltiplos de 1024, como el Explorador de Windows. */
    public static String format(long bytes) {
        if (bytes < 0) {
            throw new IllegalArgumentException("El tamaño no puede ser negativo.");
        }
        if (bytes < 1024) {
            return bytes + " B";
        }
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < UNITS.length - 1) {
            value /= 1024;
            unit++;
        }
        return String.format(SPANISH, "%.1f %s", value, UNITS[unit]);
    }
}
