#define AppName "CalendarMaker"
#define AppExeName "CalendarMaker.exe"
#define AppPublisher "CalendarMaker"

; These can be overridden from the command line via ISCC.exe /D...
#ifndef PublishDir
  #define PublishDir "..\\dist\\publish\\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\\dist\\installer"
#endif
#ifndef AppVersion
  #define AppVersion GetFileVersion(PublishDir + "\\" + AppExeName)
#endif

[Setup]
AppId={{4D78E06B-7C2E-41C5-BFD0-7B77C8E1C26A}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#AppExeName}
PrivilegesRequired=lowest

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
