package el_guardian_ivan.desktop.crypto;

import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;
import javax.crypto.spec.SecretKeySpec;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.FilterOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.SequenceInputStream;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.security.GeneralSecurityException;
import java.security.SecureRandom;
import java.util.Arrays;
import java.util.regex.Pattern;

/**
 * Cifra ficheros con AES-256-GCM y los guarda en el contenedor .hgw.
 *
 * Cabecera común (33 bytes): "HOG2" (4) | versión (1) | sal (16) | nonce (12)
 *
 * El contenido que se cifra no es solo el fichero: delante va la extensión original,
 * para que viaje cifrada y no se vea desde fuera:
 * longitud de la extensión N (1 byte) | extensión en UTF-8 sin punto (N bytes) | bytes del fichero
 * N puede ser 0 si el fichero no tenía extensión.
 *
 * Versión 1, para ficheros pequeños (hasta {@link #DEFAULT_CHUNK_THRESHOLD}):
 * cabecera | cuerpo cifrado | etiqueta GCM (16). La cabecera va como AAD.
 * Java guarda todo el cuerpo en memoria al descifrar hasta comprobar la etiqueta.
 *
 * Versión 2, para ficheros grandes, cifrada por trozos ("chunks"):
 * cabecera | tamaño de trozo (4, big-endian) | trozo 0 | trozo 1 | ... | último trozo
 * Cada trozo es: hasta "tamaño de trozo" bytes cifrados + su propia etiqueta GCM (16).
 * - El nonce del trozo i es el nonce de la cabecera con sus 4 últimos bytes XOR i.
 * - El AAD del trozo i es: cabecera completa (37 bytes) | i (8 bytes) | 1 si es el último, 0 si no.
 * - El último trozo siempre tiene menos bytes que el tamaño de trozo (puede tener 0).
 * Así, si alguien reordena, quita o añade trozos, el descifrado falla.
 * Al descifrar solo hay un trozo en memoria cada vez.
 */
public final class AesFileCipher {

    static final byte[] MAGIC = "HOG2".getBytes(StandardCharsets.US_ASCII);
    static final byte VERSION_SINGLE = 1;
    static final byte VERSION_CHUNKED = 2;
    static final int SALT_LENGTH = 16;
    static final int NONCE_LENGTH = 12;
    static final int TAG_LENGTH = 16;
    static final int HEADER_LENGTH = MAGIC.length + 1 + SALT_LENGTH + NONCE_LENGTH;
    static final int CHUNKED_HEADER_LENGTH = HEADER_LENGTH + 4;
    static final int PBKDF2_ITERATIONS = 200_000;
    static final int KEY_LENGTH_BITS = 256;
    static final long DEFAULT_CHUNK_THRESHOLD = 64L * 1024 * 1024;
    static final int DEFAULT_CHUNK_SIZE = 1024 * 1024;
    private static final int MAX_CHUNK_SIZE = 64 * 1024 * 1024;
    private static final int BUFFER_SIZE = 64 * 1024;
    /** Solo letras, números, "_" y "-": así una extensión nunca puede convertirse en una ruta. */
    private static final Pattern SAFE_EXTENSION = Pattern.compile("[A-Za-z0-9_-]{1,32}");

    private final SecureRandom random = new SecureRandom();
    private final long chunkThreshold;
    private final int chunkSize;

    public AesFileCipher() {
        this(DEFAULT_CHUNK_THRESHOLD, DEFAULT_CHUNK_SIZE);
    }

    /** Para los tests: permite probar el modo por trozos con ficheros pequeños. */
    AesFileCipher(long chunkThreshold, int chunkSize) {
        this.chunkThreshold = chunkThreshold;
        this.chunkSize = chunkSize;
    }

    /**
     * Cifra {@code input} y escribe el contenedor en {@code output}.
     * Primero escribe en un fichero temporal junto a la salida y solo al terminar bien
     * lo mueve a su sitio, para no dejar ficheros a medias. El original no se modifica.
     */
    public void encrypt(Path input, Path output, char[] password) throws IOException, GeneralSecurityException {
        checkArguments(input, output, password);
        boolean chunked = Files.size(input) > chunkThreshold;
        byte[] metadata = buildMetadata(input);

        byte[] salt = randomBytes(SALT_LENGTH);
        byte[] nonce = randomBytes(NONCE_LENGTH);
        byte[] header = buildHeader(chunked ? VERSION_CHUNKED : VERSION_SINGLE, salt, nonce);
        SecretKey key = deriveKey(password, salt);

        writeAtomically(output, out -> {
            try (InputStream in = new SequenceInputStream(
                    new ByteArrayInputStream(metadata), Files.newInputStream(input))) {
                if (chunked) {
                    encryptChunked(in, out, key, header, nonce);
                } else {
                    encryptSingle(in, out, key, header, nonce);
                }
            }
        });
    }

