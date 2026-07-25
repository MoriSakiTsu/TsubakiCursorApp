; ============================================================
; TsubakiCursorApp Installer
; Version: 0.1.0-preview
; ============================================================

#define MyAppName "TsubakiCursorApp"
#define MyAppVersion "0.1.0-preview"
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

[Languages]
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

[Run]
Filename: "{tmp}\dotnet-install.ps1"; Parameters: "-Channel 10.0 -Runtime windowsdesktop -InstallDir ""{commonpf}\dotnet"" -NoPath"; \
    Check: NeedsDotNetRuntime; StatusMsg: "Installing .NET 10 Desktop Runtime..."; \
    BeforeInstall: DownloadDotNetInstallScript;

[Code]
// ============================================
// .NET 10 Runtime detection and auto-install
// ============================================

var
  DotNetInstallScriptDownloaded: Boolean;

function GetDotNetInstallPath(): String;
begin
  Result := ExpandConstant('{commonpf}\dotnet\dotnet.exe');
  if FileExists(Result) then Exit;
  
  Result := ExpandConstant('{commonpf32}\dotnet\dotnet.exe');
  if FileExists(Result) then Exit;
  
  Result := ExpandConstant('{localappdata}\Microsoft\dotnet\dotnet.exe');
  if FileExists(Result) then Exit;
  
  Result := '';
end;

function IsRuntimeInstalled(): Boolean;
var
  VersionStr: String;
  MajorVersion: Integer;
begin
  Result := false;
  
  // Check x64 shared framework
  if RegQueryStringValue(HKLM, 
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', 
      'Version', VersionStr) then
  begin
    if Length(VersionStr) >= 2 then
    begin
      MajorVersion := StrToIntDef(Copy(VersionStr, 1, 2), 0);
      if MajorVersion >= 10 then
      begin
        Result := true;
        Exit;
      end;
    end;
  end;
  
  // Check WOW6432Node
  if RegQueryStringValue(HKLM, 
      'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', 
      'Version', VersionStr) then
  begin
    if Length(VersionStr) >= 2 then
    begin
      MajorVersion := StrToIntDef(Copy(VersionStr, 1, 2), 0);
      if MajorVersion >= 10 then
      begin
        Result := true;
        Exit;
      end;
    end;
  end;
  
  // Check x86 shared framework
  if RegQueryStringValue(HKLM, 
      'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App', 
      'Version', VersionStr) then
  begin
    if Length(VersionStr) >= 2 then
    begin
      MajorVersion := StrToIntDef(Copy(VersionStr, 1, 2), 0);
      if MajorVersion >= 10 then
      begin
        Result := true;
        Exit;
      end;
    end;
  end;
end;

function NeedsDotNetRuntime(): Boolean;
begin
  Result := not IsRuntimeInstalled();
end;

procedure DownloadDotNetInstallScript();
var
  ScriptPath: String;
  ResultCode: Integer;
begin
  ScriptPath := ExpandConstant('{tmp}\dotnet-install.ps1');
  
  if FileExists(ScriptPath) then
  begin
    DotNetInstallScriptDownloaded := true;
    Exit;
  end;
  
  DotNetInstallScriptDownloaded := false;
  
  if not Exec(ExpandConstant('{powershell}'), 
      '-ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri ' + 
      '''https://dot.net/v1/dotnet-install.ps1'' -OutFile ''' + 
      ScriptPath + ''' -UseBasicParsing -ErrorAction Stop"', 
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Exit;
    
  if (ResultCode = 0) and FileExists(ScriptPath) then
    DotNetInstallScriptDownloaded := true;
end;

function InitializeSetup(): Boolean;
var
  NeedsInstall: Boolean;
begin
  Result := true;
  DotNetInstallScriptDownloaded := false;
  
  NeedsInstall := NeedsDotNetRuntime();
  
  if NeedsInstall then
  begin
    if MsgBox(
      '.NET 10 Desktop Runtime is required to run TsubakiCursorApp.' + #13#10 + #13#10 +
      'Do you want the installer to download and install it automatically?' + #13#10 +
      '(Approx. 60 MB, internet connection required)' + #13#10 + #13#10 +
      'Click Yes to install, No to skip (app may not launch).',
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      MsgBox(
        'You chose to skip .NET Runtime installation.' + #13#10 + #13#10 +
        'TsubakiCursorApp requires .NET 10 Desktop Runtime to run.' + #13#10 +
        'You can manually download it from:' + #13#10 +
        'https://dotnet.microsoft.com/download/dotnet/10.0',
        mbInformation, MB_OK);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if NeedsDotNetRuntime() then
    begin
      MsgBox(
        'Installation complete, but .NET 10 Desktop Runtime appears to be missing.' + #13#10 + #13#10 +
        'If the application fails to start, please manually install from:' + #13#10 +
        'https://dotnet.microsoft.com/download/dotnet/10.0',
        mbInformation, MB_OK);
    end
    else
    begin
      MsgBox(
        'TsubakiCursorApp has been installed successfully!' + #13#10 + #13#10 +
        'A "Themes" folder has been created in the program directory' + #13#10 +
        'for remote theme downloads.',
        mbInformation, MB_OK);
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
      if MsgBox(
        'Do you also want to remove user data?' + #13#10 +
        'This will delete downloaded themes and backups:' + #13#10 +
        AppDataDir,
        mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(AppDataDir, true, true, true);
      end;
    end;
  end;
end;