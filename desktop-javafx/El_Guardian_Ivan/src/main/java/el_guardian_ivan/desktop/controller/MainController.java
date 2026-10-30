package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.navigation.NavigationAware;
import el_guardian_ivan.desktop.navigation.Navigator;
import el_guardian_ivan.desktop.navigation.View;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
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
    @FXML
    private StackPane contentArea;

    @FXML
    private ToggleGroup navigationGroup;

    @FXML
    private ToggleButton homeButton;

    @FXML
    private ToggleButton encryptButton;

    @FXML
    private ToggleButton decryptButton;

    @FXML
    private ToggleButton settingsButton;

    private final Map<View, Parent> loadedViews = new EnumMap<>(View.class);

    @FXML
    private void initialize() {
        homeButton.setUserData(View.HOME);
        encryptButton.setUserData(View.ENCRYPT);
        decryptButton.setUserData(View.DECRYPT);
        settingsButton.setUserData(View.SETTINGS);

        navigationGroup.selectedToggleProperty().addListener((obs, oldToggle, newToggle) -> {
            if (newToggle == null) {
                oldToggle.setSelected(true); // Siempre tiene que haber una sección marcada
            } else {
                show((View) newToggle.getUserData());
            }
        });
        homeButton.setSelected(true);
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
        FXMLLoader loader = new FXMLLoader(MainController.class.getResource(view.fxml()));
        try {
            Parent root = loader.load();
            if (loader.getController() instanceof NavigationAware aware) {
                aware.setNavigator(this);
            }
            return root;
        } catch (IOException e) {
            // Un FXML que no carga es un error de programación, no del usuario
            throw new UncheckedIOException("No se ha podido cargar " + view.fxml(), e);
        }
    }
}
