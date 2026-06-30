package com.kitecore.systemmonitor

import android.Manifest
import android.content.Context
import android.content.SharedPreferences
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.content.ContextCompat
import androidx.glance.appwidget.GlanceAppWidgetManager
import androidx.glance.appwidget.state.updateAppWidgetState
import kotlinx.coroutines.*

class MainActivity : ComponentActivity() {

    private lateinit var prefs: SharedPreferences

    private val requestPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            // Permission granted, start service
            SystemMonitorService.startService(this)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        prefs = getSharedPreferences("KiteCorePrefs", Context.MODE_PRIVATE)
        updateWidgets()

        setContent {
            KiteCoreTheme {
                MainScreen()
            }
        }
    }

    private fun updateWidgets() {
        val context = this
        val glanceManager = GlanceAppWidgetManager(context)
        CoroutineScope(Dispatchers.Default).launch {
            try {
                val glanceIds = glanceManager.getGlanceIds(SystemWidget::class.java)
                for (glanceId in glanceIds) {
                    updateAppWidgetState(context, glanceId) { state ->
                        state[SystemWidget.MascotKey] = prefs.getString("active_mascot", "Tetsu") ?: "Tetsu"
                        state[SystemWidget.ColorKey] = prefs.getString("glow_color", "#00F0FF") ?: "#00F0FF"
                        state[SystemWidget.ShowLabelsKey] = prefs.getBoolean("show_labels", true)
                        state[SystemWidget.LanguageKey] = prefs.getString("language", "es") ?: "es"
                        state[SystemWidget.OpacityKey] = prefs.getFloat("bg_opacity", 0.6f)
                    }
                    SystemWidget().update(context, glanceId)
                }
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }

    @OptIn(ExperimentalMaterial3Api::class)
    @Composable
    fun MainScreen() {
        var isServiceRunning by remember { mutableStateOf(false) }
        var activeMascot by remember { mutableStateOf(prefs.getString("active_mascot", "Tetsu") ?: "Tetsu") }
        var glowColor by remember { mutableStateOf(prefs.getString("glow_color", "#00F0FF") ?: "#00F0FF") }
        var showLabels by remember { mutableStateOf(prefs.getBoolean("show_labels", true)) }
        var language by remember { mutableStateOf(prefs.getString("language", "es") ?: "es") }
        var bgOpacity by remember { mutableStateOf(prefs.getFloat("bg_opacity", 0.6f)) }

        // Localized strings
        val isEs = language == "es"
        val titleText = if (isEs) "Monitor del Sistema" else "System Monitor"
        val subtitleText = if (isEs) "CONFIGURACIÓN DEL WIDGET" else "WIDGET CONFIGURATION"
        val serviceLabel = if (isEs) "Servicio en tiempo real" else "Real-time Monitoring"
        val serviceDesc = if (isEs) "Actualiza el widget cada 3 segundos" else "Updates widget every 3 seconds"
        val mascotLabel = if (isEs) "Mascota activa" else "Active Mascot"
        val colorLabel = if (isEs) "Color del brillo" else "Glow Color"
        val opacityLabel = if (isEs) "Opacidad del fondo" else "Background Opacity"
        val labelsToggleLabel = if (isEs) "Mostrar etiquetas extra" else "Show Extra Labels"
        val langLabel = if (isEs) "Idioma" else "Language"

        // Check if service is already running by notification/activity manager if needed,
        // for simplicity we track it with a state variable (started via button)
        LaunchedEffect(Unit) {
            // Simple check using shared preferences or service status
            isServiceRunning = prefs.getBoolean("service_running", false)
            if (isServiceRunning) {
                SystemMonitorService.startService(this@MainActivity)
            }
        }

        Scaffold(
            topBar = {
                TopAppBar(
                    title = {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            val logoResId = remember {
                                resources.getIdentifier("app_logo", "drawable", packageName)
                            }
                            if (logoResId != 0) {
                                androidx.compose.foundation.Image(
                                    painter = androidx.compose.ui.res.painterResource(id = logoResId),
                                    contentDescription = "Logo",
                                    modifier = Modifier
                                        .size(32.dp)
                                        .clip(RoundedCornerShape(8.dp))
                                )
                                Spacer(modifier = Modifier.width(10.dp))
                            }
                            Text(
                                text = titleText,
                                fontWeight = FontWeight.Bold,
                                color = Color.White
                            )
                        }
                    },
                    colors = TopAppBarDefaults.topAppBarColors(
                        containerColor = Color(0xFF121212)
                    )
                )
            }
        ) { paddingValues ->
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .background(Color(0xFF121212))
                    .padding(paddingValues)
                    .padding(16.dp)
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // Mascot Preview Image
                val previewResName = "${activeMascot.lowercase()}_idle"
                val previewResId = remember(activeMascot) {
                    resources.getIdentifier(previewResName, "drawable", packageName)
                }
                if (previewResId != 0) {
                    androidx.compose.foundation.Image(
                        painter = androidx.compose.ui.res.painterResource(id = previewResId),
                        contentDescription = activeMascot,
                        modifier = Modifier
                            .size(100.dp)
                            .align(Alignment.CenterHorizontally)
                    )
                }

                Text(
                    text = subtitleText,
                    fontSize = 11.sp,
                    fontWeight = FontWeight.Bold,
                    color = Color(0xFFFFA0A0A0),
                    fontFamily = FontFamily.Monospace
                )

                // 1. Service Switch Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF1E1E1E))
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(text = serviceLabel, fontWeight = FontWeight.Bold, color = Color.White)
                            Text(text = serviceDesc, fontSize = 12.sp, color = Color.Gray)
                        }
                        Switch(
                            checked = isServiceRunning,
                            onCheckedChange = { checked ->
                                isServiceRunning = checked
                                prefs.edit().putBoolean("service_running", checked).commit()
                                if (checked) {
                                    checkAndStartService()
                                } else {
                                    SystemMonitorService.stopService(this@MainActivity)
                                }
                            },
                            colors = SwitchDefaults.colors(
                                checkedThumbColor = Color(0xFF00F0FF),
                                checkedTrackColor = Color(0xFF005060)
                            )
                        )
                    }
                }

                // 2. Mascot Picker Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF1E1E1E))
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        DropdownSelect(
                            label = mascotLabel,
                            options = listOf("Tetsu", "Hakka"),
                            selectedOption = activeMascot,
                            onOptionSelected = { mascot ->
                                activeMascot = mascot
                                prefs.edit().putString("active_mascot", mascot).commit()
                                updateWidgets()
                            },
                            getLabel = { mascot ->
                                when (language) {
                                    "ja" -> if (mascot == "Tetsu") "黒羽鉄矢（鉄）" else "青山聡太 （ハッカー）"
                                    "zh" -> if (mascot == "Tetsu") "黑羽铁矢（铁）" else "青山聪太（黑客）"
                                    "ko" -> if (mascot == "Tetsu") "쿠로바네 테츠야 (테츠)" else "아오야마 소우타 (해커)"
                                    else -> if (mascot == "Tetsu") "Tetsuya \"Tetsu\" Kurobane" else "Souta \"Hakka\" Aoyama"
                                }
                            }
                        )
                    }
                }

                // 3. Color Picker Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF1E1E1E))
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(text = colorLabel, fontWeight = FontWeight.Bold, color = Color.White)
                        Spacer(modifier = Modifier.height(12.dp))
                        Row(
                            horizontalArrangement = Arrangement.SpaceBetween,
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            val colors = mapOf(
                                "#00F0FF" to Color(0xFF00F0FF), // Cyan
                                "#A855F7" to Color(0xFFA855F7), // Purple
                                "#10B981" to Color(0xFF10B981), // Emerald
                                "#F59E0B" to Color(0xFFF59E0B), // Amber
                                "#F43F5E" to Color(0xFFF43F5E)  // Rose
                            )
                            colors.forEach { (hex, colorVal) ->
                                Box(
                                    modifier = Modifier
                                        .size(42.dp)
                                        .clip(RoundedCornerShape(8.dp))
                                        .background(colorVal)
                                        .clickable {
                                            glowColor = hex
                                            prefs.edit().putString("glow_color", hex).commit()
                                            updateWidgets()
                                        }
                                        .padding(2.dp)
                                ) {
                                    if (glowColor == hex) {
                                        // Simple selected indicator border
                                        Box(
                                            modifier = Modifier
                                                .fillMaxSize()
                                                .clip(RoundedCornerShape(6.dp))
                                                .background(Color.White.copy(alpha = 0.3f))
                                        )
                                    }
                                }
                            }
                        }
                    }
                }

                // Background Opacity Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF1E1E1E))
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(text = opacityLabel, fontWeight = FontWeight.Bold, color = Color.White)
                            Text(text = "${(bgOpacity * 100).toInt()}%", color = Color.Gray)
                        }
                        Spacer(modifier = Modifier.height(8.dp))
                        Slider(
                            value = bgOpacity,
                            onValueChange = { value ->
                                bgOpacity = value
                                prefs.edit().putFloat("bg_opacity", value).commit()
                                updateWidgets()
                            },
                            valueRange = 0.0f..1.0f,
                            colors = SliderDefaults.colors(
                                thumbColor = Color(0xFF00F0FF),
                                activeTrackColor = Color(0xFF005060)
                            )
                        )
                    }
                }

                // 4. Language & UI Toggles Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF1E1E1E))
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = labelsToggleLabel, color = Color.White)
                            Checkbox(
                                checked = showLabels,
                                onCheckedChange = { checked ->
                                    showLabels = checked
                                    prefs.edit().putBoolean("show_labels", checked).commit()
                                    updateWidgets()
                                },
                                colors = CheckboxDefaults.colors(checkedColor = Color(0xFF00F0FF))
                            )
                        }

                        Divider(color = Color(0xFF2C2C2C), thickness = 1.dp, modifier = Modifier.padding(vertical = 8.dp))

                        val langList = listOf(
                            "es" to "Español",
                            "en" to "English",
                            "fr" to "Français",
                            "it" to "Italiano",
                            "de" to "Deutsch",
                            "pt" to "Português",
                            "zh" to "简体中文",
                            "ja" to "日本語",
                            "ko" to "한국어"
                        )
                        DropdownSelect(
                            label = langLabel,
                            options = langList,
                            selectedOption = langList.firstOrNull { it.first == language } ?: ("es" to "Español"),
                            onOptionSelected = { pair ->
                                language = pair.first
                                prefs.edit().putString("language", pair.first).commit()
                                updateWidgets()
                            },
                            getLabel = { it.second }
                        )
                    }
                }

                // 5. About Card
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF1E1E1E))
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = if (language == "es") "Acerca de" else "About",
                            fontWeight = FontWeight.Bold,
                            color = Color.White
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "Kite Core System Monitor v1.0.0",
                            fontSize = 13.sp,
                            fontWeight = FontWeight.Bold,
                            color = Color.White
                        )
                        Text(
                            text = if (language == "es") 
                                "Un monitor de recursos del sistema en tiempo real con Tetsu y Hakka." 
                            else 
                                "A real-time system resource monitor widget featuring Tetsu and Hakka.",
                            fontSize = 12.sp,
                            color = Color.Gray
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "GitHub: https://github.com/Gargadon/KiteCoreSystemMonitor",
                            fontSize = 11.sp,
                            color = Color(0xFF00F0FF),
                            modifier = Modifier.clickable {
                                try {
                                    val intent = android.content.Intent(
                                        android.content.Intent.ACTION_VIEW,
                                        android.net.Uri.parse("https://github.com/Gargadon/KiteCoreSystemMonitor")
                                    )
                                    this@MainActivity.startActivity(intent)
                                } catch (e: Exception) {
                                    // Ignore failures if no browser is installed
                                }
                            }
                        )
                    }
                }
            }
        }
    }

    private fun checkAndStartService() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (ContextCompat.checkSelfPermission(
                    this,
                    Manifest.permission.POST_NOTIFICATIONS
                ) != PackageManager.PERMISSION_GRANTED
            ) {
                requestPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
            } else {
                SystemMonitorService.startService(this)
            }
        } else {
            SystemMonitorService.startService(this)
        }
    }
}

