/**
 * Módulo de la aplicación de escritorio El Guardián Iván.
 * Declara qué partes de Java y JavaFX usa y qué deja ver a otros módulos.
 */
module el_guardian_ivan.desktop {
    requires javafx.controls; // Botones, campos, ventanas...
    requires javafx.fxml;     // Carga de las pantallas desde los ficheros .fxml
    requires java.prefs; // Preferences: recordar el idioma elegido


    // FXMLLoader necesita acceder por reflexión a los campos y métodos @FXML de los controladores
    opens el_guardian_ivan.desktop.controller to javafx.fxml;
    // JavaFX tiene que poder crear GuardianApp para arrancar la aplicación
    exports el_guardian_ivan.desktop;
}