; ============================================================
; TsubakiCursorApp Installer v0.3.0
; Self-contained - zero external dependencies
; ============================================================

#define MyAppName "TsubakiCursorApp"
#define MyAppVersion "0.3.0"
#define MyAppPublisher "MoriSakiTsu"
#define MyAppURL "https://github.com/MoriSakiTsu/TsubakiCursorApp"
#define MyAppExeName "TsubakiCursorApp.exe"

[Setup]
AppId={{B5E8F4A2-3C9D-4E1F-8A7B-2D4C6E8F0A1B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableWelcomePage=no
DisableDirPage=no
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=..\Output
OutputBaseFilename=TsubakiCursorApp-{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\Themes"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox(CustomMessage('MsgInstallSuccess'), mbInformation, MB_OK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDataDir := ExpandConstant('{localappdata}\TsubakiCursor');
    if DirExists(AppDataDir) then
    begin
      if MsgBox(CustomMessage('MsgUninstallUserData') + #13#10 + AppDataDir,
          mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(AppDataDir, True, True, True);
      end;
    end;
  end;
end;