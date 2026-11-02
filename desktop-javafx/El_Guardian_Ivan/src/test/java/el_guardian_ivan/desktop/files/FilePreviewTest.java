package el_guardian_ivan.desktop.files;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

class FilePreviewTest {
    @TempDir
    Path dir;

    @Test
    void detectaPngPorSuContenidoAunqueNoTengaExtension() throws IOException {
        Path file = Files.write(dir.resolve("foto"), new byte[]{(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n', 0, 0});
        assertEquals(FilePreview.Kind.IMAGE, FilePreview.kindOf(file));
    }

    @Test
    void detectaJpg() throws IOException {
        Path file = Files.write(dir.resolve("foto.jpg"), new byte[]{(byte) 0xFF, (byte) 0xD8, (byte) 0xFF, 0, 0});
        assertEquals(FilePreview.Kind.IMAGE, FilePreview.kindOf(file));
    }

    @Test
    void detectaTextoPlano() throws IOException {
        Path file = Files.writeString(dir.resolve("notas.txt"), "Hola, Hogwarts ✓");
        assertEquals(FilePreview.Kind.TEXT, FilePreview.kindOf(file));
    }

    @Test
    void unBinarioNoTieneVistaPrevia() throws IOException {
        Path file = Files.write(dir.resolve("datos.bin"), new byte[]{1, 0, 2, 0, 3});
        assertEquals(FilePreview.Kind.NONE, FilePreview.kindOf(file));
    }

    @Test
    void unPdfNoTieneVistaPrevia() throws IOException {
        Path file = Files.writeString(dir.resolve("doc.pdf"), "%PDF-1.7 ...");
        assertEquals(FilePreview.Kind.NONE, FilePreview.kindOf(file));
    }

    @Test
    void leeElTextoCompletoSiCabe() throws IOException {
        Path file = Files.writeString(dir.resolve("a.txt"), "línea 1\nlínea 2");
        FilePreview.Text text = FilePreview.readText(file, 1024);
        assertEquals("línea 1\nlínea 2", text.content());
        assertFalse(text.truncated());
    }

    @Test
    void recortaLosTextosLargosSinPartirCaracteres() throws IOException {
        Path file = Files.writeString(dir.resolve("largo.txt"), "ñ".repeat(100)); // 200 bytes
        FilePreview.Text text = FilePreview.readText(file, 51); // Corta a mitad de una ñ
        assertTrue(text.truncated());
        assertEquals("ñ".repeat(25), text.content());
    }

    @Test
    void quitaLaMarcaBomDeWindows() throws IOException {
        byte[] bom = {(byte) 0xEF, (byte) 0xBB, (byte) 0xBF};
        byte[] body = "hola".getBytes(StandardCharsets.UTF_8);
        byte[] data = new byte[bom.length + body.length];
        System.arraycopy(bom, 0, data, 0, bom.length);
        System.arraycopy(body, 0, data, bom.length, body.length);
        Path file = Files.write(dir.resolve("bom.txt"), data);
        assertEquals("hola", FilePreview.readText(file, 1024).content());
    }

    @Test
    void unFicheroVacioEsTextoVacio() throws IOException {
        Path file = Files.write(dir.resolve("vacio.txt"), new byte[0]);
        assertEquals(FilePreview.Kind.TEXT, FilePreview.kindOf(file));
        assertEquals("", FilePreview.readText(file, 1024).content());
    }
}
