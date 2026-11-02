package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
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

    /** El resultado siempre es un contenedor .hgw, aunque no se escriba la extensión. */
    @Override
    protected String outputFileName() {
        String name = super.outputFileName();
        return name.isEmpty() || name.toLowerCase(Locale.ROOT).endsWith(".hgw") ? name : name + ".hgw";
    }

    @Override
    protected void configureOutputChooser(FileChooser chooser) {
        chooser.setTitle(I18n.get("encrypt.chooserTitle"));
        chooser.getExtensionFilters().add(new FileChooser.ExtensionFilter(I18n.get("form.hgwFilter"), "*.hgw"));
    }

    @FXML
    private void onEncrypt() {
        if (inputFile == null) {
            showError(I18n.get("error.missingFile"), I18n.get("encrypt.missingFile"));
            return;
        }
        if (passwordField.getText().isEmpty()) {
            showError(I18n.get("error.missingPassword"), I18n.get("encrypt.missingPassword"));
            return;
        }

        Path input = inputFile;
        Path output = resolveOutputPath(outputFileName());
        if (output == null) {
            return; // Ya se ha mostrado la alerta
        }
        char[] password = takePassword();
        runInBackground(I18n.get("encrypt.running"), I18n.get("encrypt.success.title"),
                I18n.get("encrypt.success.text"), new Task<>() {
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
