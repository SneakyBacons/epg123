; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!


#define MyAppName "EPG123"
#define SourcePath "..\..\bin\Release"
#define MyAppExeName "epg123.exe"
#define MyClientName "EPG123 Client"
#define MyClientExeName "epg123Client.exe"

#dim Version[4]
#expr ParseVersion(AddBackslash(SourcePath) + MyAppExeName, Version[0], Version[1], Version[2], Version[3])
#define MyAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2]) + "." + Str(Version[3])

#define MyAppPublisher "GaRyan2"
#define MyAppURL "http://epg123.garyan2.net/"

#define MySetupBaseFilename "epg123Setup_v"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
SignTool=signtool /d "EPG123" /du "https://epg123.garyan2.net" $f
SignedUninstaller=yes
AppId={{A592C107-8384-4DFF-902E-30F5133EA626}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppendDefaultDirName=no
CloseApplications=no
DefaultDirName={pf32}\epg123
DisableWelcomePage=no
LicenseFile=docs\license.rtf
AlwaysShowDirOnReadyPage=yes
OutputDir=..\..\bin\output\setup
OutputBaseFilename={#MySetupBaseFilename + MyAppVersion}
SetupIconFile=imgs\EPG123.ico
Compression=lzma
SolidCompression=yes
ShowComponentSizes=yes
UninstallDisplayName={#MyAppName} {#MyAppVersion}
UninstallDisplayIcon={uninstallexe}
WizardImageFile=imgs\EPG123_164x319.bmp
WizardSmallImageFile=imgs\EPG123_55x55.bmp
AllowRootDirectory=True
AppCopyright=2016
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Media Center Electronic Program Guide in 1-2-3
VersionInfoCopyright=2021
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoProductTextVersion={#MyAppVersion}
DisableProgramGroupPage=yes
AppMutex=Global\{{BAEC0A11-437B-4D39-A2FA-DB56F8C977E3},Global\{{CD7E6857-7D92-4A2F-B3AB-ED8CB42C6F65}

[Types]
Name: "full"; Description: "Full Install"; MinVersion: 6.1
Name: "server"; Description: "Server Only Install"
Name: "client"; Description: "Client Only Install"; MinVersion: 6.1
Name: "custom"; Description: "Custom Install"; Flags: IsCustom

[Components]
Name: "main1"; Description: "Server Files"; Types: full server; Flags: disablenouninstallwarning
Name: "hdhr"; Description: "HDHR2MXF for SiliconDust HDHR DVR Service"; Types: custom; Flags: disablenouninstallwarning
Name: "main2"; Description: "Client Files"; Types: full client; MinVersion: 6.1; Flags: disablenouninstallwarning

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "misc\dotNetFx40_Full_setup.exe"; DestDir: "{tmp}"; Flags: dontcopy
Source: "..\..\bin\Release\epg123.exe"; DestDir: "{app}"; Flags: ignoreversion signonce; Components: main1
Source: "..\..\bin\Release\epg123.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Attribs: hidden; Components: main1
Source: "..\..\bin\Release\hdhr2mxf.exe"; DestDir: "{app}"; Flags: ignoreversion signonce; Components: hdhr
Source: "..\..\bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: main1 hdhr
Source: "..\..\bin\Release\epg123Client.exe"; DestDir: "{app}"; Flags: ignoreversion signonce; Components: main2
Source: "..\..\bin\Release\epg123Client.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Attribs: hidden; MinVersion: 6.1; OnlyBelowVersion: 6.2; Components: main2
Source: "..\..\bin\Release\epg123Transfer.exe"; DestDir: "{app}"; Flags: ignoreversion signonce; Components: main2
Source: "..\..\bin\Release\epg123Transfer.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Attribs: hidden; MinVersion: 6.1; OnlyBelowVersion: 6.2; Components: main2
Source: "..\..\bin\Release\epgTray.exe"; DestDir: "{app}"; BeforeInstall: TaskKill('epgTray.exe'); Flags: ignoreversion signonce; Components: main2
Source: "..\..\bin\Release\epgTray.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Attribs: hidden; Components: main2
Source: "docs\license.rtf"; DestDir: "{app}"; Flags: ignoreversion
Source: "docs\customLineup.xml.example"; DestDir: "{app}"; Flags: ignoreversion; Components: main1
Source: "links\EPG123 Online.url"; DestDir: "{commonprograms}\{#MyAppName}"; Flags: ignoreversion

[Icons]
Name: "{commonprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Components: main1
Name: "{commonprograms}\{#MyAppName}\{#MyClientName}"; Filename: "{app}\{#MyClientExeName}"; Components: main2
Name: "{commonprograms}\{#MyAppName}\EPG123 Transfer Tool"; Filename: "{app}\epg123Transfer.exe"; Components: main2
Name: "{commonprograms}\{#MyAppName}\HDHR2MXF Update"; Filename: "{app}\hdhr2mxf.exe"; Parameters: "-update -import"; Components: hdhr
Name: "{commonprograms}\{#MyAppName}\EPG123 Tray"; Filename: "{app}\epgTray.exe"; Components: main2
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Components: main1
Name: "{commondesktop}\{#MyClientName}"; Filename: "{app}\{#MyClientExeName}"; Tasks: desktopicon; Components: main2
Name: "{commonstartup}\EPG123 Tray"; Filename: "{app}\epgTray.exe"; Components: main2

[Registry]
; Registry keys to add epg123 and epg123client as sources to the event log
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\Media Center\EPG123"; ValueType: expandsz; ValueName: "EventMessageFile"; ValueData: "{win}\Microsoft.NET\Framework\v4.0.30319\EventLogMessages.dll;{win}\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll"; Flags: createvalueifdoesntexist noerror; Components: main1
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\Media Center\EPG123Client"; ValueType: expandsz; ValueName: "EventMessageFile"; ValueData: "{win}\Microsoft.NET\Framework\v4.0.30319\EventLogMessages.dll;{win}\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll"; Flags: createvalueifdoesntexist noerror; Components: main2

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser unchecked; Components: main1
Filename: "{app}\{#MyClientExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyClientName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser unchecked; Components: main2
Filename: "{app}\epgTray.exe"; Flags: nowait runasoriginaluser; Components: main2

[Dirs]
Name: {code:GetRootDataFolder}; Permissions: everyone-full

[InstallDelete]
Type: files; Name: "{app}\Newtonsoft.json.dll"; Components: main1 main2 hdhr
Type: files; Name: "{app}\epg123.exe"; Components: not main1
Type: files; Name: "{app}\epg123.exe.config"; Components: not main1
Type: files; Name: "{app}\hdhr2mxf.exe"; Components: not hdhr
Type: files; Name: "{app}\epg123Client.exe"; Components: not main2
Type: files; Name: "{app}\epg123Client.exe.config"; Components: not main2
Type: files; Name: "{app}\epg123Transfer.exe"; Components: not main2
Type: files; Name: "{app}\epg123Transfer.exe.config"; Components: not main2
Type: files; Name: "{app}\customLineup.xml.example"; Components: not main1
Type: files; Name: "{app}\epgTray.exe"; BeforeInstall: TaskKill('epgTray.exe'); Components: not main2
Type: files; Name: "{app}\epgTray.exe.config"; Components: not main2
Type: files; Name: "{commondesktop}\{#MyAppName}.lnk"; Components: not main1
Type: files; Name: "{commondesktop}\{#MyClientName}.lnk"; Components: not main2
Type: files; Name: "{commonprograms}\{#MyAppName}\{#MyAppName}.lnk"; Components: not main1
Type: files; Name: "{commonprograms}\{#MyAppName}\{#MyClientName}.lnk"; Components: not main2
Type: files; Name: "{commonprograms}\{#MyAppName}\EPG123 Transfer Tool.lnk"; Components: not main2
Type: files; Name: "{commonprograms}\{#MyAppName}\HDHR2MXF Update.lnk"; Components: not hdhr
Type: files; Name: "{commonstartup}\EPG123 Tray.lnk"; Components: not main2

[UninstallRun]
Filename: "taskkill"; Parameters: "/im ""epgTray.exe"" /f"; Flags: runhidden

[Code]
// determine whether .NET Framework is installed
function Framework4IsInstalled(): Boolean;
var
    success: boolean;
    install: cardinal;
begin
    success := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Install', install);
    result := success and (install = 1);
end;

// install .NET Framework
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
    ResultCode: Integer;
begin
    // check if minimum framework is installed
    if not Framework4IsInstalled() then begin
        // prompt user to install if not suppressed
        if SuppressibleMsgBox('The minimum .NET Framework is not installed. Do you wish to install .NET Framework 4.0 Client and Extended software now?', mbConfirmation, MB_YESNO, IDNO) = IDYES then begin
            // extract web bootstrap and execute
            ExtractTemporaryFile('dotNetFx40_Full_setup.exe');
            if not Exec(ExpandConstant('{tmp}\dotNetFx40_Full_setup.exe'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then begin
                MsgBox('.NET installation failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
            end;
        end;
    end;
end;

// check where the installation folder is located
function GetRootDataFolder(Param: String) : String;
begin
    if Pos(ExpandConstant('{pf32}'), ExpandConstant('{app}')) > 0 then begin
        result := ExpandConstant('{commonappdata}\{#MyAppPublisher}\') + Lowercase(ExpandConstant('{#MyAppName}'))
    end
    else begin
        result := ExpandConstant('{app}')
    end;
end;

// kill tray task
procedure TaskKill(FileName: String);
var
    ResultCode: Integer;
begin
    Exec(ExpandConstant('taskkill.exe'), '/f /im ' + '"' + FileName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;