package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
import el_guardian_ivan.desktop.files.FileLocationOpener;
import el_guardian_ivan.desktop.files.FileSizeFormatter;
import el_guardian_ivan.desktop.ui.ScalingPane;
import el_guardian_ivan.desktop.files.FilePreview;
import javafx.scene.AccessibleRole;
import javafx.scene.control.TextArea;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.scene.layout.StackPane;
import javafx.scene.layout.VBox;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Group;
import javafx.scene.Node;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.Tooltip;
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
        SUCCESS("dialog-success", "icon-check-circle", "dialog.success"),
        ERROR("dialog-error", "icon-error", "dialog.error"),
        CONFIRM("dialog-confirm", "icon-help", "dialog.confirm");

        private final String styleClass;
        private final String iconClass;
        private final String windowTitleKey;

        Kind(String styleClass, String iconClass, String windowTitleKey) {
            this.styleClass = styleClass;
            this.iconClass = iconClass;
            this.windowTitleKey = windowTitleKey;
        }
    }

    @FXML
    private Region icon;

    @FXML
    private Label titleLabel;

    @FXML
    private Label messageLabel;

    @FXML
    private VBox dialogBox;

    @FXML
    private VBox previewBox;

    @FXML
    private HBox fileBox;

    @FXML
    private Label fileNameLabel;

    @FXML
    private Label fileSizeLabel;

    @FXML
    private Label filePathLabel;

    @FXML
    private Button primaryButton;

    @FXML
    private Button closeButton;

    @FXML
    private Button cornerCloseButton;

    private static final int PREVIEW_WIDTH = 480;
    private static final int PREVIEW_HEIGHT = 220;

    private Stage stage;
    private Path file;
    private boolean confirmed;

    /** Operación terminada: muestra el fichero resultante y permite abrir su carpeta. */
    public static void success(Window owner, String title, String message, Path file) {
        success(owner, title, message, file, false);
    }

    /** Como {@link #success(Window, String, String, Path)}; con {@code preview} enseña la imagen o el texto. */
    public static void success(Window owner, String title, String message, Path file, boolean preview) {
        ResultDialog dialog = create(owner, Kind.SUCCESS, title, message);
        dialog.showFile(file);
        if (preview) {
            dialog.showPreview(file);
        }
        dialog.primaryButton.setText(I18n.get("dialog.openLocation"));
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
        dialog.closeButton.setText(I18n.get("dialog.cancel"));
        dialog.showAndWait();
        return dialog.confirmed;
    }

    private static ResultDialog create(Window owner, Kind kind, String title, String message) {
        FXMLLoader loader = new FXMLLoader(ResultDialog.class.getResource("/el_guardian_ivan/desktop/views/result-dialog.fxml"),
                I18n.bundle());
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
        // El título de la ventana es lo que anuncia el lector de pantalla al abrirse
        stage.setTitle(I18n.get(kind.windowTitleKey) + ": " + title);
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
        previewBox.setVisible(false);
        previewBox.setManaged(false);
        filePathLabel.managedProperty().bind(fileBox.managedProperty());
        filePathLabel.visibleProperty().bind(fileBox.visibleProperty());
        closeButton.setCancelButton(true); // Escape cierra el diálogo
        cornerCloseButton.setAccessibleText(I18n.get("dialog.close"));
    }

    @FXML
    private void onClose() {
        stage.close();
    }

    private void showFile(Path file) {
        this.file = file;
        fileNameLabel.setText(file.getFileName().toString());
        filePathLabel.setText(I18n.get("dialog.location", file.toAbsolutePath().getParent()));
        filePathLabel.setTooltip(new Tooltip(file.toAbsolutePath().toString()));
        try {
            fileSizeLabel.setText(FileSizeFormatter.format(Files.size(file), I18n.getLocale()));
        } catch (IOException e) {
            fileSizeLabel.setText("");
        }
        fileBox.setVisible(true);
        fileBox.setManaged(true);
    }

    /** Muestra la imagen o el texto descifrado; si no es ninguno de los dos (o falla), no enseña nada. */
    private void showPreview(Path file) {
        Node content;
        try {
            content = switch (FilePreview.kindOf(file)) {
                case IMAGE -> imagePreview(file);
                case TEXT -> textPreview(file);
                case NONE -> null;
            };
        } catch (IOException e) {
            content = null; // Sin vista previa: el archivo ya está guardado y se puede abrir desde su carpeta
        }
        if (content == null) {
            return;
        }
        previewBox.getChildren().add(0, content);
        previewBox.setVisible(true);
        previewBox.setManaged(true);
        dialogBox.setPrefWidth(PREVIEW_WIDTH + 80); // Ventana más ancha para que la vista previa luzca
    }

    private Node imagePreview(Path file) {
        // Se decodifica ya reducida y en segundo plano: una foto enorme no bloquea ni llena la memoria
        Image image = new Image(file.toUri().toString(), PREVIEW_WIDTH * 2, PREVIEW_HEIGHT * 2, true, true, true);
        ImageView view = new ImageView(image);
        view.setFitWidth(PREVIEW_WIDTH);
        view.setFitHeight(PREVIEW_HEIGHT);
        view.setPreserveRatio(true);
        view.setSmooth(true);
        view.setAccessibleRole(AccessibleRole.IMAGE_VIEW);
        view.setAccessibleText(I18n.get("dialog.preview.image", file.getFileName()));
        image.errorProperty().addListener((obs, hadError, error) -> {
            if (error) { // JavaFX no sabe dibujarla (formato raro o dañada): quitamos el hueco
                previewBox.getChildren().remove(view.getParent());
                previewBox.setVisible(false);
                previewBox.setManaged(false);
                stage.sizeToScene();
            }
        });
        StackPane frame = new StackPane(view);
        frame.getStyleClass().add("preview-image");
        frame.setMinHeight(PREVIEW_HEIGHT);
        return frame;
    }

    private Node textPreview(Path file) throws IOException {
        FilePreview.Text text = FilePreview.readText(file, FilePreview.MAX_TEXT_BYTES);
        TextArea area = new TextArea(text.content());
        area.setEditable(false);
        area.setWrapText(true);
        area.setPrefSize(PREVIEW_WIDTH, PREVIEW_HEIGHT);
        area.getStyleClass().add("preview-text");
        area.setAccessibleText(I18n.get("dialog.preview.text", file.getFileName()));
        if (!text.truncated()) {
            return area;
        }
        Label note = new Label(I18n.get("dialog.preview.truncated",
                FileSizeFormatter.format(FilePreview.MAX_TEXT_BYTES, I18n.getLocale())));
        note.getStyleClass().add("path-label");
        note.setWrapText(true);
        return new VBox(4, area, note);
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
            messageLabel.setText(I18n.get("dialog.openLocationFailed", e.getMessage()));
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
