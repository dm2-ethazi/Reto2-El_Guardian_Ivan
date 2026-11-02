package el_guardian_ivan.desktop.navigation;

/** Pantallas de la aplicación y la ruta del FXML que carga cada una (en resources/.../views). */
public enum View {
    /** Inicio. */
    HOME("home-view.fxml"),
    /** Cifrar. */
    ENCRYPT("encrypt-view.fxml"),
    /** Descifrar. */
    DECRYPT("decrypt-view.fxml"),
    /** Ajustes. */
    SETTINGS("settings-view.fxml"),
    /** Ayuda y Acerca de. */
    HELP("help-view.fxml");

    /** Ruta completa del FXML dentro de los recursos. */
    private final String fxml;

    /**
     * Asocia la pantalla con su FXML.
     *
     * @param fxml nombre del fichero dentro de la carpeta views
     */
    View(String fxml) {
        this.fxml = "/el_guardian_ivan/desktop/views/" + fxml;
    }

    /**
     * Ruta que se pasa a FXMLLoader para cargar la pantalla.
     *
     * @return la ruta del FXML
     */
    public String fxml() {
        return fxml;
    }
}
