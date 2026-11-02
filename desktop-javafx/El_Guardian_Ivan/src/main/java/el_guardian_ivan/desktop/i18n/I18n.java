package el_guardian_ivan.desktop.i18n;

import javafx.beans.property.ReadOnlyObjectProperty;
import javafx.beans.property.ReadOnlyObjectWrapper;

import java.text.MessageFormat;
import java.util.List;
import java.util.Locale;
import java.util.ResourceBundle;
import java.util.prefs.Preferences;

/**
 * Idioma de la aplicación: español (por defecto), inglés y euskera.
 * Los textos están en resources/el_guardian_ivan/desktop/i18n/messages*.properties y el idioma
 * elegido se guarda con Preferences para recordarlo la próxima vez que se abra la aplicación.
 */
public final class I18n {

    // Idiomas disponibles, en el orden en que salen en Ajustes
    /** Español (idioma por defecto). */
    public static final Locale SPANISH = Locale.forLanguageTag("es");
    /** Inglés. */
    public static final Locale ENGLISH = Locale.forLanguageTag("en");
    /** Euskera. */
    public static final Locale BASQUE = Locale.forLanguageTag("eu");
    /** Todos los idiomas que ofrece la aplicación. */
    public static final List<Locale> SUPPORTED = List.of(SPANISH, ENGLISH, BASQUE);

    /** Nombre base de los ficheros de textos (messages.properties, messages_en.properties...). */
    private static final String BUNDLE = "el_guardian_ivan.desktop.i18n.messages";
    /** Clave con la que se guarda el idioma elegido (en Windows, dentro del registro del usuario). */
    private static final String PREFERENCE_KEY = "idioma";
    /** Almacén de preferencias del usuario para esta aplicación. */
    private static final Preferences PREFERENCES = Preferences.userNodeForPackage(I18n.class);

    /** Idioma actual; observable para que la ventana se recargue al cambiarlo. */
    private static final ReadOnlyObjectWrapper<Locale> currentLocale = new ReadOnlyObjectWrapper<>(initialLocale());

    /** Clase de utilidades: no se crean objetos. */
    private I18n() {
    }

    /**
     * El guardado en Preferences; si no hay, el del sistema si está soportado; si no, español.
     *
     * @return el idioma con el que arranca la aplicación
     */
    private static Locale initialLocale() {
        String saved = PREFERENCES.get(PREFERENCE_KEY, null);
        String language = saved != null ? saved : Locale.getDefault().getLanguage();
        return SUPPORTED.stream()
                .filter(supported -> supported.getLanguage().equals(language))
                .findFirst()
                .orElse(SPANISH);
    }

    /**
     * Idioma en el que se muestra ahora la aplicación.
     *
     * @return el idioma actual
     */
    public static Locale getLocale() {
        return currentLocale.get();
    }

    /**
     * La ventana principal lo escucha para volver a cargar los textos en el nuevo idioma.
     *
     * @return la propiedad (solo lectura) con el idioma actual
     */
    public static ReadOnlyObjectProperty<Locale> localeProperty() {
        return currentLocale.getReadOnlyProperty();
    }

    /**
     * Cambia el idioma y lo guarda para la próxima vez que se abra la aplicación.
     *
     * @param newLocale idioma nuevo; tiene que estar en {@link #SUPPORTED}
     * @throws IllegalArgumentException si el idioma no está disponible
     */
    public static void setLocale(Locale newLocale) {
        if (!SUPPORTED.contains(newLocale)) {
            throw new IllegalArgumentException("Idioma no soportado: " + newLocale);
        }
        PREFERENCES.put(PREFERENCE_KEY, newLocale.getLanguage());
        currentLocale.set(newLocale);
    }

    /**
     * Todos los textos del idioma actual; FXMLLoader los usa para resolver los "%clave" de los .fxml.
     *
     * @return los textos del idioma actual
     */
    public static ResourceBundle bundle() {
        // El español está en messages.properties (sin sufijo). Si se pidiera "es", Java buscaría
        // messages_es, no lo encontraría y caería al idioma del sistema (p. ej. inglés) antes que
        // al fichero base; pidiendo Locale.ROOT se carga directamente el fichero base.
        Locale lookup = SPANISH.equals(getLocale()) ? Locale.ROOT : getLocale();
        return ResourceBundle.getBundle(BUNDLE, lookup);
    }

    /**
     * Texto de la clave en el idioma actual; los argumentos sustituyen {0}, {1}...
     *
     * @param key clave del texto, por ejemplo "app.title"
     * @param args valores para {0}, {1}...
     * @return el texto traducido
     */
    public static String get(String key, Object... args) {
        String text = bundle().getString(key);
        return args.length == 0 ? text : new MessageFormat(text, getLocale()).format(args);
    }

    /**
     * Nombre del idioma escrito en ese mismo idioma: "Español", "English", "Euskara".
     *
     * @param language idioma
     * @return su nombre con la primera letra en mayúscula
     */
    public static String displayName(Locale language) {
        String name = language.getDisplayLanguage(language);
        return name.substring(0, 1).toUpperCase(language) + name.substring(1);
    }
}
