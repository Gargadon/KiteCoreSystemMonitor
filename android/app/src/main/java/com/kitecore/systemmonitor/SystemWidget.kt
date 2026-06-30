package com.kitecore.systemmonitor

import android.content.Context
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.floatPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.glance.*
import androidx.glance.action.actionStartActivity
import androidx.glance.action.clickable
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.LinearProgressIndicator
import androidx.glance.appwidget.cornerRadius
import androidx.glance.appwidget.provideContent
import androidx.glance.background
import androidx.glance.layout.*
import androidx.glance.state.GlanceStateDefinition
import androidx.glance.state.PreferencesGlanceStateDefinition
import androidx.glance.text.FontWeight
import androidx.glance.text.Text
import androidx.glance.text.TextStyle
import androidx.glance.unit.ColorProvider

class SystemWidget : GlanceAppWidget() {

    companion object {
        val CpuKey = floatPreferencesKey("cpu_usage")
        val RamKey = floatPreferencesKey("ram_usage")
        val MascotKey = stringPreferencesKey("active_mascot")
        val ColorKey = stringPreferencesKey("glow_color")
        val ShowLabelsKey = booleanPreferencesKey("show_labels")
        val LanguageKey = stringPreferencesKey("language")
        val OpacityKey = floatPreferencesKey("bg_opacity")
    }

    override val stateDefinition: GlanceStateDefinition<*> = PreferencesGlanceStateDefinition

    override suspend fun provideGlance(context: Context, id: GlanceId) {
        provideContent {
            GlanceWidgetContent(context)
        }
    }

