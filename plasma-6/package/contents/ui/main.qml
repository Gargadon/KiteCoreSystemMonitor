import QtQuick
import QtQuick.Layouts
import QtQuick.Controls
import org.kde.plasma.core as PlasmaCore
import org.kde.plasma.plasmoid
import org.kde.plasma.components as PlasmaComponents
import org.kde.ksysguard.sensors as Sensors
import org.kde.kirigami as Kirigami

PlasmoidItem {
    id: root
    
    // Default size for the desktop widget
    width: 240
    height: 240
    
    // Disable default background to draw our own custom opacity background
    Plasmoid.backgroundHints: PlasmaCore.Types.NoBackground

    // Plasmoid configuration properties
    readonly property string mascot: Plasmoid.configuration.mascot // "tetsu" or "hakka"
    readonly property bool showLabels: Plasmoid.configuration.showLabels
    readonly property color coreColor: Plasmoid.configuration.coreColor
    readonly property double bgOpacity: Plasmoid.configuration.opacity
    
    // Determine the active glow color based on the configuration or theme default
    readonly property color activeCoreColor: {
        var col = root.coreColor.toString();
        if (col === "" || col === "#00000000" || col === "transparent" || col === "#000000") {
            return Kirigami.Theme.highlightColor;
        }
        return root.coreColor;
    }
    
    // CPU and RAM usage properties (updated by sensors)
    property real cpuUsage: 0.0
    property real ramUsage: 0.0
    
    // KSystemStats Sensor Objects
    Sensors.Sensor {
        id: cpuSensor
        sensorId: "cpu/all/usage"
        onValueChanged: {
            root.cpuUsage = value / 100.0;
            coreRing.requestPaint();
        }
    }

    Sensors.Sensor {
        id: ramSensor
        sensorId: "memory/physical/usedPercent"
        onValueChanged: {
            root.ramUsage = value / 100.0;
            coreRing.requestPaint();
        }
    }
    
    // Core rotation animation for a "live" feel
    RotationAnimation {
        id: coreRotator
        target: coreRing
        from: 0
        to: 360
        duration: 8000
        loops: Animation.Infinite
        running: true
    }

    // Custom Background Rectangle with Opacity config
    Rectangle {
        anchors.fill: parent
        color: "#121212"
        opacity: root.bgOpacity
        radius: 20
        border.width: 1.5
        border.color: "#35ffffff"
    }

    // Outer Layout
    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 8
        spacing: 4

        // 1. Kite Core Medal Visual
        Item {
            Layout.fillWidth: true
            Layout.fillHeight: true
            Layout.alignment: Qt.AlignHCenter
            
            // Outer glow ring
            Rectangle {
                id: outerGlow
                anchors.fill: parent
                anchors.margins: 4
                radius: width / 2
                color: "transparent"
                border.width: 3
                border.color: root.activeCoreColor
                opacity: 0.3
            }

            // Spinning core indicators
            Canvas {
                id: coreRing
                anchors.fill: parent
                anchors.margins: 12
                
                onPaint: {
                    var ctx = getContext("2d");
                    ctx.reset();
                    
                    var centerX = width / 2;
                    var centerY = height / 2;
                    var radius = Math.min(width, height) / 2 - 12;
                    
                    // Setup common shadow/glow style
                    ctx.lineCap = "round";
                    ctx.lineWidth = 8;
                    
                    // Draw CPU arc (Left side) with glowing gradient
                    ctx.beginPath();
                    var cpuGrad = ctx.createLinearGradient(centerX - radius, centerY + radius, centerX + radius, centerY - radius);
                    cpuGrad.addColorStop(0, root.activeCoreColor);
                    cpuGrad.addColorStop(1, Qt.lighter(root.activeCoreColor, 1.5));
                    
                    ctx.strokeStyle = cpuGrad;
                    ctx.shadowBlur = 15;
                    ctx.shadowColor = root.activeCoreColor;
                    ctx.arc(centerX, centerY, radius, Math.PI * 0.75, Math.PI * (0.75 + root.cpuUsage * 0.9), false);
                    ctx.stroke();

                    // Draw RAM arc (Right side) with glowing gradient
                    ctx.beginPath();
                    var ramGrad = ctx.createLinearGradient(centerX - radius, centerY - radius, centerX + radius, centerY + radius);
                    ramGrad.addColorStop(0, "#10b981");
                    ramGrad.addColorStop(1, "#34d399");
                    
                    ctx.strokeStyle = ramGrad;
                    ctx.shadowBlur = 15;
                    ctx.shadowColor = "#10b981";
                    ctx.arc(centerX, centerY, radius, Math.PI * 1.75, Math.PI * (1.75 + root.ramUsage * 0.9), false);
                    ctx.stroke();
                }
                
                // Redraw on stats change
                onWidthChanged: requestPaint()
                onHeightChanged: requestPaint()
            }
            
            // Central mascot / display
            ColumnLayout {
                anchors.centerIn: parent
                spacing: 2
                
                // Mascot image (reacts to CPU usage)
                Image {
                    id: mascotSprite
                    Layout.preferredWidth: 64
                    Layout.preferredHeight: 64
                    Layout.alignment: Qt.AlignHCenter
                    fillMode: Image.PreserveAspectFit
                    
                    // Reactive expressions
                    source: {
                        var base = "../assets/" + root.mascot + "_";
                        if (root.cpuUsage > 0.85) {
                            return base + "panic.webp"; // Exhausted/overheated
                        } else if (root.cpuUsage > 0.50) {
                            return base + "focused.webp"; // Concentrated
                        } else {
                            return base + "idle.webp"; // Relaxed
                        }
                    }
                }
                
                // Digital Stats Text
                Text {
                    Layout.alignment: Qt.AlignHCenter
                    text: Math.round(root.cpuUsage * 100) + "% CPU"
                    font.family: "Monospace"
                    font.pixelSize: 11
                    font.bold: true
                    color: root.cpuUsage > 0.85 ? "#ef4444" : "#ffffff"
                }
                
                Text {
                    Layout.alignment: Qt.AlignHCenter
                    text: Math.round(root.ramUsage * 100) + "% RAM"
                    font.family: "Monospace"
                    font.pixelSize: 10
                    color: "#a0a0a0"
                }
            }
        }

        // 2. Optional Labels/Legends
        RowLayout {
            Layout.fillWidth: true
            Layout.preferredHeight: root.showLabels ? 20 : 0
            visible: root.showLabels
            
            Text {
                Layout.fillWidth: true
                Layout.maximumWidth: parent.width * 0.6
                text: i18n("CORE STATUS: ONLINE")
                font.pixelSize: 9
                font.bold: true
                color: root.activeCoreColor
                elide: Text.ElideRight
                fontSizeMode: Text.Fit
                minimumPixelSize: 7
            }
            
            Item { Layout.fillWidth: true }
            
            Text {
                Layout.alignment: Qt.AlignRight
                text: i18n("BATTERY: 100%")
                font.pixelSize: 9
                color: "#a0a0a0"
                fontSizeMode: Text.Fit
                minimumPixelSize: 7
            }
        }
    }
}
