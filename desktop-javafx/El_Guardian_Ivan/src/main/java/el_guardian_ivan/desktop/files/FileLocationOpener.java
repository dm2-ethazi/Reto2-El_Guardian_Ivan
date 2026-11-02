package el_guardian_ivan.desktop.files;

import java.io.IOException;
import java.nio.file.Path;
import java.util.Locale;

/**
 * Abre el explorador de archivos del sistema en la carpeta de un fichero.
 * En Windows y macOS además deja el fichero seleccionado.
 */
public final class FileLocationOpener {

    private FileLocationOpener() {
    }

    public static void reveal(Path file) throws IOException {
        Path absolute = file.toAbsolutePath();
        String os = System.getProperty("os.name", "").toLowerCase(Locale.ROOT);
        ProcessBuilder command;
        if (os.contains("win")) {
            // "/select," y la ruta van separados para que funcione con espacios en el nombre
            command = new ProcessBuilder("explorer.exe", "/select,", absolute.toString());
        } else if (os.contains("mac")) {
            command = new ProcessBuilder("open", "-R", absolute.toString());
        } else {
            command = new ProcessBuilder("xdg-open", absolute.getParent().toString());
        }
        command.start();
    }
}
