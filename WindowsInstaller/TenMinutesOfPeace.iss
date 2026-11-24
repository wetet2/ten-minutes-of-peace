#define GameExe "TenMinutesOfPeace.exe"
#define PlayerDir "..\Builds\Windows"
#define FontDir "..\Assets\Resources\Fonts"
#ifndef GameVersion
  #define GameVersion "1.0"
#endif

[Setup]
AppId=PeacePark.TenMinutesOfPeace.Windows
AppName={cm:GameName}
AppVersion={#GameVersion}
AppPublisher=Peace Park
VersionInfoVersion={#GameVersion}
VersionInfoDescription=Ten Minutes of Peace Setup
VersionInfoProductName=Ten Minutes of Peace
DefaultDirName={localappdata}\Programs\Ten Minutes of Peace
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#GameExe}
RestartApplications=no
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir={#SourcePath}{#PlayerDir}
OutputBaseFilename=TenMinutesOfPeace-Setup

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[CustomMessages]
english.GameName=Ten Minutes of Peace
korean.GameName=유아용 게임: 십분의 평화
english.GameShortcut=Ten Minutes of Peace
korean.GameShortcut=십분의 평화
english.LaunchGame=Launch Ten Minutes of Peace
korean.LaunchGame=십분의 평화 실행

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PlayerDir}\{#GameExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PlayerDir}\UnityCrashHandler64.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PlayerDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PlayerDir}\TenMinutesOfPeace_Data\*"; DestDir: "{app}\TenMinutesOfPeace_Data"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PlayerDir}\MonoBleedingEdge\*"; DestDir: "{app}\MonoBleedingEdge"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PlayerDir}\D3D12\*"; DestDir: "{app}\D3D12"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#FontDir}\BaskinRobbins-NOTICE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#FontDir}\ComicRelief-OFL.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{cm:GameShortcut}"; Filename: "{app}\{#GameExe}"; WorkingDir: "{app}"
Name: "{autodesktop}\{cm:GameShortcut}"; Filename: "{app}\{#GameExe}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#GameExe}"; Description: "{cm:LaunchGame}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