    /**
     * Descifra el contenedor {@code input} y escribe los bytes originales en {@code output}.
     * Si la contraseña es incorrecta o el fichero está alterado, GCM lanza
     * {@link javax.crypto.AEADBadTagException} y no se publica ninguna salida.
     *
     * @return la extensión original sin punto (por ejemplo "ova"), o "" si no tenía
     */
    public String decrypt(Path input, Path output, char[] password) throws IOException, GeneralSecurityException {
        checkArguments(input, output, password);
        MetadataStrippingOutputStream[] stripper = new MetadataStrippingOutputStream[1];

        writeAtomically(output, fileOut -> {
            MetadataStrippingOutputStream out = new MetadataStrippingOutputStream(fileOut);
            stripper[0] = out;
            try (InputStream in = Files.newInputStream(input)) {
                byte[] header = in.readNBytes(HEADER_LENGTH);
                byte version = checkHeader(header);
                byte[] salt = Arrays.copyOfRange(header, MAGIC.length + 1, MAGIC.length + 1 + SALT_LENGTH);
                byte[] nonce = Arrays.copyOfRange(header, MAGIC.length + 1 + SALT_LENGTH, HEADER_LENGTH);
                SecretKey key = deriveKey(password, salt);
                if (version == VERSION_CHUNKED) {
                    decryptChunked(in, out, key, header, nonce);
                } else {
                    decryptSingle(in, out, key, header, nonce);
                }
            }
            out.checkComplete();
        });
        return stripper[0].extension();
    }

