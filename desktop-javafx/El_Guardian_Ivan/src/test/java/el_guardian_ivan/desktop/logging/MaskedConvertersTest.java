package el_guardian_ivan.desktop.logging;

import ch.qos.logback.classic.Level;
import ch.qos.logback.classic.LoggerContext;
import ch.qos.logback.classic.PatternLayout;
import ch.qos.logback.classic.spi.LoggingEvent;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Comprueba que el patrón de logback.xml enmascara el mensaje y la traza de la excepción. */
class MaskedConvertersTest {

    /** Con el patrón de logback.xml no salen ni la contraseña ni la carpeta del usuario, pero sí la traza del error. */
    @Test
    void maskedPatternHidesMessageAndExceptionData() {
        String home = System.getProperty("user.home");
        LoggerContext context = new LoggerContext();
        PatternLayout layout = new PatternLayout();
        layout.setContext(context);
        layout.getInstanceConverterMap().putAll(Map.of(
                "maskedMsg", MaskedMessageConverter::new,
                "maskedEx", MaskedThrowableConverter::new));
        layout.setPattern("%maskedMsg%n%maskedEx");
        layout.start();

        LoggingEvent event = new LoggingEvent("test", context.getLogger("test"), Level.ERROR,
                "Fallo con password={} en {}", new IOException("No se puede leer " + home + "/a.txt"),
                new Object[]{"Hogwarts123", home + "/b.txt"});
        String text = layout.doLayout(event);

        assertFalse(text.contains("Hogwarts123"), text);
        assertFalse(text.contains(home), text);
        assertTrue(text.contains("IOException"), text); // La traza sigue ahí
        assertTrue(text.contains("~/a.txt") || text.contains("~\\a.txt"), text);
    }
}
