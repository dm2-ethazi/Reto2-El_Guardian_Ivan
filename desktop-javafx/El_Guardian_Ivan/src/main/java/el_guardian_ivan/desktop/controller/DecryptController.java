package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.files.FileTypeDetector;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.stage.FileChooser;

import java.io.File;
import java.nio.file.Path;
import java.util.Arrays;

/** Pantalla Descifrar: contenedor .hgw + contraseña -> fichero original. */
public class DecryptController extends FileOperationController {

    @FXML
    private void onDecrypt() {
        if (inputFile == null) {
            showError("Falta el fichero", "Selecciona primero el fichero .hgw que quieres descifrar.");
            return;
        }
        if (passwordField.getText().isEmpty()) {
            showError("Falta la contraseña", "Introduce la contraseña con la que se cifró el fichero.");
            return;
        }

        FileChooser chooser = new FileChooser();
        chooser.setTitle("Guardar fichero descifrado");
        chooser.setInitialDirectory(inputFile.toAbsolutePath().getParent().toFile());
        // Si el usuario no escribe extensión, se usa la original guardada (cifrada) en el .hgw
        chooser.setInitialFileName(withoutExtension(inputFile.getFileName().toString()));
        File selected = chooser.showSaveDialog(getWindow());
        if (selected == null) {
            return; // El usuario ha cancelado el diálogo
        }

        Path input = inputFile;
        Path output = toValidOutputPath(selected);
        if (output == null) {
            return; // Ya se ha mostrado la alerta
        }
        char[] password = takePassword();
        runInBackground("Descifrando...", "Fichero descifrado", new Task<>() {
            @Override
            protected Path call() throws Exception {
                try {
                    String originalExtension = aesFileCipher.decrypt(input, output, password, this::updateProgress);
                    return FileTypeDetector.addExtensionIfMissing(output, originalExtension);
                } finally {
                    Arrays.fill(password, '\0'); // No dejamos la contraseña en memoria
                }
            }
        });
    }
}
