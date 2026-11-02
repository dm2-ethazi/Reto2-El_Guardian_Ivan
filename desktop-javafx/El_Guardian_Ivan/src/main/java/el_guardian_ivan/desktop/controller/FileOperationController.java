package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
import el_guardian_ivan.desktop.crypto.AesFileCipher;
import el_guardian_ivan.desktop.files.FileNameValidator;
import el_guardian_ivan.desktop.files.FileSizeFormatter;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.scene.control.Button;
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
    protected Label inputPathLabel;

    @FXML
    protected Label outputPathLabel;

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
            String text = I18n.get(shown ? "form.hidePassword" : "form.showPassword");
            showPasswordToggle.setAccessibleText(text);
            showPasswordToggle.setTooltip(new Tooltip(text));
        });
        showPasswordToggle.setAccessibleText(I18n.get("form.showPassword"));
        showPasswordToggle.setTooltip(new Tooltip(I18n.get("form.showPassword")));
        outputFolderButton.setAccessibleText(I18n.get("form.chooseOutput"));
        outputFolderButton.setTooltip(new Tooltip(I18n.get("form.chooseOutput")));
        selectButton.setTooltip(new Tooltip(I18n.get("form.chooseInput") + " (Ctrl+O)"));
        // Los lectores de pantalla leen como ayuda del botón la ruta que se muestra debajo
        outputFolderButton.accessibleHelpProperty().bind(outputPathLabel.textProperty());

        // Las rutas solo se ven cuando hay algo que mostrar
        for (Label pathLabel : new Label[]{inputPathLabel, outputPathLabel}) {
            pathLabel.managedProperty().bind(pathLabel.textProperty().isNotEmpty());
            pathLabel.visibleProperty().bind(pathLabel.textProperty().isNotEmpty());
        }
        outputNameField.textProperty().addListener((obs, oldName, newName) -> updateOutputPath());
    }

    /** Nombre final del fichero de salida a partir de lo escrito en el paso 3. */
    protected String outputFileName() {
        return outputNameField.getText().trim();
    }

    /** Muestra bajo el paso 3 la ruta completa donde se guardará el resultado. */
    private void updateOutputPath() {
        String name = outputFileName();
        if (outputDirectory == null || name.isEmpty()) {
            setPath(outputPathLabel, null, "form.outputPath");
            return;
        }
        try {
            setPath(outputPathLabel, outputDirectory.resolve(name), "form.outputPath");
        } catch (InvalidPathException e) {
            setPath(outputPathLabel, outputDirectory, "form.outputPath"); // El nombre no es válido: solo la carpeta
        }
    }

    /** Escribe la ruta con su prefijo ("Ruta: ...") y deja la ruta completa en el tooltip por si se recorta. */
    private static void setPath(Label label, Path path, String key) {
        if (path == null) {
            label.setText("");
            label.setTooltip(null);
        } else {
            label.setText(I18n.get(key, path));
            label.setTooltip(new Tooltip(path.toString()));
        }
    }

    /** Nombre que se propone para el fichero de salida a partir del de entrada. */
    protected abstract String suggestOutputName(String inputName);

    /** Permite a cada pantalla ajustar el diálogo de guardar (título, filtros...). */
    protected abstract void configureOutputChooser(FileChooser chooser);

    @FXML
    protected void onSelectInputFile() {
        FileChooser chooser = new FileChooser();
        chooser.setTitle(I18n.get("form.chooseInput"));
        File selected = chooser.showOpenDialog(getWindow());
        if (selected == null) {
            return; // El usuario ha cancelado el diálogo
        }

        try {
            Path path = selected.toPath();
            if (!Files.isRegularFile(path)) {
                throw new IOException(I18n.get("error.notRegular"));
            }
            if (!Files.isReadable(path)) {
                throw new IOException(I18n.get("error.notReadable"));
            }
            long size = Files.size(path);
            inputFile = path;
            inputFileLabel.setText(path.getFileName().toString());
            inputFileLabel.setTooltip(new Tooltip(path.toString())); // Ruta completa al pasar el ratón
            inputSizeLabel.setText(FileSizeFormatter.format(size, I18n.getLocale()));
            setPath(inputPathLabel, path.toAbsolutePath(), "form.inputPath");
            selectButton.setAccessibleHelp(inputPathLabel.getText());
            outputDirectory = path.toAbsolutePath().getParent();
            outputNameField.setText(suggestOutputName(path.getFileName().toString()));
            updateOutputPath();
        } catch (IOException | InvalidPathException e) {
            inputFile = null;
            inputFileLabel.setText(I18n.get("form.noFile"));
            inputFileLabel.setTooltip(null);
            inputSizeLabel.setText("");
            setPath(inputPathLabel, null, "form.inputPath");
            showError(I18n.get("error.openFile"), e.getMessage());
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
        updateOutputPath(); // Por si el nombre no cambia pero sí la carpeta
    }

    /**
     * Ejecuta la operación en un hilo aparte para que la ventana no se congele con ficheros grandes.
     * Los métodos setOnSucceeded/setOnFailed se ejecutan de vuelta en el hilo de JavaFX.
     */
    protected void runInBackground(String runningText, String successTitle, String successMessage, Task<Path> task) {
        progressBar.progressProperty().bind(task.progressProperty());
        setStatus(runningText, null);
        setControlsDisabled(true);

        task.setOnSucceeded(event -> {
            finishTask();
            setStatus("✓ " + successTitle, "status-success");
            ResultDialog.success(getWindow(), successTitle, successMessage, task.getValue(), showsPreview());
        });
        task.setOnFailed(event -> {
            finishTask();
            setStatus("✗ " + I18n.get("form.failed"), "status-error");
            showFailure(task.getException());
        });

        Thread thread = new Thread(task, "cifrado-aes");
        thread.setDaemon(true); // Si se cierra la ventana, no deja la aplicación abierta
        thread.start();
    }

    /** Texto bajo la barra de progreso; el símbolo ✓/✗ (incluido en Inter) evita que el resultado dependa solo del color. */
    /** Si al terminar se enseña el resultado (imagen o texto) en la ventana de éxito. */
    protected boolean showsPreview() {
        return false;
    }

    private void setStatus(String text, String styleClass) {
        statusLabel.getStyleClass().removeAll("status-success", "status-error");
        if (styleClass != null) {
            statusLabel.getStyleClass().add(styleClass);
        }
        statusLabel.setText(text);
    }

    private void finishTask() {
        progressBar.progressProperty().unbind();
        progressBar.setProgress(0);
        setControlsDisabled(false);
    }

    private void showFailure(Throwable error) {
        if (error instanceof AEADBadTagException) {
            showError(I18n.get("error.wrongPassword.title"), I18n.get("error.wrongPassword.text"));
        } else if (error instanceof IllegalArgumentException) {
            showError(I18n.get("error.notAllowed"), error.getMessage());
        } else if (error instanceof IOException) {
            showError(I18n.get("error.io"), error.getMessage());
        } else if (error instanceof GeneralSecurityException) {
            showError(I18n.get("error.crypto"), error.getMessage());
        } else {
            showError(I18n.get("error.unexpected"), String.valueOf(error));
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
            showError(I18n.get("error.invalidName"), problem);
            return null;
        }
        Path output;
        try {
            output = outputDirectory.resolve(name);
        } catch (InvalidPathException e) {
            showError(I18n.get("error.invalidPath"), I18n.get("error.invalidPath.text", e.getReason()));
            return null;
        }
        if (Files.exists(output) && !ResultDialog.confirm(getWindow(), I18n.get("confirm.exists.title"),
                I18n.get("confirm.exists.text", output.getFileName()), I18n.get("confirm.replace"))) {
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

    protected void showError(String title, String message) {
        ResultDialog.error(getWindow(), title, message);
    }
}
