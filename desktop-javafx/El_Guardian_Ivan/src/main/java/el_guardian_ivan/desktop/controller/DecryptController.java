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

    /** FXMLLoader crea el controlador al cargar decrypt-view.fxml. */
    public DecryptController() {
    }

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

    /** El diálogo de "Cambiar ubicación" no filtra: el resultado puede ser de cualquier tipo. */
    @Override
    protected void configureOutputChooser(FileChooser chooser) {
        chooser.setTitle(I18n.get("decrypt.chooserTitle"));
    }

    /**
     * Botón "Descifrar archivo" (o Intro): comprueba que hay fichero y contraseña y descifra
     * en segundo plano. Si la contraseña no es la correcta, el cifrado lo detecta y avisa.
     */
    @FXML
    private void onDecrypt() {
        // Antes de empezar, avisamos de lo que falte en lugar de fallar a mitad
        if (inputFile == null) {
            showError(I18n.get("error.missingFile"), I18n.get("decrypt.missingFile"));
            return;
        }
        if (passwordField.getText().isEmpty()) {
            showError(I18n.get("error.missingPassword"), I18n.get("decrypt.missingPassword"));
            return;
        }

        // Copias locales: el hilo de fondo no debe leer campos que el usuario podría cambiar mientras tanto
        Path input = inputFile;
        Path output = resolveOutputPath(outputFileName());
        if (output == null) {
            return; // Ya se ha mostrado la alerta
        }
        char[] password = takePassword(); // La contraseña sale del campo y este se vacía
        runInBackground(I18n.get("decrypt.running"), I18n.get("decrypt.success.title"),
                I18n.get("decrypt.success.text"), new Task<>() {
            @Override
            protected Path call() throws Exception {
                try {
                    // El .hgw guarda (cifrada) la extensión original, por ejemplo "png"
                    String originalExtension = aesFileCipher.decrypt(input, output, password, this::updateProgress);
                    // Si el usuario no puso extensión en el nombre de salida, se añade la original
                    return FileTypeDetector.addExtensionIfMissing(output, originalExtension);
                } finally {
                    Arrays.fill(password, '\0'); // No dejamos la contraseña en memoria
                }
            }
        });
    }
}
