package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.crypto.AesFileCipher;
import el_guardian_ivan.desktop.files.FileNameValidator;
import el_guardian_ivan.desktop.files.FileSizeFormatter;
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
import java.nio.file.InvalidPathException;
import java.nio.file.Path;
import java.security.GeneralSecurityException;

/**
 * Parte común de las pantallas Cifrar y Descifrar: elegir el fichero de entrada,
 * leer la contraseña, ejecutar la operación en segundo plano y mostrar el resultado.
 * Los campos @FXML de esta clase los rellena el FXML de cada pantalla.
 */
public abstract class FileOperationController {
    @FXML
    protected Label inputFileLabel;

    @FXML
    protected PasswordField passwordField;

    @FXML
    protected Button selectButton;

    @FXML
    protected Button runButton;

    @FXML
    protected ProgressBar progressBar;

    @FXML
    protected Label statusLabel;

    protected Path inputFile;

    protected final AesFileCipher aesFileCipher = new AesFileCipher();

    @FXML
    protected void onSelectInputFile() {
        FileChooser chooser = new FileChooser();
        chooser.setTitle("Seleccionar fichero de entrada");
        File selected = chooser.showOpenDialog(getWindow());
        if (selected == null) {
            return; // El usuario ha cancelado el diálogo
        }

        try {
            Path path = selected.toPath();
            if (!Files.isRegularFile(path)) {
                throw new IOException("No es un fichero normal.");
            }
            if (!Files.isReadable(path)) {
                throw new IOException("No se puede leer el fichero.");
            }
            long size = Files.size(path);
            inputFile = path;
            inputFileLabel.setText(path + " (" + FileSizeFormatter.format(size) + ")");
        } catch (IOException | InvalidPathException e) {
            inputFile = null;
            inputFileLabel.setText("Ningún fichero seleccionado");
            showError("No se ha podido abrir el fichero", e.getMessage());
        }
    }

    /**
     * Ejecuta la operación en un hilo aparte para que la ventana no se congele con ficheros grandes.
     * Los métodos setOnSucceeded/setOnFailed se ejecutan de vuelta en el hilo de JavaFX.
     */
    protected void runInBackground(String runningText, String successHeader, Task<Path> task) {
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

    /**
     * Convierte el fichero elegido en una ruta y comprueba que su nombre sea válido en Windows y Linux.
     * Devuelve {@code null} (tras mostrar una alerta) si no lo es.
     */
    protected Path toValidOutputPath(File selected) {
        String problem = FileNameValidator.validate(selected.getName());
        if (problem != null) {
            showError("Nombre de fichero no válido", problem);
            return null;
        }
        try {
            return selected.toPath();
        } catch (InvalidPathException e) {
            showError("Ruta no válida", "No se puede usar la ruta elegida: " + e.getReason());
            return null;
        }
    }

    /** Copia la contraseña y la borra del campo: no se queda escrita en la ventana. */
    protected char[] takePassword() {
        char[] password = passwordField.getText().toCharArray();
        passwordField.clear();
        return password;
    }

    private void setControlsDisabled(boolean disabled) {
        selectButton.setDisable(disabled);
        runButton.setDisable(disabled);
        passwordField.setDisable(disabled);
    }

    /** "hogwarts.png" -> "hogwarts", como indica la propuesta técnica (apartado 6.2). */
    protected static String withoutExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
    }

    protected Window getWindow() {
        return inputFileLabel.getScene().getWindow();
    }

    protected void showInfo(String header, String content) {
        Alert alert = new Alert(Alert.AlertType.INFORMATION);
        alert.setTitle("Información");
        alert.setHeaderText(header);
        alert.setContentText(content);
        alert.showAndWait();
    }

    protected void showError(String header, String content) {
        Alert alert = new Alert(Alert.AlertType.ERROR);
        alert.setTitle("Error");
        alert.setHeaderText(header);
        alert.setContentText(content);
        alert.showAndWait();
    }
}
