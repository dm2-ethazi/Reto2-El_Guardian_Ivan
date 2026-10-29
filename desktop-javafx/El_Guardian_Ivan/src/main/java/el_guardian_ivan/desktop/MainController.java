package el_guardian_ivan.desktop;

import el_guardian_ivan.desktop.crypto.AesFileCipher;
import el_guardian_ivan.desktop.files.FileTypeDetector;
import javafx.fxml.FXML;
import javafx.scene.control.Alert;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
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
            inputFileLabel.setText(path + " (" + size + " bytes)");
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

        Path output = selected.toPath();
        char[] password = passwordField.getText().toCharArray();
        try {
            aesFileCipher.encrypt(inputFile, output, password);
            showInfo("Fichero cifrado", "Se ha guardado en:\n" + output);
        } catch (IllegalArgumentException e) {
            showError("No se puede cifrar", e.getMessage());
        } catch (IOException e) {
            showError("Error de lectura o escritura", e.getMessage());
        } catch (GeneralSecurityException e) {
            showError("Error de cifrado", e.getMessage());
        } finally {
            Arrays.fill(password, '\0'); // No dejamos la contraseña en memoria
        }
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

        Path output = selected.toPath();
        char[] password = passwordField.getText().toCharArray();
        try {
            String originalExtension = aesFileCipher.decrypt(inputFile, output, password);
            output = FileTypeDetector.addExtensionIfMissing(output, originalExtension);
            showInfo("Fichero descifrado", "Se ha guardado en:\n" + output);
        } catch (AEADBadTagException e) {
            showError("No se ha podido descifrar", "La contraseña es incorrecta o el fichero está dañado.");
        } catch (IllegalArgumentException e) {
            showError("No se puede descifrar", e.getMessage());
        } catch (IOException e) {
            showError("Error de lectura o escritura", e.getMessage());
        } catch (GeneralSecurityException e) {
            showError("Error de descifrado", e.getMessage());
        } finally {
            Arrays.fill(password, '\0'); // No dejamos la contraseña en memoria
        }
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
