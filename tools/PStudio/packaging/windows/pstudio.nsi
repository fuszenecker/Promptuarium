; The Windows installer of PStudio (NSIS, after the one of PentaGrammata).
;
; Built by build-windows-installer.ps1, which publishes PStudio (win-x64, self-contained) and runs:
;   makensis -DAPP_VERSION=8.1.0 -DPUBLISH_DIR=<publish folder> -DOUT_FILE=<setup.exe> pstudio.nsi
;
; It installs PStudio and pedit, its command line counterpart, for every user (64-bit Program Files), with Start Menu
; entries (PStudio, and a command prompt for pedit), an optional desktop shortcut, the .prmt file association
; (optional) and an entry in Apps / Add or Remove Programs. Installing again (e.g. a newer
; version) removes the previous installation first.

Unicode True
SetCompressor /SOLID lzma

;--------------------------------
; Definitions (APP_VERSION, PUBLISH_DIR and OUT_FILE come from the command line)

!ifndef APP_VERSION
  !define APP_VERSION "0.0.0"
!endif
!ifndef PUBLISH_DIR
  !define PUBLISH_DIR "..\..\..\..\publish\pstudio-win-x64"
!endif
!ifndef OUT_FILE
  !define OUT_FILE "pstudio-${APP_VERSION}-win-x64-setup.exe"
!endif
; The four numbers of the file version: 8.1.0 is 8.1.0.0 (a pre-release suffix is left out by the build script)
!ifndef VI_VERSION
  !define VI_VERSION "0.0.0.0"
!endif

!define APP_NAME      "PStudio"
!define APP_LONG_NAME "Promptuarium Studio"
!define APP_PUBLISHER "Fuszenecker Róbert"
!define APP_EXE       "pstudio.exe"
!define CLI_EXE       "pedit.exe"
!define APP_URL       "https://github.com/fuszenecker/Promptuarium"
!define APP_ICON      "..\..\Assets\pstudio.ico"
!define REG_APP       "Software\Promptuarium\PStudio"
!define REG_UNINSTALL "Software\Microsoft\Windows\CurrentVersion\Uninstall\PStudio"

; The file type of the Promptuarium containers (as the Debian package registers application/x-promptuarium)
!define FILE_EXT      ".prmt"
!define FILE_PROGID   "Promptuarium.Container"
!define FILE_TYPE     "Promptuarium container"
!define FILE_MIME     "application/x-promptuarium"

;--------------------------------
; Includes

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "x64.nsh"

;--------------------------------
; The installer

Name             "${APP_NAME} ${APP_VERSION}"
Caption          "${APP_LONG_NAME} ${APP_VERSION} Setup"
OutFile          "${OUT_FILE}"
InstallDir       "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "${REG_APP}" "InstallDir"
RequestExecutionLevel admin
BrandingText     "${APP_LONG_NAME} ${APP_VERSION}"
ShowInstDetails  hide
ShowUninstDetails hide

VIProductVersion "${VI_VERSION}"
VIAddVersionKey "ProductName"     "${APP_LONG_NAME}"
VIAddVersionKey "ProductVersion"  "${APP_VERSION}"
VIAddVersionKey "FileVersion"     "${APP_VERSION}"
VIAddVersionKey "FileDescription" "${APP_LONG_NAME} Setup"
VIAddVersionKey "CompanyName"     "${APP_PUBLISHER}"
VIAddVersionKey "LegalCopyright"  "Copyright 2012-2026, ${APP_PUBLISHER}"

;--------------------------------
; Pages

!define MUI_ICON   "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"
!define MUI_ABORTWARNING
!define MUI_COMPONENTSPAGE_NODESC
!define MUI_FINISHPAGE_RUN      "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Start ${APP_NAME}"
!define MUI_FINISHPAGE_LINK          "Promptuarium on GitHub"
!define MUI_FINISHPAGE_LINK_LOCATION "${APP_URL}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\..\..\LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; Initialization: a 64-bit Windows, the 64-bit registry, and the folder of an earlier installation

Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "${APP_NAME} needs a 64-bit Windows."
    Abort
  ${EndIf}

  SetRegView 64
  ReadRegStr $0 HKLM "${REG_APP}" "InstallDir"
  ${If} $0 != ""
    StrCpy $INSTDIR $0
  ${EndIf}
FunctionEnd

Function un.onInit
  SetRegView 64
FunctionEnd

;--------------------------------
; Sections

