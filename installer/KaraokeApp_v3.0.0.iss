[Setup]
AppName=KaraokeApp
AppVersion=3.0.0
DefaultDirName={autopf}\KaraokeApp
DefaultGroupName=KaraokeApp
DisableProgramGroupPage=no
OutputDir=..\installer_output
OutputBaseFilename=KaraokeApp_Setup_v3.0.0
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "..\release_package\KaraokeApp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\release_package\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\KaraokeApp"; Filename: "{app}\KaraokeApp.exe"
Name: "{userdesktop}\KaraokeApp"; Filename: "{app}\KaraokeApp.exe"; Tasks: desktopicon

[Registry]
Root: HKCR; Subkey: ".mid"; ValueType: string; ValueName: ""; ValueData: "KaraokeApp.mid"; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".kar"; ValueType: string; ValueName: ""; ValueData: "KaraokeApp.kar"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "KaraokeApp.mid\shell\open\command"; ValueType: string; ValueName: ""; ValueData: '"{app}\KaraokeApp.exe" "%1"'
Root: HKCR; Subkey: "KaraokeApp.kar\shell\open\command"; ValueType: string; ValueName: ""; ValueData: '"{app}\KaraokeApp.exe" "%1"'

[Run]
Filename: "{app}\KaraokeApp.exe"; Description: "Launch KaraokeApp"; Flags: nowait postinstall skipifsilent