    private static void encryptSingle(InputStream in, OutputStream out, SecretKey key, byte[] header, byte[] nonce)
            throws IOException, GeneralSecurityException {
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.ENCRYPT_MODE, key, new GCMParameterSpec(TAG_LENGTH * 8, nonce));
        cipher.updateAAD(header);
        out.write(header);
        byte[] buffer = new byte[BUFFER_SIZE];
        int read;
        while ((read = in.read(buffer)) != -1) {
            byte[] chunk = cipher.update(buffer, 0, read);
            if (chunk != null) {
                out.write(chunk);
            }
        }
        out.write(cipher.doFinal()); // Último bloque + etiqueta GCM
    }

    private static void decryptSingle(InputStream in, OutputStream out, SecretKey key, byte[] header, byte[] nonce)
            throws IOException, GeneralSecurityException {
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(TAG_LENGTH * 8, nonce));
        cipher.updateAAD(header);
        byte[] buffer = new byte[BUFFER_SIZE];
        int read;
        while ((read = in.read(buffer)) != -1) {
            byte[] chunk = cipher.update(buffer, 0, read);
            if (chunk != null) {
                out.write(chunk);
            }
        }
        out.write(cipher.doFinal()); // Aquí se comprueba la etiqueta GCM
    }

    private void encryptChunked(InputStream in, OutputStream out, SecretKey key, byte[] header, byte[] nonce)
            throws IOException, GeneralSecurityException {
        byte[] fullHeader = ByteBuffer.allocate(CHUNKED_HEADER_LENGTH).put(header).putInt(chunkSize).array();
        out.write(fullHeader);
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        long index = 0;
        boolean last = false;
        while (!last) {
            byte[] plain = in.readNBytes(chunkSize);
            last = plain.length < chunkSize; // Un trozo incompleto (o vacío) marca el final
            cipher.init(Cipher.ENCRYPT_MODE, key, new GCMParameterSpec(TAG_LENGTH * 8, chunkNonce(nonce, index)));
            cipher.updateAAD(chunkAad(fullHeader, index, last));
            out.write(cipher.doFinal(plain));
            index++;
        }
    }

    private static void decryptChunked(InputStream in, OutputStream out, SecretKey key, byte[] header, byte[] nonce)
            throws IOException, GeneralSecurityException {
        byte[] sizeBytes = in.readNBytes(4);
        if (sizeBytes.length < 4) {
            throw new IOException("El fichero .hgw está incompleto.");
        }
        int chunkSize = ByteBuffer.wrap(sizeBytes).getInt();
        if (chunkSize <= 0 || chunkSize > MAX_CHUNK_SIZE) {
            throw new IOException("El fichero .hgw tiene un tamaño de trozo no válido.");
        }
        byte[] fullHeader = ByteBuffer.allocate(CHUNKED_HEADER_LENGTH).put(header).put(sizeBytes).array();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        long index = 0;
        boolean last = false;
        while (!last) {
            byte[] encrypted = in.readNBytes(chunkSize + TAG_LENGTH);
            last = encrypted.length < chunkSize + TAG_LENGTH;
            cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(TAG_LENGTH * 8, chunkNonce(nonce, index)));
            cipher.updateAAD(chunkAad(fullHeader, index, last));
            out.write(cipher.doFinal(encrypted)); // Cada trozo se comprueba por separado
            index++;
        }
    }

    private static byte[] chunkNonce(byte[] baseNonce, long index) throws IOException {
        if (index > 0xFFFF_FFFFL) {
            throw new IOException("El fichero es demasiado grande.");
        }
        byte[] nonce = baseNonce.clone();
        int counter = (int) index;
        for (int i = 0; i < 4; i++) {
            nonce[NONCE_LENGTH - 1 - i] ^= (byte) (counter >>> (8 * i));
        }
        return nonce;
    }

    private static byte[] chunkAad(byte[] fullHeader, long index, boolean last) {
        return ByteBuffer.allocate(fullHeader.length + 8 + 1)
                .put(fullHeader)
                .putLong(index)
                .put((byte) (last ? 1 : 0))
                .array();
    }

    /** Prepara el prefijo con la extensión: longitud (1 byte) + extensión en UTF-8. */
    private static byte[] buildMetadata(Path input) {
        String name = input.getFileName().toString();
        int dot = name.lastIndexOf('.');
        String extension = dot > 0 ? name.substring(dot + 1) : "";
        if (!SAFE_EXTENSION.matcher(extension).matches()) {
            extension = ""; // Extensión rara o inexistente: no la guardamos
        }
        byte[] ext = extension.getBytes(StandardCharsets.UTF_8);
        byte[] metadata = new byte[1 + ext.length];
        metadata[0] = (byte) ext.length;
        System.arraycopy(ext, 0, metadata, 1, ext.length);
        return metadata;
    }

    /**
     * Recibe el contenido descifrado, se queda con el prefijo de la extensión
     * y deja pasar al fichero solo los bytes originales.
     */
    private static final class MetadataStrippingOutputStream extends FilterOutputStream {
        private int extensionLength = -1; // -1: todavía no hemos leído el byte de longitud
        private final ByteArrayOutputStream extension = new ByteArrayOutputStream();

        MetadataStrippingOutputStream(OutputStream out) {
            super(out);
        }

        @Override
        public void write(int b) throws IOException {
            write(new byte[]{(byte) b}, 0, 1);
        }

        @Override
        public void write(byte[] b, int off, int len) throws IOException {
            if (extensionLength < 0 && len > 0) {
                extensionLength = b[off] & 0xFF;
                off++;
                len--;
            }
            int missing = Math.max(0, extensionLength - extension.size());
            int toExtension = Math.min(missing, len);
            extension.write(b, off, toExtension);
            out.write(b, off + toExtension, len - toExtension);
        }

        void checkComplete() throws IOException {
            if (extensionLength < 0 || extension.size() < extensionLength) {
                throw new IOException("El fichero .hgw está incompleto.");
            }
        }

        String extension() {
            String ext = extension.toString(StandardCharsets.UTF_8);
            return SAFE_EXTENSION.matcher(ext).matches() ? ext : "";
        }
    }

    private static void checkArguments(Path input, Path output, char[] password) throws IOException {
        if (password == null || password.length == 0) {
            throw new IllegalArgumentException("La contraseña no puede estar vacía.");
        }
        if (Files.exists(output) && Files.isSameFile(input, output)) {
            throw new IllegalArgumentException("El fichero de salida no puede ser el mismo que el de entrada.");
        }
    }

    private static byte checkHeader(byte[] header) throws IOException {
        if (header.length < HEADER_LENGTH
                || !Arrays.equals(Arrays.copyOf(header, MAGIC.length), MAGIC)) {
            throw new IOException("El fichero no es un contenedor .hgw válido.");
        }
        byte version = header[MAGIC.length];
        if (version != VERSION_SINGLE && version != VERSION_CHUNKED) {
            throw new IOException("Versión de contenedor .hgw no admitida: " + version);
        }
        return version;
    }

    /** Escribe en un temporal junto a {@code output} y solo lo mueve a su sitio si todo ha ido bien. */
    private static void writeAtomically(Path output, StreamWriter writer) throws IOException, GeneralSecurityException {
        Path parent = output.toAbsolutePath().getParent();
        Path temp = Files.createTempFile(parent, ".hgw-", ".tmp");
        boolean success = false;
        try {
            try (OutputStream out = Files.newOutputStream(temp)) {
                writer.write(out);
            }
            Files.move(temp, output, StandardCopyOption.REPLACE_EXISTING);
            success = true;
        } finally {
            if (!success) {
                Files.deleteIfExists(temp);
            }
        }
    }

    @FunctionalInterface
    private interface StreamWriter {
        void write(OutputStream out) throws IOException, GeneralSecurityException;
    }

    static SecretKey deriveKey(char[] password, byte[] salt) throws GeneralSecurityException {
        PBEKeySpec spec = new PBEKeySpec(password, salt, PBKDF2_ITERATIONS, KEY_LENGTH_BITS);
        try {
            byte[] keyBytes = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).getEncoded();
            return new SecretKeySpec(keyBytes, "AES");
        } finally {
            spec.clearPassword();
        }
    }

    private static byte[] buildHeader(byte version, byte[] salt, byte[] nonce) {
        byte[] header = Arrays.copyOf(MAGIC, HEADER_LENGTH);
        header[MAGIC.length] = version;
        System.arraycopy(salt, 0, header, MAGIC.length + 1, SALT_LENGTH);
        System.arraycopy(nonce, 0, header, MAGIC.length + 1 + SALT_LENGTH, NONCE_LENGTH);
        return header;
    }

    private byte[] randomBytes(int length) {
        byte[] bytes = new byte[length];
        random.nextBytes(bytes);
        return bytes;
    }
}
