package el_guardian_ivan.desktop.crypto;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import javax.crypto.AEADBadTagException;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.DigestInputStream;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.HexFormat;
import java.util.Random;

import static org.junit.jupiter.api.Assertions.*;

class AesFileCipherTest {

    private static final char[] PASSWORD = "alohomora".toCharArray();
    private static final int TAG = AesFileCipher.TAG_LENGTH;

    @TempDir
    Path dir;

    private final AesFileCipher cipher = new AesFileCipher();
    // Trozos de 1 KiB para probar varios trozos con ficheros pequeños
    private final AesFileCipher smallChunks = new AesFileCipher(1024);

    // --- Recuperación exacta (propuesta 6.5: 0 B, 1 B, 1 MiB y 20 MiB, comparando con SHA-256) ---

    @ParameterizedTest
    @ValueSource(ints = {0, 1, 1024 * 1024, 20 * 1024 * 1024})
    void decryptRecoversExactBytes(int size) throws Exception {
        byte[] original = randomBytes(size);
        Path input = Files.write(dir.resolve("original.bin"), original);
        Path encrypted = dir.resolve("original.hgw");
        Path recovered = dir.resolve("recuperado.bin");

        cipher.encrypt(input, encrypted, PASSWORD.clone());
        cipher.decrypt(encrypted, recovered, PASSWORD.clone());

        assertEquals(sha256(input), sha256(recovered));
        assertArrayEquals(original, Files.readAllBytes(recovered));
        assertArrayEquals(original, Files.readAllBytes(input)); // Original intacto
    }

    @ParameterizedTest
    @ValueSource(ints = {0, 1, 1018, 1019, 1020, 1021, 5 * 1024, 300_000})
    void severalChunksRecoverExactBytes(int size) throws Exception {
        byte[] original = randomBytes(size);
        Path input = Files.write(dir.resolve("original.bin"), original);
        Path encrypted = dir.resolve("original.hgw");
        Path recovered = dir.resolve("recuperado.bin");

        smallChunks.encrypt(input, encrypted, PASSWORD.clone());
        int content = 4 + size; // Prefijo con la extensión "bin" + datos
        int chunks = content / 1024 + 1;
        assertEquals(AesFileCipher.HEADER_LENGTH + content + chunks * TAG, Files.size(encrypted));

        // Se descifra con el constructor normal: el tamaño de trozo se lee de la cabecera
        cipher.decrypt(encrypted, recovered, PASSWORD.clone());
        assertArrayEquals(original, Files.readAllBytes(recovered));
    }

    // --- Formato ---

    @Test
    void encryptWritesHeader() throws Exception {
        Path input = Files.writeString(dir.resolve("mensaje.txt"), "Travesura realizada");
        Path output = dir.resolve("mensaje.hgw");

        cipher.encrypt(input, output, PASSWORD.clone());

        byte[] encrypted = Files.readAllBytes(output);
        assertArrayEquals(AesFileCipher.MAGIC, Arrays.copyOf(encrypted, 4));
        assertEquals(AesFileCipher.VERSION, encrypted[4]);
        // Cabecera + (1 + "txt" + 19 bytes) en un único trozo + etiqueta
        assertEquals(AesFileCipher.HEADER_LENGTH + 4 + 19 + TAG, encrypted.length);
    }

    @Test
    void eachEncryptionUsesNewSaltAndNonce() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        cipher.encrypt(input, dir.resolve("1.hgw"), PASSWORD.clone());
        cipher.encrypt(input, dir.resolve("2.hgw"), PASSWORD.clone());

