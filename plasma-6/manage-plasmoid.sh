#!/usr/bin/env bash

# manage-plasmoid.sh - Script para instalar, reinstalar o eliminar el plasmoide Kite Core System Monitor.

APPLET_ID="org.gargadon.kitecoresystemmonitor"
LOCALE_ID="plasma_applet_org.gargadon.kitecoresystemmonitor"

show_help() {
    echo "Uso: ./manage-plasmoid.sh [opción]"
    echo ""
    echo "Opciones:"
    echo "  install, -i      Compila traducciones e instala el plasmoide de forma limpia."
    echo "  uninstall, -u    Elimina el plasmoide y las traducciones instaladas del sistema."
    echo "  reinstall, -r    Realiza una desinstalación completa seguida de una instalación."
    echo "  help, -h         Muestra este mensaje de ayuda."
    echo ""
}

compile_translations() {
    echo "=== Compilando archivos de traducción (.po -> .mo) ==="
    if [ -f "po/compile.py" ]; then
        python3 po/compile.py
    else
        echo "Error: No se encontró po/compile.py"
        exit 1
    fi
}

install_plasmoid() {
    echo "=== Instalando el plasmoide usando kpackagetool6 ==="
    if [ -d "package" ]; then
        kpackagetool6 --type Plasma/Applet --install package/
    else
        echo "Error: No se encontró el directorio 'package/'"
        exit 1
    fi
}

remove_plasmoid() {
    echo "=== Eliminando el plasmoide del sistema ==="
    # Eliminar usando kpackagetool6 si es posible
    kpackagetool6 --type Plasma/Applet --remove $APPLET_ID 2>/dev/null
    
    # Asegurar borrado físico de la carpeta local
    local_dir="$HOME/.local/share/plasma/plasmoids/$APPLET_ID"
    if [ -d "$local_dir" ]; then
        rm -rf "$local_dir"
        echo "Carpeta local eliminada: $local_dir"
    fi

    # Eliminar archivos locales de traducción del sistema del usuario
    echo "=== Eliminando archivos de traducción locales del sistema ==="
    rm -f $HOME/.local/share/locale/*/LC_MESSAGES/${LOCALE_ID}.mo
    echo "Archivos .mo locales eliminados."
}

case "$1" in
    install|-i)
        compile_translations
        install_plasmoid
        echo "¡Instalación completada con éxito!"
        ;;
    uninstall|-u)
        remove_plasmoid
        echo "¡Eliminación completada con éxito!"
        ;;
    reinstall|-r)
        remove_plasmoid
        compile_translations
        install_plasmoid
        echo "¡Reinstalación completada con éxito!"
        ;;
    help|-h|"")
        show_help
        ;;
    *)
        echo "Opción no válida: $1"
        show_help
        exit 1
        ;;
esac
