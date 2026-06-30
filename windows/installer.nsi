; installer.nsi - NSIS Script for Kite Core System Monitor
Unicode true

; Product Name and Publisher
!define PRODUCT_NAME "Kite Core System Monitor"
!define PRODUCT_VERSION "1.0.1"
!define PRODUCT_PUBLISHER "Kite Core Dev"
!define PRODUCT_WEB_SITE "https://github.com/dkant/KiteCoreSystemMonitor"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\KiteCoreWindowsMonitor.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; Target Architecture
!include "x64.nsh"
!ifndef ARCH
  !define ARCH "win-x64"
!endif

; Modern UI 2
!include "MUI2.nsh"

; MUI Settings
!define MUI_ABORTWARNING
!define MUI_ICON "Assets\AppIcon.ico"
!define MUI_UNICON "Assets\AppIcon.ico"

; Installer Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Uninstaller Pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "English"

; General Settings
Name "${PRODUCT_NAME}"
OutFile "bin\Release\net10.0-windows10.0.26100.0\${ARCH}\KiteCoreSystemMonitor_Setup_${ARCH}.exe"
InstallDir "$PROGRAMFILES64\KiteCoreSystemMonitor"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUninstDetails show

Section "Principal" SEC01
  ; Ensure we install in Program Files (64-bit)
  ${If} ${RunningX64}
    SetRegView 64
  ${EndIf}

  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  
  ; Copy the publish output recursively (EXE, DLLs, assets)
  File /r "bin\Release\net10.0-windows10.0.26100.0\${ARCH}\publish\*.*"
  File "Assets\AppIcon.ico"
  
  ; Create shortcuts
  CreateDirectory "$SMPROGRAMS\Kite Core System Monitor"
  CreateShortcut "$SMPROGRAMS\Kite Core System Monitor\Kite Core System Monitor.lnk" "$INSTDIR\KiteCoreWindowsMonitor.exe" "" "$INSTDIR\AppIcon.ico"
  CreateShortcut "$DESKTOP\Kite Core System Monitor.lnk" "$INSTDIR\KiteCoreWindowsMonitor.exe" "" "$INSTDIR\AppIcon.ico"
  
  ; Write registry keys for installation path
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\KiteCoreWindowsMonitor.exe"
  
  ; Write uninstaller
  WriteUninstaller "$INSTDIR\uninstaller.exe"
  
  ; Write registry keys for Windows Add/Remove Programs
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninstaller.exe"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\AppIcon.ico"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
SectionEnd

Section Uninstall
  ${If} ${RunningX64}
    SetRegView 64
  ${EndIf}

  ; Delete shortcuts
  Delete "$SMPROGRAMS\Kite Core System Monitor\Kite Core System Monitor.lnk"
  Delete "$DESKTOP\Kite Core System Monitor.lnk"
  RMDir "$SMPROGRAMS\Kite Core System Monitor"
  
  ; Delete installation directory recursively (removes all DLLs, EXE, assets, uninstaller)
  RMDir /r "$INSTDIR"
  
  ; Delete registry entries
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  DeleteRegKey HKLM "${PRODUCT_UNINST_KEY}"
SectionEnd
