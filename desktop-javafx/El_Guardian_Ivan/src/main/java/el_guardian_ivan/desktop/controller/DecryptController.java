package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
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

    /** Al descifrar se ve el resultado al momento si es una imagen o texto plano. */
    @Override
    protected boolean showsPreview() {
        return true;
    }

    @Override
    protected void configureOutputChooser(FileChooser chooser) {
        chooser.setTitle(I18n.get("decrypt.chooserTitle"));
    }

    @FXML
    private void onDecrypt() {
        if (inputFile == null) {
            showError(I18n.get("error.missingFile"), I18n.get("decrypt.missingFile"));
            return;
        }
        if (passwordField.getText().isEmpty()) {
            showError(I18n.get("error.missingPassword"), I18n.get("decrypt.missingPassword"));
            return;
        }

        Path input = inputFile;
        Path output = resolveOutputPath(outputFileName());
        if (output == null) {
            return; // Ya se ha mostrado la alerta
        }
        char[] password = takePassword();
        runInBackground(I18n.get("decrypt.running"), I18n.get("decrypt.success.title"),
                I18n.get("decrypt.success.text"), new Task<>() {
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
