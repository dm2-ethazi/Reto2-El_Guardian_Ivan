package com.elguardian.ivan.crypto

import java.nio.ByteBuffer
import java.nio.charset.CharacterCodingException
import java.nio.charset.CodingErrorAction
import java.nio.charset.StandardCharsets

/**
 * Valida el contenido de los .txt de César antes de mostrarlo en Android.
 * Solo se aceptan ficheros de texto UTF-8. Se permiten tabulaciones y saltos de línea,
 * pero no bytes nulos ni otros caracteres de control propios de archivos binarios.
 */
object CesarTextValidator {
    fun decodificar(bytes: ByteArray): String {
        if (bytes.isEmpty()) {
            throw IllegalArgumentException("El archivo está vacío. Selecciona un .txt con texto cifrado.")
        }

        val texto = try {
            val decodificador = StandardCharsets.UTF_8.newDecoder()
                .onMalformedInput(CodingErrorAction.REPORT)
                .onUnmappableCharacter(CodingErrorAction.REPORT)
            decodificador.decode(ByteBuffer.wrap(bytes)).toString()
        } catch (_: CharacterCodingException) {
            throw IllegalArgumentException("El archivo no contiene texto UTF-8 válido o está dañado.")
        }

        // Algunos editores añaden una marca UTF-8 BOM al principio del archivo.
        val sinBom = texto.removePrefix("\uFEFF")
        if (sinBom.isBlank()) {
            throw IllegalArgumentException("El archivo no contiene texto para descifrar (solo espacios o saltos de línea).")
        }

        if (sinBom.any { c ->
                c == '\u0000' || (c.code < 32 && c != '\n' && c != '\r' && c != '\t') ||
                    c.code in 127..159
            }) {
            throw IllegalArgumentException(
                "El archivo contiene caracteres de control no permitidos (por ejemplo, bytes nulos). " +
                    "Comprueba que sea un .txt de texto válido."
            )
        }

        return sinBom
    }
}