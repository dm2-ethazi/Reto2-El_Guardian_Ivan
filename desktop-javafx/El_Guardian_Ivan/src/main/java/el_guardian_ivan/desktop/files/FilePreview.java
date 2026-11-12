package el_guardian_ivan.desktop.files;

import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.charset.CharacterCodingException;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Locale;
import java.util.Set;

/**
 * Decide si un fichero descifrado se puede previsualizar (imagen o texto plano) y lee el texto.
 * No depende de JavaFX para poder probarlo con tests normales.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public final class FilePreview {
    /** Tope de texto que se muestra: suficiente para ver el contenido sin cargar ficheros enormes. */
    public static final int MAX_TEXT_BYTES = 100 * 1024;

    /** Formatos de imagen que JavaFX sabe dibujar. */
    private static final Set<String> IMAGE_EXTENSIONS = Set.of("png", "jpg", "jpeg", "gif", "bmp");

    /** Qué vista previa se puede enseñar: imagen, texto o ninguna. */
    public enum Kind {
        /** PNG, JPG, GIF o BMP: se muestra la imagen. */
        IMAGE,
        /** Texto plano en UTF-8: se muestra el texto. */
        TEXT,
        /** Cualquier otro formato: sin vista previa. */
        NONE
    }

    /**
     * Texto leído para la vista previa; {@code truncated} indica que el fichero era más largo.
     *
     * @param content texto leído
     * @param truncated {@code true} si el fichero tenía más texto
     */
    public record Text(String content, boolean truncated) {
    }

    /** Clase de utilidades: no se crean objetos. */
    private FilePreview() {
    }

    /**
     * Se fija primero en el contenido (lo fiable) y, si no lo reconoce, en la extensión.
     *
     * @param file fichero descifrado
     * @return IMAGE, TEXT o NONE
     * @throws IOException si no se puede leer el fichero
     */
    public static Kind kindOf(Path file) throws IOException {
        String detected = FileTypeDetector.detectExtension(file);
        if (IMAGE_EXTENSIONS.contains(detected)) {
            return Kind.IMAGE;
        }
        if (detected.equals("txt")) {
            return Kind.TEXT;
        }
        String name = file.getFileName().toString();
        String extension = name.substring(name.lastIndexOf('.') + 1).toLowerCase(Locale.ROOT);
        return name.contains(".") && IMAGE_EXTENSIONS.contains(extension) ? Kind.IMAGE : Kind.NONE;
    }

    /**
     * Lee como mucho {@code maxBytes} en UTF-8; lo que no sea válido se sustituye por "�".
     *
     * @param file fichero de texto
     * @param maxBytes cuántos bytes leer como mucho
     * @return el texto y si se ha recortado
     * @throws IOException si no se puede leer el fichero
     */
    public static Text readText(Path file, int maxBytes) throws IOException {
        byte[] data;
        boolean truncated;
        try (InputStream in = Files.newInputStream(file)) {
            data = in.readNBytes(maxBytes);
            truncated = in.read() != -1; // Si queda algo más, el fichero era más largo que el tope
        }
        // Si se ha cortado, el último carácter puede haber quedado a medias: se descarta
        int length = truncated ? FileTypeDetector.trimIncompleteUtf8(data) : data.length;
        int start = length >= 3 && (data[0] & 0xFF) == 0xEF && (data[1] & 0xFF) == 0xBB && (data[2] & 0xFF) == 0xBF
                ? 3 : 0; // Quita la marca BOM que añaden algunos editores de Windows
        try {
            String content = StandardCharsets.UTF_8.newDecoder()
                    .onMalformedInput(CodingErrorAction.REPLACE)
                    .onUnmappableCharacter(CodingErrorAction.REPLACE)
                    .decode(ByteBuffer.wrap(data, start, length - start))
                    .toString();
            return new Text(content, truncated);
        } catch (CharacterCodingException e) {
            throw new IOException(e); // Con REPLACE no ocurre, pero el compilador lo exige
        }
    }
}
