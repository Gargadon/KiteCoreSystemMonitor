# Kite Core System Monitor — Roadmap & Plan

Este documento detalla el plan de desarrollo para implementar por completo el monitor de sistema de **Kite Core** en KDE Plasma 6.

---

## 1. Integración con Sensores de Plasma 6 (KSystemStats)

En KDE Plasma 6, el motor anterior de sensores (ksysguard dataengines) ha sido descontinuado en favor de la biblioteca moderna **libksysguard** y sus interfaces de sensores. Para conectar la interfaz gráfica QML con datos reales de CPU y RAM, debemos instanciar objetos `Sensor` en `main.qml`:

```qml
import org.kde.ksysguard.sensors as Sensors

Sensors.Sensor {
    id: cpuSensor
    sensorId: "cpu/all/usage"
    updateInterval: 2000
    onValueChanged: {
        root.cpuUsage = value / 100.0; // Normalizar a rango 0.0 - 1.0
        coreRing.requestPaint();
    }
}

Sensors.Sensor {
    id: ramSensor
    sensorId: "memory/physical/usedPercent"
    updateInterval: 2000
    onValueChanged: {
        root.ramUsage = value / 100.0; // Normalizar a rango 0.0 - 1.0
        coreRing.requestPaint();
    }
}
```

### Sensores comunes en KDE:
*   `cpu/all/usage`: Uso total de procesador (porcentaje).
*   `memory/physical/usedPercent`: Porcentaje de memoria RAM en uso.
*   `battery/all/chargePercent`: Porcentaje de carga de batería (opcional para el indicador del Core).

---

## 2. Preparación y Diseño de Sprites (Mascotas)

Las imágenes reactivas del widget se ubicarán en `package/contents/assets/`. Las expresiones de los personajes (Tetsu y Hakka) cambiarán de forma dinámica según la carga de la CPU:

| Estado de CPU | Expresión | Archivo sugerido | Reacción visual |
| :--- | :--- | :--- | :--- |
| **Bajo** (<50%) | Idle (Relajado) | `tetsu_idle.webp` / `hakka_idle.webp` | Personaje durmiendo o sonriendo. |
| **Medio** (50% - 85%) | Focused (Concentrado) | `tetsu_focused.webp` / `hakka_focused.webp` | Personaje tecleando o mirando fijamente. |
| **Alto** (>85%) | Panic (Sobrecargado) | `tetsu_panic.webp` / `hakka_panic.webp` | Personaje sudando o asustado por sobrecalentamiento. |

*Nota*: Puedes utilizar imágenes con canal alfa transparente y formato WebP para minimizar el peso del paquete.

---

## 3. Próximos pasos sugeridos para la siguiente sesión

1.  **Añadir los Assets**: Guardar los sprites con fondo transparente en una nueva carpeta `package/contents/assets/`.
2.  **Activar los Sensores**: Descomentar y configurar los componentes `Sensors.Sensor` en `main.qml`.
3.  **Refinar el renderizado de Canvas**:
    *   Dibujar gradientes circulares para el anillo de CPU y RAM.
    *   Añadir efectos de brillo (*blur* o sombras) alrededor del cometa central para simular el resplandor de la energía del Core.
4.  **Habilitar Soporte para Temas del Sistema**: Usar la paleta de colores nativa de KDE (KDE Kirigami o PlasmaCore.ColorScope) para que el color por defecto del widget se adapte automáticamente al tema de escritorio claro/oscuro elegido por el usuario.
