; Per-user installer. No admin and no code-signing certificate.
; Installs beside the portable zip, into %LOCALAPPDATA%\Programs\CamOnKeyLight.
; Settings stay in %LOCALAPPDATA%\CamOnKeyLight and are not removed.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

#define MyAppName "Cam On Key Light"
#define MyAppExe "CamOnKeyLight.exe"

[Setup]
AppId={{B6D91F42-7C18-4E5A-9A30-2F84C7D1E6AB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=CamOnKeyLight
VersionInfoVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\CamOnKeyLight
DisableDirPage=yes
DisableProgramGroupPage=yes
UsePreviousAppDir=no
PrivilegesRequired=lowest
OutputBaseFilename=CamOnKeyLight-Setup
OutputDir=.
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
CloseApplicationsFilter=CamOnKeyLight.exe,AutoKeyLight.exe
RestartApplications=no
AppMutex=Techboy.CamOnKeyLight
UninstallDisplayIcon={app}\{#MyAppExe}
UninstallDisplayName={#MyAppName}
SetupIconFile=..\CamOnKeyLight\Resources\Icon.ico
LicenseFile=..\LICENSE
ChangesAssociations=no

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Comment: "Turn Elgato lights on and off with the webcam"

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Start {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  OldUninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{8F4E2C1A-6B37-4F0E-9C2D-7A1B5E9D4C33}_is1';

var
  StartupWasOn: Boolean;

function EndsWithText(const Value, Suffix: string): Boolean;
begin
  Result := (Length(Value) >= Length(Suffix)) and (CompareText(Copy(Value, Length(Value) - Length(Suffix) + 1, Length(Suffix)), Suffix) = 0);
end;

function Unquote(const Value: string): string;
var
  Text: string;
begin
  Text := Trim(Value);
  if (Length(Text) >= 2) and (Text[1] = '"') and (Text[Length(Text)] = '"') then
    Text := Copy(Text, 2, Length(Text) - 2);
  Result := Text;
end;

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
  Result := EndsWithText(Text, 'CamOnKeyLight.exe') or EndsWithText(Text, 'AutoKeyLight.exe');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Raw, Uninstall: string;
  ResultCode: Integer;
begin
  Result := '';
  StartupWasOn := False;
  if RegQueryStringValue(HKCU, RunKey, 'AutoKeyLight', Raw) and IsOurCommand(Raw) then
    StartupWasOn := True;
  if RegQueryStringValue(HKCU, RunKey, 'MyApp', Raw) and IsOurCommand(Raw) then
    StartupWasOn := True;
  if RegQueryStringValue(HKCU, RunKey, 'CamOnKeyLight', Raw) and IsOurCommand(Raw) then
    StartupWasOn := True;
  if RegQueryStringValue(HKCU, OldUninstallKey, 'UninstallString', Uninstall) then
    Exec(Unquote(Uninstall), '/VERYSILENT /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(ExpandConstant('{userprograms}\Auto Key Light.lnk')) then
    DeleteFile(ExpandConstant('{userprograms}\Auto Key Light.lnk'));
end;

procedure RewriteStartup;
var
  Raw, Quoted: string;
begin
  Quoted := '"' + ExpandConstant('{app}\{#MyAppExe}') + '"';
  if StartupWasOn or (RegQueryStringValue(HKCU, RunKey, 'CamOnKeyLight', Raw) and IsOurCommand(Raw)) then
    RegWriteStringValue(HKCU, RunKey, 'CamOnKeyLight', Quoted);
  if RegQueryStringValue(HKCU, RunKey, 'AutoKeyLight', Raw) and IsOurCommand(Raw) then
    RegDeleteValue(HKCU, RunKey, 'AutoKeyLight');
  if RegQueryStringValue(HKCU, RunKey, 'MyApp', Raw) and IsOurCommand(Raw) then
    RegDeleteValue(HKCU, RunKey, 'MyApp');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    RewriteStartup;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Raw, Quoted: string;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;
  Quoted := '"' + ExpandConstant('{app}\{#MyAppExe}') + '"';
  if RegQueryStringValue(HKCU, RunKey, 'CamOnKeyLight', Raw) and (CompareText(Raw, Quoted) = 0) then
    RegDeleteValue(HKCU, RunKey, 'CamOnKeyLight');
  if RegQueryStringValue(HKCU, RunKey, 'AutoKeyLight', Raw) and IsOurCommand(Raw) and (Pos(Lowercase(ExpandConstant('{app}')), Lowercase(Raw)) > 0) then
    RegDeleteValue(HKCU, RunKey, 'AutoKeyLight');
end;
