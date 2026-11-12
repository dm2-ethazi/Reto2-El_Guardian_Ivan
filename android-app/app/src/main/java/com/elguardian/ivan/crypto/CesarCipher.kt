package com.elguardian.ivan.crypto

import kotlin.text.iterator

/** Descifrado Cesar para letras ASCII, conservando otros caracteres. */
object CesarCipher {
    fun descifrar(texto: String, desplazamiento: Int): String {
        val paso = ((desplazamiento % 26) + 26) % 26
        return buildString(texto.length) {
            for (c in texto) {
                append(
                    when (c) {
                        in 'A'..'Z' -> 'A' + ((c - 'A' - paso + 26) % 26)
                        in 'a'..'z' -> 'a' + ((c - 'a' - paso + 26) % 26)
                        else -> c
                    }
                )
            }
        }
    }
}