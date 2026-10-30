package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.files.FileTypeDetector;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.stage.FileChooser;

import java.nio.file.Path;
import java.util.Arrays;

/** Pantalla Descifrar: contenedor .hgw + contraseña -> fichero original. */
public class DecryptController extends FileOperationController {

    /** Sin extensión: si el usuario no escribe una, se usa la original guardada (cifrada) en el .hgw. */
    @Override
    protected String suggestOutputName(String inputName) {
        return withoutExtension(inputName);
    }

    @Override
    protected void configureOutputChooser(FileChooser chooser) {
        chooser.setTitle("Guardar fichero descifrado");
    }

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

        Path input = inputFile;
        Path output = resolveOutputPath(outputNameField.getText().trim());
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
