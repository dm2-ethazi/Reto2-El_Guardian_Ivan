package el_guardian_ivan.desktop.logging;

import java.nio.file.Path;
import java.util.Locale;

/**
 * Cómo se describen los ficheros en los registros: solo el tipo y el tamaño, nunca el nombre ni la
 * ruta, porque el nombre de un fichero ya puede revelar algo privado ("nominas_2025.pdf").
 */
public final class LogFormat {

    /** Clase de utilidades: no se crean objetos. */
    private LogFormat() {
    }

    /**
     * Descripción no sensible de un fichero, por ejemplo "fichero .png de 1.5 MB".
     *
     * @param file fichero (puede ser {@code null})
     * @param size tamaño en bytes, o un número negativo si no se conoce
     * @return la descripción
     */
    public static String describe(Path file, long size) {
        String extension = extension(file);
        String type = extension.isEmpty() ? "fichero sin extensión" : "fichero ." + extension;
        return size < 0 ? type : type + " de " + bytes(size);
    }

    /**
     * Extensión del fichero en minúsculas, sin punto; "" si no tiene o no parece una extensión normal.
     *
     * @param file fichero (puede ser {@code null})
     * @return la extensión
     */
    static String extension(Path file) {
        if (file == null || file.getFileName() == null) {
            return "";
        }
        String name = file.getFileName().toString();
        int dot = name.lastIndexOf('.');
        String extension = dot > 0 ? name.substring(dot + 1).toLowerCase(Locale.ROOT) : "";
        // Solo extensiones cortas y normales: "informe.secreto de Ana" no es una extensión
        return extension.matches("[a-z0-9]{1,10}") ? extension : "";
    }

    /**
     * Tamaño con punto decimal fijo para que los registros sean iguales en cualquier idioma.
     *
     * @param bytes tamaño en bytes
     * @return por ejemplo "512 B" o "1.5 MB"
     */
    static String bytes(long bytes) {
        if (bytes < 1024) {
            return bytes + " B";
        }
        String[] units = {"KB", "MB", "GB", "TB"};
        double value = bytes;
        int unit = -1;
        while (value >= 1024 && unit < units.length - 1) {
            value /= 1024;
            unit++;
        }
        return String.format(Locale.ROOT, "%.1f %s", value, units[unit]);
    }
}
