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
import el_guardian_ivan.desktop.logging.LogFormat;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

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
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public abstract class FileOperationController {

    /** Lo usan las pantallas Cifrar y Descifrar al crearse. */
    protected FileOperationController() {
    }

    /**
     * Registro de la pantalla: sale como EncryptController o DecryptController en el log.
     * Nunca se registran la contraseña, el contenido ni el nombre o la ruta de los ficheros.
     */
    protected final Logger log = LoggerFactory.getLogger(getClass());
    // ---- Paso 1: fichero de entrada ----

    /** Nombre del fichero elegido (o "Ningún fichero seleccionado"). */
    @FXML
    protected Label inputFileLabel;

    /** Tamaño del fichero elegido, por ejemplo "3,2 MB". */
    @FXML
    protected Label inputSizeLabel;

    /** Ruta completa del fichero elegido, debajo del recuadro. */
    @FXML
    protected Label inputPathLabel;

    /** Botón "Examinar...". */
    @FXML
    protected Button selectButton;

    // ---- Paso 2: contraseña ----

    /** Campo que oculta la contraseña con puntos. */
    @FXML
    protected PasswordField passwordField;

    /** Campo de texto normal que se muestra en lugar de passwordField al pulsar el ojo. */
    @FXML
    protected TextField visiblePasswordField;

    /** Botón del ojo: muestra u oculta la contraseña. */
    @FXML
    protected ToggleButton showPasswordToggle;

    // ---- Paso 3: fichero de salida ----

    /** Nombre del fichero que se va a crear. */
    @FXML
    protected TextField outputNameField;

    /** Botón "Cambiar ubicación...". */
    @FXML
    protected Button outputFolderButton;

    /** Ruta completa donde se guardará el resultado ("Se guardará en: ..."). */
    @FXML
    protected Label outputPathLabel;

    // ---- Parte inferior: progreso y botón principal ----

    /** Botón "Cifrar archivo" / "Descifrar archivo". */
    @FXML
    protected Button runButton;

    /** Avanza mientras se procesa el fichero. */
    @FXML
    protected ProgressBar progressBar;

    /** Mensaje bajo la barra: "Cifrando...", "✓ Archivo cifrado correctamente"... */
    @FXML
    protected Label statusLabel;

    /** Fichero elegido en el paso 1; {@code null} mientras no haya ninguno. */
    protected Path inputFile;

    /** Carpeta donde se guardará el resultado; por defecto, la del fichero de entrada. */
    protected Path outputDirectory;

    /** Hace el cifrado y descifrado de verdad (AES-256-GCM). */
    protected final AesFileCipher aesFileCipher = new AesFileCipher();

    /** FXMLLoader lo llama al cargar la pantalla: conecta los controles entre sí. */
    @FXML
    private void initialize() {
        // Los dos campos comparten el texto; el ojo decide cuál de ellos se ve
        visiblePasswordField.textProperty().bindBidirectional(passwordField.textProperty());
        visiblePasswordField.visibleProperty().bind(showPasswordToggle.selectedProperty());
        visiblePasswordField.managedProperty().bind(showPasswordToggle.selectedProperty());
        passwordField.visibleProperty().bind(showPasswordToggle.selectedProperty().not());
        passwordField.managedProperty().bind(showPasswordToggle.selectedProperty().not());
        // El texto que leen los lectores de pantalla cambia según la contraseña se vea o no
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
        // Al escribir otro nombre de salida se actualiza la ruta de "Se guardará en"
        outputNameField.textProperty().addListener((obs, oldName, newName) -> updateOutputPath());
    }

    /**
     * Nombre final del fichero de salida a partir de lo escrito en el paso 3.
     *
     * @return el nombre sin espacios al principio ni al final (vacío si no hay)
     */
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

    /**
     * Escribe la ruta con su prefijo ("Ruta: ...") y deja la ruta completa en el tooltip por si se recorta.
     *
     * @param label etiqueta donde se escribe
     * @param path ruta que se muestra; {@code null} vacía la etiqueta
     * @param key clave del texto con el prefijo, por ejemplo "form.inputPath"
     */
    private static void setPath(Label label, Path path, String key) {
        if (path == null) {
            label.setText("");
            label.setTooltip(null);
        } else {
            label.setText(I18n.get(key, path));
            label.setTooltip(new Tooltip(path.toString()));
        }
    }

    /**
     * Nombre que se propone para el fichero de salida a partir del de entrada.
     *
     * @param inputName nombre del fichero de entrada, por ejemplo "foto.png"
     * @return el nombre propuesto, por ejemplo "foto.hgw"
     */
    protected abstract String suggestOutputName(String inputName);

    /**
     * Permite a cada pantalla ajustar el diálogo de guardar (título, filtros...).
     *
     * @param chooser diálogo de guardar que se va a abrir
     */
    protected abstract void configureOutputChooser(FileChooser chooser);

    /**
     * Botón "Examinar..." (o Ctrl+O): abre el selector de archivos y, si el elegido se puede leer,
     * rellena el paso 1 y propone nombre y carpeta para el paso 3.
     */
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
            // Comprobaciones previas para dar un mensaje claro en lugar de fallar al cifrar
            if (!Files.isRegularFile(path)) {
                throw new IOException(I18n.get("error.notRegular"));
            }
            if (!Files.isReadable(path)) {
                throw new IOException(I18n.get("error.notReadable"));
            }
            long size = Files.size(path);
            log.debug("Fichero de entrada elegido: {}", LogFormat.describe(path, size));
            inputFile = path;
            inputFileLabel.setText(path.getFileName().toString());
            inputFileLabel.setTooltip(new Tooltip(path.toString())); // Ruta completa al pasar el ratón
            inputSizeLabel.setText(FileSizeFormatter.format(size, I18n.getLocale()));
            setPath(inputPathLabel, path.toAbsolutePath(), "form.inputPath");
            selectButton.setAccessibleHelp(inputPathLabel.getText());
            // Por defecto el resultado se guarda junto al fichero original
            outputDirectory = path.toAbsolutePath().getParent();
            outputNameField.setText(suggestOutputName(path.getFileName().toString()));
            updateOutputPath();
        } catch (IOException | InvalidPathException e) {
            log.warn("El fichero elegido no se puede usar: {}", e.toString());
            // No se puede usar: volvemos al estado "sin fichero" y explicamos el motivo
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
        // El diálogo se abre en la carpeta y con el nombre que ya hay puestos
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
     *
     * @param runningText texto mientras trabaja, por ejemplo "Cifrando..."
     * @param successTitle título de la ventana de éxito
     * @param successMessage mensaje de la ventana de éxito
     * @param task trabajo que se ejecuta; devuelve la ruta del fichero creado
     */
    protected void runInBackground(String runningText, String successTitle, String successMessage, Task<Path> task) {
        // La barra sigue el progreso de la tarea y los controles se bloquean mientras dura
        progressBar.progressProperty().bind(task.progressProperty());
        setStatus(runningText, null);
        setControlsDisabled(true);
        long start = System.nanoTime();
        log.info("Operación iniciada con {}", LogFormat.describe(inputFile, sizeOrUnknown(inputFile)));

        task.setOnSucceeded(event -> {
            Path result = task.getValue();
            log.info("Operación terminada en {} ms: {}", (System.nanoTime() - start) / 1_000_000,
                    LogFormat.describe(result, sizeOrUnknown(result)));
            finishTask();
            setStatus("✓ " + successTitle, "status-success");
            ResultDialog.success(getWindow(), successTitle, successMessage, task.getValue(), showsPreview());
        });
        task.setOnFailed(event -> {
            logFailure(task.getException(), (System.nanoTime() - start) / 1_000_000);
            finishTask();
            setStatus("✗ " + I18n.get("form.failed"), "status-error");
            showFailure(task.getException());
        });

        // Hilo propio para el trabajo pesado; la ventana sigue respondiendo
        Thread thread = new Thread(task, "cifrado-aes");
        thread.setDaemon(true); // Si se cierra la ventana, no deja la aplicación abierta
        thread.start();
    }

    /**
     * Si al terminar se enseña el resultado (imagen o texto) en la ventana de éxito.
     *
     * @return {@code true} para enseñar la vista previa; aquí {@code false}
     */
    protected boolean showsPreview() {
        return false;
    }

    /**
     * Texto bajo la barra de progreso; el símbolo ✓/✗ (incluido en Inter) evita que el resultado dependa solo del color.
     *
     * @param text texto que se muestra
     * @param styleClass "status-success", "status-error" o {@code null} para el estilo normal
     */
    private void setStatus(String text, String styleClass) {
        statusLabel.getStyleClass().removeAll("status-success", "status-error");
        if (styleClass != null) {
            statusLabel.getStyleClass().add(styleClass);
        }
        statusLabel.setText(text);
    }

    /** Deja la pantalla lista para otra operación: barra a cero y controles activos. */
    private void finishTask() {
        progressBar.progressProperty().unbind();
        progressBar.setProgress(0);
        setControlsDisabled(false);
    }

    /**
     * Registra por qué ha fallado la operación. Una contraseña incorrecta es algo normal y va sin traza;
     * los demás errores llevan la traza completa (Logback la enmascara antes de escribirla).
     *
     * @param error excepción con la que terminó la tarea
     * @param millis milisegundos que duró
     */
    private void logFailure(Throwable error, long millis) {
        if (error instanceof AEADBadTagException) {
            log.warn("Operación rechazada tras {} ms: contraseña incorrecta o fichero alterado", millis);
        } else if (error instanceof IllegalArgumentException) {
            log.warn("Operación no permitida: {}", error.getMessage());
        } else {
            log.error("La operación ha fallado tras {} ms", millis, error);
        }
    }

    /**
     * Tamaño del fichero para el registro, sin que un error al leerlo afecte a la operación.
     *
     * @param file fichero (puede ser {@code null})
     * @return el tamaño en bytes, o -1 si no se puede saber
     */
    private static long sizeOrUnknown(Path file) {
        try {
            return file == null ? -1 : Files.size(file);
        } catch (IOException | SecurityException e) {
            return -1;
        }
    }

    /**
     * Traduce el error de la tarea a un mensaje que el usuario entienda.
     *
     * @param error excepción con la que terminó la tarea
     */
    private void showFailure(Throwable error) {
        // AEADBadTagException: GCM no ha podido verificar los datos (contraseña mala o fichero alterado)
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
     *
     * @param name nombre del fichero de salida
     * @return la ruta completa, o {@code null} si no se puede usar o el usuario no quiere reemplazar
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

    /**
     * Copia la contraseña y la borra del campo: no se queda escrita en la ventana.
     *
     * @return la contraseña como array de caracteres (se puede borrar después, a diferencia de un String)
     */
    protected char[] takePassword() {
        char[] password = passwordField.getText().toCharArray();
        passwordField.clear();
        return password;
    }

    /**
     * Bloquea (o desbloquea) los controles para que no se cambie nada durante la operación.
     *
     * @param disabled {@code true} para bloquear, {@code false} para desbloquear
     */
    private void setControlsDisabled(boolean disabled) {
        selectButton.setDisable(disabled);
        runButton.setDisable(disabled);
        passwordField.setDisable(disabled);
        visiblePasswordField.setDisable(disabled);
        showPasswordToggle.setDisable(disabled);
        outputNameField.setDisable(disabled);
        outputFolderButton.setDisable(disabled);
    }

    /**
     * "hogwarts.png" -> "hogwarts", como indica la propuesta técnica (apartado 6.2).
     *
     * @param fileName nombre del fichero
     * @return el nombre sin la última extensión
     */
    protected static String withoutExtension(String fileName) {
        int dot = fileName.lastIndexOf('.');
        return dot > 0 ? fileName.substring(0, dot) : fileName;
    }

    /**
     * Ventana principal: los diálogos se abren encima de ella.
     *
     * @return la ventana que contiene esta pantalla
     */
    protected Window getWindow() {
        return inputFileLabel.getScene().getWindow();
    }

    /**
     * Muestra la ventana de error con el título y el mensaje dados.
     *
     * @param title título del error
     * @param message explicación para el usuario
     */
    protected void showError(String title, String message) {
        ResultDialog.error(getWindow(), title, message);
    }
}