        byte[] first = Arrays.copyOf(Files.readAllBytes(dir.resolve("1.hgw")), AesFileCipher.HEADER_LENGTH);
        byte[] second = Arrays.copyOf(Files.readAllBytes(dir.resolve("2.hgw")), AesFileCipher.HEADER_LENGTH);
        assertFalse(Arrays.equals(Arrays.copyOfRange(first, 5, 21), Arrays.copyOfRange(second, 5, 21)), "sal");
        assertFalse(Arrays.equals(Arrays.copyOfRange(first, 21, 33), Arrays.copyOfRange(second, 21, 33)), "nonce");
    }

    @Test
    void passwordIsNotStoredInContainer() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());

        String raw = new String(Files.readAllBytes(encrypted), StandardCharsets.ISO_8859_1);
        assertFalse(raw.contains("alohomora"));
    }

    @Test
    void reportsProgressUntilTheWholeFile() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(5000));
        Path encrypted = dir.resolve("a.hgw");
        long[] last = new long[2];

        smallChunks.encrypt(input, encrypted, PASSWORD.clone(), (done, total) -> {
            last[0] = done;
            last[1] = total;
        });
        assertEquals(5000, last[0]);
        assertEquals(5000, last[1]);

        cipher.decrypt(encrypted, dir.resolve("b.bin"), PASSWORD.clone(), (done, total) -> {
            last[0] = done;
            last[1] = total;
        });
        assertEquals(Files.size(encrypted), last[0]);
        assertEquals(Files.size(encrypted), last[1]);
    }

    // --- Extensión original guardada cifrada ---

    @Test
    void decryptReturnsOriginalExtension() throws Exception {
        Path input = Files.write(dir.resolve("maquina.ova"), new byte[]{1, 2, 3});
        Path encrypted = dir.resolve("maquina.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());

        assertEquals("ova", cipher.decrypt(encrypted, dir.resolve("salida"), PASSWORD.clone()));
        assertArrayEquals(new byte[]{1, 2, 3}, Files.readAllBytes(dir.resolve("salida")));
    }

    @Test
    void fileWithoutExtensionReturnsEmpty() throws Exception {
        Path input = Files.writeString(dir.resolve("LEEME"), "hola");
        Path encrypted = dir.resolve("leeme.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());

        assertEquals("", cipher.decrypt(encrypted, dir.resolve("salida"), PASSWORD.clone()));
        assertEquals("hola", Files.readString(dir.resolve("salida")));
    }

    @Test
    void extensionIsNotVisibleInContainer() throws Exception {
        Path input = Files.writeString(dir.resolve("secreto.extensionvisible"), "x");
        Path encrypted = dir.resolve("secreto.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());

        String raw = new String(Files.readAllBytes(encrypted), StandardCharsets.ISO_8859_1);
        assertFalse(raw.contains("extensionvisible"));
    }

    // --- Errores: no se publica nada y el original no se toca ---

    @Test
    void wrongPasswordFailsAndWritesNothing() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(3000));
        Path encrypted = dir.resolve("a.hgw");
        Path recovered = dir.resolve("b.bin");
        smallChunks.encrypt(input, encrypted, PASSWORD.clone());

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, recovered, "otra".toCharArray()));
        assertFalse(Files.exists(recovered));
        assertEquals(2, countFiles()); // Ni salida ni temporales
    }

    @Test
    void tamperedFileFails() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        byte[] bytes = Files.readAllBytes(encrypted);
        bytes[AesFileCipher.HEADER_LENGTH] ^= 1;
        Files.write(encrypted, bytes);

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.txt"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("b.txt")));
    }

    @Test
    void tamperedHeaderFails() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        byte[] bytes = Files.readAllBytes(encrypted);
        bytes[10] ^= 1; // Un byte de la sal
        Files.write(encrypted, bytes);

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.txt"), PASSWORD.clone()));
    }

    @Test
    void truncatedFileFails() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(3000));
        Path encrypted = dir.resolve("a.hgw");
        smallChunks.encrypt(input, encrypted, PASSWORD.clone());
        byte[] bytes = Files.readAllBytes(encrypted);
        // Quitamos el último trozo entero: sin la marca de "último" debe fallar
        int lastChunk = (4 + 3000) - 2 * 1024 + TAG;
        Files.write(encrypted, Arrays.copyOf(bytes, bytes.length - lastChunk));

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.bin"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("b.bin")));
    }

    @Test
    void swappedChunksFail() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(3000));
        Path encrypted = dir.resolve("a.hgw");
        smallChunks.encrypt(input, encrypted, PASSWORD.clone());
        byte[] bytes = Files.readAllBytes(encrypted);
        int start = AesFileCipher.HEADER_LENGTH;
        int len = 1024 + TAG;
        byte[] first = Arrays.copyOfRange(bytes, start, start + len);
        System.arraycopy(bytes, start + len, bytes, start, len);
        System.arraycopy(first, 0, bytes, start + len, len);
        Files.write(encrypted, bytes);

        assertThrows(AEADBadTagException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b.bin"), PASSWORD.clone()));
    }

    @Test
    void fileWithoutHeaderIsRejected() throws Exception {
        Path notHgw = Files.writeString(dir.resolve("normal.txt"), "esto no es un hgw");

        assertThrows(IOException.class,
                () -> cipher.decrypt(notHgw, dir.resolve("b.txt"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("b.txt")));
    }

    @Test
    void sameInputAndOutputIsRejected() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");

        assertThrows(IllegalArgumentException.class, () -> cipher.encrypt(input, input, PASSWORD.clone()));
        assertEquals("hola", Files.readString(input));
    }

    @Test
    void emptyPasswordIsRejected() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");

        assertThrows(IllegalArgumentException.class,
                () -> cipher.encrypt(input, dir.resolve("a.hgw"), new char[0]));
    }

    private static byte[] randomBytes(int size) {
        byte[] bytes = new byte[size];
        new Random(size).nextBytes(bytes);
        return bytes;
    }

    private static String sha256(Path file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        try (var in = new DigestInputStream(Files.newInputStream(file), digest)) {
            in.transferTo(OutputStream.nullOutputStream());
        }
        return HexFormat.of().formatHex(digest.digest());
    }

    private long countFiles() throws IOException {
        try (var files = Files.list(dir)) {
            return files.count();
        }
    }
}
