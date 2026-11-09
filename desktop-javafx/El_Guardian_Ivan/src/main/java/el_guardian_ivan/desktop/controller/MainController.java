package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
import el_guardian_ivan.desktop.navigation.NavigationAware;
import el_guardian_ivan.desktop.navigation.Navigator;
import el_guardian_ivan.desktop.navigation.View;
import javafx.application.Platform;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.geometry.Pos;
import javafx.scene.Parent;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonBase;
import javafx.scene.control.ScrollPane;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.Control;
import javafx.scene.input.KeyEvent;
import javafx.scene.input.MouseEvent;
import javafx.scene.input.ScrollEvent;
import javafx.scene.control.Tooltip;
import javafx.scene.input.KeyCode;
import javafx.scene.input.KeyCodeCombination;
import javafx.scene.input.KeyCombination;
import javafx.scene.control.ToggleButton;
import javafx.scene.control.ToggleGroup;
import javafx.scene.layout.StackPane;
import javafx.scene.layout.Pane;
import javafx.geometry.Bounds;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.io.UncheckedIOException;
import java.util.EnumMap;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Ventana principal: menú lateral a la izquierda y la pantalla elegida en el centro.
 * Cada pantalla se carga de su FXML la primera vez y se guarda, así no pierde lo que el usuario
 * haya escrito al cambiar de sección.
 */
public class MainController implements Navigator {

    /** Registro de esta clase (ver logback.xml). */
    private static final Logger log = LoggerFactory.getLogger(MainController.class);

    /** FXMLLoader crea el controlador al cargar main-view.fxml. */
    public MainController() {
    }
    /** Zona central donde se coloca la pantalla elegida. */
    @FXML
    private StackPane contentArea;

    /** Nodo raíz, que recibe las teclas tanto en las pantallas como en el panel superpuesto. */
    @FXML
    private StackPane rootPane;

    /** Capa transparente que contiene pequeñas etiquetas, igual que los KeyTips de Excel. */
    @FXML
    private Pane keyTipsLayer;

    /** Acciones activas mientras las pistas de Alt están a la vista. */
    private final Map<KeyCode, Runnable> keyTipActions = new EnumMap<>(KeyCode.class);
    private final List<KeyTip> keyTips = new ArrayList<>();

    private boolean altPressed;
    private boolean altCombinedWithOtherKey;

    private record KeyTip(Node target, Label badge) { }

    /** Agrupa los botones del menú para que solo uno pueda estar marcado. */

    @FXML
    private ToggleGroup navigationGroup;

    // Botones del menú lateral, uno por pantalla
    /** Botón Inicio. */
    @FXML
    private ToggleButton homeButton;

    /** Botón Cifrar. */
    @FXML
    private ToggleButton encryptButton;

    /** Botón Descifrar. */
    @FXML
    private ToggleButton decryptButton;

    /** Botón Ajustes. */
    @FXML
    private ToggleButton settingsButton;

    /** Botón Ayuda (abajo del todo). */
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

        // Los filtros pertenecen a este nodo: al reconstruirse la interfaz por un cambio de
        // idioma se eliminan con el nodo anterior, sin acumular listeners en la Scene.
        rootPane.addEventFilter(KeyEvent.KEY_PRESSED, this::handleKeyPressed);
        rootPane.addEventFilter(KeyEvent.KEY_RELEASED, this::handleKeyReleased);
        rootPane.addEventFilter(MouseEvent.MOUSE_PRESSED, event -> hideKeyTips());
        rootPane.addEventFilter(ScrollEvent.SCROLL, event -> {
            if (keyTipsLayer.isVisible()) {
                Platform.runLater(this::positionKeyTips);
            }
        });
        // Una capa no gestionada por el layout nunca altera el tamaño de los botones.
        rootPane.widthProperty().addListener((obs, oldValue, newValue) -> resizeKeyTipLayer());
        rootPane.heightProperty().addListener((obs, oldValue, newValue) -> resizeKeyTipLayer());

