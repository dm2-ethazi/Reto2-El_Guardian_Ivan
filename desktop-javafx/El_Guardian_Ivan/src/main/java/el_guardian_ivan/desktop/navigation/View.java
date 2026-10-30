package el_guardian_ivan.desktop.navigation;

/** Pantallas de la aplicación y la ruta del FXML que carga cada una (en resources/.../views). */
public enum View {
    HOME("home-view.fxml"),
    ENCRYPT("encrypt-view.fxml"),
    DECRYPT("decrypt-view.fxml"),
    SETTINGS("settings-view.fxml");

    private final String fxml;

    View(String fxml) {
        this.fxml = "/el_guardian_ivan/desktop/views/" + fxml;
    }

    public String fxml() {
        return fxml;
    }
}
