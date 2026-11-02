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

    public static final Locale SPANISH = Locale.forLanguageTag("es");
    public static final Locale ENGLISH = Locale.forLanguageTag("en");
    public static final Locale BASQUE = Locale.forLanguageTag("eu");
    public static final List<Locale> SUPPORTED = List.of(SPANISH, ENGLISH, BASQUE);

    private static final String BUNDLE = "el_guardian_ivan.desktop.i18n.messages";
    private static final String PREFERENCE_KEY = "idioma";
    private static final Preferences PREFERENCES = Preferences.userNodeForPackage(I18n.class);

    private static final ReadOnlyObjectWrapper<Locale> locale = new ReadOnlyObjectWrapper<>(initialLocale());

    private I18n() {
    }

    /** El guardado en Preferences; si no hay, el del sistema si está soportado; si no, español. */
    private static Locale initialLocale() {
        String saved = PREFERENCES.get(PREFERENCE_KEY, null);
        String language = saved != null ? saved : Locale.getDefault().getLanguage();
        return SUPPORTED.stream()
                .filter(supported -> supported.getLanguage().equals(language))
                .findFirst()
                .orElse(SPANISH);
    }

    public static Locale getLocale() {
        return locale.get();
    }

    /** La ventana principal lo escucha para volver a cargar los textos en el nuevo idioma. */
    public static ReadOnlyObjectProperty<Locale> localeProperty() {
        return locale.getReadOnlyProperty();
    }

    public static void setLocale(Locale newLocale) {
        if (!SUPPORTED.contains(newLocale)) {
            throw new IllegalArgumentException("Idioma no soportado: " + newLocale);
        }
        PREFERENCES.put(PREFERENCE_KEY, newLocale.getLanguage());
        locale.set(newLocale);
    }

    public static ResourceBundle bundle() {
        // El español está en messages.properties (sin sufijo). Si se pidiera "es", Java buscaría
        // messages_es, no lo encontraría y caería al idioma del sistema (p. ej. inglés) antes que
        // al fichero base; pidiendo Locale.ROOT se carga directamente el fichero base.
        Locale lookup = SPANISH.equals(getLocale()) ? Locale.ROOT : getLocale();
        return ResourceBundle.getBundle(BUNDLE, lookup);
    }

    /** Texto de la clave en el idioma actual; los argumentos sustituyen {0}, {1}... */
    public static String get(String key, Object... args) {
        String text = bundle().getString(key);
        return args.length == 0 ? text : new MessageFormat(text, getLocale()).format(args);
    }

    /** Nombre del idioma escrito en ese mismo idioma: "Español", "English", "Euskara". */
    public static String displayName(Locale language) {
        String name = language.getDisplayLanguage(language);
        return name.substring(0, 1).toUpperCase(language) + name.substring(1);
    }
}
