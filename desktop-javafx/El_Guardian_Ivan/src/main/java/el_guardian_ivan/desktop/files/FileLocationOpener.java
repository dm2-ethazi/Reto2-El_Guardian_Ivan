package el_guardian_ivan.desktop.files;

import java.io.IOException;
import java.nio.file.Path;
import java.util.Locale;

/**
 * Abre el explorador de archivos del sistema en la carpeta de un fichero.
 * En Windows y macOS además deja el fichero seleccionado.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public final class FileLocationOpener {

    /** Clase de utilidades: no se crean objetos. */
    private FileLocationOpener() {
    }

    /**
     * Abre la carpeta de {@code file}; lanza IOException si el sistema no puede abrirla.
     *
     * @param file fichero cuya carpeta se abre
     * @throws IOException si no se puede lanzar el explorador
     */
    public static void reveal(Path file) throws IOException {
        Path absolute = file.toAbsolutePath();
        // Cada sistema tiene su propio comando para abrir el explorador
        String os = System.getProperty("os.name", "").toLowerCase(Locale.ROOT);
        ProcessBuilder command;
        if (os.contains("win")) {
            // "/select," y la ruta van separados para que funcione con espacios en el nombre
            command = new ProcessBuilder("explorer.exe", "/select,", absolute.toString());
        } else if (os.contains("mac")) {
            command = new ProcessBuilder("open", "-R", absolute.toString());
        } else {
            // En Linux no hay forma estándar de seleccionar el fichero: se abre su carpeta
            command = new ProcessBuilder("xdg-open", absolute.getParent().toString());
        }
        command.start(); // No esperamos a que termine: el explorador sigue abierto por su cuenta
    }
}
