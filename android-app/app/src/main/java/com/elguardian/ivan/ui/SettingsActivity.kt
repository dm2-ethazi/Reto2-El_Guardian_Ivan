package com.elguardian.ivan.ui

import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import androidx.appcompat.app.AppCompatDelegate
import androidx.appcompat.widget.AppCompatSpinner
import androidx.appcompat.widget.SwitchCompat
import androidx.core.os.LocaleListCompat
import com.elguardian.ivan.R

/** Confirmation applies to sharing both decrypted text and decrypted images. */
class SettingsActivity : GuardianActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_settings)
        configurarVentana()
        configurarNavegacion(ajustesActivo = true)
        val prefs = getSharedPreferences("guardian_prefs", MODE_PRIVATE)
        findViewById<SwitchCompat>(R.id.switch_confirm_share).apply {
            isChecked = prefs.getBoolean("confirmar_compartir", true)
            setOnCheckedChangeListener { _, activado ->
                prefs.edit().putBoolean("confirmar_compartir", activado).apply()
            }
        }

        val codes = listOf("es", "eu", "en")
        val labels = listOf("Español", "Euskara", "English")
        val spinner = findViewById<AppCompatSpinner>(R.id.spinner_language)
        spinner.adapter = ArrayAdapter(this, R.layout.item_language_selected, labels).also {
            it.setDropDownViewResource(R.layout.item_language_dropdown)
        }
        val selected = prefs.getString("app_language", "es") ?: "es"
        spinner.setSelection(codes.indexOf(selected).coerceAtLeast(0), false)
        // Do not change language during initial Spinner initialization / Activity recreation.
        spinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: AdapterView<*>?) = Unit
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                val next = codes[position]
                if (next == prefs.getString("app_language", "es")) return
                prefs.edit().putString("app_language", next).apply()
                AppCompatDelegate.setApplicationLocales(LocaleListCompat.forLanguageTags(next))
            }
        }
    }
}