; ============================================================
; TsubakiCursorApp Installer
; Version: 0.2.0
; ============================================================

#define MyAppName "TsubakiCursorApp"
#define MyAppVersion "0.2.0"
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
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=..\Output
OutputBaseFilename=TsubakiCursorApp-{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ShowLanguageDialog=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

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
var
  RuntimeMissing: Boolean;

function IsRuntimeInstalled(): Boolean;
var
  VersionStr: String;
  MajorVersion: Integer;
begin
  Result := False;
  
  if RegQueryStringValue(HKLM, 
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', 
      'Version', VersionStr) then
  begin
    if Length(VersionStr) >= 2 then
    begin
      MajorVersion := StrToIntDef(Copy(VersionStr, 1, 2), 0);
      if MajorVersion >= 10 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
  
  if RegQueryStringValue(HKLM, 
      'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', 
      'Version', VersionStr) then
  begin
    if Length(VersionStr) >= 2 then
    begin
      MajorVersion := StrToIntDef(Copy(VersionStr, 1, 2), 0);
      if MajorVersion >= 10 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
  
  if RegQueryStringValue(HKLM, 
      'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App', 
      'Version', VersionStr) then
  begin
    if Length(VersionStr) >= 2 then
    begin
      MajorVersion := StrToIntDef(Copy(VersionStr, 1, 2), 0);
      if MajorVersion >= 10 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrCode: Integer;
begin
  Result := True;
  RuntimeMissing := not IsRuntimeInstalled();
  
  if RuntimeMissing then
  begin
    if MsgBox(CustomMessage('MsgRuntimeMissing'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/10.0', '', '', SW_SHOW, ewNoWait, ErrCode);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if RuntimeMissing then
    begin
      if MsgBox(CustomMessage('MsgInstallSuccessNoRuntime'), mbConfirmation, MB_YESNO) = IDYES then
      begin
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/10.0', '', '', SW_SHOW, ewNoWait, ErrCode);
      end;
    end
    else
    begin
      MsgBox(CustomMessage('MsgInstallSuccess'), mbInformation, MB_OK);
    end;
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