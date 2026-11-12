package el_guardian_ivan.desktop.logging;

import ch.qos.logback.classic.pattern.MessageConverter;
import ch.qos.logback.classic.spi.ILoggingEvent;

/**
 * Conversor de Logback para {@code %maskedMsg} en logback.xml: igual que {@code %msg},
 * pero pasando el mensaje por {@link SensitiveDataMasker} antes de escribirlo.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public class MaskedMessageConverter extends MessageConverter {

    /** Logback lo crea por su nombre al leer logback.xml. */
    public MaskedMessageConverter() {
    }

    /**
     * Mensaje del evento ya formateado (con los {} sustituidos) y enmascarado.
     *
     * @param event evento de log que se está escribiendo
     * @return el mensaje sin datos sensibles
     */
    @Override
    public String convert(ILoggingEvent event) {
        return SensitiveDataMasker.maskText(super.convert(event));
    }
}
