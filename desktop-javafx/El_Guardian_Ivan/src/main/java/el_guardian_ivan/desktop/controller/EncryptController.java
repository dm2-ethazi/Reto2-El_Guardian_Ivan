package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.stage.FileChooser;

import java.nio.file.Path;
import java.util.Arrays;
import java.util.Locale;

/**
 * Pantalla Cifrar: fichero de entrada + contraseña -> contenedor .hgw.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public class EncryptController extends FileOperationController {

    /** FXMLLoader crea el controlador al cargar encrypt-view.fxml. */
    public EncryptController() {
    }

    /** Nombre propuesto para el resultado: "foto.png" pasa a "foto.hgw". */
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

    /** El diálogo de "Cambiar ubicación" solo enseña ficheros .hgw. */
    @Override
    protected void configureOutputChooser(FileChooser chooser) {
        chooser.setTitle(I18n.get("encrypt.chooserTitle"));
        chooser.getExtensionFilters().add(new FileChooser.ExtensionFilter(I18n.get("form.hgwFilter"), "*.hgw"));
    }

    /**
     * Botón "Cifrar archivo" (o Intro): comprueba que hay fichero y contraseña, pregunta si
     * hay que reemplazar el destino y cifra en segundo plano para no congelar la ventana.
     */
    @FXML
    private void onEncrypt() {
        // Antes de empezar, avisamos de lo que falte en lugar de fallar a mitad
        if (inputFile == null) {
            showError(I18n.get("error.missingFile"), I18n.get("encrypt.missingFile"));
            return;
        }
        if (passwordField.getText().isEmpty()) {
            showError(I18n.get("error.missingPassword"), I18n.get("encrypt.missingPassword"));
            return;
        }

        // Copias locales: el hilo de fondo no debe leer campos que el usuario podría cambiar mientras tanto
        Path input = inputFile;
        Path output = resolveOutputPath(outputFileName());
        if (output == null) {
            return; // Ya se ha mostrado la alerta
        }
        char[] password = takePassword(); // La contraseña sale del campo y este se vacía
        runInBackground(I18n.get("encrypt.running"), I18n.get("encrypt.success.title"),
                I18n.get("encrypt.success.text"), new Task<>() {
            @Override
            protected Path call() throws Exception {
                try {
                    // updateProgress mueve la barra de progreso según los bytes leídos
                    aesFileCipher.encrypt(input, output, password, this::updateProgress);
                    return output;
                } finally {
                    Arrays.fill(password, '\0'); // No dejamos la contraseña en memoria
                }
            }
        });
    }
}
