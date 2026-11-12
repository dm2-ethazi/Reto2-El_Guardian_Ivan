package el_guardian_ivan.desktop.logging;

import ch.qos.logback.classic.pattern.ThrowableProxyConverter;
import ch.qos.logback.classic.spi.IThrowableProxy;

/**
 * Conversor de Logback para {@code %maskedEx} en logback.xml: escribe la traza de la excepción
 * como {@code %ex}, pero enmascarada. Hace falta porque los mensajes de las excepciones de ficheros
 * llevan la ruta completa (con el nombre del usuario).
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public class MaskedThrowableConverter extends ThrowableProxyConverter {

    /** Logback lo crea por su nombre al leer logback.xml. */
    public MaskedThrowableConverter() {
    }

    /**
     * Traza completa de la excepción (con sus causas) ya enmascarada.
     *
     * @param proxy la excepción tal como la guarda Logback
     * @return la traza sin datos sensibles
     */
    @Override
    protected String throwableProxyToString(IThrowableProxy proxy) {
        return SensitiveDataMasker.maskText(super.throwableProxyToString(proxy));
    }
}
