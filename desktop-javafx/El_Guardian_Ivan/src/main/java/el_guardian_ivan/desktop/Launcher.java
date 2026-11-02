package el_guardian_ivan.desktop;

import javafx.application.Application;

/**
 * Punto de entrada del programa.
 * Existe aparte de GuardianApp porque, al ejecutar un .jar con todo incluido, Java exige que
 * la clase principal no herede de Application; esta clase solo se encarga de arrancar JavaFX.
 */
public class Launcher {
    public static void main(String[] args) {
        Application.launch(GuardianApp.class, args);
    }
}
