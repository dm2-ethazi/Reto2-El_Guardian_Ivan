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

/**
 * Pruebas de FilePreview: qué ficheros tienen vista previa (imagen o texto) y cómo se lee el texto
 * (recorte sin partir caracteres, marca BOM, fichero vacío).
 */
class FilePreviewTest {
    /** Carpeta temporal que JUnit crea y borra en cada prueba. */
    @TempDir
    Path dir;

    /**
     * Un PNG se reconoce por su contenido aunque no tenga extensión.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void detectaPngPorSuContenidoAunqueNoTengaExtension() throws IOException {
        Path file = Files.write(dir.resolve("foto"), new byte[]{(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n', 0, 0});
        assertEquals(FilePreview.Kind.IMAGE, FilePreview.kindOf(file));
    }

    /**
     * Un JPG tiene vista previa de imagen.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void detectaJpg() throws IOException {
        Path file = Files.write(dir.resolve("foto.jpg"), new byte[]{(byte) 0xFF, (byte) 0xD8, (byte) 0xFF, 0, 0});
        assertEquals(FilePreview.Kind.IMAGE, FilePreview.kindOf(file));
    }

    /**
     * Un texto plano tiene vista previa de texto.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void detectaTextoPlano() throws IOException {
        Path file = Files.writeString(dir.resolve("notas.txt"), "Hola, Hogwarts ✓");
        assertEquals(FilePreview.Kind.TEXT, FilePreview.kindOf(file));
    }

    /**
     * Un binario no tiene vista previa.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void unBinarioNoTieneVistaPrevia() throws IOException {
        Path file = Files.write(dir.resolve("datos.bin"), new byte[]{1, 0, 2, 0, 3});
        assertEquals(FilePreview.Kind.NONE, FilePreview.kindOf(file));
    }

    /**
     * Un PDF no tiene vista previa.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void unPdfNoTieneVistaPrevia() throws IOException {
        Path file = Files.writeString(dir.resolve("doc.pdf"), "%PDF-1.7 ...");
        assertEquals(FilePreview.Kind.NONE, FilePreview.kindOf(file));
    }

    /**
     * Un texto corto se lee entero.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void leeElTextoCompletoSiCabe() throws IOException {
        Path file = Files.writeString(dir.resolve("a.txt"), "línea 1\nlínea 2");
        FilePreview.Text text = FilePreview.readText(file, 1024);
        assertEquals("línea 1\nlínea 2", text.content());
        assertFalse(text.truncated());
    }

    /**
     * Un texto largo se recorta sin partir caracteres de varios bytes.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void recortaLosTextosLargosSinPartirCaracteres() throws IOException {
        Path file = Files.writeString(dir.resolve("largo.txt"), "ñ".repeat(100)); // 200 bytes
        FilePreview.Text text = FilePreview.readText(file, 51); // Corta a mitad de una ñ
        assertTrue(text.truncated());
        assertEquals("ñ".repeat(25), text.content());
    }

    /**
     * Se quita la marca BOM que añade el Bloc de notas.
     *
     * @throws IOException si falla la lectura o la escritura
     */
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

    /**
     * Un fichero vacío se ve como texto vacío.
     *
     * @throws IOException si falla la lectura o la escritura
     */
    @Test
    void unFicheroVacioEsTextoVacio() throws IOException {
        Path file = Files.write(dir.resolve("vacio.txt"), new byte[0]);
        assertEquals(FilePreview.Kind.TEXT, FilePreview.kindOf(file));
        assertEquals("", FilePreview.readText(file, 1024).content());
    }
}
