import os
import subprocess

translations = {
    "es": {
        "Active Mascot Companion:": "Mascota activa:",
        "Display Metadata:": "Mostrar metadatos:",
        "Show Status and Battery Labels": "Mostrar etiquetas de estado y batería",
        "Kite Core Glow (Hex Color):": "Brillo de Kite Core (Color Hex):",
        "CORE STATUS: ONLINE": "ESTADO DEL NÚCLEO: EN LÍNEA",
        "BATTERY: 100%": "BATERÍA: 100%"
    },
    "pt": {
        "Active Mascot Companion:": "Mascote activa:",
        "Display Metadata:": "Mostrar metadatos:",
        "Show Status and Battery Labels": "Mostrar etiquetas de status e bateria",
        "Kite Core Glow (Hex Color):": "Brilho do Kite Core (Cor Hex):",
        "CORE STATUS: ONLINE": "STATUS DO NÚCLEO: ONLINE",
        "BATTERY: 100%": "BATERIA: 100%"
    },
    "fr": {
        "Active Mascot Companion:": "Mascotte active :",
        "Display Metadata:": "Afficher les métadonnées :",
        "Show Status and Battery Labels": "Afficher les étiquettes de statut et de batterie",
        "Kite Core Glow (Hex Color):": "Lueur de Kite Core (Couleur Hex) :",
        "CORE STATUS: ONLINE": "STATUT DU CŒUR : EN LIGNE",
        "BATTERY: 100%": "BATTERIE : 100 %"
    },
    "it": {
        "Active Mascot Companion:": "Mascotte attiva:",
        "Display Metadata:": "Mostra metadati:",
        "Show Status and Battery Labels": "Mostra le etichette di stato e batteria",
        "Kite Core Glow (Hex Color):": "Bagliore del Kite Core (Colore Hex):",
        "CORE STATUS: ONLINE": "STATO DEL CORE: IN LINEA",
        "BATTERY: 100%": "BATTERIA: 100%"
    },
    "de": {
        "Active Mascot Companion:": "Aktives Maskottchen:",
        "Display Metadata:": "Metadaten anzeigen:",
        "Show Status and Battery Labels": "Status- und Batterielabel anzeigen",
        "Kite Core Glow (Hex Color):": "Kite-Core-Glühen (Hex-Farbe):",
        "CORE STATUS: ONLINE": "KERNSTATUS: ONLINE",
        "BATTERY: 100%": "BATTERIE: 100%"
    },
    "ja": {
        "Active Mascot Companion:": "有効なマスコットキャラクター:",
        "Display Metadata:": "メタデータを表示:",
        "Show Status and Battery Labels": "ステータスとバッテリーのラベルを表示",
        "Kite Core Glow (Hex Color):": "Kite Coreの輝き (Hexカラー):",
        "CORE STATUS: ONLINE": "コアステータス: オンライン",
        "BATTERY: 100%": "バッテリー: 100%"
    },
    "zh_CN": {
        "Active Mascot Companion:": "启用的吉祥物伙伴:",
        "Display Metadata:": "显示元数据:",
        "Show Status and Battery Labels": "显示状态和电池标签",
        "Kite Core Glow (Hex Color):": "Kite Core 发光 (十六进制颜色):",
        "CORE STATUS: ONLINE": "核心状态: 在线",
        "BATTERY: 100%": "电池: 100%"
    },
    "ko": {
        "Active Mascot Companion:": "활성화된 마스코트:",
        "Display Metadata:": "메타데이터 표시:",
        "Show Status and Battery Labels": "상태 및 배터리 표시",
        "Kite Core Glow (Hex Color):": "Kite Core 광원 (Hex 색상):",
        "CORE STATUS: ONLINE": "코어 상태: 온라인",
        "BATTERY: 100%": "배터리: 100%"
    }
}

applet_id = "plasma_applet_org.gargadon.kitecoresystemmonitor"

# 1. Write the template file (.pot)
pot_content = [
    'msgid ""',
    'msgstr ""',
    '"Project-Id-Version: \\n"',
    '"POT-Creation-Date: \\n"',
    '"PO-Revision-Date: \\n"',
    '"Last-Translator: \\n"',
    '"Language-Team: \\n"',
    '"MIME-Version: 1.0\\n"',
    '"Content-Type: text/plain; charset=UTF-8\\n"',
    '"Content-Transfer-Encoding: 8bit\\n"',
    ""
]

# We use keys from "es" (all keys are same) as the source strings
for msgid in translations["es"].keys():
    pot_content.append(f'msgid "{msgid}"')
    pot_content.append('msgstr ""')
    pot_content.append("")

pot_path = "po/kitecoresystemmonitor.pot"
with open(pot_path, "w", encoding="utf-8") as f:
    f.write("\n".join(pot_content))
print(f"Generated template: {pot_path}")

# 2. Write and compile the .po files for each language
for lang, msgs in translations.items():
    po_content = [
        'msgid ""',
        'msgstr ""',
        '"Content-Type: text/plain; charset=UTF-8\\n"',
        '"Content-Transfer-Encoding: 8bit\\n"',
        f'"Language: {lang}\\n"',
        ""
    ]
    
    for msgid, msgstr in msgs.items():
        po_content.append(f'msgid "{msgid}"')
        po_content.append(f'msgstr "{msgstr}"')
        po_content.append("")
        
    po_path = f"po/{lang}.po"
    with open(po_path, "w", encoding="utf-8") as f:
        f.write("\n".join(po_content))
    print(f"Generated source PO file: {po_path}")
    
    # 1. Place compiled .mo in the package structure
    locale_dir = f"package/contents/locale/{lang}/LC_MESSAGES"
    os.makedirs(locale_dir, exist_ok=True)
    
    mo_path = f"{locale_dir}/{applet_id}.mo"
    subprocess.run(["msgfmt", "-o", mo_path, po_path], check=True)
    print(f"Compiled in package: {po_path} -> {mo_path}")
    
    # 2. Place compiled .mo in the user's local system locale directory so Plasma/gettext can find it
    user_locale_dir = os.path.expanduser(f"~/.local/share/locale/{lang}/LC_MESSAGES")
    os.makedirs(user_locale_dir, exist_ok=True)
    
    user_mo_path = f"{user_locale_dir}/{applet_id}.mo"
    subprocess.run(["msgfmt", "-o", user_mo_path, po_path], check=True)
    print(f"Compiled in user system: {po_path} -> {user_mo_path}")

print("All translations generated and compiled successfully!")
