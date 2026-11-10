; Per-user installer. No admin and no code-signing certificate.
; Installs beside the portable zip, into %LOCALAPPDATA%\Programs\AutoKeyLight.
; Settings stay in %LOCALAPPDATA%\AutoKeyLight and are not removed.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

#define MyAppName "Auto Key Light"
#define MyAppExe "AutoKeyLight.exe"

[Setup]
AppId={{8F4E2C1A-6B37-4F0E-9C2D-7A1B5E9D4C33}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=AutoKeyLight
VersionInfoVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\AutoKeyLight
DisableDirPage=yes
DisableProgramGroupPage=yes
UsePreviousAppDir=no
PrivilegesRequired=lowest
OutputBaseFilename=AutoKeyLight-Setup
OutputDir=.
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
CloseApplicationsFilter={#MyAppExe}
RestartApplications=no
AppMutex=Techboy.AutoKeyLight
UninstallDisplayIcon={app}\{#MyAppExe}
UninstallDisplayName={#MyAppName}
SetupIconFile=..\AutoKeyLight\Resources\Icon.ico
LicenseFile=..\LICENSE
ChangesAssociations=no

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Comment: "Turn Elgato lights on and off with the webcam"

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Start {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsOurCommand(const Raw: string): Boolean;
var
  Text: string;
  ExeAt: Integer;
begin
  Text := Trim(Raw);
  if (Length(Text) >= 2) and (Text[1] = '"') then
  begin
    ExeAt := Pos('"', Copy(Text, 2, MaxInt));
    if ExeAt > 0 then
      Text := Copy(Text, 2, ExeAt - 1);
  end;
  Result := (Length(Text) >= 16) and (CompareText(Copy(Text, Length(Text) - 15, 16), 'AutoKeyLight.exe') = 0);
end;

procedure RewriteStartup;
var
  Raw: string;
  Quoted: string;
begin
  Quoted := '"' + ExpandConstant('{app}\{#MyAppExe}') + '"';
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoKeyLight', Raw) and IsOurCommand(Raw) then
    RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoKeyLight', Quoted);
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MyApp', Raw) and IsOurCommand(Raw) then
  begin
    RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoKeyLight', Quoted);
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'MyApp');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    RewriteStartup;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Raw: string;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoKeyLight', Raw) and IsOurCommand(Raw) then
  begin
    if CompareText(Raw, '"' + ExpandConstant('{app}\{#MyAppExe}') + '"') = 0 then
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'AutoKeyLight');
  end;
end;
