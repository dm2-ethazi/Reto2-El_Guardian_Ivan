package com.elguardian.ivan.crypto

import java.io.ByteArrayOutputStream
import java.io.File
import java.io.IOException
import java.io.InputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets
import javax.crypto.AEADBadTagException
import javax.crypto.Cipher
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.PBEKeySpec
import javax.crypto.spec.SecretKeySpec

/**
 * Lector compatible con los contenedores .hgw HOG2 de JavaFX.
 *
 * Cabecera: 4 bytes "HOG2" + version 1 + salt (16) + nonce (12)
 * + tamaño de trozo uint32 big-endian (4) = 37 bytes.
 * Cada trozo: AES-256-GCM con etiqueta de 16 bytes, nonce base XOR indice,
 * AAD = cabecera + indice uint64 BE + indicador de ultimo trozo.
 */
object HgwDecryptor {
    private const val HEADER_LENGTH = 37
    private const val TAG_LENGTH = 16
    private const val MAX_CHUNK_SIZE = 64 * 1024 * 1024
    private const val ITERATIONS = 200_000
    private val validExtension = Regex("[A-Za-z0-9_-]{0,32}")
    private val imageExtensions = setOf("png", "jpg", "jpeg", "webp")

    data class Result(val file: File, val extension: String)

    @Throws(Exception::class)
    fun decrypt(input: InputStream, password: String, directory: File): Result {
        require(password.isNotEmpty()) { "Introduce la contraseña" }
        if (!directory.exists() && !directory.mkdirs()) {
            throw IOException("No se pudo preparar la carpeta temporal")
        }
        val header = ByteArray(HEADER_LENGTH)
        input.readFully(header)
        if (!header.copyOfRange(0, 4).contentEquals("HOG2".toByteArray(StandardCharsets.US_ASCII)) ||
            header[4] != 1.toByte()
        ) {
            throw IOException("No es un contenedor .hgw valido")
        }
        val chunkSize = ByteBuffer.wrap(header, 33, 4).order(ByteOrder.BIG_ENDIAN).int.toLong() and 0xFFFF_FFFFL
        if (chunkSize < 1L || chunkSize > MAX_CHUNK_SIZE.toLong()) {
            throw IOException("Tamano de trozo no valido en .hgw")
        }
        val salt = header.copyOfRange(5, 21)
        val baseNonce = header.copyOfRange(21, 33)
        val passwordChars = password.toCharArray()
        val keySpec = PBEKeySpec(passwordChars, salt, ITERATIONS, 256)
        val key = try {
            val keyBytes = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
                .generateSecret(keySpec).encoded
            SecretKeySpec(keyBytes, "AES").also { keyBytes.fill(0) }
        } finally {
            keySpec.clearPassword()
            passwordChars.fill('\u0000')
        }
        val ciphertext = ByteArray(chunkSize.toInt() + TAG_LENGTH)
        val temporary = File.createTempFile("guardian_", ".tmp", directory)
        var finalFile: File? = null
        try {
            val extensionBytes = ByteArrayOutputStream(32)
            var expectedExtensionBytes = -1
            var extension: String? = null
            var index = 0L
            temporary.outputStream().buffered().use { output ->
                while (true) {
                    // Un trozo completo NO es el ultimo: HOG2 siempre añade
                    // un trozo corto (a veces solo la etiqueta final).
                    val bytesRead = input.readUntilFullOrEof(ciphertext)
                    if (bytesRead < TAG_LENGTH) {
                        throw IOException("Contenedor incompleto: falta el trozo final")
                    }
                    if (index > 0xFFFF_FFFFL) throw IOException("Demasiados trozos")
                    val last = bytesRead < ciphertext.size
                    val nonce = baseNonce.copyOf()
                    for (j in 0 until 4) {
                        val mask = ((index ushr (24 - j * 8)) and 0xFF).toInt()
                        nonce[8 + j] = (nonce[8 + j].toInt() xor mask).toByte()
                    }
                    val additional = ByteBuffer.allocate(HEADER_LENGTH + 8 + 1)
                        .order(ByteOrder.BIG_ENDIAN)
                        .put(header).putLong(index).put(if (last) 1.toByte() else 0.toByte())
                        .array()
                    val cipher = Cipher.getInstance("AES/GCM/NoPadding")
                    cipher.init(Cipher.DECRYPT_MODE, key, GCMParameterSpec(128, nonce))
                    cipher.updateAAD(additional)
                    val plain = try {
                        cipher.doFinal(ciphertext, 0, bytesRead)
                    } catch (_: AEADBadTagException) {
                        throw SecurityException("Contraseña incorrecta o archivo .hgw alterado")
                    }
                    var cursor = 0
                    if (expectedExtensionBytes < 0 && plain.isNotEmpty()) {
                        expectedExtensionBytes = plain[0].toInt() and 0xFF
                        if (expectedExtensionBytes > 32) throw IOException("Extension no valida")
                        cursor++
                    }
                    while (cursor < plain.size && extensionBytes.size() < expectedExtensionBytes) {
                        extensionBytes.write(plain[cursor].toInt())
                        cursor++
                    }
                    if (expectedExtensionBytes >= 0 && extensionBytes.size() == expectedExtensionBytes && extension == null) {
                        val extracted = String(extensionBytes.toByteArray(), StandardCharsets.UTF_8)
                        if (!validExtension.matches(extracted)) throw IOException("Extension no valida")
                        extension = extracted
                    }
                    if (extension != null && cursor < plain.size) {
                        output.write(plain, cursor, plain.size - cursor)
                    }
                    if (last) break
                    index++
                }
            }
            val ext = extension ?: throw IOException("Falta la extension del contenido")
            if (ext.lowercase() !in imageExtensions) {
                throw IOException("El contenedor no incluye una imagen PNG, JPG o WebP")
            }
            val outputFile = File(directory, "guardian_${System.nanoTime()}.${ext.lowercase()}")
            if (!temporary.renameTo(outputFile)) throw IOException("No se pudo publicar la imagen")
            finalFile = outputFile
            return Result(outputFile, ext.lowercase())
        } catch (e: Exception) {
            finalFile?.delete()
            throw e
        } finally {
            temporary.delete()
        }
    }

    private fun InputStream.readFully(bytes: ByteArray) {
        var offset = 0
        while (offset < bytes.size) {
            val count = read(bytes, offset, bytes.size - offset)
            if (count == -1) throw IOException("Contenedor .hgw incompleto")
            if (count > 0) offset += count
        }
    }

    private fun InputStream.readUntilFullOrEof(buffer: ByteArray): Int {
        var offset = 0
        while (offset < buffer.size) {
            val read = read(buffer, offset, buffer.size - offset)
            if (read == -1) break
            if (read > 0) offset += read
        }
        return offset
    }
}