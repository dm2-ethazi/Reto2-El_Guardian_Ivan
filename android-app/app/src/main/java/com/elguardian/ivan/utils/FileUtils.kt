package com.elguardian.ivan.utils

import android.content.Context
import android.net.Uri
import android.provider.OpenableColumns
import java.util.Locale

/** Informacion de ficheros obtenidos mediante ContentResolver (sin rutas absolutas). */
object FileUtils {
    fun nombre(contexto: Context, uri: Uri): String {
        try {
            contexto.contentResolver.query(
                uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null
            )?.use { cursor ->
                if (cursor.moveToFirst()) {
                    val indice = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME)
                    if (indice >= 0) {
                        return cursor.getString(indice) ?: "archivo"
                    }
                }
            }
        } catch (_: Exception) {
            // Algunos proveedores no exponen estos metadatos.
        }
        return uri.lastPathSegment?.substringAfterLast('/') ?: "archivo"
    }

    fun tamano(contexto: Context, uri: Uri): Long? {
        try {
            contexto.contentResolver.query(
                uri, arrayOf(OpenableColumns.SIZE), null, null, null
            )?.use { cursor ->
                if (cursor.moveToFirst()) {
                    val indice = cursor.getColumnIndex(OpenableColumns.SIZE)
                    if (indice >= 0 && !cursor.isNull(indice)) return cursor.getLong(indice)
                }
            }
        } catch (_: Exception) {
            // El proveedor puede ocultar el tamaño.
        }
        return null
    }

    fun tamanoLegible(bytes: Long?): String {
        if (bytes == null || bytes < 0) return "Tamano desconocido"
        return when {
            bytes < 1024 -> "$bytes B"
            bytes < 1024 * 1024 -> String.format(Locale.ROOT, "%.1f KiB", bytes / 1024.0)
            else -> String.format(Locale.ROOT, "%.1f MiB", bytes / 1048576.0)
        }
    }
}