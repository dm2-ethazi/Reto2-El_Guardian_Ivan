package el_guardian_ivan.desktop.crypto;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import javax.crypto.AEADBadTagException;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.NoSuchFileException;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HexFormat;
import java.util.List;
import java.util.Random;

import static org.junit.jupiter.api.Assertions.*;

/** Casos límite: nombres raros, rutas imposibles, ficheros corruptos, progreso y recuperación exacta. */
class AesFileCipherEdgeCasesTest {

    private static final char[] PASSWORD = "alohomora".toCharArray();

    @TempDir
    Path dir;

    private final AesFileCipher cipher = new AesFileCipher();
    private final AesFileCipher smallChunks = new AesFileCipher(1024);

    // --- Nombres de fichero ---

    @ParameterizedTest
    @ValueSource(strings = {
            "con espacios.txt", "acentos ñandú €.txt", "varios.puntos.tar.gz", "sin_extension",
            ".oculto", "MAYUSCULAS.PNG", "terminado.en.punto.", "emoji 🧙.bin"
    })
    void unusualNamesRoundTrip(String name) throws Exception {
        Path input = Files.writeString(dir.resolve(name), "contenido de " + name);
        Path encrypted = dir.resolve("salida.hgw");
        Path recovered = dir.resolve("recuperado");

        cipher.encrypt(input, encrypted, PASSWORD.clone());
        String extension = cipher.decrypt(encrypted, recovered, PASSWORD.clone());

        assertEquals(Files.readString(input), Files.readString(recovered));
        assertTrue(extension.matches("[A-Za-z0-9_-]{0,32}"), "extensión segura: " + extension);
    }

    @Test
    void extensionsAreRecoveredOrDropped() throws Exception {
        assertEquals("gz", roundTripExtension("varios.puntos.tar.gz"));
        assertEquals("PNG", roundTripExtension("MAYUSCULAS.PNG"));
        assertEquals("", roundTripExtension(".oculto"));       // Fichero oculto: no es extensión
        assertEquals("", roundTripExtension("terminado.en.punto."));
        assertEquals("", roundTripExtension("raro.ext con espacio"));
        assertEquals("", roundTripExtension("larga." + "x".repeat(40)));
    }

    // --- Rutas imposibles ---

    @Test
    void missingInputFails() {
        assertThrows(NoSuchFileException.class,
                () -> cipher.encrypt(dir.resolve("no-existe.txt"), dir.resolve("a.hgw"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("a.hgw")));
    }

    @Test
    void directoryAsInputFails() throws Exception {
        Path folder = Files.createDirectory(dir.resolve("carpeta"));
        assertThrows(IOException.class, () -> cipher.encrypt(folder, dir.resolve("a.hgw"), PASSWORD.clone()));
        assertEquals(1, countFiles(dir)); // Solo la carpeta, sin temporales
    }

    @Test
    void outputInMissingFolderFails() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        assertThrows(IOException.class,
                () -> cipher.encrypt(input, dir.resolve("no-existe").resolve("a.hgw"), PASSWORD.clone()));
    }

