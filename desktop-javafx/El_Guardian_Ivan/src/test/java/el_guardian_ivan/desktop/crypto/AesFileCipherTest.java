package el_guardian_ivan.desktop.crypto;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import javax.crypto.AEADBadTagException;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.Random;

import static org.junit.jupiter.api.Assertions.*;

class AesFileCipherTest {

    @TempDir
    Path dir;

    private final AesFileCipher cipher = new AesFileCipher();

    @Test
    void encryptWritesHeaderAndKeepsOriginal() throws Exception {
        Path input = dir.resolve("mensaje.txt");
        byte[] original = "Travesura realizada".getBytes();
        Files.write(input, original);
        Path output = dir.resolve("mensaje.hgw");

        cipher.encrypt(input, output, "alohomora".toCharArray());

        byte[] encrypted = Files.readAllBytes(output);
        assertArrayEquals(AesFileCipher.MAGIC, Arrays.copyOf(encrypted, 4));
        assertEquals(AesFileCipher.VERSION_SINGLE, encrypted[4]);
        // +4: prefijo cifrado con la extensión (1 byte de longitud + "txt")
        assertEquals(AesFileCipher.HEADER_LENGTH + 4 + original.length + 16, encrypted.length);
        assertArrayEquals(original, Files.readAllBytes(input));
    }

    @Test
    void encryptEmptyFileProducesValidContainer() throws Exception {
        Path input = Files.createFile(dir.resolve("vacio.bin"));
        Path output = dir.resolve("vacio.hgw");

        cipher.encrypt(input, output, "alohomora".toCharArray());

        assertEquals(AesFileCipher.HEADER_LENGTH + 4 + 16, Files.size(output));
    }

    @Test
    void encryptRejectsSameInputAndOutput() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");

