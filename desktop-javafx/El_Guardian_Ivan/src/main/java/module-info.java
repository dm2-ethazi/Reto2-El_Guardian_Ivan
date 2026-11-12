/**
 * Módulo de la aplicación de escritorio El Guardián Iván.
 * Declara qué partes de Java y JavaFX usa y qué deja ver a otros módulos.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
module el_guardian_ivan.desktop {
    requires javafx.controls; // Botones, campos, ventanas...
    requires javafx.fxml;     // Carga de las pantallas desde los ficheros .fxml
    requires java.prefs; // Preferences: recordar el idioma elegido
    requires org.slf4j;  // API de registros (logs) que usa el código
    requires ch.qos.logback.classic; // Logback escribe los registros; aquí se amplía con el enmascarado
    requires ch.qos.logback.core;


    // FXMLLoader necesita acceder por reflexión a los campos y métodos @FXML de los controladores
    opens el_guardian_ivan.desktop.controller to javafx.fxml;
    // Logback crea por su nombre (desde logback.xml) los conversores que enmascaran datos sensibles
    exports el_guardian_ivan.desktop.logging to ch.qos.logback.core;
    // JavaFX tiene que poder crear GuardianApp para arrancar la aplicación
    exports el_guardian_ivan.desktop;
}