        // Atajos de teclado: se registran en la escena en cuanto la ventana existe
        contentArea.sceneProperty().addListener((obs, oldScene, scene) -> {
            if (scene != null) {
                registerShortcuts(scene.getAccelerators());
            }
        });
    }

    /**
     * Ctrl+1..4 cambia de sección y Ctrl+O abre el selector de archivo de la pantalla actual.
     *
     * @param accelerators atajos de la escena, donde se añaden los nuevos
     */
    private void registerShortcuts(Map<KeyCombination, Runnable> accelerators) {
        ToggleButton[] sections = {homeButton, encryptButton, decryptButton, settingsButton};
        KeyCode[] digits = {KeyCode.DIGIT1, KeyCode.DIGIT2, KeyCode.DIGIT3, KeyCode.DIGIT4};
        for (int i = 0; i < sections.length; i++) {
            ToggleButton section = sections[i];
            KeyCombination shortcut = new KeyCodeCombination(digits[i], KeyCombination.CONTROL_DOWN);
            // put() sustituye el atajo anterior si la ventana se reconstruye al cambiar de idioma
            accelerators.put(shortcut, () -> {
                hideKeyTips();
                section.setSelected(true);
                section.requestFocus();
            });
            section.setTooltip(new Tooltip(section.getText() + " (" + shortcut.getDisplayText() + ")"));
        }
        // F1 abre la ayuda, como en la mayoría de programas de escritorio
        KeyCombination help = new KeyCodeCombination(KeyCode.F1);
        accelerators.put(help, () -> {
            hideKeyTips();
            helpButton.setSelected(true);
            helpButton.requestFocus();
        });
        helpButton.setTooltip(new Tooltip(helpButton.getText() + " (" + help.getDisplayText() + ")"));
        accelerators.put(new KeyCodeCombination(KeyCode.O, KeyCombination.CONTROL_DOWN), () -> {
            hideKeyTips();
            Node selectButton = contentArea.getChildren().isEmpty() ? null
                    : contentArea.getChildren().get(0).lookup("#selectButton");
            if (selectButton instanceof Button button && !button.isDisabled()) {
                button.requestFocus();
                button.fire();
            }
        });
    }

    /**
     * Pulsar y soltar Alt activa/desactiva las pistas sin interceptar Alt+Tab,
     * Alt+F4 ni los demás atajos nativos del sistema.
     */
    private void handleKeyPressed(KeyEvent event) {
        if (event.getCode() == KeyCode.ALT) {
            // Reestablece el estado aunque Windows haya robado antes Alt+Tab.
            altPressed = true;
            altCombinedWithOtherKey = false;
            return;
        }
        if (altPressed) {
            altCombinedWithOtherKey = true;
            hideKeyTips();
            return;
        }
        if (!keyTipsLayer.isVisible()) {
            return;
        }
        if (event.getCode() == KeyCode.ESCAPE) {
            hideKeyTips();
            event.consume();
            return;
        }
        // Con las pistas visibles, una sola tecla activa el control indicado.
        // No se roban Ctrl+O, F1 ni otras combinaciones que ya existían.
        if (!event.isControlDown() && !event.isMetaDown() && !event.isAltDown()) {
            Runnable action = keyTipActions.get(event.getCode());
            if (action != null) {
                hideKeyTips();
                action.run();
                event.consume();
                return;
            }
        }
        // Otra tecla desactiva las pistas sin bloquear la escritura normal.
        if (event.getCode() != KeyCode.SHIFT && event.getCode() != KeyCode.CONTROL) {
            hideKeyTips();
        }
    }

    private void handleKeyReleased(KeyEvent event) {
        if (event.getCode() == KeyCode.ALT) {
            if (altPressed && !altCombinedWithOtherKey) {
                if (keyTipsLayer.isVisible()) {
                    hideKeyTips();
                } else {
                    showKeyTips();
                }
                event.consume();
            }
            altPressed = false;
            altCombinedWithOtherKey = false;
        }
    }

    /** Crea solamente pistas para los controles presentes en la pantalla actual. */
    private void showKeyTips() {
        keyTipsLayer.getChildren().clear();
        keyTipActions.clear();
        keyTips.clear();

        // Navegación lateral: 1..4, tal como los Ctrl+1..4 ya implementados.
        addKeyTip(KeyCode.DIGIT1, "1", homeButton, () -> homeButton.setSelected(true));
        addKeyTip(KeyCode.DIGIT2, "2", encryptButton, () -> encryptButton.setSelected(true));
        addKeyTip(KeyCode.DIGIT3, "3", decryptButton, () -> decryptButton.setSelected(true));
        addKeyTip(KeyCode.DIGIT4, "4", settingsButton, () -> settingsButton.setSelected(true));
        addKeyTip(KeyCode.A, "A", helpButton, () -> helpButton.setSelected(true));

        Node current = contentArea.getChildren().isEmpty() ? null : contentArea.getChildren().get(0);
        if (current != null) {
            // Accesos en Inicio.
            addButtonKeyTip(current, KeyCode.C, "C", "#homeEncryptButton");
            addButtonKeyTip(current, KeyCode.D, "D", "#homeDecryptButton");

            // Acciones de Cifrar y Descifrar (se omiten si el botón está deshabilitado).
            addButtonKeyTip(current, KeyCode.O, "O", "#selectButton");
            addButtonKeyTip(current, KeyCode.U, "U", "#outputFolderButton");
            addButtonKeyTip(current, KeyCode.P, "P", "#showPasswordToggle");
            addButtonKeyTip(current, KeyCode.R, "R", "#runButton");

            // Selector de idioma en Ajustes.
            Node language = current.lookup("#languageBox");
            if (language instanceof ComboBox<?> comboBox) {
                addKeyTip(KeyCode.L, "L", comboBox, () -> {
                    comboBox.requestFocus();
                    comboBox.show();
                });
            }
        }
        resizeKeyTipLayer();
        keyTipsLayer.setVisible(true);
        keyTipsLayer.toFront();
        // Posicionar después de aplicar CSS y layout, incluyendo el ScalingPane.
        Platform.runLater(this::positionKeyTips);
    }

    /** Registra una acción y coloca su etiqueta sobre el botón, sin capturar clics. */
    private void addKeyTip(KeyCode key, String caption, Node target, Runnable action) {
        if (target == null || !target.isVisible() || target instanceof Control control && control.isDisabled()) {
            return;
        }
        Label badge = new Label(caption);
        badge.getStyleClass().add("keytip-badge");
        badge.setMouseTransparent(true);
        keyTipActions.put(key, action);
        keyTips.add(new KeyTip(target, badge));
        keyTipsLayer.getChildren().add(badge);
    }

    private void addButtonKeyTip(Node current, KeyCode key, String caption, String selector) {
        Node target = current.lookup(selector);
        if (target instanceof ButtonBase button) {
            addKeyTip(key, caption, button, button::fire);
        }
    }

    private void resizeKeyTipLayer() {
        keyTipsLayer.resize(rootPane.getWidth(), rootPane.getHeight());
        if (keyTipsLayer.isVisible()) {
            Platform.runLater(this::positionKeyTips);
        }
    }

    /** Las posiciones se calculan en coordenadas del root, no en píxeles de pantalla. */
    private void positionKeyTips() {
        if (!keyTipsLayer.isVisible() || rootPane.getScene() == null) {
            return;
        }
        keyTipsLayer.applyCss();
        double rootWidth = keyTipsLayer.getWidth();
        double rootHeight = keyTipsLayer.getHeight();
        for (KeyTip tip : keyTips) {
            Node target = tip.target();
            Label badge = tip.badge();
            if (target.getScene() != rootPane.getScene() || !target.isVisible()) {
                badge.setVisible(false);
                continue;
            }
            Bounds sceneBounds = target.localToScene(target.getBoundsInLocal());
            if (sceneBounds == null) {
                badge.setVisible(false);
                continue;
            }
            Bounds bounds = rootPane.sceneToLocal(sceneBounds);
            double width = badge.prefWidth(-1);
            double height = badge.prefHeight(-1);
            // Las pistas se anclan cerca de la esquina superior derecha del botón.
            double x = Math.min(rootWidth - width - 3,
                    Math.max(3, bounds.getMaxX() - width - 8));
            double y = Math.min(rootHeight - height - 3,
                    Math.max(3, bounds.getMinY() + 3));
            boolean onScreen = bounds.getMaxX() > 0 && bounds.getMinX() < rootWidth
                    && bounds.getMaxY() > 0 && bounds.getMinY() < rootHeight;
            // No pintar etiquetas sobre controles que el ScrollPane haya recortado.
            Node ancestor = target.getParent();
            while (ancestor != null && !(ancestor instanceof ScrollPane)) {
                ancestor = ancestor.getParent();
            }
            if (ancestor instanceof ScrollPane scrollPane) {
                Bounds viewport = rootPane.sceneToLocal(scrollPane.localToScene(scrollPane.getBoundsInLocal()));
                onScreen = onScreen && bounds.getMaxX() > viewport.getMinX()
                        && bounds.getMinX() < viewport.getMaxX()
                        && bounds.getMaxY() > viewport.getMinY()
                        && bounds.getMinY() < viewport.getMaxY();
            }
            badge.setVisible(onScreen);
            if (onScreen) {
                badge.resizeRelocate(x, y, width, height);
            }
        }
    }

    private void hideKeyTips() {
        if (keyTipsLayer.isVisible()) {
            keyTipsLayer.setVisible(false);
            keyTipsLayer.getChildren().clear();
            keyTipActions.clear();
            keyTips.clear();
        }
    }

    @Override
    public void navigateTo(View view) {
        navigationGroup.getToggles().stream()
                .filter(toggle -> toggle.getUserData() == view)
                .findFirst()
                .ifPresent(toggle -> toggle.setSelected(true)); // El listener muestra la pantalla
    }

    /**
     * Pone en la zona central la pantalla indicada (cargándola si es la primera vez).
     *
     * @param view pantalla que se muestra
     */
    private void show(View view) {
        log.debug("Pantalla: {}", view);
        contentArea.getChildren().setAll(loadedViews.computeIfAbsent(view, this::load));
    }

    /**
     * Carga el FXML de una pantalla en el idioma actual.
     *
     * @param view pantalla que se carga
     * @return la pantalla ya metida en su panel con desplazamiento
     */
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
     *
     * @param view pantalla que se envuelve
     * @return el panel con desplazamiento que contiene la pantalla
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