        assertThrows(IllegalArgumentException.class,
                () -> cipher.encrypt(input, input, "alohomora".toCharArray()));
        assertEquals("hola", Files.readString(input));
    }

    @Test
    void encryptRejectsEmptyPassword() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");

        assertThrows(IllegalArgumentException.class,
                () -> cipher.encrypt(input, dir.resolve("a.hgw"), new char[0]));
    }

    @ParameterizedTest
    @ValueSource(ints = {0, 1, 1024 * 1024, 20 * 1024 * 1024})
    void decryptRecoversExactBytes(int size) throws Exception {
        byte[] original = new byte[size];
        new Random(size).nextBytes(original);
        Path input = Files.write(dir.resolve("original.bin"), original);
        Path encrypted = dir.resolve("original.hgw");
        Path recovered = dir.resolve("recuperado.bin");

        cipher.encrypt(input, encrypted, "alohomora".toCharArray());
        cipher.decrypt(encrypted, recovered, "alohomora".toCharArray());

        assertArrayEquals(original, Files.readAllBytes(recovered));
    }

    @Test
    void decryptWithWrongPasswordFailsAndWritesNothing() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        Path recovered = dir.resolve("b.txt");
        cipher.encrypt(input, encrypted, "alohomora".toCharArray());

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, recovered, "otra".toCharArray()));
        assertFalse(Files.exists(recovered));
        assertEquals(2, countFiles()); // Ni salida ni temporales
    }

    @Test
    void decryptTamperedFileFails() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, "alohomora".toCharArray());
        byte[] bytes = Files.readAllBytes(encrypted);
        bytes[AesFileCipher.HEADER_LENGTH] ^= 1;
        Files.write(encrypted, bytes);

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.txt"), "alohomora".toCharArray()));
    }

    @Test
    void decryptRejectsFileWithoutHeader() throws Exception {
        Path notHgw = Files.writeString(dir.resolve("normal.txt"), "esto no es un hgw");

        assertThrows(IOException.class,
                () -> cipher.decrypt(notHgw, dir.resolve("b.txt"), "alohomora".toCharArray()));
        assertFalse(Files.exists(dir.resolve("b.txt")));
    }

    private long countFiles() throws IOException {
        try (var files = Files.list(dir)) {
            return files.count();
        }
    }

    // --- Versión 2: por trozos. Usamos trozos de 1 KiB para probarlo con ficheros pequeños ---

    private final AesFileCipher chunkedCipher = new AesFileCipher(-1, 1024); // Umbral -1: siempre por trozos

    @ParameterizedTest
    @ValueSource(ints = {0, 1, 1023, 1024, 1025, 5 * 1024, 300_000})
    void chunkedRecoversExactBytes(int size) throws Exception {
        byte[] original = new byte[size];
        new Random(size).nextBytes(original);
        Path input = Files.write(dir.resolve("original.bin"), original);
        Path encrypted = dir.resolve("original.hgw");
        Path recovered = dir.resolve("recuperado.bin");

        chunkedCipher.encrypt(input, encrypted, "alohomora".toCharArray());
        byte[] container = Files.readAllBytes(encrypted);
        assertEquals(AesFileCipher.VERSION_CHUNKED, container[4]);
        int content = 4 + size; // Prefijo con "bin" + datos
        int chunks = content / 1024 + 1;
        assertEquals(AesFileCipher.CHUNKED_HEADER_LENGTH + content + chunks * AesFileCipher.TAG_LENGTH, container.length);

        // Se descifra con el constructor normal: el tamaño de trozo se lee del fichero
        cipher.decrypt(encrypted, recovered, "alohomora".toCharArray());
        assertArrayEquals(original, Files.readAllBytes(recovered));
    }

    @Test
    void chunkedWithWrongPasswordFails() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), new byte[3000]);
        Path encrypted = dir.resolve("a.hgw");
        chunkedCipher.encrypt(input, encrypted, "alohomora".toCharArray());

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.bin"), "otra".toCharArray()));
        assertFalse(Files.exists(dir.resolve("b.bin")));
    }

    @Test
    void chunkedTruncatedFileFails() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), new byte[3000]);
        Path encrypted = dir.resolve("a.hgw");
        chunkedCipher.encrypt(input, encrypted, "alohomora".toCharArray());
        byte[] bytes = Files.readAllBytes(encrypted);
        // Quitamos el último trozo entero: sin la marca de "último" debe fallar
        int lastChunk = (4 + 3000) - 2 * 1024 + AesFileCipher.TAG_LENGTH;
        Files.write(encrypted, Arrays.copyOf(bytes, bytes.length - lastChunk));

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.bin"), "alohomora".toCharArray()));
        assertFalse(Files.exists(dir.resolve("b.bin")));
    }

    @Test
    void chunkedSwappedChunksFail() throws Exception {
        byte[] original = new byte[3000];
        new Random(1).nextBytes(original);
        Path input = Files.write(dir.resolve("a.bin"), original);
        Path encrypted = dir.resolve("a.hgw");
        chunkedCipher.encrypt(input, encrypted, "alohomora".toCharArray());
        byte[] bytes = Files.readAllBytes(encrypted);
        int start = AesFileCipher.CHUNKED_HEADER_LENGTH;
        int len = 1024 + AesFileCipher.TAG_LENGTH;
        byte[] first = Arrays.copyOfRange(bytes, start, start + len);
        System.arraycopy(bytes, start + len, bytes, start, len);
        System.arraycopy(first, 0, bytes, start + len, len);
        Files.write(encrypted, bytes);

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.bin"), "alohomora".toCharArray()));
    }

    // --- Extensión original guardada cifrada ---

    @Test
    void decryptReturnsOriginalExtension() throws Exception {
        Path input = Files.write(dir.resolve("maquina.ova"), new byte[]{1, 2, 3});
        Path encrypted = dir.resolve("maquina.hgw");
        cipher.encrypt(input, encrypted, "alohomora".toCharArray());

        assertEquals("ova", cipher.decrypt(encrypted, dir.resolve("salida"), "alohomora".toCharArray()));
        assertArrayEquals(new byte[]{1, 2, 3}, Files.readAllBytes(dir.resolve("salida")));
        assertEquals("ova", chunkedDecryptExtension(input));
    }

    @Test
    void fileWithoutExtensionReturnsEmpty() throws Exception {
        Path input = Files.writeString(dir.resolve("LEEME"), "hola");
        Path encrypted = dir.resolve("leeme.hgw");
        cipher.encrypt(input, encrypted, "alohomora".toCharArray());

        assertEquals("", cipher.decrypt(encrypted, dir.resolve("salida"), "alohomora".toCharArray()));
        assertEquals("hola", Files.readString(dir.resolve("salida")));
    }

    @Test
    void extensionIsNotVisibleInContainer() throws Exception {
        Path input = Files.writeString(dir.resolve("secreto.extensionvisible"), "x");
        Path encrypted = dir.resolve("secreto.hgw");
        cipher.encrypt(input, encrypted, "alohomora".toCharArray());

        String raw = new String(Files.readAllBytes(encrypted), java.nio.charset.StandardCharsets.ISO_8859_1);
        assertFalse(raw.contains("extensionvisible"));
    }

    private String chunkedDecryptExtension(Path input) throws Exception {
        Path encrypted = dir.resolve("trozos.hgw");
        chunkedCipher.encrypt(input, encrypted, "alohomora".toCharArray());
        return cipher.decrypt(encrypted, dir.resolve("trozos.out"), "alohomora".toCharArray());
    }
}
