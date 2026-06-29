import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import org.kde.plasma.core as PlasmaCore
import org.kde.plasma.components as PlasmaComponents

Item {
    id: configGeneral
    
    // Width and height guidelines for the settings window
    width: 380
    height: 200

    // Properties prefixed with cfg_ are automatically bound by Plasma config engine
    property alias cfg_mascot: mascotCombo.currentValue
    property alias cfg_showLabels: showLabelsCheck.checked
    property alias cfg_coreColor: coreColorInput.text

    GridLayout {
        anchors.fill: parent
        anchors.margins: 16
        columns: 2
        rowSpacing: 16
        columnSpacing: 12

        // Mascot Selection
        Label {
            text: i18n("Active Mascot Companion:")
            Layout.alignment: Qt.AlignVCenter
        }
        ComboBox {
            id: mascotCombo
            Layout.fillWidth: true
            model: ListModel {
                id: mascotModel
                ListElement { text: "Tetsuya \"Tetsu\" Kurobane (Owner)"; value: "tetsu" }
                ListElement { text: "Souta \"Hakka\" Aoyama (Hacker)"; value: "hakka" }
            }
            textRole: "text"
            valueRole: "value"
            
            // Find current index based on loaded config
            Component.onCompleted: {
                mascotModel.setProperty(0, "text", i18n("Tetsuya \"Tetsu\" Kurobane (Owner)"));
                mascotModel.setProperty(1, "text", i18n("Souta \"Hakka\" Aoyama (Hacker)"));
                for (var i = 0; i < model.count; i++) {
                    if (model.get(i).value === Plasmoid.configuration.mascot) {
                        currentIndex = i;
                        break;
                    }
                }
            }
        }

        // Labels Toggle
        Label {
            text: i18n("Display Metadata:")
            Layout.alignment: Qt.AlignVCenter
        }
        CheckBox {
            id: showLabelsCheck
            text: i18n("Show Status and Battery Labels")
            Layout.fillWidth: true
        }

        // Core Color customization
        Label {
            text: i18n("Kite Core Glow (Hex Color):")
            Layout.alignment: Qt.AlignVCenter
        }
        TextField {
            id: coreColorInput
            Layout.fillWidth: true
            placeholderText: "#f59e0b"
        }
    }
}
