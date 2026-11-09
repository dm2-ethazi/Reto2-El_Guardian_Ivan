package com.elguardian.ivan.ui

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.text.method.ScrollingMovementMethod
import android.view.MotionEvent
import android.view.View
import android.widget.EditText
import android.widget.TextView
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import com.elguardian.ivan.crypto.CesarCipher
import com.elguardian.ivan.crypto.CesarTextValidator
import com.elguardian.ivan.utils.FileUtils
import com.elguardian.ivan.R
import java.io.ByteArrayOutputStream
import java.io.IOException
import java.util.concurrent.Executors

/** Seleccion y descifrado de mensajes de texto con el algoritmo Cesar. */
class CesarActivity : GuardianActivity() {
    private var archivo: Uri? = null
    private var textoDescifrado: String? = null
    private val worker = Executors.newSingleThreadExecutor()

    private val abrir = registerForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) seleccionar(uri)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_cesar)
        configurarVentana()
        configurarNavegacion()
        // Scroll del resultado sin introducir otro ScrollView dentro del principal.
        // Esta pantalla conserva el layout original, evitando problemas de inflado o gestos.
        val resultado = findViewById<TextView>(R.id.result_text)
        resultado.movementMethod = ScrollingMovementMethod.getInstance()
        resultado.setOnTouchListener { view, event ->
            when (event.actionMasked) {
                MotionEvent.ACTION_DOWN -> {
                    if (resultado.canScrollVertically(1) || resultado.canScrollVertically(-1)) {
                        view.parent?.requestDisallowInterceptTouchEvent(true)
                    }
                }
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                    view.parent?.requestDisallowInterceptTouchEvent(false)
                }
            }
            false // El propio TextView gestiona el desplazamiento.
        }
        findViewById<View>(R.id.btn_browse).setOnClickListener {
            abrir.launch(arrayOf("text/plain", "application/octet-stream"))
        }
        findViewById<View>(R.id.btn_decrypt).setOnClickListener { descifrar() }
        findViewById<EditText>(R.id.input_shift).addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: Editable?) {
                val campo = findViewById<EditText>(R.id.input_shift)
                campo.error = if (s.toString().trim().toIntOrNull() == null)
                    getString(R.string.error_shift_hint) else null
            }
        })
        findViewById<View>(R.id.btn_share_text).setOnClickListener {
            val texto = textoDescifrado ?: return@setOnClickListener
            val confirm = getSharedPreferences("guardian_prefs", MODE_PRIVATE)
                .getBoolean("confirmar_compartir", true)
            if (confirm) {
                AlertDialog.Builder(this)
                    .setTitle(R.string.share_text_confirm_title)
                    .setMessage(R.string.share_text_confirm_message)
                    .setNegativeButton(R.string.cancel, null)
                    .setPositiveButton(R.string.share) { _, _ -> compartirTexto(texto) }
                    .show()
            } else compartirTexto(texto)
        }
        // Restaurar el archivo y la clave al cambiar la orientación.
        // El resultado grande no se guarda en el Bundle: se reconstruye desde el .txt.
        if (savedInstanceState != null) {
            savedInstanceState.getString("cesar_uri")?.let { seleccionar(Uri.parse(it)) }
            savedInstanceState.getString("cesar_shift")?.let {
                findViewById<EditText>(R.id.input_shift).setText(it)
            }
            if (archivo != null && savedInstanceState.getBoolean("cesar_descifrado_visible")) {
                descifrar()
            }
        } else {
            @Suppress("DEPRECATION")
            val recibido = intent.getParcelableExtra<Uri>("archivo_uri")
            if (recibido != null) seleccionar(recibido)
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putString("cesar_uri", archivo?.toString())
        outState.putString("cesar_shift", findViewById<EditText>(R.id.input_shift).text.toString())
        outState.putBoolean("cesar_descifrado_visible", textoDescifrado != null)
        super.onSaveInstanceState(outState)
    }

    private fun seleccionar(uri: Uri) {
        val nombre = FileUtils.nombre(this, uri)
        if (!nombre.endsWith(".txt", ignoreCase = true)) {
            mostrarError(getString(R.string.error_txt_type))
            return
        }
        archivo = uri
        textoDescifrado = null
        findViewById<TextView>(R.id.selected_file).text = nombre
        val tamano = FileUtils.tamano(this, uri)
        findViewById<TextView>(R.id.file_size).text = FileUtils.tamanoLegible(tamano)
        findViewById<TextView>(R.id.result_text).setText(R.string.result_placeholder)
        findViewById<TextView>(R.id.result_text).scrollTo(0, 0)
        findViewById<View>(R.id.btn_share_text).visibility = View.GONE
        // Aviso inmediato si el proveedor informa de que el fichero tiene 0 bytes.
        // También se comprueba el contenido al descifrar, por si oculta el tamaño.
        if (tamano == 0L) mostrarError(getString(R.string.error_txt_empty))
    }

    private fun descifrar() {
        val uri = archivo ?: run { mostrarError(getString(R.string.error_txt_no_file)); return }
        val valor = findViewById<EditText>(R.id.input_shift).text.toString().trim().toIntOrNull()
        if (valor == null) {
            mostrarError(getString(R.string.error_shift_numeric))
            return
        }
        val boton = findViewById<View>(R.id.btn_decrypt)
        boton.isEnabled = false
        // Evita que se comparta un descifrado anterior si esta operación falla.
        textoDescifrado = null
        findViewById<View>(R.id.btn_share_text).visibility = View.GONE
        findViewById<TextView>(R.id.result_text).setText(R.string.result_placeholder)
        worker.execute {
            try {
                val texto = contentResolver.openInputStream(uri)?.use { entrada ->
                    // Evitamos cargar ficheros enormes y bloquear el dispositivo.
                    val limite = 2 * 1024 * 1024
                    val acumulado = ByteArrayOutputStream()
                    val buffer = ByteArray(8192)
                    while (true) {
                        val n = entrada.read(buffer)
                        if (n == -1) break
                        if (acumulado.size() + n > limite) {
                            throw IOException("El texto supera el limite de 2 MiB")
                        }
                        acumulado.write(buffer, 0, n)
                    }
                    val bytes = acumulado.toByteArray()
                    CesarTextValidator.decodificar(bytes)
                } ?: throw IOException("No se puede abrir el archivo")
                val descifrado = CesarCipher.descifrar(texto, valor)
                runOnUiThread {
                    textoDescifrado = descifrado
                    findViewById<TextView>(R.id.result_text).text = descifrado
                    findViewById<TextView>(R.id.result_text).scrollTo(0, 0)
                    findViewById<View>(R.id.btn_share_text).visibility = View.VISIBLE
                    boton.isEnabled = true
                }
            } catch (e: Exception) {
                runOnUiThread {
                    boton.isEnabled = true
                    val mensaje = when (e) {
                        is IllegalArgumentException -> when {
                            e.message.orEmpty().contains("vacío") -> getString(R.string.error_txt_empty)
                            e.message.orEmpty().contains("no contiene texto") -> getString(R.string.error_txt_blank)
                            e.message.orEmpty().contains("UTF-8") -> getString(R.string.error_txt_utf8)
                            e.message.orEmpty().contains("caracteres de control") -> getString(R.string.error_txt_control)
                            else -> getString(R.string.error_txt_process)
                        }
                        is IOException -> if (e.message == "El texto supera el limite de 2 MiB")
                            getString(R.string.error_txt_too_large) else getString(R.string.error_txt_read)
                        else -> getString(R.string.error_txt_process)
                    }
                    mostrarError(mensaje)
                }
            }
        }
    }

    private fun compartirTexto(texto: String) {
        try {
            val enviar = Intent(Intent.ACTION_SEND).apply {
                type = "text/plain"
                putExtra(Intent.EXTRA_TEXT, texto)
            }
            startActivity(Intent.createChooser(enviar, getString(R.string.chooser_text_title)))
        } catch (_: Exception) {
            mostrarError(getString(R.string.error_share_message))
        }
    }

    private fun mostrarError(mensaje: String) {
        if (!isFinishing && !isDestroyed) AlertDialog.Builder(this)
            .setTitle(R.string.error_operation_title)
            .setMessage(mensaje)
            .setPositiveButton(R.string.ok, null)
            .show()
    }

    override fun onDestroy() {
        worker.shutdownNow()
        super.onDestroy()
    }
}