package el_guardian_ivan.desktop.files;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.nio.file.Files;
import java.nio.file.Path;

import static org.junit.jupiter.api.Assertions.*;

class FileTypeDetectorTest {

    @TempDir
    Path dir;

    @Test
    void detectsPng() throws Exception {
        Path file = Files.write(dir.resolve("imagen"),
                new byte[]{(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n', 0, 0, 0, 13});
        assertEquals("png", FileTypeDetector.detectExtension(file));
    }

    @Test
    void detectsUtf8Text() throws Exception {
        Path file = Files.writeString(dir.resolve("mensaje"), "Travesura realizada, señor Filch ñ€");
        assertEquals("txt", FileTypeDetector.detectExtension(file));
    }

    @Test
    void detectsUnknownBinary() throws Exception {
        Path file = Files.write(dir.resolve("datos"), new byte[]{1, 2, 0, (byte) 0xFE, 5});
        assertEquals("bin", FileTypeDetector.detectExtension(file));
    }

    @Test
    void addsExtensionWhenMissing() throws Exception {
        Path file = Files.writeString(dir.resolve("prueba"), "hola");
        Path result = FileTypeDetector.addExtensionIfMissing(file, "");
        assertEquals(dir.resolve("prueba.txt"), result);
        assertTrue(Files.exists(result));
        assertFalse(Files.exists(file));
    }

    @Test
    void keepsExtensionChosenByUser() throws Exception {
        Path file = Files.writeString(dir.resolve("prueba.md"), "hola");
        assertEquals(file, FileTypeDetector.addExtensionIfMissing(file, ""));
    }

    @Test
    void doesNotOverwriteExistingFile() throws Exception {
        Path existing = Files.writeString(dir.resolve("prueba.txt"), "no me borres");
        Path file = Files.writeString(dir.resolve("prueba"), "hola");
        assertEquals(file, FileTypeDetector.addExtensionIfMissing(file, ""));
        assertEquals("no me borres", Files.readString(existing));
    }

    @Test
    void usesPreferredExtensionWhenGiven() throws Exception {
        Path file = Files.writeString(dir.resolve("maquina"), "no parece una ova");
        assertEquals(dir.resolve("maquina.ova"), FileTypeDetector.addExtensionIfMissing(file, "ova"));
    }
}
