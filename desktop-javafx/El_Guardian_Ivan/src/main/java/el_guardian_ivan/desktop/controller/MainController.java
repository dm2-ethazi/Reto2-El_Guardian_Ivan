package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
import el_guardian_ivan.desktop.navigation.NavigationAware;
import el_guardian_ivan.desktop.navigation.Navigator;
import el_guardian_ivan.desktop.navigation.View;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.geometry.Pos;
import javafx.scene.Parent;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.ScrollPane;
import javafx.scene.control.Tooltip;
import javafx.scene.input.KeyCode;
import javafx.scene.input.KeyCodeCombination;
import javafx.scene.input.KeyCombination;
import javafx.scene.control.ToggleButton;
import javafx.scene.control.ToggleGroup;
import javafx.scene.layout.StackPane;

import java.io.IOException;
import java.io.UncheckedIOException;
import java.util.EnumMap;
import java.util.Map;

/**
 * Ventana principal: menú lateral a la izquierda y la pantalla elegida en el centro.
 * Cada pantalla se carga de su FXML la primera vez y se guarda, así no pierde lo que el usuario
 * haya escrito al cambiar de sección.
 */
public class MainController implements Navigator {
    /** Zona central donde se coloca la pantalla elegida. */
    @FXML
    private StackPane contentArea;

    /** Agrupa los botones del menú para que solo uno pueda estar marcado. */

    @FXML
    private ToggleGroup navigationGroup;

    // Botones del menú lateral, uno por pantalla
    @FXML
    private ToggleButton homeButton;

    @FXML
    private ToggleButton encryptButton;

    @FXML
    private ToggleButton decryptButton;

    @FXML
    private ToggleButton settingsButton;

    @FXML
    private ToggleButton helpButton;

    /** Pantallas ya cargadas: se reutilizan para conservar lo escrito al volver a ellas. */
    private final Map<View, Parent> loadedViews = new EnumMap<>(View.class);

    /** FXMLLoader lo llama al cargar el menú: enlaza cada botón con su pantalla y abre Inicio. */
    @FXML
    private void initialize() {
        // Cada botón guarda la pantalla que abre
        homeButton.setUserData(View.HOME);
        encryptButton.setUserData(View.ENCRYPT);
        decryptButton.setUserData(View.DECRYPT);
        settingsButton.setUserData(View.SETTINGS);
        helpButton.setUserData(View.HELP);

        // Al marcar otro botón del menú se muestra su pantalla
        navigationGroup.selectedToggleProperty().addListener((obs, oldToggle, newToggle) -> {
            if (newToggle == null) {
                oldToggle.setSelected(true); // Siempre tiene que haber una sección marcada
            } else {
                show((View) newToggle.getUserData());
            }
        });
        homeButton.setSelected(true);

        // Atajos de teclado: se registran en la escena en cuanto la ventana existe
        contentArea.sceneProperty().addListener((obs, oldScene, scene) -> {
            if (scene != null) {
                registerShortcuts(scene.getAccelerators());
            }
        });
    }

    /** Ctrl+1..4 cambia de sección y Ctrl+O abre el selector de archivo de la pantalla actual. */
    private void registerShortcuts(Map<KeyCombination, Runnable> accelerators) {
        ToggleButton[] sections = {homeButton, encryptButton, decryptButton, settingsButton};
        KeyCode[] digits = {KeyCode.DIGIT1, KeyCode.DIGIT2, KeyCode.DIGIT3, KeyCode.DIGIT4};
        for (int i = 0; i < sections.length; i++) {
            ToggleButton section = sections[i];
            KeyCombination shortcut = new KeyCodeCombination(digits[i], KeyCombination.CONTROL_DOWN);
            // put() sustituye el atajo anterior si la ventana se reconstruye al cambiar de idioma
            accelerators.put(shortcut, () -> {
                section.setSelected(true);
                section.requestFocus();
            });
            section.setTooltip(new Tooltip(section.getText() + " (" + shortcut.getDisplayText() + ")"));
        }
        // F1 abre la ayuda, como en la mayoría de programas de escritorio
        KeyCombination help = new KeyCodeCombination(KeyCode.F1);
        accelerators.put(help, () -> {
            helpButton.setSelected(true);
            helpButton.requestFocus();
        });
        helpButton.setTooltip(new Tooltip(helpButton.getText() + " (" + help.getDisplayText() + ")"));
        accelerators.put(new KeyCodeCombination(KeyCode.O, KeyCombination.CONTROL_DOWN), () -> {
            Node selectButton = contentArea.getChildren().isEmpty() ? null
                    : contentArea.getChildren().get(0).lookup("#selectButton");
            if (selectButton instanceof Button button && !button.isDisabled()) {
                button.requestFocus();
                button.fire();
            }
        });
    }

    @Override
    public void navigateTo(View view) {
        navigationGroup.getToggles().stream()
                .filter(toggle -> toggle.getUserData() == view)
                .findFirst()
                .ifPresent(toggle -> toggle.setSelected(true)); // El listener muestra la pantalla
    }

    private void show(View view) {
        contentArea.getChildren().setAll(loadedViews.computeIfAbsent(view, this::load));
    }

    private Parent load(View view) {
        FXMLLoader loader = new FXMLLoader(MainController.class.getResource(view.fxml()), I18n.bundle());
        try {
            Parent root = loader.load();
            if (loader.getController() instanceof NavigationAware aware) {
                aware.setNavigator(this);
            }
            return scrollable(root);
        } catch (IOException e) {
            // Un FXML que no carga es un error de programación, no del usuario
            throw new UncheckedIOException("No se ha podido cargar " + view.fxml(), e);
        }
    }

    /**
     * Si la ventana es más pequeña que la pantalla, aparece una barra de desplazamiento en lugar
     * de cortar el contenido. El StackPane centra las pantallas que tienen un ancho máximo.
     */
    private static Parent scrollable(Parent view) {
        StackPane centered = new StackPane(view);
        centered.setAlignment(Pos.TOP_CENTER);
        ScrollPane scroll = new ScrollPane(centered);
        scroll.setFitToWidth(true);
        scroll.setFitToHeight(true);
        scroll.setHbarPolicy(ScrollPane.ScrollBarPolicy.NEVER);
        scroll.getStyleClass().add("view-scroll");
        return scroll;
    }
}
