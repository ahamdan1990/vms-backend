; ============================================================================
; Visitor Management System — Windows Installer
; Built with Inno Setup 6  (https://jrsoftware.org/isinfo.php)
;
; HOW TO COMPILE:
;   Run build-installer.ps1 — it prepares all files and calls ISCC.exe
;   Or manually: ISCC.exe vms-setup.iss
;
; REQUIRED in prereqs\ before compiling:
;   dotnet-hosting-9.0.x-win.exe   (.NET 9 Hosting Bundle)
;   Redis-x64-5.0.14.msi           (Redis for Windows — tporadowski port)
;   SQLEXPR_x64_ENU.exe            (SQL Server 2022 Express)
; ============================================================================

#define MyAppName       "Visitor Management System"
; Version is overridden at compile time by build-installer.ps1 using /DMyAppVersion=x.y.z
#ifndef MyAppVersion
  #define MyAppVersion  "1.0.0"
#endif
#define MyAppPublisher  "Security Engineering"
#define MyAppURL        "https://securityeng.com"
#define MyAppExe        "VisitorManagementSystem.Api.exe"

[Setup]
AppId={{C7A2D4F1-83BE-4E9A-B6CD-12345678ABCD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/support
AppUpdatesURL={#MyAppURL}/updates
DefaultDirName=C:\VMS\app
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputBaseFilename=VMS-Setup-{#MyAppVersion}
; SetupIconFile=assets\vms.ico   ; Uncomment and add your icon file to assets\vms.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExe}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut to open VMS"; GroupDescription: "Additional icons:"; Flags: unchecked

; ============================================================================
; BUNDLED FILES
; ============================================================================

[Files]
; ── Application ──────────────────────────────────────────────────────────────
; All app files — overwrite on update EXCEPT the two config files below
Source: "dist\app\*";     DestDir: "{app}";               Flags: recursesubdirs createallsubdirs ignoreversion
; Config files: onlyifdoesntexist = written by installer on first install, preserved on reinstall
Source: "dist\app\appsettings.Production.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "dist\app\wwwroot\config.js";           DestDir: "{app}\wwwroot"; Flags: ignoreversion onlyifdoesntexist

; ── Install scripts (deployed, cleaned up after install) ─────────────────────
Source: "scripts\*";      DestDir: "{app}\_vms_install";  Flags: recursesubdirs ignoreversion

; ── Prerequisites (extracted to temp, deleted after install) ─────────────────
Source: "prereqs\dotnet-hosting-9.0.14-win.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "prereqs\Redis-x64-5.0.14.1.msi";        DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "prereqs\SQLEXPRADV_x64_ENU.exe";         DestDir: "{tmp}"; Flags: deleteafterinstall

; ============================================================================
; SHORTCUTS
; ============================================================================

[Icons]
Name: "{group}\Open VMS in Browser";              Filename: "{app}\_vms_install\open-vms.bat";     WorkingDir: "{app}"
Name: "{group}\Restart VMS";                      Filename: "{app}\_vms_install\restart-vms.bat"; WorkingDir: "{app}"
Name: "{group}\Upgrade VMS";                      Filename: "{app}\_vms_install\upgrade-vms.bat"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Visitor Management System";  Filename: "{app}\_vms_install\open-vms.bat";    Tasks: desktopicon

; ============================================================================
; POST-INSTALL: open browser
; ============================================================================

[Run]
Filename: "{app}\_vms_install\open-vms.bat"; Description: "Open VMS in browser now"; Flags: postinstall nowait shellexec skipifsilent unchecked

; ============================================================================
; UNINSTALL
; ============================================================================

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\_vms_install\uninstall.ps1"" -AppPath ""{app}"""; Flags: runhidden waituntilterminated

; ============================================================================
; PASCAL CODE
; ============================================================================

[Code]

// ── Global variables ──────────────────────────────────────────────────────────
var
  PageMode   : TInputOptionWizardPage;  // Deployment mode
  PageServer : TInputQueryWizardPage;   // Server URL
  PageSql    : TInputQueryWizardPage;   // SQL app password
  PageSmtp   : TInputQueryWizardPage;   // SMTP (optional)
  InstallLog : TStrings;

// ── Helpers ───────────────────────────────────────────────────────────────────

procedure LogMsg(Msg: String);
var
  LogDir, LogPath: String;
begin
  InstallLog.Add(Msg);
  Log(Msg);

  LogDir := ExpandConstant('{app}\logs');
  if DirExists(ExpandConstant('{app}')) then begin
    if not DirExists(LogDir) then
      ForceDirectories(LogDir);

    LogPath := AddBackslash(LogDir) + 'vms-install.log';
    try
      InstallLog.SaveToFile(LogPath);
    except
      Log('Could not write install log incrementally: ' + LogPath);
    end;
  end;
end;

function EscapePowerShellSingleQuoted(Value: String): String;
begin
  Result := Value;
  StringChange(Result, '''', '''''');
end;

function GetStepLogPath(ScriptName: String): String;
begin
  Result := ExpandConstant('{app}\logs\' + ScriptName + '.log');
end;

procedure AppendStepLogToInstallLog(ScriptName: String);
var
  StepLogPath: String;
  StepOutput: TStringList;
  I: Integer;
begin
  StepLogPath := GetStepLogPath(ScriptName);
  if not FileExists(StepLogPath) then
    Exit;

  StepOutput := TStringList.Create;
  try
    StepOutput.LoadFromFile(StepLogPath);
    if StepOutput.Count = 0 then
      Exit;

    InstallLog.Add('[PS] OUTPUT BEGIN: ' + ScriptName);
    for I := 0 to StepOutput.Count - 1 do
      InstallLog.Add('  ' + StepOutput[I]);
    InstallLog.Add('[PS] OUTPUT END: ' + ScriptName);
    LogMsg('[PS] Detailed output captured for: ' + ScriptName);
  finally
    StepOutput.Free;
  end;
end;

function GetLastStepLogLine(ScriptName: String): String;
var
  StepLogPath: String;
  StepOutput: TStringList;
  I: Integer;
begin
  Result := '';
  StepLogPath := GetStepLogPath(ScriptName);
  if not FileExists(StepLogPath) then
    Exit;

  StepOutput := TStringList.Create;
  try
    StepOutput.LoadFromFile(StepLogPath);
    for I := StepOutput.Count - 1 downto 0 do begin
      if Trim(StepOutput[I]) <> '' then begin
        Result := Trim(StepOutput[I]);
        Exit;
      end;
    end;
  finally
    StepOutput.Free;
  end;
end;

function RunPS(ScriptName: String; ExtraArgs: String): Boolean;
var
  ScriptPath, Command, StepLogPath, FailureDetails: String;
  ResultCode: Integer;
begin
  ScriptPath := ExpandConstant('{app}\_vms_install\' + ScriptName);
  StepLogPath := GetStepLogPath(ScriptName);
  if FileExists(StepLogPath) then
    DeleteFile(StepLogPath);

  Command :=
    '-ExecutionPolicy Bypass -NonInteractive -Command "& { & ''' +
    EscapePowerShellSingleQuoted(ScriptPath) + ''' ' + Trim(ExtraArgs) +
    ' *> ''' + EscapePowerShellSingleQuoted(StepLogPath) + '''; exit $LASTEXITCODE }"';
  LogMsg('[PS] Running: ' + ScriptName);

  Result := Exec('powershell.exe', Command, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if (not Result) or (ResultCode <> 0) then begin
    AppendStepLogToInstallLog(ScriptName);
    FailureDetails := GetLastStepLogLine(ScriptName);
    LogMsg('[PS] FAILED: ' + ScriptName + ' (exit ' + IntToStr(ResultCode) + ')');
    MsgBox('Installation step failed: ' + ScriptName + #13#10 +
           'Exit code: ' + IntToStr(ResultCode) + #13#10#13#10 +
           'Last error: ' + FailureDetails + #13#10#13#10 +
           'Detailed logs:' + #13#10 +
           ExpandConstant('{app}\logs\vms-install.log') + #13#10 +
           StepLogPath,
           mbError, MB_OK);
    Result := False;
  end else
    LogMsg('[PS] OK: ' + ScriptName);
end;

function IsSqlServerInstalled: Boolean;
var
  Value: String;
begin
  // Check for SQLEXPRESS instance in registry
  Result := RegQueryStringValue(HKLM,
    'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL',
    'SQLEXPRESS', Value);
  if not Result then
    Result := RegQueryStringValue(HKLM64,
      'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL',
      'SQLEXPRESS', Value);
end;

function IsRedisInstalled: Boolean;
begin
  Result := FileExists('C:\Program Files\Redis\redis-server.exe') or
            DirExists('C:\Program Files\Redis');
end;

function IsDotNetHostingInstalled: Boolean;
var
  Value: String;
begin
  // ASP.NET Core Module V2 presence indicates hosting bundle is installed
  Result := FileExists(ExpandConstant('{sys}\inetsrv\aspnetcoremodulev2\aspnetcorev2.dll'));
  if not Result then
    Result := RegQueryStringValue(HKLM64,
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost',
      'Version', Value);
end;

function GetConfigFile: String;
begin
  Result := ExpandConstant('{app}\_vms_install\install-config.json');
end;

function GetSelectedDeploymentMode: String;
begin
  if Assigned(PageMode) and PageMode.Values[1] then
    Result := 'Standalone'
  else
    Result := 'Server';
end;

function JsonEscape(Value: String): String;
begin
  Result := Value;
  StringChange(Result, '\', '\\');
  StringChange(Result, '"', '\"');
  StringChange(Result, #13#10, '\n');
  StringChange(Result, #13, '\n');
  StringChange(Result, #10, '\n');
  StringChange(Result, #9, '\t');
end;

function IsStrongSqlPassword(Value: String): Boolean;
var
  I: Integer;
  Ch: Char;
  HasUpper, HasLower, HasDigit, HasSpecial: Boolean;
begin
  HasUpper := False;
  HasLower := False;
  HasDigit := False;
  HasSpecial := False;

  for I := 1 to Length(Value) do begin
    Ch := Value[I];
    if (Ch >= 'A') and (Ch <= 'Z') then
      HasUpper := True
    else if (Ch >= 'a') and (Ch <= 'z') then
      HasLower := True
    else if (Ch >= '0') and (Ch <= '9') then
      HasDigit := True
    else
      HasSpecial := True;
  end;

  Result :=
    (Length(Value) >= 12) and
    HasUpper and
    HasLower and
    HasDigit and
    HasSpecial;
end;

function ExtractHostAndPort(Url: String): String;
var
  Work: String;
  SlashPos: Integer;
begin
  Work := Trim(Url);
  if Pos('://', Work) > 0 then
    Delete(Work, 1, Pos('://', Work) + 2);

  SlashPos := Pos('/', Work);
  if SlashPos > 0 then
    Delete(Work, SlashPos, Length(Work));

  Result := Work;
end;

function ExtractHost(Url: String): String;
var
  HostAndPort: String;
  ColonPos: Integer;
begin
  HostAndPort := ExtractHostAndPort(Url);
  ColonPos := Pos(':', HostAndPort);
  if ColonPos > 0 then
    Result := Copy(HostAndPort, 1, ColonPos - 1)
  else
    Result := HostAndPort;
end;

function ExtractPort(Url: String): String;
var
  HostAndPort: String;
  ColonPos: Integer;
begin
  HostAndPort := ExtractHostAndPort(Url);
  ColonPos := Pos(':', HostAndPort);
  if ColonPos > 0 then
    Result := Copy(HostAndPort, ColonPos + 1, Length(HostAndPort))
  else
    Result := '';
end;

function IsIPv4Literal(Host: String): Boolean;
var
  I, DotCount: Integer;
  Ch: Char;
begin
  Result := Host <> '';
  DotCount := 0;

  for I := 1 to Length(Host) do begin
    Ch := Host[I];
    if Ch = '.' then
      DotCount := DotCount + 1
    else if (Ch < '0') or (Ch > '9') then begin
      Result := False;
      Exit;
    end;
  end;

  Result := Result and (DotCount = 3);
end;

procedure WriteInstallConfig;
var
  Json, ConfigFile, AppPathEscaped: String;
  ServerUrl, RedisConnection, SmtpHost, SmtpPort, SmtpUser, SmtpPass, SmtpFrom, DeploymentMode, SqlPassword: String;
begin
  ServerUrl := PageServer.Values[0];
  RedisConnection := PageServer.Values[1];
  SqlPassword := PageSql.Values[0];
  SmtpHost  := PageSmtp.Values[0];
  SmtpPort  := PageSmtp.Values[1];
  SmtpUser  := PageSmtp.Values[2];
  SmtpPass  := PageSmtp.Values[3];
  SmtpFrom  := PageSmtp.Values[4];
  DeploymentMode := GetSelectedDeploymentMode;

  // Ensure server URL has no trailing slash
  while (Length(ServerUrl) > 0) and (ServerUrl[Length(ServerUrl)] = '/') do
    Delete(ServerUrl, Length(ServerUrl), 1);

  if Trim(RedisConnection) = '' then
    RedisConnection := '127.0.0.1:6379';

  AppPathEscaped := ExpandConstant('{app}');

  Json :=
    '{' + #13#10 +
    '  "AppPath":      "' + JsonEscape(AppPathEscaped) + '",' + #13#10 +
    '  "SqlServer":    ".\\SQLEXPRESS",' + #13#10 +
    '  "SqlPassword":  "' + JsonEscape(SqlPassword) + '",' + #13#10 +
    '  "DeploymentMode": "' + JsonEscape(DeploymentMode) + '",' + #13#10 +
    '  "ServerUrl":    "' + JsonEscape(ServerUrl) + '",' + #13#10 +
    '  "RedisConnection": "' + JsonEscape(RedisConnection) + '",' + #13#10 +
    '  "SmtpHost":     "' + JsonEscape(SmtpHost)  + '",' + #13#10 +
    '  "SmtpPort":     "' + JsonEscape(SmtpPort)  + '",' + #13#10 +
    '  "SmtpUsername": "' + JsonEscape(SmtpUser)  + '",' + #13#10 +
    '  "SmtpPassword": "' + JsonEscape(SmtpPass)  + '",' + #13#10 +
    '  "SmtpFromEmail":"' + JsonEscape(SmtpFrom)  + '"'  + #13#10 +
    '}';

  ConfigFile := GetConfigFile;
  if not SaveStringToFile(ConfigFile, Json, False) then
    RaiseException('Failed to write install-config.json to: ' + ConfigFile);

  LogMsg('[Setup] install-config.json written to ' + ConfigFile);
end;

// ── Prerequisite installation ─────────────────────────────────────────────────

procedure InstallDotNetHostingBundle;
var
  ResultCode: Integer;
begin
  if IsDotNetHostingInstalled then begin
    LogMsg('[Prereq] .NET 9 Hosting Bundle already installed — skipping.');
    Exit;
  end;
  LogMsg('[Prereq] Installing .NET 9 Hosting Bundle...');
  WizardForm.StatusLabel.Caption := 'Installing .NET 9 Hosting Bundle...';
  WizardForm.StatusLabel.Update;
  Exec(ExpandConstant('{tmp}\dotnet-hosting-9.0.14-win.exe'),
       '/install /quiet /norestart',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if ResultCode = 0 then
    LogMsg('[Prereq] .NET 9 Hosting Bundle installed.')
  else if ResultCode = 3010 then
    LogMsg('[Prereq] .NET 9 Hosting Bundle installed (reboot required).')
  else
    LogMsg('[Prereq] WARNING: Hosting bundle exit code = ' + IntToStr(ResultCode));
end;

procedure InstallRedis;
var
  ResultCode: Integer;
begin
  if IsRedisInstalled then begin
    LogMsg('[Prereq] Redis already installed — skipping.');
    // Ensure service is running
    Exec('sc.exe', 'start Redis', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exit;
  end;
  LogMsg('[Prereq] Installing Redis for Windows...');
  WizardForm.StatusLabel.Caption := 'Installing Redis...';
  WizardForm.StatusLabel.Update;
  Exec('msiexec.exe',
       '/i "' + ExpandConstant('{tmp}\Redis-x64-5.0.14.1.msi') + '" /quiet /norestart ADDLOCAL=all',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if ResultCode = 0 then
    LogMsg('[Prereq] Redis installed.')
  else
    LogMsg('[Prereq] WARNING: Redis exit code = ' + IntToStr(ResultCode));
  // Start the service
  Exec('sc.exe', 'start Redis', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure InstallSqlServerExpress;
var
  ResultCode: Integer;
begin
  if IsSqlServerInstalled then begin
    LogMsg('[Prereq] SQL Server Express already installed — skipping.');
    Exit;
  end;
  LogMsg('[Prereq] Installing SQL Server Express 2022 (this may take 5-10 minutes)...');
  WizardForm.StatusLabel.Caption := 'Installing SQL Server Express (5-10 min)...';
  WizardForm.StatusLabel.Update;
  Exec(ExpandConstant('{tmp}\SQLEXPRADV_x64_ENU.exe'),
       '/q /ACTION=Install /FEATURES=SQLEngine' +
       ' /INSTANCENAME=SQLEXPRESS' +
       ' /SQLSVCACCOUNT="NT AUTHORITY\NETWORK SERVICE"' +
       ' /SQLSYSADMINACCOUNTS="BUILTIN\Administrators"' +
       ' /TCPENABLED=1 /NPENABLED=0' +
       ' /IACCEPTSQLSERVERLICENSETERMS' +
       ' /HIDECONSOLE',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if (ResultCode = 0) or (ResultCode = 3010) then
    LogMsg('[Prereq] SQL Server Express installed.')
  else begin
    LogMsg('[Prereq] WARNING: SQL Server exit code = ' + IntToStr(ResultCode));
    MsgBox('SQL Server Express installation may have failed (exit code ' +
           IntToStr(ResultCode) + ').' + #13#10 +
           'You may need to install SQL Server manually and re-run this installer.',
           mbError, MB_OK);
  end;
end;

// ── Wizard initialization ─────────────────────────────────────────────────────

procedure InitializeWizard;
var
  DefaultHost: String;
begin
  InstallLog := TStringList.Create;

  DefaultHost := LowerCase(GetEnv('COMPUTERNAME'));
  if Trim(DefaultHost) = '' then
    DefaultHost := 'localhost';

  PageMode := CreateInputOptionPage(wpLicense,
    'Deployment Mode',
    'Choose how this installation will be used.',
    'Server mode is recommended when the app may be opened from other machines or mobile devices.' + #13#10 +
    'Both modes will bind localhost, the computer name, and the local IP addresses for this Windows machine.',
    True, False);
  PageMode.Add('Server installation (recommended for shared or network access)');
  PageMode.Add('Standalone installation (single Windows machine use)');
  PageMode.SelectedValueIndex := 0;

  // Page 2: Server URL
  PageServer := CreateInputQueryPage(PageMode.ID,
    'Server Configuration',
    'How will users access the Visitor Management System?',
    'Enter the preferred URL users will recognize first.' + #13#10 +
    'This installer will also configure HTTPS access for localhost, the computer name, and the local IP addresses on this machine.' + #13#10 +
    'Use 127.0.0.1:6379 for Redis when it is local.');
  PageServer.Add('Server URL:', False);
  PageServer.Add('Redis connection string:', False);
  PageServer.Values[0] := 'https://' + DefaultHost;
  PageServer.Values[1] := '127.0.0.1:6379';

  PageSql := CreateInputQueryPage(PageServer.ID,
    'Database Credentials',
    'Set the SQL password used by the application.',
    'The installer creates or updates the SQL login named vms_app.' + #13#10 +
    'Choose a strong password and keep it in your deployment records.');
  PageSql.Add('Application SQL Password:', True);
  PageSql.Add('Confirm SQL Password:',     True);

  // Page 4: SMTP (optional)
  PageSmtp := CreateInputQueryPage(PageSql.ID,
    'Email Configuration (Optional)',
    'Configure email notifications.',
    'Leave SMTP Server blank to skip email setup.' + #13#10 +
    'You can configure this later in the VMS admin panel.');
  PageSmtp.Add('SMTP Server (e.g. smtp.gmail.com):',    False);
  PageSmtp.Add('SMTP Port:',                             False);
  PageSmtp.Add('SMTP Username (email address):',         False);
  PageSmtp.Add('SMTP Password:',                         True);
  PageSmtp.Add('From Email Address:',                    False);
  PageSmtp.Values[1] := '587';
end;

// ── Validation ────────────────────────────────────────────────────────────────

function NextButtonClick(CurPageID: Integer): Boolean;
var
  HostName, PortValue, SqlPassword, SqlPasswordConfirm: String;
begin
  Result := True;
  if CurPageID = PageServer.ID then begin
    if Trim(PageServer.Values[0]) = '' then begin
      MsgBox('Please enter the Server URL.', mbError, MB_OK);
      Result := False;
    end else if (Pos('http://', LowerCase(PageServer.Values[0])) = 0) and
                (Pos('https://', LowerCase(PageServer.Values[0])) = 0) then begin
      MsgBox('Server URL must start with http:// or https://', mbError, MB_OK);
      Result := False;
    end else begin
      HostName := LowerCase(ExtractHost(PageServer.Values[0]));
      PortValue := ExtractPort(PageServer.Values[0]);

      if HostName = '' then begin
        MsgBox('Server URL must include a hostname.', mbError, MB_OK);
        Result := False;
      end else if (PortValue <> '') and (PortValue <> '80') and (PortValue <> '443') then begin
        MsgBox('Custom ports are not supported by this installer. Use the default IIS ports 80 and 443.', mbError, MB_OK);
        Result := False;
      end;
    end;
  end else if CurPageID = PageSql.ID then begin
    SqlPassword := PageSql.Values[0];
    SqlPasswordConfirm := PageSql.Values[1];

    if Trim(SqlPassword) = '' then begin
      MsgBox('Please enter the SQL password for vms_app.', mbError, MB_OK);
      Result := False;
    end else if SqlPassword <> SqlPasswordConfirm then begin
      MsgBox('The SQL password and confirmation do not match.', mbError, MB_OK);
      Result := False;
    end else if not IsStrongSqlPassword(SqlPassword) then begin
      MsgBox('The SQL password must be at least 12 characters and include uppercase, lowercase, number, and special character.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

// ── Main installation orchestration ──────────────────────────────────────────

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFile, LogPath: String;
begin
  if CurStep = ssPostInstall then begin
    ConfigFile := GetConfigFile;

    // ── Step 1: Install Redis and SQL Server ──────────────────────────────────
    // These use prereq MSI/EXE files extracted to {tmp}.  They MUST run here
    // (ssPostInstall) because {tmp} files are extracted during ssInstall — they
    // are not yet available when CurStepChanged(ssInstall) fires, which causes
    // msiexec to return exit code 1619 (package not found).
    WizardForm.StatusLabel.Caption := 'Installing Redis...';
    WizardForm.StatusLabel.Update;
    InstallRedis;

    WizardForm.StatusLabel.Caption := 'Installing SQL Server Express (5-10 min)...';
    WizardForm.StatusLabel.Update;
    InstallSqlServerExpress;

    // ── Step 2: Write install config ─────────────────────────────────────────
    WizardForm.StatusLabel.Caption := 'Writing configuration...';
    WizardForm.StatusLabel.Update;
    WriteInstallConfig;

    // ── Step 3: Enable IIS features ──────────────────────────────────────────
    WizardForm.StatusLabel.Caption := 'Enabling IIS features...';
    WizardForm.StatusLabel.Update;
    if not RunPS('01-enable-iis.ps1', '') then Exit;

    // ── Step 4: Install .NET Hosting Bundle (AFTER IIS exists) ───────────────
    // IIS must be present so the Hosting Bundle can register AspNetCoreModuleV2.
    WizardForm.StatusLabel.Caption := 'Installing .NET 9 Hosting Bundle...';
    WizardForm.StatusLabel.Update;
    InstallDotNetHostingBundle;

    // ── Step 5: Generate secrets & configure app ──────────────────────────────
    WizardForm.StatusLabel.Caption := 'Configuring application...';
    WizardForm.StatusLabel.Update;
    if not RunPS('02-configure-app.ps1', '-ConfigFile ''' + ConfigFile + '''') then Exit;

    // ── Step 6: Create SQL database & login ───────────────────────────────────
    WizardForm.StatusLabel.Caption := 'Setting up database...';
    WizardForm.StatusLabel.Update;
    if not RunPS('03-setup-sql.ps1', '-ConfigFile ''' + ConfigFile + '''') then Exit;

    // ── Step 7: Configure IIS site ────────────────────────────────────────────
    WizardForm.StatusLabel.Caption := 'Configuring IIS...';
    WizardForm.StatusLabel.Update;
    if not RunPS('04-configure-iis.ps1', '-ConfigFile ''' + ConfigFile + '''') then Exit;

    // ── Write install log ─────────────────────────────────────────────────────
    LogPath := ExpandConstant('{app}\logs\vms-install.log');
    try
      InstallLog.SaveToFile(LogPath);
    except
      Log('Could not write install log: ' + LogPath);
    end;

    // ── Clean up config file (contains secrets) ───────────────────────────────
    // Note: open-vms.bat is written by 04-configure-iis.ps1 using the HTTPS URL.
    DeleteFile(ConfigFile);

    // Done
    LogMsg('=== VMS installation complete. ===');
    WizardForm.StatusLabel.Caption := 'Installation complete.';
    WizardForm.StatusLabel.Update;

    MsgBox('VMS was installed in ' + LowerCase(GetSelectedDeploymentMode()) + ' mode.' + #13#10#13#10 +
           'Preferred URL: ' + PageServer.Values[0] + #13#10 +
           'Also configured on this machine: https://localhost, https://' + LowerCase(GetEnv('COMPUTERNAME')) + ', and the local IP addresses.' + #13#10#13#10 +
           'If Firefox was already open during setup, close it completely and open it again before testing HTTPS.',
           mbInformation, MB_OK);
  end;
end;

// ── Custom Ready page text ─────────────────────────────────────────────────────

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo,
  MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result :=
    'The following will be installed and configured:' + NewLine + NewLine +
    Space + '- IIS Web Server + WebSockets'                    + NewLine +
    Space + '- .NET 9 Hosting Bundle'                          + NewLine +
    Space + '- Redis 5.0 (Windows Service)'                    + NewLine +
    Space + '- SQL Server 2022 Express (if not installed)'     + NewLine +
    Space + '- Self-signed SSL certificate (HTTPS on port 443)' + NewLine +
    Space + '- VMS Application Files'                          + NewLine + NewLine +
    'Install location:' + NewLine + Space + MemoDirInfo        + NewLine + NewLine +
    'Deployment mode:' + NewLine + Space + GetSelectedDeploymentMode + NewLine +
    'Server URL:' + NewLine + Space + PageServer.Values[0] + NewLine +
    'SQL login:' + NewLine + Space + 'vms_app' + NewLine +
    'Redis connection:' + NewLine + Space + PageServer.Values[1] + NewLine +
    'HTTP traffic will redirect to HTTPS automatically.' + NewLine +
    'This installer will configure HTTPS access for localhost, the computer name, and the local IP addresses on this machine.' + NewLine;

  if PageSmtp.Values[0] <> '' then
    Result := Result + NewLine + 'SMTP Server:' + NewLine + Space + PageSmtp.Values[0];
end;

// ── Cleanup on destroy ────────────────────────────────────────────────────────

procedure DeinitializeSetup;
begin
  if Assigned(InstallLog) then
    InstallLog.Free;
end;
