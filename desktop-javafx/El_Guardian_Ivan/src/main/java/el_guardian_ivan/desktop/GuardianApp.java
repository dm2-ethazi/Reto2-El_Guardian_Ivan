package el_guardian_ivan.desktop;

import el_guardian_ivan.desktop.ui.ScalingPane;
import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.geometry.Rectangle2D;
import javafx.scene.text.Font;
import javafx.stage.Screen;
import javafx.stage.Stage;

import java.io.IOException;
import java.io.InputStream;
import java.io.UncheckedIOException;
import java.util.Objects;

public class GuardianApp extends Application {

    /** Fuentes incluidas en resources/.../fonts (licencia OFL). */
    private static final String[] FONTS = {"Cinzel-Bold.ttf", "Inter-Regular.ttf", "Inter-SemiBold.ttf"};

    @Override
    public void start(Stage stage) throws IOException {
        loadFonts();
        FXMLLoader fxmlLoader = new FXMLLoader(GuardianApp.class.getResource("views/main-view.fxml"));
        // ScalingPane ajusta el tamaño de toda la interfaz al de la ventana
        // Tamaño inicial de diseño, salvo en pantallas pequeñas, donde se usa el 90 % del espacio disponible
        Rectangle2D screen = Screen.getPrimary().getVisualBounds();
        double width = Math.min(1000, screen.getWidth() * 0.9);
        double height = Math.min(650, screen.getHeight() * 0.9);
        Scene scene = new Scene(new ScalingPane(fxmlLoader.load()), width, height);
        scene.getStylesheets().add(Objects.requireNonNull(
                GuardianApp.class.getResource("css/hogwarts.css")).toExternalForm());
        stage.setTitle("El Guardián Iván");
        stage.setMinWidth(640);
        stage.setMinHeight(420);
        stage.setScene(scene);
        stage.show();
    }

    /** Registra las fuentes antes de cargar el CSS para que este pueda usarlas por su nombre. */
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
