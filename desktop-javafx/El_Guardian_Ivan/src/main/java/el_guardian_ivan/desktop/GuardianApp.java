package el_guardian_ivan.desktop;

import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.text.Font;
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
        Scene scene = new Scene(fxmlLoader.load(), 1000, 650);
        scene.getStylesheets().add(Objects.requireNonNull(
                GuardianApp.class.getResource("css/hogwarts.css")).toExternalForm());
        stage.setTitle("El Guardián Iván");
        stage.setMinWidth(800);
        stage.setMinHeight(550);
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
