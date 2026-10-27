#define AppName "Tommy's Time Tracking Tables"
#define AppVersion "0.3.0"
#define SourceRoot AddBackslash(SourcePath) + "..\artifacts\release\TommysTimeTrackingTables"

[Setup]
AppId={{17AA17B2-C8E2-4A45-A338-C24D60DAED75}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion} Preview
AppPublisher=Goatonastick
DefaultDirName={localappdata}\Programs\TommysTimeTrackingTables
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\TimersApp.exe
SetupIconFile=..\src\TimersApp.Desktop\Resources\T4.ico
OutputDir=..\artifacts\installer
OutputBaseFilename=Tommys-Time-Tracking-Tables-Setup-0.3.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
InfoBeforeFile=BeforeInstall.txt
UninstallDisplayName={#AppName}

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceRoot}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\TimersApp.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\TimersApp.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\TimersApp.exe"; Description: "Open {#AppName}"; Flags: nowait postinstall skipifsilent

; Workspace data is stored separately in %LOCALAPPDATA%\TimersApp.
; Deliberately no UninstallDelete entry: uninstall must preserve timers and backups.
