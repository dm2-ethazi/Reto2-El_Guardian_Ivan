package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.crypto.AesFileCipher;
import el_guardian_ivan.desktop.files.FileNameValidator;
import el_guardian_ivan.desktop.files.FileSizeFormatter;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.scene.control.Alert;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.ProgressBar;
import javafx.scene.control.TextField;
import javafx.scene.control.ToggleButton;
import javafx.scene.control.Tooltip;
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
 * Parte común de las pantallas Cifrar y Descifrar, organizadas en tres pasos:
 * 1. elegir el fichero de entrada, 2. escribir la contraseña y 3. indicar el fichero de salida.
 * Después ejecuta la operación en segundo plano y muestra el resultado.
 * Los campos @FXML de esta clase los rellena el FXML de cada pantalla.
 */
public abstract class FileOperationController {
    @FXML
    protected Label inputFileLabel;

    @FXML
    protected Label inputSizeLabel;

    @FXML
    protected PasswordField passwordField;

    /** Campo de texto normal que se muestra en lugar de passwordField al pulsar el ojo. */
    @FXML
    protected TextField visiblePasswordField;

    @FXML
    protected ToggleButton showPasswordToggle;

    @FXML
    protected TextField outputNameField;

    @FXML
    protected Button outputFolderButton;

    @FXML
    protected Button selectButton;

    @FXML
    protected Button runButton;

    @FXML
    protected ProgressBar progressBar;

    @FXML
    protected Label statusLabel;

    protected Path inputFile;

    /** Carpeta donde se guardará el resultado; por defecto, la del fichero de entrada. */
    protected Path outputDirectory;

    protected final AesFileCipher aesFileCipher = new AesFileCipher();

    @FXML
    private void initialize() {
        // Los dos campos comparten el texto; el ojo decide cuál de ellos se ve
        visiblePasswordField.textProperty().bindBidirectional(passwordField.textProperty());
        visiblePasswordField.visibleProperty().bind(showPasswordToggle.selectedProperty());
        visiblePasswordField.managedProperty().bind(showPasswordToggle.selectedProperty());
        passwordField.visibleProperty().bind(showPasswordToggle.selectedProperty().not());
        passwordField.managedProperty().bind(showPasswordToggle.selectedProperty().not());
        showPasswordToggle.selectedProperty().addListener((obs, wasShown, shown) -> {
            String text = shown ? "Ocultar contraseña" : "Mostrar contraseña";
            showPasswordToggle.setAccessibleText(text);
            showPasswordToggle.setTooltip(new Tooltip(text));
        });
        showPasswordToggle.setAccessibleText("Mostrar contraseña");
        showPasswordToggle.setTooltip(new Tooltip("Mostrar contraseña"));
        outputFolderButton.setAccessibleText("Elegir dónde guardar");
        outputFolderButton.setTooltip(new Tooltip("Elegir dónde guardar"));
    }

    /** Nombre que se propone para el fichero de salida a partir del de entrada. */
    protected abstract String suggestOutputName(String inputName);

    /** Permite a cada pantalla ajustar el diálogo de guardar (título, filtros...). */
    protected abstract void configureOutputChooser(FileChooser chooser);

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
            inputFileLabel.setText(path.getFileName().toString());
            inputFileLabel.setTooltip(new Tooltip(path.toString())); // Ruta completa al pasar el ratón
            inputSizeLabel.setText(FileSizeFormatter.format(size));
            outputDirectory = path.toAbsolutePath().getParent();
            outputNameField.setText(suggestOutputName(path.getFileName().toString()));
        } catch (IOException | InvalidPathException e) {
            inputFile = null;
            inputFileLabel.setText("Ningún fichero seleccionado");
            inputFileLabel.setTooltip(null);
            inputSizeLabel.setText("");
            showError("No se ha podido abrir el fichero", e.getMessage());
        }
    }

    /** Botón de la carpeta del paso 3: elige dónde guardar y con qué nombre. */
    @FXML
    protected void onChooseOutput() {
        FileChooser chooser = new FileChooser();
        configureOutputChooser(chooser);
        if (outputDirectory != null && Files.isDirectory(outputDirectory)) {
            chooser.setInitialDirectory(outputDirectory.toFile());
        }
        chooser.setInitialFileName(outputNameField.getText().trim());
        File selected = chooser.showSaveDialog(getWindow());
        if (selected == null) {
            return; // El usuario ha cancelado el diálogo
        }
        outputDirectory = selected.toPath().toAbsolutePath().getParent();
        outputNameField.setText(selected.getName());
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
     * Construye la ruta de salida con la carpeta elegida y el nombre del paso 3, comprobando que el
     * nombre sea válido en Windows y Linux y pidiendo confirmación si el fichero ya existe.
     * Devuelve {@code null} (tras mostrar una alerta) si no se puede usar.
     */
    protected Path resolveOutputPath(String name) {
        String problem = FileNameValidator.validate(name);
        if (problem != null) {
            showError("Nombre de fichero no válido", problem);
            return null;
        }
        Path output;
        try {
            output = outputDirectory.resolve(name);
        } catch (InvalidPathException e) {
            showError("Ruta no válida", "No se puede usar la ruta elegida: " + e.getReason());
            return null;
        }
        if (Files.exists(output) && !confirm("El fichero ya existe",
                "¿Quieres reemplazar " + output.getFileName() + "?")) {
            return null;
        }
        return output;
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
        visiblePasswordField.setDisable(disabled);
        showPasswordToggle.setDisable(disabled);
        outputNameField.setDisable(disabled);
        outputFolderButton.setDisable(disabled);
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

    protected boolean confirm(String header, String content) {
        Alert alert = new Alert(Alert.AlertType.CONFIRMATION);
        alert.setTitle("Confirmar");
        alert.setHeaderText(header);
        alert.setContentText(content);
        return alert.showAndWait().filter(ButtonType.OK::equals).isPresent();
    }

    protected void showError(String header, String content) {
        Alert alert = new Alert(Alert.AlertType.ERROR);
        alert.setTitle("Error");
        alert.setHeaderText(header);
        alert.setContentText(content);
        alert.showAndWait();
    }
}
