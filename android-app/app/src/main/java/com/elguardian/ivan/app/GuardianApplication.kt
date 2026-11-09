package com.elguardian.ivan.app

import android.app.Application
import androidx.appcompat.app.AppCompatDelegate
import androidx.core.os.LocaleListCompat

class GuardianApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        val selected = getSharedPreferences("guardian_prefs", MODE_PRIVATE)
            .getString("app_language", "es") ?: "es"
        if (selected in setOf("es", "eu", "en")) {
            AppCompatDelegate.setApplicationLocales(LocaleListCompat.forLanguageTags(selected))
        }
    }
}