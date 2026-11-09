package com.elguardian.ivan.ui

import android.content.ClipData
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.os.Bundle
import android.view.View
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.core.content.FileProvider
import com.elguardian.ivan.utils.FileUtils
import com.elguardian.ivan.R
import java.io.File
import java.io.IOException

/** Vista previa, guardado y comparticion del resultado descifrado. */
class ResultActivity : GuardianActivity() {
    private lateinit var imagen: File
    private lateinit var extension: String
    private var bitmap: Bitmap? = null

    private val guardar = registerForActivityResult(ActivityResultContracts.StartActivityForResult()) { resultado ->
        val uri = resultado.data?.data
        if (resultado.resultCode == RESULT_OK && uri != null) {
            try {
                contentResolver.openOutputStream(uri)?.use { destino ->
                    imagen.inputStream().use { origen -> origen.copyTo(destino) }
                } ?: throw IOException(getString(R.string.error_save_message))
                Toast.makeText(this, getString(R.string.image_saved), Toast.LENGTH_LONG).show()
            } catch (e: Exception) {
                AlertDialog.Builder(this).setTitle(R.string.error_save_title)
                    .setMessage(getString(R.string.error_save_message))
                    .setPositiveButton(R.string.ok, null).show()
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_result)
        configurarVentana()
        configurarNavegacion()

        val ruta = intent.getStringExtra("result_path")
        val ext = intent.getStringExtra("result_extension")
        if (ruta.isNullOrBlank() || ext == null || ext !in setOf("png", "jpg", "jpeg", "webp")) {
            terminarConError(); return
        }
        val carpetaSegura = File(cacheDir, "descifrados").canonicalFile
        val archivo = File(ruta).canonicalFile
        if (!archivo.parentFile.equals(carpetaSegura) || !archivo.isFile) {
            terminarConError(); return
        }
        imagen = archivo
        extension = ext
        bitmap = cargarVistaPrevia(archivo)
        if (bitmap == null) { terminarConError(); return }
        findViewById<ImageView>(R.id.image_preview).setImageBitmap(bitmap)
        findViewById<TextView>(R.id.result_filename).text = "imagen_descifrada.$extension"
        findViewById<TextView>(R.id.result_size).text = FileUtils.tamanoLegible(imagen.length())
        findViewById<View>(R.id.btn_view).setOnClickListener { mostrarImagen() }
        findViewById<View>(R.id.btn_save).setOnClickListener {
            val mime = when (extension) {
                "jpg", "jpeg" -> "image/jpeg"
                "webp" -> "image/webp"
                else -> "image/png"
            }
            guardar.launch(Intent(Intent.ACTION_CREATE_DOCUMENT).apply {
                addCategory(Intent.CATEGORY_OPENABLE)
                type = mime
                putExtra(Intent.EXTRA_TITLE, "imagen_descifrada.$extension")
            })
        }
        findViewById<View>(R.id.btn_share).setOnClickListener { confirmarYCompartir() }
    }

    private fun terminarConError() {
        AlertDialog.Builder(this).setTitle(R.string.result_unavailable_title)
            .setMessage(R.string.result_unavailable_message)
            .setPositiveButton(R.string.back) { _, _ -> finish() }
            .setOnCancelListener { finish() }
            .show()
    }

    private fun cargarVistaPrevia(file: File): Bitmap? {
        val opciones = BitmapFactory.Options().apply { inJustDecodeBounds = true }
        BitmapFactory.decodeFile(file.absolutePath, opciones)
        if (opciones.outWidth <= 0 || opciones.outHeight <= 0) return null
        var escala = 1
        while (opciones.outWidth / escala > 1600 || opciones.outHeight / escala > 1600) {
            escala *= 2
        }
        return BitmapFactory.decodeFile(file.absolutePath, BitmapFactory.Options().apply {
            inSampleSize = escala
        })
    }

    private fun mostrarImagen() {
        val imagenAmpliada = ImageView(this).apply {
            setImageBitmap(bitmap)
            adjustViewBounds = true
            scaleType = ImageView.ScaleType.FIT_CENTER
            setPadding(12, 16, 12, 16)
        }
        AlertDialog.Builder(this).setTitle(R.string.preview).setView(imagenAmpliada)
            .setPositiveButton(R.string.close, null).show()
    }

    private fun confirmarYCompartir() {
        val confirmar = getSharedPreferences("guardian_prefs", MODE_PRIVATE)
            .getBoolean("confirmar_compartir", true)
        if (confirmar) {
            AlertDialog.Builder(this)
                .setTitle(R.string.share_image_confirm_title)
                .setMessage(R.string.share_image_confirm_message)
                .setNegativeButton(R.string.cancel, null)
                .setPositiveButton(R.string.share) { _, _ -> compartir() }
                .show()
        } else compartir()
    }

    private fun compartir() {
        try {
            val uri = FileProvider.getUriForFile(this, "$packageName.fileprovider", imagen)
            val mime = when (extension) {
                "jpg", "jpeg" -> "image/jpeg"
                "webp" -> "image/webp"
                else -> "image/png"
            }
            val enviar = Intent(Intent.ACTION_SEND).apply {
                type = mime
                putExtra(Intent.EXTRA_STREAM, uri)
                clipData = ClipData.newRawUri("imagen_descifrada.$extension", uri)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            startActivity(Intent.createChooser(enviar, getString(R.string.chooser_image_title)))
        } catch (e: Exception) {
            AlertDialog.Builder(this).setTitle(R.string.error_share_title)
                .setMessage(R.string.error_share_message)
                .setPositiveButton(R.string.ok, null).show()
        }
    }
}