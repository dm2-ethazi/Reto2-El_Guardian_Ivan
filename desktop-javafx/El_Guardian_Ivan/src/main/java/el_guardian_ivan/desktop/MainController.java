package el_guardian_ivan.desktop;

import el_guardian_ivan.desktop.crypto.AesFileCipher;
import el_guardian_ivan.desktop.files.FileSizeFormatter;
import el_guardian_ivan.desktop.files.FileTypeDetector;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.scene.control.Alert;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.ProgressBar;
import javafx.stage.FileChooser;
import javafx.stage.Window;

import javax.crypto.AEADBadTagException;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.GeneralSecurityException;
import java.util.Arrays;

public class MainController {
    @FXML
    private Label inputFileLabel;

    @FXML
    private PasswordField passwordField;

    @FXML
    private Button selectButton;

    @FXML
    private Button encryptButton;

    @FXML
    private Button decryptButton;

    @FXML
    private ProgressBar progressBar;

    @FXML
    private Label statusLabel;

    private Path inputFile;

    private final AesFileCipher aesFileCipher = new AesFileCipher();

    @FXML
    protected void onSelectInputFile() {
        FileChooser chooser = new FileChooser();
        chooser.setTitle("Seleccionar fichero de entrada");
        File selected = chooser.showOpenDialog(getWindow());
        if (selected == null) {
            return; // El usuario ha cancelado el diálogo
        }

        Path path = selected.toPath();
        try {
            if (!Files.isReadable(path)) {
                throw new IOException("No se puede leer el fichero.");
            }
            long size = Files.size(path);
            inputFile = path;
            inputFileLabel.setText(path + " (" + FileSizeFormatter.format(size) + ")");
        } catch (IOException e) {
            inputFile = null;
            inputFileLabel.setText("Ningún fichero seleccionado");
            showError("No se ha podido abrir el fichero", e.getMessage());
        }
    }

    @FXML
    protected void onEncrypt() {
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
        Path output = selected.toPath();
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

    @FXML
    protected void onDecrypt() {
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
        Path output = selected.toPath();
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

    /**
     * Ejecuta la operación en un hilo aparte para que la ventana no se congele con ficheros grandes.
     * Los métodos setOnSucceeded/setOnFailed se ejecutan de vuelta en el hilo de JavaFX.
     */
    private void runInBackground(String runningText, String successHeader, Task<Path> task) {
        progressBar.progressProperty().bind(task.progressProperty());
        statusLabel.setText(runningText);
        setControlsDisabled(true);

        task.setOnSucceeded(event -> {
            finishTask();
            statusLabel.setText(successHeader);
            showInfo(successHeader, "Se ha guardado en:\n" + task.getValue());
        });
        task.setOnFailed(event -> {
            finishTask();
            statusLabel.setText("La operación ha fallado");
            showFailure(task.getException());
        });

        Thread thread = new Thread(task, "cifrado-aes");
        thread.setDaemon(true); // Si se cierra la ventana, no deja la aplicación abierta
        thread.start();
    }

    private void finishTask() {
        progressBar.progressProperty().unbind();
        progressBar.setProgress(0);
        setControlsDisabled(false);
    }

    private void showFailure(Throwable error) {
        if (error instanceof AEADBadTagException) {
            showError("No se ha podido descifrar", "La contraseña es incorrecta o el fichero está dañado.");
        } else if (error instanceof IllegalArgumentException) {
            showError("Operación no permitida", error.getMessage());
        } else if (error instanceof IOException) {
            showError("Error de lectura o escritura", error.getMessage());
        } else if (error instanceof GeneralSecurityException) {
            showError("Error de cifrado", error.getMessage());
        } else {
            showError("Error inesperado", String.valueOf(error));
        }
    }

    /** Copia la contraseña y la borra del campo: no se queda escrita en la ventana. */
    private char[] takePassword() {
        char[] password = passwordField.getText().toCharArray();
        passwordField.clear();
        return password;
    }

    private void setControlsDisabled(boolean disabled) {
        selectButton.setDisable(disabled);
        encryptButton.setDisable(disabled);
        decryptButton.setDisable(disabled);
        passwordField.setDisable(disabled);
    }

    /** "hogwarts.png" -> "hogwarts", como indica la propuesta técnica (apartado 6.2). */
    private static String withoutExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
    }

    private Window getWindow() {
        return inputFileLabel.getScene().getWindow();
    }

    private void showInfo(String header, String content) {
        Alert alert = new Alert(Alert.AlertType.INFORMATION);
        alert.setTitle("Información");
        alert.setHeaderText(header);
        alert.setContentText(content);
        alert.showAndWait();
    }

    private void showError(String header, String content) {
        Alert alert = new Alert(Alert.AlertType.ERROR);
        alert.setTitle("Error");
        alert.setHeaderText(header);
        alert.setContentText(content);
        alert.showAndWait();
    }
}
