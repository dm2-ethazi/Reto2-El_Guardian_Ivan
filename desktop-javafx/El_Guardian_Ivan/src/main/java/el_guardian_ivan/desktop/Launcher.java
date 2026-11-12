package el_guardian_ivan.desktop;

import javafx.application.Application;

/**
 * Punto de entrada del programa.
 * Existe aparte de GuardianApp porque, al ejecutar un .jar con todo incluido, Java exige que
 * la clase principal no herede de Application; esta clase solo se encarga de arrancar JavaFX.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public class Launcher {

    /** Solo contiene {@link #main(String[])}: no se crean objetos. */
    private Launcher() {
    }

    /**
     * Arranca la aplicación.
     *
     * @param args argumentos de la línea de comandos (se pasan a JavaFX)
     */
    public static void main(String[] args) {
        Application.launch(GuardianApp.class, args);
    }
}
