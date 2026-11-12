package el_guardian_ivan.desktop.files;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.nio.file.Files;
import java.nio.file.Path;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Pruebas de FileTypeDetector: reconoce los formatos por su contenido y añade la extensión
 * solo cuando el fichero no tiene, sin pisar ficheros existentes.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
class FileTypeDetectorTest {

    /** Carpeta temporal que JUnit crea y borra en cada prueba. */
    @TempDir
    Path dir;

    /**
     * Un PNG se reconoce por sus primeros bytes.
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void detectsPng() throws Exception {
        Path file = Files.write(dir.resolve("imagen"),
                new byte[]{(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n', 0, 0, 0, 13});
        assertEquals("png", FileTypeDetector.detectExtension(file));
    }

    /**
     * Un texto UTF-8 se reconoce como texto.
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void detectsUtf8Text() throws Exception {
        Path file = Files.writeString(dir.resolve("mensaje"), "Travesura realizada, señor Filch ñ€");
        assertEquals("txt", FileTypeDetector.detectExtension(file));
    }

    /**
     * Un binario desconocido se detecta como "bin".
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void detectsUnknownBinary() throws Exception {
        Path file = Files.write(dir.resolve("datos"), new byte[]{1, 2, 0, (byte) 0xFE, 5});
        assertEquals("bin", FileTypeDetector.detectExtension(file));
    }

    /**
     * Si el nombre no tiene extensión, se añade la detectada.
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void addsExtensionWhenMissing() throws Exception {
        Path file = Files.writeString(dir.resolve("prueba"), "hola");
        Path result = FileTypeDetector.addExtensionIfMissing(file, "");
        assertEquals(dir.resolve("prueba.txt"), result);
        assertTrue(Files.exists(result));
        assertFalse(Files.exists(file));
    }

    /**
     * Si el usuario ya puso una extensión, se respeta.
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void keepsExtensionChosenByUser() throws Exception {
        Path file = Files.writeString(dir.resolve("prueba.md"), "hola");
        assertEquals(file, FileTypeDetector.addExtensionIfMissing(file, ""));
    }

    /**
     * Al añadir la extensión no se pisa un fichero que ya existe.
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void doesNotOverwriteExistingFile() throws Exception {
        Path existing = Files.writeString(dir.resolve("prueba.txt"), "no me borres");
        Path file = Files.writeString(dir.resolve("prueba"), "hola");
        assertEquals(file, FileTypeDetector.addExtensionIfMissing(file, ""));
        assertEquals("no me borres", Files.readString(existing));
    }

    /**
     * Si se conoce la extensión original, se usa esa.
     *
     * @throws Exception si falla algo inesperado al leer, escribir o cifrar
     */
    @Test
    void usesPreferredExtensionWhenGiven() throws Exception {
        Path file = Files.writeString(dir.resolve("maquina"), "no parece una ova");
        assertEquals(dir.resolve("maquina.ova"), FileTypeDetector.addExtensionIfMissing(file, "ova"));
    }
}
