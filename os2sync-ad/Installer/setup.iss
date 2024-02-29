; This file is a script that allows to build the OrgSyncer instalation package
; To generate the installer, define the variable MyAppSourceDir MUST point to the Directory where the dll's should be copied from
; The script may be executed from the console-mode compiler - iscc "c:\isetup\samples\my script.iss" or from the Inno Setup Compiler UI
#define AppId "{{8fa4fb0b-7bf1-42c1-a46a-f04d1a00316a}"
#define AppSourceDir "Z:\projects\os2sync-organisation-ad\OS2syncAD\bin\publish"
#define AppName "OS2syncAD"
#define AppVersion "4.1.0"
#define AppPublisher "Digital Identity"
#define AppURL "http://digital-identity.dk/"
#define AppExeName "OS2syncAD.exe"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={pf}\{#AppPublisher}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputBaseFilename=OS2syncADSetup
Compression=lzma
SolidCompression=yes
SourceDir={#AppSourceDir}
OutputDir=..\..\..\Installer


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "OS2syncAD.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "OS2syncAD.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "mssql\*"; DestDir: "{app}\mssql"; Flags: ignoreversion

[Run]
Filename: "{app}\OS2syncAD.exe"; Parameters: "install" 

[UninstallRun]
Filename: "{app}\OS2syncAD.exe"; Parameters: "uninstall"

[Code]
 function InitializeSetup: Boolean;
  begin
    Result := True;
  end;