@Composable
fun KiteCoreTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = darkColorScheme(
            primary = Color(0xFF00F0FF),
            background = Color(0xFF121212),
            surface = Color(0xFF1E1E1E)
        ),
        content = content
    )
}

@Composable
fun <T> DropdownSelect(
    label: String,
    options: List<T>,
    selectedOption: T,
    onOptionSelected: (T) -> Unit,
    getLabel: (T) -> String
) {
    var expanded by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxWidth()) {
        Text(text = label, fontWeight = FontWeight.Bold, color = Color.White)
        Spacer(modifier = Modifier.height(8.dp))
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(8.dp))
                .background(Color(0xFF2C2C2C))
                .clickable { expanded = true }
                .padding(horizontal = 16.dp, vertical = 12.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(text = getLabel(selectedOption), color = Color.White)
                Text(text = "▼", color = Color.Gray, fontSize = 12.sp)
            }
            DropdownMenu(
                expanded = expanded,
                onDismissRequest = { expanded = false },
                modifier = Modifier
                    .background(Color(0xFF2C2C2C))
            ) {
                options.forEach { option ->
                    DropdownMenuItem(
                        text = { Text(text = getLabel(option), color = Color.White) },
                        onClick = {
                            onOptionSelected(option)
                            expanded = false
                        }
                    )
                }
            }
        }
    }
}
