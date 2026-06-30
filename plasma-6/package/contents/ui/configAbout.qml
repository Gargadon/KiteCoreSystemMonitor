import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import org.kde.kirigami as Kirigami

Item {
    id: configAbout
    width: 380
    height: 200

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 16
        spacing: 12

        Label {
            text: "Kite Core System Monitor"
            font.bold: true
            font.pointSize: 14
        }

        Label {
            text: i18n("Version 1.0.1")
            font.italic: true
        }

        Label {
            text: i18n("A system monitor widget themed around the Kite Core electronic medals from Kite Wars.")
            Layout.fillWidth: true
            wrapMode: Text.Wrap
        }

        Label {
            text: i18n("Developed by David Kantún")
        }

        Label {
            text: "GitHub: <a href=\"https://github.com/Gargadon/KiteCoreSystemMonitor\">https://github.com/Gargadon/KiteCoreSystemMonitor</a>"
            textFormat: Text.RichText
            onLinkActivated: (link) => Qt.openUrlExternally(link)
        }
        
        Item {
            Layout.fillHeight: true
        }
    }
}
