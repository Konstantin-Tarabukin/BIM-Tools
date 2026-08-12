#define MyAppName "BIM Tools"
#define MyAppVersion "0.4.1"
#define MyAppPublisher "TarabukinConst"

#define ProjectDir ".."
#define BuildDir ProjectDir + "\bin\Release"
#define UpdaterBuildDir "..\..\BIMToolsUpdater\bin\Release"

#if !FileExists(BuildDir + "\RevitCopyParams.dll")
#error "Сначала соберите проект RevitCopyParams в Release."
#endif

#if !FileExists(BuildDir + "\Newtonsoft.Json.dll")
#error "Newtonsoft.Json.dll not found in Release."
#endif

#if !FileExists(UpdaterBuildDir + "\BIMToolsUpdater.exe")
#error "BIMToolsUpdater.exe not found in BIMToolsUpdater\bin\Release. Сначала соберите Solution в Release."
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

; Нужны права администратора для удаления старой
; версии из ProgramData.
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

; ============================================================
; ОСНОВНАЯ DLL
; ============================================================

Source: "{#BuildDir}\RevitCopyParams.dll"; \
    DestDir: "{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams"; \
    Flags: ignoreversion overwritereadonly restartreplace uninsrestartdelete


; ============================================================
; NEWTONSOFT.JSON
; ============================================================

Source: "{#BuildDir}\Newtonsoft.Json.dll"; \
    DestDir: "{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams"; \
    Flags: ignoreversion overwritereadonly restartreplace uninsrestartdelete


; ============================================================
; РЕСУРСЫ
; ============================================================

Source: "{#ProjectDir}\Resources\*"; \
    DestDir: "{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams\Resources"; \
    Flags: ignoreversion overwritereadonly restartreplace recursesubdirs createallsubdirs


; ============================================================
; UPDATER
; ============================================================

Source: "{#UpdaterBuildDir}\BIMToolsUpdater.exe"; \
DestDir: "{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams\Updater"; \
Flags: ignoreversion overwritereadonly restartreplace uninsrestartdelete


[UninstallDelete]

; Удаляем установленный плагин из AppData

Type: filesandordirs; \
    Name: "{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams"

; Удаляем .addin

Type: files; \
    Name: "{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams.addin"


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
        '{userappdata}\Autodesk\Revit\Addins\2023'
      )
    );

end;


procedure RemoveOldProgramDataInstallation();
var
  OldPluginDir: String;
  OldAddinFile: String;
begin

  OldPluginDir :=
    ExpandConstant(
      '{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams'
    );

  OldAddinFile :=
    ExpandConstant(
      '{commonappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams.addin'
    );


  { Удаляем старую папку плагина }

  if DirExists(OldPluginDir) then
  begin

    DelTree(
      OldPluginDir,
      True,
      True,
      True
    );

  end;


  { Удаляем старый .addin }

  if FileExists(OldAddinFile) then
  begin

    DeleteFile(OldAddinFile);

  end;

end;


procedure CreateAddinFile();
var
  AddinPath: String;
  AddinText: String;
  DllPath: String;
begin

  AddinPath :=
    ExpandConstant(
      '{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams.addin'
    );


  DllPath :=
    ExpandConstant(
      '{userappdata}\Autodesk\Revit\Addins\2023\RevitCopyParams\RevitCopyParams.dll'
    );


  AddinText :=
    '<?xml version="1.0" encoding="utf-8" standalone="no"?>'
    + #13#10
    + '<RevitAddIns>'
    + #13#10
    + #13#10
    + '  <AddIn Type="Application">'
    + #13#10
    + #13#10
    + '    <Name>RevitCopyParams</Name>'
    + #13#10
    + #13#10
    + '    <Assembly>'
    + DllPath
    + '</Assembly>'
    + #13#10
    + #13#10
    + '    <AddInId>7AC1B09C-4C4D-4DA6-869F-4CE027F1A8B2</AddInId>'
    + #13#10
    + #13#10
    + '    <FullClassName>RevitCopyParams.App</FullClassName>'
    + #13#10
    + #13#10
    + '    <VendorId>BIMT</VendorId>'
    + #13#10
    + #13#10
    + '  </AddIn>'
    + #13#10
    + #13#10
    + '</RevitAddIns>';


  ForceDirectories(
    ExpandConstant(
      '{userappdata}\Autodesk\Revit\Addins\2023'
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

  { ==========================================================
    Проверяем, закрыт ли Revit
    ========================================================== }

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


  { ==========================================================
    Проверяем наличие Revit 2023
    ========================================================== }

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

  { ==========================================================
    Перед установкой файлов удаляем старую ProgramData-версию
    ========================================================== }

  if CurStep = ssInstall then
  begin

    RemoveOldProgramDataInstallation();

  end;


  { ==========================================================
    После установки создаём новый .addin в AppData
    ========================================================== }

  if CurStep = ssPostInstall then
  begin

    CreateAddinFile();

  end;

end;