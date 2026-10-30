package el_guardian_ivan.desktop.controller;

import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.stage.FileChooser;

import java.io.File;
import java.nio.file.Path;
import java.util.Arrays;

/** Pantalla Cifrar: fichero de entrada + contraseña -> contenedor .hgw. */
public class EncryptController extends FileOperationController {

    @FXML
    private void onEncrypt() {
        if (inputFile == null) {
            showError("Falta el fichero", "Selecciona primero el fichero que quieres cifrar.");
            return;
        }
        if (passwordField.getText().isEmpty()) {
            showError("Falta la contraseña", "Introduce una contraseña para cifrar el fichero.");
            return;
        }

        FileChooser chooser = new FileChooser();
        chooser.setTitle("Guardar fichero cifrado");
        chooser.getExtensionFilters().add(new FileChooser.ExtensionFilter("Contenedor cifrado (*.hgw)", "*.hgw"));
        chooser.setInitialDirectory(inputFile.toAbsolutePath().getParent().toFile());
        chooser.setInitialFileName(withoutExtension(inputFile.getFileName().toString()) + ".hgw");
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
        runInBackground("Cifrando...", "Fichero cifrado", new Task<>() {
            @Override
            protected Path call() throws Exception {
                try {
                    aesFileCipher.encrypt(input, output, password, this::updateProgress);
                    return output;
                } finally {
                    Arrays.fill(password, '\0'); // No dejamos la contraseña en memoria
                }
            }
        });
    }
}