Section "${APP_NAME}" SecMain
  SectionIn RO

  ; An earlier installation is removed first, so that no file of the old version stays behind
  ${If} ${FileExists} "$INSTDIR\uninstall.exe"
    ExecWait '"$INSTDIR\uninstall.exe" /S _?=$INSTDIR'
    Delete "$INSTDIR\uninstall.exe"
  ${EndIf}

  SetOutPath "$INSTDIR"
  File /r "${PUBLISH_DIR}\*.*"
  WriteUninstaller "$INSTDIR\uninstall.exe"

  WriteRegStr HKLM "${REG_APP}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${REG_APP}" "Version"    "${APP_VERSION}"

  ; Apps / Add or Remove Programs
  WriteRegStr   HKLM "${REG_UNINSTALL}" "DisplayName"          "${APP_LONG_NAME}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "DisplayVersion"       "${APP_VERSION}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "Publisher"            "${APP_PUBLISHER}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "InstallLocation"      "$INSTDIR"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "DisplayIcon"          "$INSTDIR\${APP_EXE}"
  WriteRegStr   HKLM "${REG_UNINSTALL}" "UninstallString"      '"$INSTDIR\uninstall.exe"'
  WriteRegStr   HKLM "${REG_UNINSTALL}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
  WriteRegStr   HKLM "${REG_UNINSTALL}" "URLInfoAbout"         "${APP_URL}"
  WriteRegDWORD HKLM "${REG_UNINSTALL}" "NoModify" 1
  WriteRegDWORD HKLM "${REG_UNINSTALL}" "NoRepair" 1

  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "${REG_UNINSTALL}" "EstimatedSize" "$0"

  ; The Start Menu entry (for every user)
  SetShellVarContext all
  CreateShortcut "$SMPROGRAMS\${APP_LONG_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0

  ; pedit is a command line tool: a command prompt in its folder, with its help
  CreateShortcut "$SMPROGRAMS\pedit (command prompt).lnk" "$SYSDIR\cmd.exe" '/K "cd /d "$INSTDIR" && pedit help"' \
    "$SYSDIR\cmd.exe" 0 SW_SHOWNORMAL "" "pedit, the command line tool of Promptuarium trees"
SectionEnd

Section "Open .prmt files with ${APP_NAME}" SecAssociation
  ; The program, its file type, and the extension (also offered in "Open with")
  WriteRegStr HKLM "Software\Classes\${FILE_PROGID}" "" "${FILE_TYPE}"
  WriteRegStr HKLM "Software\Classes\${FILE_PROGID}\DefaultIcon" "" "$INSTDIR\${APP_EXE},0"
  WriteRegStr HKLM "Software\Classes\${FILE_PROGID}\shell\open\command" "" '"$INSTDIR\${APP_EXE}" "%1"'
  WriteRegStr HKLM "Software\Classes\${FILE_EXT}" "" "${FILE_PROGID}"
  WriteRegStr HKLM "Software\Classes\${FILE_EXT}" "Content Type" "${FILE_MIME}"
  WriteRegStr HKLM "Software\Classes\${FILE_EXT}\OpenWithProgids" "${FILE_PROGID}" ""
  WriteRegStr HKLM "Software\Classes\Applications\${APP_EXE}" "FriendlyAppName" "${APP_LONG_NAME}"
  WriteRegStr HKLM "Software\Classes\Applications\${APP_EXE}\SupportedTypes" "${FILE_EXT}" ""
  WriteRegStr HKLM "${REG_APP}" "FileAssociation" "1"

  ; Explorer shows the new icon and the new program at once
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'
SectionEnd

Section /o "Desktop shortcut" SecDesktop
  SetShellVarContext all
  CreateShortcut "$DESKTOP\${APP_LONG_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
SectionEnd

;--------------------------------
; Uninstaller

Section "Uninstall"
  ; A running PStudio or pedit would keep its files
  nsExec::Exec 'taskkill /F /IM "${APP_EXE}"'
  nsExec::Exec 'taskkill /F /IM "${CLI_EXE}"'

  SetShellVarContext all
  Delete "$SMPROGRAMS\${APP_LONG_NAME}.lnk"
  Delete "$SMPROGRAMS\pedit (command prompt).lnk"
  Delete "$DESKTOP\${APP_LONG_NAME}.lnk"

  ; The file association, only if it is still the one of PStudio
  ReadRegStr $0 HKLM "Software\Classes\${FILE_EXT}" ""
  ${If} $0 == "${FILE_PROGID}"
    DeleteRegValue HKLM "Software\Classes\${FILE_EXT}" ""
  ${EndIf}
  DeleteRegValue HKLM "Software\Classes\${FILE_EXT}\OpenWithProgids" "${FILE_PROGID}"
  DeleteRegKey /ifempty HKLM "Software\Classes\${FILE_EXT}\OpenWithProgids"
  DeleteRegKey HKLM "Software\Classes\${FILE_PROGID}"
  DeleteRegKey HKLM "Software\Classes\Applications\${APP_EXE}"
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0, p 0, p 0)'

  DeleteRegKey HKLM "${REG_UNINSTALL}"
  DeleteRegKey HKLM "${REG_APP}"
  DeleteRegKey /ifempty HKLM "Software\Promptuarium"

  ; The folder is removed only if it is the folder of PStudio (it has pstudio.exe), never e.g. Program Files itself
  ${If} ${FileExists} "$INSTDIR\${APP_EXE}"
    RMDir /r "$INSTDIR"
  ${Else}
    Delete "$INSTDIR\uninstall.exe"
    RMDir "$INSTDIR"
  ${EndIf}
SectionEnd
