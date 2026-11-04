package el_guardian_ivan.desktop.files;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.charset.CharacterCodingException;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;

/**
 * Adivina la extensión de un fichero mirando sus primeros bytes ("magic numbers").
 * Se usa al descifrar cuando el .hgw no trae la extensión original (el fichero no tenía).
 */
public final class FileTypeDetector {

    /** Solo se leen los primeros 4 KB: basta para reconocer el tipo sin leer el fichero entero. */
    private static final int SAMPLE_SIZE = 4096;

    // Firmas con las que empiezan siempre estos formatos
    /** PNG: 89 "PNG" y los bytes de control CR LF SUB LF. */
    private static final byte[] PNG = {(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'};
    /** JPEG: FF D8 FF. */
    private static final byte[] JPG = {(byte) 0xFF, (byte) 0xD8, (byte) 0xFF};
    /** GIF: "GIF8" (versiones 87a y 89a). */
    private static final byte[] GIF = {'G', 'I', 'F', '8'};
    /** PDF: "%PDF-". */
    private static final byte[] PDF = {'%', 'P', 'D', 'F', '-'};
    /** ZIP (también .docx, .xlsx, .jar...): "PK" 03 04. */
    private static final byte[] ZIP = {'P', 'K', 0x03, 0x04};

    /** Clase de utilidades: no se crean objetos. */
    private FileTypeDetector() {
    }

    /**
     * Devuelve la extensión sin punto: "png", "jpg", "gif", "pdf", "zip", "txt" o "bin".
     *
     * @param file fichero que se analiza
     * @return la extensión deducida
     * @throws IOException si no se puede leer el fichero
     */
    public static String detectExtension(Path file) throws IOException {
        byte[] sample;
        try (InputStream in = Files.newInputStream(file)) {
            sample = in.readNBytes(SAMPLE_SIZE);
        }
        if (startsWith(sample, PNG)) return "png";
        if (startsWith(sample, JPG)) return "jpg";
        if (startsWith(sample, GIF)) return "gif";
        if (startsWith(sample, PDF)) return "pdf";
        if (startsWith(sample, ZIP)) return "zip";
        if (looksLikeText(sample)) return "txt";
        return "bin";
    }

    /**
     * Si {@code file} no tiene extensión, le pone {@code preferredExtension}; si esta viene vacía,
     * la deduce del contenido. Devuelve la ruta final.
     *
     * @param file fichero recién descifrado
     * @param preferredExtension extensión guardada en el .hgw, o vacía
     * @return la ruta final (la misma si no se ha renombrado)
     * @throws IOException si no se puede leer o renombrar el fichero
     */
    public static Path addExtensionIfMissing(Path file, String preferredExtension) throws IOException {
        String name = file.getFileName().toString();
        if (name.lastIndexOf('.') > 0) {
            return file; // Ya tiene extensión: respetamos lo que eligió el usuario
        }
        String extension = preferredExtension == null || preferredExtension.isEmpty()
                ? detectExtension(file)
                : preferredExtension;
        Path target = file.resolveSibling(name + "." + extension);
        if (Files.exists(target)) {
            return file; // No sobrescribimos nada que ya exista
        }
        return Files.move(file, target);
    }

    /**
     * Si {@code data} empieza por los bytes de {@code prefix}.
     *
     * @param data bytes del fichero
     * @param prefix firma que se busca
     * @return {@code true} si coinciden los primeros bytes
     */
    private static boolean startsWith(byte[] data, byte[] prefix) {
        return data.length >= prefix.length && Arrays.equals(data, 0, prefix.length, prefix, 0, prefix.length);
    }

    /**
     * Es texto si no tiene bytes nulos y los bytes forman UTF-8 válido (que incluye el ASCII).
     *
     * @param sample primeros bytes del fichero
     * @return {@code true} si parece texto
     */
    private static boolean looksLikeText(byte[] sample) {
        if (sample.length == 0) {
            return true; // Un fichero vacío lo tratamos como texto
        }
        for (byte b : sample) {
            if (b == 0) {
                return false; // Un byte nulo casi seguro indica binario
            }
        }
        try {
            StandardCharsets.UTF_8.newDecoder()
                    .onMalformedInput(CodingErrorAction.REPORT)
                    .onUnmappableCharacter(CodingErrorAction.REPORT)
                    .decode(ByteBuffer.wrap(sample, 0, trimIncompleteUtf8(sample)));
            return true;
        } catch (CharacterCodingException e) {
            return false;
        }
    }

    /**
     * La muestra puede cortar un carácter UTF-8 por la mitad al final: lo descartamos.
     *
     * @param sample bytes leídos
     * @return cuántos bytes usar para no partir el último carácter
     */
    static int trimIncompleteUtf8(byte[] sample) {
        int end = sample.length;
        int i = end - 1;
        int continuation = 0;
        while (i >= 0 && continuation < 3 && (sample[i] & 0xC0) == 0x80) {
            i--;
            continuation++;
        }
        if (i >= 0 && (sample[i] & 0xC0) == 0xC0) {
            int expected = (sample[i] & 0xE0) == 0xC0 ? 1 : (sample[i] & 0xF0) == 0xE0 ? 2 : 3;
            if (continuation < expected) {
                return i;
            }
        }
        return end;
    }
}
