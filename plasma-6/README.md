# Kite Core System Monitor (KDE Plasma 6)

Un plasmoide para el escritorio **KDE Plasma 6** tematizado con el núcleo de energía (*Kite Core*) del universo de **Kite Wars Breakout**. Monitorea el uso de CPU y memoria RAM y reacciona de forma animada con las expresiones de Tetsu Kurobane y Hakka Aoyama según la carga de trabajo de tu sistema operativo.

---

## Estructura del Proyecto

```
KiteCoreSystemMonitor/
├── README.md                     # Instrucciones de uso
├── GEMINI.md                     # Plan de desarrollo y roadmap
├── po/                           # Archivos de traducción editables
│   ├── kitecoresystemmonitor.pot # Plantilla de traducciones
│   ├── [lang].po                 # Traducciones específicas de cada idioma
│   └── compile.py                # Script de compilación de traducciones
└── package/                      # Directorio raíz del plasmoide instalado
    ├── metadata.json             # Declaración de applet para Plasma 6 (con KPackageStructure)
    └── contents/
        ├── assets/               # Sprites chibi reactivos para Tetsu y Hakka
        ├── config/
        │   ├── main.xml          # Definición de variables persistentes
        │   └── config.qml        # Categorías del panel de configuración
        ├── locale/               # Archivos .mo binarios compilados para traducción
        └── ui/
            ├── main.qml          # Interfaz y renderizado con sensores, gradients, glow y auto-ajuste de texto
            └── configGeneral.qml # Interfaz gráfica de ajustes
```

---

## Cómo Instalar y Probar localmente

### Requisitos previos

Asegúrate de estar ejecutando **KDE Plasma 6** y contar con las herramientas de desarrollo de KDE:

```bash
# Verificar versión de Plasma
plasmashell --version
```

### 1. Instalación, reinstalación y eliminación rápida
Se ha incluido un script en bash en la raíz del proyecto para facilitar el ciclo de desarrollo local:

- **Instalar por primera vez**:
  ```bash
  ./manage-plasmoid.sh install
  ```
- **Reinstalar (aplicar cambios de código y traducciones)**:
  ```bash
  ./manage-plasmoid.sh reinstall
  ```
- **Eliminar del sistema por completo**:
  ```bash
  ./manage-plasmoid.sh uninstall
  ```

### 2. Ejecutar y testear sin reiniciar plasmashell
Puedes probar la interfaz del widget en una ventana aislada utilizando `plasmoidviewer`:

```bash
plasmoidviewer -a org.gargadon.kitecoresystemmonitor
```

---

## Localización e Idiomas

El widget está preparado para su traducción dinámica y cuenta con soporte de catálogo gettext para los siguientes idiomas: Español, Portugués, Francés, Italiano, Alemán, Japonés, Chino y Coreano.

### Modificar o agregar traducciones:
1. Abre el archivo `.po` correspondiente en la carpeta `po/` (ej. `po/es.po`).
2. Edita o añade las cadenas correspondientes en el bloque de traducciones.
3. Para compilar tus cambios de vuelta a la estructura de la aplicación, ejecuta el script helper en Python:

```bash
python3 po/compile.py
```

Esto generará automáticamente los archivos binarios `.mo` requeridos dentro de la carpeta `package/contents/locale/` y el widget estará listo para reinstalarse.

---
## Licencia

Este es un proyecto multi-licencia:
- El **código fuente** está bajo la Licencia [MIT](file:///home/gargadon/Source/KiteCoreSystemMonitor/LICENSE).
- Los **assets gráficos** (imágenes, sprites e ilustraciones en la raíz y en `package/contents/assets/`) están bajo la licencia [Creative Commons Attribution-NoDerivatives 4.0 International (CC BY-ND 4.0)](file:///home/gargadon/Source/KiteCoreSystemMonitor/LICENSE).

Consulta el archivo [LICENSE](file:///home/gargadon/Source/KiteCoreSystemMonitor/LICENSE) para más detalles.
