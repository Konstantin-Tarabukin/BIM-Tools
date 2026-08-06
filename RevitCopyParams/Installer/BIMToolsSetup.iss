#define MyAppName "BIM Tools"
#define MyAppVersion "0.4.1"
#define MyAppPublisher "TarabukinConst"
#define ProjectDir ".."
#define BuildDir ProjectDir + "\bin\Release"
#if !FileExists(BuildDir + "\RevitCopyParams.dll")
  #error "Сначала соберите проект в Release."
#endif
#if !FileExists(BuildDir + "\RevitCopyParams.dll")
  #error "Release build not found."
#endif

#if !FileExists(BuildDir + "\Newtonsoft.Json.dll")
  #error "Newtonsoft.Json.dll not found in Release."
#endif

#if !DirExists(ProjectDir + "\Resources")
  #error "Resources folder not found."
#endif

[Setup]

AppId={{7AC1B09C-4C4D-4DA6-869F-4CE027F1A8B2}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

VersionInfoVersion={#MyAppVersion}.0
VersionInfoProductVersion={#MyAppVersion}.0

DefaultDirName={autopf}\BIM Tools
DefaultGroupName={#MyAppName}

PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

Compression=lzma2
SolidCompression=yes

OutputDir=Output
OutputBaseFilename=BIMToolsSetup_{#MyAppVersion}

DisableDirPage=yes
DisableProgramGroupPage=yes

Uninstallable=yes



[Files]

Source: "{#BuildDir}\RevitCopyParams.dll"; \
DestDir: "{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams"; \
Flags: ignoreversion overwritereadonly restartreplace uninsrestartdelete

Source: "{#BuildDir}\Newtonsoft.Json.dll"; \
DestDir: "{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams"; \
Flags: ignoreversion overwritereadonly restartreplace uninsrestartdelete



Source: "{#ProjectDir}\Resources\*"; \
DestDir: "{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams\Resources"; \
Flags: ignoreversion overwritereadonly restartreplace recursesubdirs createallsubdirs



[UninstallDelete]

Type: filesandordirs; \
Name: "{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams"


Type: files; \
Name: "{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams.addin"



[Code]


function IsRevitRunning(): Boolean;
var
  ResultCode: Integer;

begin

  Result := False;

  Exec(
    'cmd.exe',
    '/C tasklist | findstr /I "Revit.exe"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode
  );

  if ResultCode = 0 then
    Result := True;

end;



function IsRevit2023Installed(): Boolean;

begin

  Result :=
    DirExists(
      ExpandConstant(
        '{commonappdata}\Autodesk\Revit\Addins\2023'
      )
    );

end;



procedure CreateAddinFile();

var
  AddinPath: String;
  AddinText: String;

begin


  AddinPath :=
    ExpandConstant(
      '{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams.addin'
    );


  AddinText :=

    '<?xml version="1.0" encoding="utf-8" standalone="no"?>'
    + #13#10 +

    '<RevitAddIns>'
    + #13#10 +

    '  <AddIn Type="Application">'
    + #13#10 +

    '    <Name>RevitCopyParams</Name>'
    + #13#10 +

    '    <Assembly>'
    +

      ExpandConstant(
        '{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams\RevitCopyParams.dll'
      )

    +

    '</Assembly>'
    + #13#10 +

    '    <AddInId>7AC1B09C-4C4D-4DA6-869F-4CE027F1A8B2</AddInId>'
    + #13#10 +

    '    <FullClassName>RevitCopyParams.App</FullClassName>'
    + #13#10 +

    '    <VendorId>BIMT</VendorId>'
    + #13#10 +

    '  </AddIn>'
    + #13#10 +

    '</RevitAddIns>';


  ForceDirectories(
    ExpandConstant(
      '{commonappdata}\Autodesk\Revit\Addins\2023'
    )
  );


  SaveStringToFile(
    AddinPath,
    AddinText,
    False
  );


end;



function InitializeSetup(): Boolean;

var
  MsgBoxResult: Integer;

begin


  if IsRevitRunning() then

  begin

    MsgBox(

      'Revit сейчас запущен.' + #13#10 +
      'Закройте Revit перед установкой BIM Tools.',

      mbError,

      MB_OK

    );


    Result := False;
    Exit;

  end;



  if not IsRevit2023Installed() then

  begin


    MsgBoxResult :=

      MsgBox(

        'Revit 2023 не найден автоматически.' + #13#10 +
        'Продолжить установку BIM Tools?',

        mbConfirmation,

        MB_YESNO

      );


    Result :=
      MsgBoxResult = IDYES;


    Exit;

  end;



  Result := True;


end;




procedure CurStepChanged(CurStep: TSetupStep);

begin

  if CurStep = ssPostInstall then

  begin

    CreateAddinFile();

  end;

end;