    @Test
    void outputInsideAFileFails() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        // La "carpeta" de destino es en realidad un fichero: no se puede escribir dentro
        assertThrows(IOException.class,
                () -> cipher.encrypt(input, input.resolve("a.hgw"), PASSWORD.clone()));
        assertEquals("hola", Files.readString(input));
    }

    @Test
    void existingOutputIsKeptWhenDecryptionFails() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        Path existing = Files.writeString(dir.resolve("importante.txt"), "no me borres");

        assertThrows(AEADBadTagException.class, () -> cipher.decrypt(encrypted, existing, "otra".toCharArray()));
        assertEquals("no me borres", Files.readString(existing));
    }

    @Test
    void decryptingOverItsOwnInputIsRejected() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        byte[] before = Files.readAllBytes(encrypted);

        assertThrows(IllegalArgumentException.class, () -> cipher.decrypt(encrypted, encrypted, PASSWORD.clone()));
        assertArrayEquals(before, Files.readAllBytes(encrypted));
    }

    // --- Ficheros vacíos y corruptos ---

    @Test
    void emptyContainerIsRejected() throws Exception {
        Path empty = Files.createFile(dir.resolve("vacio.hgw"));
        assertThrows(IOException.class, () -> cipher.decrypt(empty, dir.resolve("b"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("b")));
    }

    @Test
    void headerOnlyContainerIsRejected() throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        Files.write(encrypted, Arrays.copyOf(Files.readAllBytes(encrypted), AesFileCipher.HEADER_LENGTH));

        assertThrows(AEADBadTagException.class, () -> cipher.decrypt(encrypted, dir.resolve("b"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("b")));
    }

    @ParameterizedTest
    @ValueSource(ints = {1, 4, 20, 36})
    void truncatedHeaderIsRejected(int length) throws Exception {
        Path input = Files.writeString(dir.resolve("a.txt"), "hola");
        Path encrypted = dir.resolve("a.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        Files.write(encrypted, Arrays.copyOf(Files.readAllBytes(encrypted), length));

        assertThrows(IOException.class, () -> cipher.decrypt(encrypted, dir.resolve("b"), PASSWORD.clone()));
    }

    @Test
    void wrongVersionIsRejected() throws Exception {
        Path encrypted = validContainer();
        byte[] bytes = Files.readAllBytes(encrypted);
        bytes[4] = 9;
        Files.write(encrypted, bytes);

        IOException e = assertThrows(IOException.class,
                () -> cipher.decrypt(encrypted, dir.resolve("b"), PASSWORD.clone()));
        assertTrue(e.getMessage().contains("Versión"));
    }

    @ParameterizedTest
    @ValueSource(ints = {0, -1, 64 * 1024 * 1024 + 1, Integer.MAX_VALUE})
    void invalidChunkSizeIsRejected(int chunkSize) throws Exception {
        Path encrypted = validContainer();
        byte[] bytes = Files.readAllBytes(encrypted);
        ByteBuffer.wrap(bytes, 33, 4).putInt(chunkSize);
        Files.write(encrypted, bytes);

        assertThrows(IOException.class, () -> cipher.decrypt(encrypted, dir.resolve("b"), PASSWORD.clone()));
    }

    @Test
    void randomBytesWithMagicAreRejected() throws Exception {
        byte[] bytes = new byte[5000];
        new Random(3).nextBytes(bytes);
        System.arraycopy("HOG2".getBytes(StandardCharsets.US_ASCII), 0, bytes, 0, 4);
        bytes[4] = 1;
        ByteBuffer.wrap(bytes, 33, 4).putInt(1024);
        Path corrupt = Files.write(dir.resolve("corrupto.hgw"), bytes);

        assertThrows(AEADBadTagException.class, () -> cipher.decrypt(corrupt, dir.resolve("b"), PASSWORD.clone()));
        assertFalse(Files.exists(dir.resolve("b")));
    }

    @Test
    void everySingleFlippedByteIsDetected() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(2500));
        Path encrypted = dir.resolve("a.hgw");
        smallChunks.encrypt(input, encrypted, PASSWORD.clone());
        byte[] original = Files.readAllBytes(encrypted);

        for (int i = 0; i < original.length; i += 97) { // Muestreo por todo el fichero, cabecera incluida
            byte[] bytes = original.clone();
            bytes[i] ^= 0x01;
            Files.write(encrypted, bytes);
            int position = i;
            assertThrows(Exception.class, () -> cipher.decrypt(encrypted, dir.resolve("b"), PASSWORD.clone()),
                    "byte " + position);
            assertFalse(Files.exists(dir.resolve("b")), "byte " + position);
        }
    }

    @Test
    void extraBytesAtTheEndAreDetected() throws Exception {
        Path encrypted = validContainer();
        byte[] bytes = Files.readAllBytes(encrypted);
        Files.write(encrypted, Arrays.copyOf(bytes, bytes.length + 10));

        assertThrows(AEADBadTagException.class, () -> cipher.decrypt(encrypted, dir.resolve("b"), PASSWORD.clone()));
    }

    // --- Progreso ---

    @Test
    void progressIsMonotonicAndReachesTheEnd() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(3 * 1024 * 1024 + 7));
        long size = Files.size(input);
        List<Long> values = new ArrayList<>();

        cipher.encrypt(input, dir.resolve("a.hgw"), PASSWORD.clone(), (done, total) -> {
            assertEquals(size, total);
            values.add(done);
        });

        assertTrue(values.size() >= 3, "varios avisos de progreso");
        for (int i = 1; i < values.size(); i++) {
            assertTrue(values.get(i) >= values.get(i - 1));
        }
        assertEquals(Files.size(input), values.get(values.size() - 1));
    }

    // --- Recuperación exacta ---

    @Test
    void recoversAllByteValuesAndPatterns() throws Exception {
        byte[][] samples = {
                allByteValues(),
                new byte[1024 * 1024],                     // Todo ceros, justo 1 MiB
                "línea 1\r\nlínea 2\n\tfin".getBytes(StandardCharsets.UTF_8),
                {(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'},
                randomBytes(1024 * 1024 - 4),              // Prefijo + datos = exactamente 1 trozo
                randomBytes(1024 * 1024 - 3)               // Un byte más: obliga a un segundo trozo
        };
        for (byte[] original : samples) {
            Path input = Files.write(dir.resolve("o.bin"), original);
            cipher.encrypt(input, dir.resolve("o.hgw"), PASSWORD.clone());
            cipher.decrypt(dir.resolve("o.hgw"), dir.resolve("r.bin"), PASSWORD.clone());
            assertEquals(sha256(original), sha256(Files.readAllBytes(dir.resolve("r.bin"))));
            assertArrayEquals(original, Files.readAllBytes(dir.resolve("r.bin")));
        }
    }

    @Test
    void sameFileEncryptedTwiceGivesDifferentContainersButSameResult() throws Exception {
        Path input = Files.write(dir.resolve("a.bin"), randomBytes(10_000));
        cipher.encrypt(input, dir.resolve("1.hgw"), PASSWORD.clone());
        cipher.encrypt(input, dir.resolve("2.hgw"), PASSWORD.clone());
        assertFalse(Arrays.equals(Files.readAllBytes(dir.resolve("1.hgw")), Files.readAllBytes(dir.resolve("2.hgw"))));

        cipher.decrypt(dir.resolve("1.hgw"), dir.resolve("1.bin"), PASSWORD.clone());
        cipher.decrypt(dir.resolve("2.hgw"), dir.resolve("2.bin"), PASSWORD.clone());
        assertArrayEquals(Files.readAllBytes(dir.resolve("1.bin")), Files.readAllBytes(dir.resolve("2.bin")));
    }

    private String roundTripExtension(String name) throws Exception {
        Path input = Files.writeString(dir.resolve(name), "x");
        cipher.encrypt(input, dir.resolve("e.hgw"), PASSWORD.clone());
        return cipher.decrypt(dir.resolve("e.hgw"), dir.resolve("e.out"), PASSWORD.clone());
    }

    private Path validContainer() throws Exception {
        Path input = Files.writeString(dir.resolve("valido.txt"), "hola");
        Path encrypted = dir.resolve("valido.hgw");
        cipher.encrypt(input, encrypted, PASSWORD.clone());
        return encrypted;
    }

    private static byte[] allByteValues() {
        byte[] bytes = new byte[256];
        for (int i = 0; i < 256; i++) {
            bytes[i] = (byte) i;
        }
        return bytes;
    }

    private static byte[] randomBytes(int size) {
        byte[] bytes = new byte[size];
        new Random(size).nextBytes(bytes);
        return bytes;
    }

    private static String sha256(byte[] data) throws Exception {
        return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(data));
    }

    private static long countFiles(Path folder) throws IOException {
        try (var files = Files.list(folder)) {
            return files.count();
        }
    }
}
