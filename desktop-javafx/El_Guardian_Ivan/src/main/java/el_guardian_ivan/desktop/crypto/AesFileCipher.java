package el_guardian_ivan.desktop.crypto;

import el_guardian_ivan.desktop.i18n.I18n;
import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;
import javax.crypto.spec.SecretKeySpec;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.FilterInputStream;
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
 * El formato completo está documentado en docs/formato-hgw.md.
 *
 * Cabecera (37 bytes): "HOG2" (4) | versión 1 (1) | sal (16) | nonce base (12) | tamaño de trozo (4, big-endian)
 *
 * Después vienen los trozos. Cada trozo es: hasta "tamaño de trozo" bytes cifrados + su etiqueta GCM (16).
 * - El nonce del trozo i es el nonce base con sus 4 últimos bytes XOR i.
 * - El AAD del trozo i es: cabecera completa (37 bytes) | i (8 bytes) | 1 si es el último, 0 si no.
 * - El último trozo siempre tiene menos bytes que el tamaño de trozo (puede tener 0).
 * Así, si alguien reordena, quita o añade trozos, el descifrado falla.
 *
 * Se cifra por trozos porque con una sola etiqueta GCM Java no entrega nada al descifrar
 * hasta comprobarla, y tendría que guardar el fichero entero en memoria.
 * Con trozos solo hay uno en memoria cada vez, sea cual sea el tamaño del fichero.
 *
 * El contenido que se cifra no es solo el fichero: delante va la extensión original,
 * para que viaje cifrada y no se vea desde fuera:
 * longitud de la extensión N (1 byte) | extensión en UTF-8 sin punto (N bytes) | bytes del fichero
 * N puede ser 0 si el fichero no tenía extensión.
 */
public final class AesFileCipher {

    /** Marca de los 4 primeros bytes: identifica un fichero .hgw. */
    static final byte[] MAGIC = "HOG2".getBytes(StandardCharsets.US_ASCII);
    /** Versión del formato; si algún día cambia, los ficheros viejos se reconocerán. */
    static final byte VERSION = 1;
    /** Sal aleatoria para derivar la clave: la misma contraseña da claves distintas en cada fichero. */
    static final int SALT_LENGTH = 16;
    /** Nonce (número de un solo uso) de GCM: 12 bytes es el tamaño recomendado. */
    static final int NONCE_LENGTH = 12;
    /** Etiqueta de autenticación de GCM que va al final de cada trozo (128 bits). */
    static final int TAG_LENGTH = 16;
    /** Cabecera sin el tamaño de trozo: marca + versión + sal + nonce. */
    private static final int BASE_HEADER_LENGTH = MAGIC.length + 1 + SALT_LENGTH + NONCE_LENGTH;
    /** Cabecera completa: la anterior más 4 bytes con el tamaño de trozo (37 bytes en total). */
    static final int HEADER_LENGTH = BASE_HEADER_LENGTH + 4;
    /** Repeticiones de PBKDF2: hacen lento probar contraseñas a lo bruto. */
    static final int PBKDF2_ITERATIONS = 200_000;
    /** AES-256: clave de 256 bits. */
    static final int KEY_LENGTH_BITS = 256;
    /** Se cifra de 1 MB en 1 MB: es lo máximo que hay en memoria a la vez. */
    static final int DEFAULT_CHUNK_SIZE = 1024 * 1024;
    /** Tope al leer un .hgw: uno manipulado no puede obligar a reservar memoria sin límite. */
    private static final int MAX_CHUNK_SIZE = 64 * 1024 * 1024;
    /** Solo letras, números, "_" y "-": así una extensión nunca puede convertirse en una ruta. */
    private static final Pattern SAFE_EXTENSION = Pattern.compile("[A-Za-z0-9_-]{1,32}");

    /** Generador aleatorio criptográfico para la sal y el nonce. */
    private final SecureRandom random = new SecureRandom();
    /** Tamaño de trozo con el que cifra este objeto. */
    private final int chunkSize;

    /** Cifrador con el tamaño de trozo normal (1 MB). */
    public AesFileCipher() {
        this(DEFAULT_CHUNK_SIZE);
    }

    /** Para los tests: permite usar trozos pequeños y probar varios trozos con ficheros pequeños. */
    AesFileCipher(int chunkSize) {
        this.chunkSize = chunkSize;
    }

    /**
     * Cifra {@code input} y escribe el contenedor en {@code output}.
     * Primero escribe en un fichero temporal junto a la salida y solo al terminar bien
     * lo mueve a su sitio, para no dejar ficheros a medias. El original no se modifica.
     */
    public void encrypt(Path input, Path output, char[] password) throws IOException, GeneralSecurityException {
        encrypt(input, output, password, ProgressListener.NONE);
    }

