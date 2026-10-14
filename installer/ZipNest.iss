#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#define AppName "ZipNest"
#define RepoRoot ".."

[Setup]
AppId={{F7D1F1D9-B4A0-4747-A4A8-A6C54423F0CC}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Seth Rickert
AppPublisherURL=https://github.com/sethrickert/ZipNest
AppSupportURL=https://github.com/sethrickert/ZipNest/issues
DefaultDirName={localappdata}\Programs\ZipNest
DefaultGroupName=ZipNest
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#RepoRoot}\artifacts
OutputBaseFilename=ZipNest-{#AppVersion}-setup-x64
SetupIconFile={#RepoRoot}\assets\ZipNestSetup.ico
UninstallDisplayIcon={app}\ZipNest.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
ChangesAssociations=yes
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName=ZipNest
VersionInfoDescription=ZipNest Setup
VersionInfoCompany=Seth Rickert
LicenseFile={#RepoRoot}\LICENSE

[Tasks]
Name: "explorerintegration"; Description: "Add “Extract && Nest” and “Extract && Organize” to archive right-click menus"; GroupDescription: "Windows Explorer:"; Flags: checkedonce
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#RepoRoot}\artifacts\ZipNest-portable\ZipNest.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\artifacts\ZipNest-portable\ZipNest.exe.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#RepoRoot}\artifacts\ZipNest-portable\7zip\*"; DestDir: "{app}\7zip"; Flags: ignoreversion recursesubdirs
Source: "{#RepoRoot}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ZipNest settings"; Filename: "{app}\ZipNest.exe"; Parameters: "--settings"
Name: "{autodesktop}\ZipNest"; Filename: "{app}\ZipNest.exe"; Parameters: "--settings"; Tasks: desktopicon

[Run]
Filename: "{app}\ZipNest.exe"; Parameters: "--register-defaults"; Flags: runhidden waituntilterminated
Filename: "{app}\ZipNest.exe"; Parameters: "--install-integration"; Tasks: explorerintegration; Flags: runhidden waituntilterminated
Filename: "{app}\ZipNest.exe"; Parameters: "--open-default-apps"; Description: "Choose ZipNest for ZIP, 7z, and RAR files in Windows Default Apps"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{app}\ZipNest.exe"; Parameters: "--unregister"; Flags: runhidden waituntilterminated; RunOnceId: "ZipNestUnregister"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep = ssPostInstall) and not WizardIsTaskSelected('explorerintegration') then
    Exec(ExpandConstant('{app}\ZipNest.exe'), '--remove-integration', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
