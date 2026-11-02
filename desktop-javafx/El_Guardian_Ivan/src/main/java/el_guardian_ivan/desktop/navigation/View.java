package el_guardian_ivan.desktop.navigation;

/** Pantallas de la aplicación y la ruta del FXML que carga cada una (en resources/.../views). */
public enum View {
    HOME("home-view.fxml"),
    ENCRYPT("encrypt-view.fxml"),
    DECRYPT("decrypt-view.fxml"),
    SETTINGS("settings-view.fxml"),
    HELP("help-view.fxml");

    /** Ruta completa del FXML dentro de los recursos. */
    private final String fxml;

    View(String fxml) {
        this.fxml = "/el_guardian_ivan/desktop/views/" + fxml;
    }

    /** Ruta que se pasa a FXMLLoader para cargar la pantalla. */
    public String fxml() {
        return fxml;
    }
}
