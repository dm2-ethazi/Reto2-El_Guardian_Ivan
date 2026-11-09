package el_guardian_ivan.desktop.logging;

import org.junit.jupiter.api.Test;

import java.nio.file.Path;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;

/** Comprueba que los ficheros se describen sin nombre ni ruta. */
class LogFormatTest {

    /** Un fichero se describe solo por su extensión y tamaño, sin su nombre. */
    @Test
    void describesOnlyTypeAndSize() {
        String text = LogFormat.describe(Path.of("secretos", "nominas_2025.PDF"), 1536);
        assertEquals("fichero .pdf de 1.5 KB", text);
        assertFalse(text.contains("nominas"));
    }

    /** Ficheros sin extensión, con "extensiones" raras o sin tamaño conocido. */
    @Test
    void describesFilesWithoutExtensionOrSize() {
        assertEquals("fichero sin extensión", LogFormat.describe(Path.of("LEEME"), -1));
        assertEquals("fichero sin extensión de 10 B", LogFormat.describe(Path.of("informe.secreto de Ana"), 10));
        assertEquals("fichero sin extensión", LogFormat.describe(null, -1));
    }

    /** Los tamaños llevan siempre punto decimal, sea cual sea el idioma. */
    @Test
    void formatsSizesWithDotInAnyLanguage() {
        assertEquals("512 B", LogFormat.bytes(512));
        assertEquals("1.0 MB", LogFormat.bytes(1024 * 1024));
        assertEquals("2.0 GB", LogFormat.bytes(2L * 1024 * 1024 * 1024));
    }
}
