package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.files.FileLocationOpener;
import el_guardian_ivan.desktop.files.FileSizeFormatter;
import el_guardian_ivan.desktop.ui.ScalingPane;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Group;
import javafx.scene.Node;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.effect.ColorAdjust;
import javafx.scene.layout.HBox;
import javafx.scene.layout.Region;
import javafx.scene.paint.Color;
import javafx.scene.transform.Scale;
import javafx.stage.Modality;
import javafx.stage.Stage;
import javafx.stage.StageStyle;
import javafx.stage.Window;

import java.io.IOException;
import java.io.UncheckedIOException;
import java.nio.file.Files;
import java.nio.file.Path;

/**
 * Diálogo con el estilo de la aplicación (boceto de la propuesta) para sustituir a las alertas
 * estándar de JavaFX. Cada tipo lleva su icono y su color, así el resultado no depende solo del color.
 */
public class ResultDialog {

    private enum Kind {
        SUCCESS("dialog-success", "icon-check-circle", "Operación completada"),
        ERROR("dialog-error", "icon-error", "Error"),
        CONFIRM("dialog-confirm", "icon-help", "Confirmar");

        private final String styleClass;
        private final String iconClass;
        private final String windowTitle;

        Kind(String styleClass, String iconClass, String windowTitle) {
            this.styleClass = styleClass;
            this.iconClass = iconClass;
            this.windowTitle = windowTitle;
        }
    }

    @FXML
    private Region icon;

    @FXML
    private Label titleLabel;

    @FXML
    private Label messageLabel;

    @FXML
    private HBox fileBox;

    @FXML
    private Label fileNameLabel;

    @FXML
    private Label fileSizeLabel;

    @FXML
    private Button primaryButton;

    @FXML
    private Button closeButton;

    @FXML
    private Button cornerCloseButton;

    private Stage stage;
    private Path file;
    private boolean confirmed;

    /** Operación terminada: muestra el fichero resultante y permite abrir su carpeta. */
    public static void success(Window owner, String title, String message, Path file) {
        ResultDialog dialog = create(owner, Kind.SUCCESS, title, message);
        dialog.showFile(file);
        dialog.primaryButton.setText("Abrir ubicación");
        dialog.primaryButton.setOnAction(event -> dialog.openLocation());
        dialog.showAndWait();
    }

    public static void error(Window owner, String title, String message) {
        ResultDialog dialog = create(owner, Kind.ERROR, title, message);
        dialog.hidePrimaryButton();
        dialog.showAndWait();
    }

    /** Pregunta antes de una acción; devuelve {@code true} solo si se pulsa el botón de confirmar. */
    public static boolean confirm(Window owner, String title, String message, String confirmText) {
        ResultDialog dialog = create(owner, Kind.CONFIRM, title, message);
        dialog.primaryButton.setText(confirmText);
        dialog.primaryButton.setGraphic(null); // El icono de carpeta solo tiene sentido en "Abrir ubicación"
        dialog.primaryButton.setOnAction(event -> {
            dialog.confirmed = true;
            dialog.stage.close();
        });
        dialog.closeButton.setText("Cancelar");
        dialog.showAndWait();
        return dialog.confirmed;
    }

    private static ResultDialog create(Window owner, Kind kind, String title, String message) {
        FXMLLoader loader = new FXMLLoader(ResultDialog.class.getResource("/el_guardian_ivan/desktop/views/result-dialog.fxml"));
        Parent root;
        try {
            root = loader.load();
        } catch (IOException e) {
            throw new UncheckedIOException(e);
        }
        ResultDialog dialog = loader.getController();
        root.getStyleClass().add(kind.styleClass);
        dialog.icon.getStyleClass().add(kind.iconClass);
        dialog.titleLabel.setText(title);
        dialog.messageLabel.setText(message);

        Stage stage = new Stage(StageStyle.TRANSPARENT); // Sin la barra de Windows, como en el boceto
        stage.initOwner(owner);
        stage.initModality(Modality.WINDOW_MODAL);
        stage.setTitle(kind.windowTitle);
        // Mismo tamaño relativo que la ventana principal; el Group hace que la escena mida el contenido ya escalado
        if (owner != null && owner.getScene() != null && owner.getScene().getRoot() instanceof ScalingPane scaling) {
            root.getTransforms().add(new Scale(scaling.getScale(), scaling.getScale()));
        }
        Scene scene = new Scene(new Group(root));
        scene.setFill(Color.TRANSPARENT);
        if (owner != null && owner.getScene() != null) {
            scene.getStylesheets().setAll(owner.getScene().getStylesheets());
        }
        stage.setScene(scene);
        dialog.stage = stage;
        return dialog;
    }

    @FXML
    private void initialize() {
        fileBox.setVisible(false);
        fileBox.setManaged(false);
        closeButton.setCancelButton(true); // Escape cierra el diálogo
        cornerCloseButton.setAccessibleText("Cerrar");
    }

    @FXML
    private void onClose() {
        stage.close();
    }

    private void showFile(Path file) {
        this.file = file;
        fileNameLabel.setText(file.getFileName().toString());
        try {
            fileSizeLabel.setText(FileSizeFormatter.format(Files.size(file)));
        } catch (IOException e) {
            fileSizeLabel.setText("");
        }
        fileBox.setVisible(true);
        fileBox.setManaged(true);
    }

    private void hidePrimaryButton() {
        primaryButton.setVisible(false);
        primaryButton.setManaged(false);
        closeButton.setDefaultButton(true);
    }

    private void openLocation() {
        try {
            FileLocationOpener.reveal(file);
            stage.close();
        } catch (IOException e) {
            messageLabel.setText("No se ha podido abrir la carpeta: " + e.getMessage());
        }
    }

    /** Oscurece la ventana principal mientras el diálogo está abierto. */
    private void showAndWait() {
        Node ownerRoot = stage.getOwner() == null ? null : stage.getOwner().getScene().getRoot();
        if (ownerRoot != null) {
            ownerRoot.setEffect(new ColorAdjust(0, 0, -0.35, 0));
        }
        Node focusTarget = primaryButton.isManaged() ? primaryButton : closeButton;
        stage.setOnShown(event -> focusTarget.requestFocus());
        try {
            stage.showAndWait();
        } finally {
            if (ownerRoot != null) {
                ownerRoot.setEffect(null);
            }
        }
    }
}
