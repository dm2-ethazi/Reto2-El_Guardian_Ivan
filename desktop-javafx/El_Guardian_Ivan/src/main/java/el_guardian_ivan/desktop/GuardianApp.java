package el_guardian_ivan.desktop;

import el_guardian_ivan.desktop.i18n.I18n;
import el_guardian_ivan.desktop.navigation.Navigator;
import el_guardian_ivan.desktop.navigation.View;
import el_guardian_ivan.desktop.ui.ScalingPane;
import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.geometry.Rectangle2D;
import javafx.scene.text.Font;
import javafx.stage.Screen;
import javafx.scene.image.Image;
import javafx.stage.Stage;

import java.io.IOException;
import java.io.InputStream;
import java.io.UncheckedIOException;
import java.util.Objects;

public class GuardianApp extends Application {

    /** Fuentes incluidas en resources/.../fonts (licencia OFL). */
    private static final String[] FONTS = {"Cinzel-Bold.ttf", "Inter-Regular.ttf", "Inter-SemiBold.ttf"};

    @Override
    public void start(Stage stage) {
        loadFonts();
        // Tamaño inicial de diseño, salvo en pantallas pequeñas, donde se usa el 90 % del espacio disponible
        Rectangle2D screen = Screen.getPrimary().getVisualBounds();
        double width = Math.min(1000, screen.getWidth() * 0.9);
        double height = Math.min(650, screen.getHeight() * 0.9);
        Scene scene = new Scene(loadMainWindow(View.HOME), width, height);
        scene.getStylesheets().add(Objects.requireNonNull(
                GuardianApp.class.getResource("css/hogwarts.css")).toExternalForm());
        stage.setTitle(I18n.get("app.title"));
        addIcons(stage);
        stage.setMinWidth(640);
        stage.setMinHeight(420);
        stage.setScene(scene);
        stage.show();

        // Al cambiar de idioma en Ajustes se vuelven a cargar las pantallas con los nuevos textos
        I18n.localeProperty().addListener((obs, oldLocale, newLocale) -> {
            scene.setRoot(loadMainWindow(View.SETTINGS));
            stage.setTitle(I18n.get("app.title"));
        });
    }

    /** Carga la ventana principal en el idioma actual, mostrando la pantalla indicada. */
    private static Parent loadMainWindow(View initialView) {
        FXMLLoader loader = new FXMLLoader(GuardianApp.class.getResource("views/main-view.fxml"), I18n.bundle());
        try {
            Parent root = loader.load();
            loader.<Navigator>getController().navigateTo(initialView);
            // ScalingPane ajusta el tamaño de toda la interfaz al de la ventana
            return new ScalingPane(root);
        } catch (IOException e) {
            throw new UncheckedIOException(e);
        }
    }

    /** Registra las fuentes antes de cargar el CSS para que este pueda usarlas por su nombre. */
    /**
     * Icono de la ventana y de la barra de tareas. Se dan varios tamaños y el sistema elige
     * el que mejor encaja (16 px en la barra de título, 32-48 px en la barra de tareas...).
     */
    private static void addIcons(Stage stage) {
        for (String name : new String[]{"icon-16.png", "icon-24.png", "icon-32.png", "icon-48.png",
                "icon-64.png", "icon-128.png", "logo.png"}) {
            stage.getIcons().add(new Image(GuardianApp.class.getResource("images/" + name).toExternalForm()));
        }
    }

    private static void loadFonts() {
        for (String font : FONTS) {
            try (InputStream in = GuardianApp.class.getResourceAsStream("fonts/" + font)) {
                if (in == null || Font.loadFont(in, 14) == null) {
                    throw new IllegalStateException("No se pudo cargar la fuente " + font);
                }
            } catch (IOException e) {
                throw new UncheckedIOException(e);
            }
        }
    }
}
