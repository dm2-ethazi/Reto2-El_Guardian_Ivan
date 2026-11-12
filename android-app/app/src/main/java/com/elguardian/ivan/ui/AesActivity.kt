package com.elguardian.ivan.ui

import android.content.Intent
import android.graphics.BitmapFactory
import android.net.Uri
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.text.method.HideReturnsTransformationMethod
import android.text.method.PasswordTransformationMethod
import android.view.View
import android.widget.EditText
import android.widget.ImageView
import android.widget.ProgressBar
import android.widget.TextView
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import com.elguardian.ivan.utils.FileUtils
import com.elguardian.ivan.crypto.HgwDecryptor
import com.elguardian.ivan.R
import java.io.File
import java.io.IOException
import java.util.concurrent.Executors

/** Descifrado AES/HOG2 fuera del hilo principal para mantener la interfaz fluida. */
class AesActivity : GuardianActivity() {
    private var archivo: Uri? = null
    private val worker = Executors.newSingleThreadExecutor()
    private val abrir = registerForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) seleccionar(uri)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_aes)
        configurarVentana()
        configurarNavegacion()
        findViewById<View>(R.id.btn_browse).setOnClickListener {
            abrir.launch(arrayOf("application/octet-stream", "application/x-hgw", "*/*"))
        }
        findViewById<View>(R.id.btn_decrypt).setOnClickListener { descifrar() }
        val password = findViewById<EditText>(R.id.input_password)
        password.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: Editable?) {
                actualizarBoton()
            }
        })
        actualizarBoton()
        val botonOjo = findViewById<ImageView>(R.id.btn_show_password)
        botonOjo.setOnClickListener {
            val estabaOculta = password.transformationMethod is PasswordTransformationMethod
            password.transformationMethod = if (estabaOculta)
                HideReturnsTransformationMethod.getInstance()
            else
                PasswordTransformationMethod.getInstance()
            botonOjo.setImageResource(
                if (estabaOculta) R.drawable.ic_eye_open24 else R.drawable.ic_eye_closed
            )
            password.setSelection(password.text.length)
        }
        if (savedInstanceState != null) {
            savedInstanceState.getString("aes_uri")?.let { seleccionar(Uri.parse(it)) }
        } else {
            @Suppress("DEPRECATION")
            val recibido = intent.getParcelableExtra<Uri>("archivo_uri")
            if (recibido != null) seleccionar(recibido)
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putString("aes_uri", archivo?.toString())
        // La contrasena no se copia manualmente al Bundle.
        super.onSaveInstanceState(outState)
    }

    private fun seleccionar(uri: Uri) {
        val nombre = FileUtils.nombre(this, uri)
        // Nunca conservar la seleccion anterior si el usuario elige un fichero no valido.
        archivo = null
        findViewById<TextView>(R.id.selected_file).setText(R.string.file_placeholder)
        findViewById<TextView>(R.id.file_size).text = "HOG2 · AES-256-GCM"
        actualizarBoton()
        if (!nombre.endsWith(".hgw", ignoreCase = true)) {
            mostrarError(getString(R.string.error_aes_type))
            return
        }
        val tamano = FileUtils.tamano(this, uri)
        if (tamano == 0L) {
            mostrarError(getString(R.string.error_aes_empty))
            return
        }
        archivo = uri
        findViewById<TextView>(R.id.selected_file).text = nombre
        findViewById<TextView>(R.id.file_size).text = FileUtils.tamanoLegible(tamano)
        findViewById<TextView>(R.id.status).visibility = View.GONE
        actualizarBoton()
    }

    private fun actualizarBoton() {
        val disponible = archivo != null && findViewById<EditText>(R.id.input_password).text.isNotEmpty()
        findViewById<View>(R.id.btn_decrypt).apply {
            isEnabled = disponible
            alpha = if (disponible) 1f else 0.65f
        }
    }

    private fun descifrar() {
        val uri = archivo ?: run { mostrarError(getString(R.string.error_aes_no_file)); return }
        val clave = findViewById<EditText>(R.id.input_password).text.toString()
        if (clave.isEmpty()) {
            mostrarError(getString(R.string.error_aes_password))
            return
        }
        val boton = findViewById<View>(R.id.btn_decrypt)
        boton.isEnabled = false
        findViewById<ProgressBar>(R.id.progress).visibility = View.VISIBLE
        findViewById<TextView>(R.id.status).apply {
            visibility = View.VISIBLE
            setText(R.string.processing)
        }
        worker.execute {
            try {
                val carpeta = File(cacheDir, "descifrados")
                // Eliminamos resultados antiguos (sin historial ni almacenamiento permanente).
                carpeta.listFiles()?.filter { it.isFile && System.currentTimeMillis() - it.lastModified() > 24 * 60 * 60 * 1000L }
                    ?.forEach { it.delete() }
                val resultado = contentResolver.openInputStream(uri)?.use { entrada ->
                    HgwDecryptor.decrypt(entrada.buffered(), clave, carpeta)
                } ?: throw IOException("No se pudo abrir el archivo seleccionado")
                val check = BitmapFactory.Options().apply { inJustDecodeBounds = true }
                BitmapFactory.decodeFile(resultado.file.absolutePath, check)
                if (check.outWidth <= 0 || check.outHeight <= 0) {
                    resultado.file.delete()
                    throw IOException("El archivo descifrado no contiene una imagen valida")
                }
                runOnUiThread {
                    if (!isFinishing && !isDestroyed) {
                        findViewById<EditText>(R.id.input_password).text?.clear()
                        startActivity(Intent(this, ResultActivity::class.java).apply {
                            putExtra("result_path", resultado.file.absolutePath)
                            putExtra("result_extension", resultado.extension)
                        })
                        findViewById<ProgressBar>(R.id.progress).visibility = View.GONE
                        actualizarBoton()
                        findViewById<TextView>(R.id.status).visibility = View.GONE
                    }
                }
            } catch (e: OutOfMemoryError) {
                runOnUiThread { finalizarError(getString(R.string.error_aes_memory)) }
            } catch (e: Exception) {
                runOnUiThread {
                    val mensaje = when {
                        e is SecurityException -> getString(R.string.error_aes_wrong_password)
                        e is IOException && e.message == "El archivo descifrado no contiene una imagen valida" ->
                            getString(R.string.error_aes_bad_image)
                        e is IOException -> getString(R.string.error_aes_invalid)
                        else -> getString(R.string.error_aes_generic)
                    }
                    finalizarError(mensaje)
                }
            }
        }
    }

    private fun finalizarError(mensaje: String) {
        if (isFinishing || isDestroyed) return
        findViewById<ProgressBar>(R.id.progress).visibility = View.GONE
        findViewById<TextView>(R.id.status).visibility = View.GONE
        actualizarBoton()
        mostrarError(mensaje)
    }

    private fun mostrarError(mensaje: String) {
        if (!isFinishing && !isDestroyed) AlertDialog.Builder(this)
            .setTitle(R.string.error_aes_title)
            .setMessage(mensaje)
            .setPositiveButton(R.string.ok, null)
            .show()
    }

    override fun onDestroy() {
        worker.shutdownNow()
        super.onDestroy()
    }
}