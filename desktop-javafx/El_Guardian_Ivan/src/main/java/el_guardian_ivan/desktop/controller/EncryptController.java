package el_guardian_ivan.desktop.controller;

import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.stage.FileChooser;

import java.nio.file.Path;
import java.util.Arrays;
import java.util.Locale;

/** Pantalla Cifrar: fichero de entrada + contraseña -> contenedor .hgw. */
public class EncryptController extends FileOperationController {

    @Override
    protected String suggestOutputName(String inputName) {
        return withoutExtension(inputName) + ".hgw";
    }

    @Override
    protected void configureOutputChooser(FileChooser chooser) {
        chooser.setTitle("Guardar fichero cifrado");
        chooser.getExtensionFilters().add(new FileChooser.ExtensionFilter("Contenedor cifrado (*.hgw)", "*.hgw"));
    }

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

        String name = outputNameField.getText().trim();
        if (!name.toLowerCase(Locale.ROOT).endsWith(".hgw")) {
            name += ".hgw"; // El resultado siempre es un contenedor .hgw
        }
        Path input = inputFile;
        Path output = resolveOutputPath(name);
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
