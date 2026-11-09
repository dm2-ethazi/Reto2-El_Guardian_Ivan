package com.elguardian.ivan.ui

import android.content.Intent
import android.view.View
import android.widget.ImageView
import android.widget.TextView
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import com.elguardian.ivan.R

/** Funciones comunes de navegacion y adaptacion a las barras de Android. */
abstract class GuardianActivity : AppCompatActivity() {

    protected fun configurarVentana() {
        enableEdgeToEdge()
        ViewCompat.setOnApplyWindowInsetsListener(findViewById(R.id.main)) { vista, insets ->
            val barras = insets.getInsets(WindowInsetsCompat.Type.systemBars())
            vista.setPadding(barras.left, barras.top, barras.right, barras.bottom)
            insets
        }
    }

    protected fun configurarNavegacion(ajustesActivo: Boolean = false) {
        findViewById<View>(R.id.nav_inicio).setOnClickListener {
            if (this !is MainActivity) {
                val inicio = Intent(this, MainActivity::class.java)
                inicio.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
                startActivity(inicio)
            }
        }
        findViewById<View>(R.id.nav_ajustes).setOnClickListener {
            if (this !is SettingsActivity) {
                startActivity(Intent(this, SettingsActivity::class.java))
            }
        }
        findViewById<ImageView>(R.id.nav_inicio_icon).setImageResource(
            if (ajustesActivo) R.drawable.icon_inicio_inactivo else R.drawable.icon_inicio
        )
        findViewById<ImageView>(R.id.nav_ajustes_icon).setImageResource(
            if (ajustesActivo) R.drawable.icon_ajustes_activo else R.drawable.icon_ajustes
        )
        findViewById<TextView>(R.id.nav_inicio_text).setTextColor(
            getColor(if (ajustesActivo) R.color.disabled_rose else R.color.gold)
        )
        findViewById<TextView>(R.id.nav_ajustes_text).setTextColor(
            getColor(if (ajustesActivo) R.color.gold else R.color.disabled_rose)
        )
        findViewById<View>(R.id.btn_back)?.setOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }
    }
}