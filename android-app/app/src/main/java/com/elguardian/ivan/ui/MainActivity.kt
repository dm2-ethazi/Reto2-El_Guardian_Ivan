package com.elguardian.ivan.ui

import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import com.elguardian.ivan.utils.FileUtils
import com.elguardian.ivan.R

/** Pantalla de inicio y recepcion de archivos compartidos desde otras aplicaciones. */
class MainActivity : GuardianActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        configurarVentana()
        configurarNavegacion()

        findViewById<View>(R.id.card_cesar).setOnClickListener {
            startActivity(Intent(this, CesarActivity::class.java))
        }
        findViewById<View>(R.id.card_aes).setOnClickListener {
            startActivity(Intent(this, AesActivity::class.java))
        }
        findViewById<View>(R.id.top_settings).setOnClickListener {
            startActivity(Intent(this, SettingsActivity::class.java))
        }
        if (savedInstanceState == null) abrirArchivoCompartido(intent)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        abrirArchivoCompartido(intent)
    }

    private fun abrirArchivoCompartido(compartido: Intent?) {
        if (compartido?.action != Intent.ACTION_SEND) return
        @Suppress("DEPRECATION")
        val uri: Uri? = if (Build.VERSION.SDK_INT >= 33) {
            compartido.getParcelableExtra(Intent.EXTRA_STREAM, Uri::class.java)
        } else {
            compartido.getParcelableExtra(Intent.EXTRA_STREAM)
        }
        val archivo = uri ?: compartido.clipData?.takeIf { it.itemCount > 0 }
            ?.getItemAt(0)?.uri
        if (archivo == null) {
            Toast.makeText(this, getString(R.string.received_no_file), Toast.LENGTH_LONG).show()
            return
        }
        val nombre = FileUtils.nombre(this, archivo).lowercase()
        when {
            nombre.endsWith(".hgw") -> enviarArchivo(AesActivity::class.java, archivo)
            nombre.endsWith(".txt") -> enviarArchivo(CesarActivity::class.java, archivo)
            else -> AlertDialog.Builder(this)
                .setTitle(R.string.received_file_title)
                .setMessage(R.string.received_file_question)
                .setPositiveButton("AES (.hgw)") { _, _ ->
                    enviarArchivo(AesActivity::class.java, archivo)
                }
                .setNegativeButton(R.string.received_cesar_choice) { _, _ ->
                    enviarArchivo(CesarActivity::class.java, archivo)
                }
                .setNeutralButton(R.string.cancel, null)
                .show()
        }
    }

    private fun enviarArchivo(destino: Class<*>, uri: Uri) {
        val siguiente = Intent(this, destino).putExtra("archivo_uri", uri)
        siguiente.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        startActivity(siguiente)
    }
}