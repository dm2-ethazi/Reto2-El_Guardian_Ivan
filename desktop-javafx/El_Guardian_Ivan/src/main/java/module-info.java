module el_guardian_ivan.desktop {
    requires javafx.controls;
    requires javafx.fxml;
    requires java.prefs; // Preferences: recordar el idioma elegido


    opens el_guardian_ivan.desktop.controller to javafx.fxml;
    exports el_guardian_ivan.desktop;
}