package com.kitecore.systemmonitor

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == Intent.ACTION_BOOT_COMPLETED) {
            val prefs = context.getSharedPreferences("KiteCorePrefs", Context.MODE_PRIVATE)
            val isServiceRunning = prefs.getBoolean("service_running", false)
            if (isServiceRunning) {
                SystemMonitorService.startService(context)
            }
        }
    }
}