    /** Igual que {@link #encrypt(Path, Path, char[])}, avisando del progreso a {@code progress}. */
    public void encrypt(Path input, Path output, char[] password, ProgressListener progress)
            throws IOException, GeneralSecurityException {
        checkArguments(input, output, password);
        long total = Files.size(input);
        byte[] metadata = buildMetadata(input); // Extensión original, que se cifra junto al contenido

        // Sal y nonce nuevos en cada cifrado: nunca se repiten aunque se cifre el mismo fichero
        byte[] salt = randomBytes(SALT_LENGTH);
        byte[] nonce = randomBytes(NONCE_LENGTH);
        byte[] header = buildHeader(salt, nonce, chunkSize);
        SecretKey key = deriveKey(password, salt);

        writeAtomically(output, out -> {
            // Lo que se cifra es: prefijo con la extensión seguido de los bytes del fichero
            try (InputStream in = new SequenceInputStream(
                    new ByteArrayInputStream(metadata),
                    new ProgressInputStream(Files.newInputStream(input), total, progress))) {
                encryptChunks(in, out, key, header, nonce, chunkSize);
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
        return decrypt(input, output, password, ProgressListener.NONE);
    }

    /** Igual que {@link #decrypt(Path, Path, char[])}, avisando del progreso a {@code progress}. */
    public String decrypt(Path input, Path output, char[] password, ProgressListener progress)
            throws IOException, GeneralSecurityException {
        checkArguments(input, output, password);
        long total = Files.size(input);
        MetadataStrippingOutputStream[] stripper = new MetadataStrippingOutputStream[1];

        writeAtomically(output, fileOut -> {
            MetadataStrippingOutputStream out = new MetadataStrippingOutputStream(fileOut);
            stripper[0] = out;
            try (InputStream in = new ProgressInputStream(Files.newInputStream(input), total, progress)) {
                // La cabecera va sin cifrar: de ella salen la sal y el nonce para repetir la derivación
                byte[] header = in.readNBytes(HEADER_LENGTH);
                int fileChunkSize = checkHeader(header);
                byte[] salt = Arrays.copyOfRange(header, MAGIC.length + 1, MAGIC.length + 1 + SALT_LENGTH);
                byte[] nonce = Arrays.copyOfRange(header, MAGIC.length + 1 + SALT_LENGTH, BASE_HEADER_LENGTH);
                SecretKey key = deriveKey(password, salt);
                decryptChunks(in, out, key, header, nonce, fileChunkSize);
            }
            out.checkComplete(); // Un fichero cortado antes de la extensión tampoco es válido
        });
        return stripper[0].extension();
    }

    /** Escribe la cabecera y luego cifra la entrada trozo a trozo hasta el último (incompleto). */
    private static void encryptChunks(InputStream in, OutputStream out, SecretKey key, byte[] header,
                                      byte[] nonce, int chunkSize) throws IOException, GeneralSecurityException {
        out.write(header);
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        long index = 0;
        boolean last = false;
        while (!last) {
            byte[] plain = in.readNBytes(chunkSize);
            last = plain.length < chunkSize; // Un trozo incompleto (o vacío) marca el final
            cipher.init(Cipher.ENCRYPT_MODE, key, new GCMParameterSpec(TAG_LENGTH * 8, chunkNonce(nonce, index)));
            cipher.updateAAD(chunkAad(header, index, last));
            out.write(cipher.doFinal(plain));
            index++;
        }
    }

    /** Descifra trozo a trozo; cada trozo cifrado ocupa su tamaño más los 16 bytes de la etiqueta. */
    private static void decryptChunks(InputStream in, OutputStream out, SecretKey key, byte[] header,
                                      byte[] nonce, int chunkSize) throws IOException, GeneralSecurityException {
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
        long index = 0;
        boolean last = false;
        while (!last) {
            byte[] encrypted = in.readNBytes(chunkSize + TAG_LENGTH);
            last = encrypted.length < chunkSize + TAG_LENGTH;
            cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(TAG_LENGTH * 8, chunkNonce(nonce, index)));
            cipher.updateAAD(chunkAad(header, index, last));
            out.write(cipher.doFinal(encrypted)); // Cada trozo se comprueba por separado
            index++;
        }
    }

    /**
     * Nonce del trozo {@code index}: el base con sus 4 últimos bytes mezclados (XOR) con el número
     * de trozo. Así cada trozo usa un nonce distinto, que es obligatorio en GCM.
     */
    private static byte[] chunkNonce(byte[] baseNonce, long index) throws IOException {
        // Con 4 bytes de contador caben 2^32 trozos (unos 4 PB con trozos de 1 MB)
        if (index > 0xFFFF_FFFFL) {
            throw new IOException(I18n.get("cipher.tooLarge"));
        }
        byte[] nonce = baseNonce.clone();
        int counter = (int) index;
        for (int i = 0; i < 4; i++) {
            nonce[NONCE_LENGTH - 1 - i] ^= (byte) (counter >>> (8 * i));
        }
        return nonce;
    }

    /**
     * Datos autenticados (no cifrados) del trozo: cabecera + número de trozo + marca de último.
     * Si alguien cambia la cabecera, reordena trozos o corta el fichero, la etiqueta no cuadra.
     */
    private static byte[] chunkAad(byte[] header, long index, boolean last) {
        return ByteBuffer.allocate(header.length + 8 + 1)
                .put(header)
                .putLong(index)
                .put((byte) (last ? 1 : 0))
                .array();
    }

    /** Recibe cuántos bytes del fichero de entrada se han procesado de un total. */
    @FunctionalInterface
    public interface ProgressListener {
        /** Para cuando no interesa el progreso. */
        ProgressListener NONE = (done, total) -> { };

        /** {@code done} bytes procesados de {@code total}. */
        void onProgress(long done, long total);
    }

    /** Cuenta los bytes que se leen del fichero de entrada para informar del progreso. */
    private static final class ProgressInputStream extends FilterInputStream {
        private final long total;
        private final ProgressListener progress;
        private long done;

        ProgressInputStream(InputStream in, long total, ProgressListener progress) {
            super(in);
            this.total = total;
            this.progress = progress;
        }

        // Los dos read() avisan de cuántos bytes se han leído de verdad
        @Override
        public int read() throws IOException {
            int b = super.read();
            if (b != -1) {
                advance(1);
            }
            return b;
        }

        @Override
        public int read(byte[] b, int off, int len) throws IOException {
            int read = super.read(b, off, len);
            if (read > 0) {
                advance(read);
            }
            return read;
        }

        private void advance(long bytes) {
            done += bytes;
            progress.onProgress(done, total);
        }
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
            // El primer byte que llega es la longitud de la extensión
            if (extensionLength < 0 && len > 0) {
                extensionLength = b[off] & 0xFF;
                off++;
                len--;
            }
            // Los siguientes bytes completan la extensión y el resto es el contenido del fichero
            int missing = Math.max(0, extensionLength - extension.size());
            int toExtension = Math.min(missing, len);
            extension.write(b, off, toExtension);
            out.write(b, off + toExtension, len - toExtension);
        }

        /** Comprueba que al menos ha llegado la extensión entera. */
        void checkComplete() throws IOException {
            if (extensionLength < 0 || extension.size() < extensionLength) {
                throw new IOException(I18n.get("cipher.incomplete"));
            }
        }

        /** Extensión original sin punto; vacía si no era segura. */
        String extension() {
            String ext = extension.toString(StandardCharsets.UTF_8);
            return SAFE_EXTENSION.matcher(ext).matches() ? ext : "";
        }
    }

    /** Rechaza contraseña vacía y que entrada y salida sean el mismo fichero (se destruiría). */
    private static void checkArguments(Path input, Path output, char[] password) throws IOException {
        if (password == null || password.length == 0) {
            throw new IllegalArgumentException(I18n.get("cipher.emptyPassword"));
        }
        if (Files.exists(output) && Files.isSameFile(input, output)) {
            throw new IllegalArgumentException(I18n.get("cipher.sameFile"));
        }
    }

    /** Valida la cabecera y devuelve el tamaño de trozo. */
    private static int checkHeader(byte[] header) throws IOException {
        if (header.length < HEADER_LENGTH
                || !Arrays.equals(Arrays.copyOf(header, MAGIC.length), MAGIC)) {
            throw new IOException(I18n.get("cipher.notHgw"));
        }
        byte version = header[MAGIC.length];
        if (version != VERSION) {
            throw new IOException(I18n.get("cipher.version", version));
        }
        int chunkSize = ByteBuffer.wrap(header, BASE_HEADER_LENGTH, 4).getInt();
        if (chunkSize <= 0 || chunkSize > MAX_CHUNK_SIZE) {
            throw new IOException(I18n.get("cipher.chunkSize"));
        }
        return chunkSize;
    }

    /** Escribe en un temporal junto a {@code output} y solo lo mueve a su sitio si todo ha ido bien. */
    private static void writeAtomically(Path output, StreamWriter writer) throws IOException, GeneralSecurityException {
        Path parent = output.toAbsolutePath().getParent();
        // En la misma carpeta, para que el movimiento final sea un simple cambio de nombre
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

    /** Lo que se escribe dentro del temporal (cifrado o descifrado). */
    @FunctionalInterface
    private interface StreamWriter {
        void write(OutputStream out) throws IOException, GeneralSecurityException;
    }

    /** Convierte contraseña + sal en una clave AES de 256 bits con PBKDF2-HMAC-SHA256. */
    static SecretKey deriveKey(char[] password, byte[] salt) throws GeneralSecurityException {
        PBEKeySpec spec = new PBEKeySpec(password, salt, PBKDF2_ITERATIONS, KEY_LENGTH_BITS);
        try {
            byte[] keyBytes = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).getEncoded();
            return new SecretKeySpec(keyBytes, "AES");
        } finally {
            spec.clearPassword(); // Borra la copia de la contraseña que guarda PBEKeySpec
        }
    }

    /** Monta los 37 bytes de la cabecera en el orden del formato. */
    private static byte[] buildHeader(byte[] salt, byte[] nonce, int chunkSize) {
        return ByteBuffer.allocate(HEADER_LENGTH)
                .put(MAGIC)
                .put(VERSION)
                .put(salt)
                .put(nonce)
                .putInt(chunkSize)
                .array();
    }

    /** {@code length} bytes aleatorios seguros. */
    private byte[] randomBytes(int length) {
        byte[] bytes = new byte[length];
        random.nextBytes(bytes);
        return bytes;
    }
}
