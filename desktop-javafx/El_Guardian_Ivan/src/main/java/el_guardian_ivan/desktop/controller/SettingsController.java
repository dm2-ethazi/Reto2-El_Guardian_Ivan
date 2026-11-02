package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.i18n.I18n;
import javafx.fxml.FXML;
import javafx.scene.control.ComboBox;
import javafx.util.StringConverter;

import java.util.Locale;

/** Pantalla Ajustes: de momento, el idioma de la aplicación. */
public class SettingsController {

    @FXML
    private ComboBox<Locale> languageBox;

    @FXML
    private void initialize() {
        languageBox.getItems().setAll(I18n.SUPPORTED);
        languageBox.setConverter(new StringConverter<>() {
            @Override
            public String toString(Locale language) {
                return language == null ? "" : I18n.displayName(language);
            }

            @Override
            public Locale fromString(String text) {
                return null; // El desplegable no es editable
            }
        });
        languageBox.setValue(I18n.getLocale());
        // I18n guarda la elección y la ventana principal se vuelve a cargar en el nuevo idioma
        languageBox.valueProperty().addListener((obs, oldLanguage, newLanguage) -> {
            if (newLanguage != null && !newLanguage.equals(I18n.getLocale())) {
                I18n.setLocale(newLanguage);
            }
        });
    }
}