    @Composable
    private fun GlanceWidgetContent(context: Context) {
        val prefs = currentState<Preferences>()
        val cpuUsage = prefs[CpuKey] ?: 0.0f
        val ramUsage = prefs[RamKey] ?: 0.0f
        val activeMascot = prefs[MascotKey] ?: "Tetsu"
        val showLabels = prefs[ShowLabelsKey] ?: true
        val language = prefs[LanguageKey] ?: "es"
        val opacity = prefs[OpacityKey] ?: 0.6f

        // Mascot state based on CPU usage
        val stateType = when {
            cpuUsage > 0.85f -> "panic"
            cpuUsage > 0.50f -> "focused"
            else -> "idle"
        }

        val mascotState = when (language) {
            "es" -> when (stateType) {
                "panic" -> "Pánico"
                "focused" -> "Enfocado"
                else -> "Inactivo"
            }
            "fr" -> when (stateType) {
                "panic" -> "Panique"
                "focused" -> "Concentré"
                else -> "Inactif"
            }
            "it" -> when (stateType) {
                "panic" -> "Panico"
                "focused" -> "Concentrato"
                else -> "Inattivo"
            }
            "de" -> when (stateType) {
                "panic" -> "Panik"
                "focused" -> "Fokussiert"
                else -> "Inaktiv"
            }
            "pt" -> when (stateType) {
                "panic" -> "Pânico"
                "focused" -> "Focado"
                else -> "Inativo"
            }
            "zh" -> when (stateType) {
                "panic" -> "恐慌"
                "focused" -> "专注"
                else -> "空闲"
            }
            "ja" -> when (stateType) {
                "panic" -> "パニック"
                "focused" -> "集中"
                else -> "アイドル"
            }
            "ko" -> when (stateType) {
                "panic" -> "패닉"
                "focused" -> "집중"
                else -> "대기"
            }
            else -> when (stateType) { // English (en) fallback
                "panic" -> "Panic"
                "focused" -> "Focused"
                else -> "Idle"
            }
        }

        // Glow Color based on setting
        val colorHex = prefs[ColorKey] ?: "#00F0FF"
        val activeColor = ColorProvider(Color(android.graphics.Color.parseColor(colorHex)))
        val inactiveColor = ColorProvider(Color(0x35FFFFFF))

        // Status text translations
        val coreStatus = when (language) {
            "es" -> "NÚCLEO: EN LÍNEA"
            "fr" -> "NOYAU: EN LIGNE"
            "it" -> "NUCLEO: ONLINE"
            "de" -> "KERN: ONLINE"
            "pt" -> "NÚCLEO: ONLINE"
            "zh" -> "核心: 在线"
            "ja" -> "コア: オンライン"
            "ko" -> "코어: 온라인"
            else -> "CORE: ONLINE" // English fallback
        }

        val systemStatus = when (language) {
            "es" -> "SISTEMA: ACTIVO"
            "fr" -> "SYSTÈME: ACTIF"
            "it" -> "SISTEMA: ATTIVO"
            "de" -> "SYSTEM: AKTIV"
            "pt" -> "SISTEMA: ATIVO"
            "zh" -> "系统: 活跃"
            "ja" -> "システム: アクティブ"
            "ko" -> "시스템: 활성"
            else -> "SYSTEM: ACTIVE" // English fallback
        }

        val mascotDisplayName = when (language) {
            "ja" -> if (activeMascot == "Tetsu") "鉄" else "ハッカー"
            "zh" -> if (activeMascot == "Tetsu") "铁" else "黑客"
            "ko" -> if (activeMascot == "Tetsu") "테츠" else "해커"
            else -> activeMascot
        }

        val alphaInt = (opacity * 255).toInt().coerceIn(0, 255)
        val backgroundColorInt = (alphaInt shl 24) or 0x00121212

        Box(
            modifier = GlanceModifier
                .fillMaxSize()
                .background(ColorProvider(Color(backgroundColorInt)))
                .padding(12.dp)
                .cornerRadius(20.dp)
                .clickable(actionStartActivity<MainActivity>())
        ) {
            Column(
                modifier = GlanceModifier.fillMaxSize(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                // Mascot name and state
                Row(
                    modifier = GlanceModifier.fillMaxWidth(),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = "$mascotDisplayName ($mascotState)",
                        style = TextStyle(
                            color = ColorProvider(Color.White),
                            fontSize = 13.sp,
                            fontWeight = FontWeight.Bold
                        )
                    )
                }

                Spacer(modifier = GlanceModifier.height(6.dp))

                // Mascot Image
                val stateKey = when {
                    cpuUsage > 0.85f -> "panic"
                    cpuUsage > 0.50f -> "focused"
                    else -> "idle"
                }
                val drawableName = "${activeMascot.lowercase()}_$stateKey"
                val drawableId = context.resources.getIdentifier(drawableName, "drawable", context.packageName)
                if (drawableId != 0) {
                    Image(
                        provider = ImageProvider(drawableId),
                        contentDescription = activeMascot,
                        modifier = GlanceModifier.size(60.dp)
                    )
                }

                Spacer(modifier = GlanceModifier.height(6.dp))

                // CPU Metric
                Column(modifier = GlanceModifier.fillMaxWidth().padding(horizontal = 8.dp)) {
                    Row(modifier = GlanceModifier.fillMaxWidth()) {
                        Text(
                            text = "CPU: ${(cpuUsage * 100).toInt()}%",
                            style = TextStyle(color = ColorProvider(Color.White), fontSize = 11.sp)
                        )
                    }
                    Spacer(modifier = GlanceModifier.height(2.dp))
                    LinearProgressIndicator(
                        progress = cpuUsage,
                        modifier = GlanceModifier.fillMaxWidth().height(6.dp),
                        color = activeColor,
                        backgroundColor = inactiveColor
                    )
                }

                Spacer(modifier = GlanceModifier.height(10.dp))

                // RAM Metric
                Column(modifier = GlanceModifier.fillMaxWidth().padding(horizontal = 8.dp)) {
                    Row(modifier = GlanceModifier.fillMaxWidth()) {
                        Text(
                            text = "RAM: ${(ramUsage * 100).toInt()}%",
                            style = TextStyle(color = ColorProvider(Color.White), fontSize = 11.sp)
                        )
                    }
                    Spacer(modifier = GlanceModifier.height(2.dp))
                    LinearProgressIndicator(
                        progress = ramUsage,
                        modifier = GlanceModifier.fillMaxWidth().height(6.dp),
                        color = ColorProvider(Color(0xFF10B981)), // Emerald
                        backgroundColor = inactiveColor
                    )
                }

                if (showLabels) {
                    Spacer(modifier = GlanceModifier.height(8.dp))
                    Row(
                        modifier = GlanceModifier.fillMaxWidth().padding(horizontal = 8.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(
                            text = "$coreStatus  |  $systemStatus",
                            style = TextStyle(
                                color = ColorProvider(Color(0xFFA0A0A0.toInt())),
                                fontSize = 9.sp,
                                fontWeight = FontWeight.Bold
                            )
                        )
                    }
                }
            }
        }
    }
}
