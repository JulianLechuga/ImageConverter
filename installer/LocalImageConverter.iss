; =====================================================================
; Inno Setup Script for Local Image Converter
; Windows 10/11 64-bit Native Desktop Application
; 100% Offline & Private Image Converter
; =====================================================================

#define MyAppName "Local Image Converter"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Local Image Converter"
#define MyAppURL "https://github.com/JulianLechuga/Menu-app"
#define MyAppExeName "LocalImageConverter.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".lic"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; App Identity
AppId={{C8E28B93-5A3E-4B7D-9F4D-3725F18E9B12}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation Paths & Architecture
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Privilege mode (Allows both per-user or administrative install)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Output Configuration
OutputDir=..\dist
OutputBaseFilename=LocalImageConverter-Setup-{#MyAppVersion}
SetupIconFile=..\src\LocalImageConverter.App\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Visual & Metadata
DisableProgramGroupPage=yes
LicenseFile=..\THIRD_PARTY_NOTICES.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application binaries and self-contained .NET runtimes from publish folder
Source: "..\dist\LocalImageConverter\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
