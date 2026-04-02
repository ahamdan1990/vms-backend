<#
.SYNOPSIS
    Creates the VMS database, SQL login, and user in SQL Server Express.
    Uses Windows Authentication (trusted connection) during installation -- no SA password needed.
    Reads the generated SQL password from install-config.json (written by 02-configure-app.ps1).
#>
param(
    [Parameter(Mandatory)][string]$ConfigFile
)
$ErrorActionPreference = 'Stop'

$config      = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$SqlServer   = $config.SqlServer
$SqlPassword = $config.SqlPassword
$EscapedSqlPassword = $SqlPassword.Replace("'", "''")

Write-Host "[SQL] Setting up database on $SqlServer..." -ForegroundColor Cyan

$sqlcmd = 'sqlcmd'
if (-not (Get-Command $sqlcmd -ErrorAction SilentlyContinue)) {
    # Try well-known sqlcmd locations for SQL Server 2022, 2019, 2017, 2014, 2012
    $candidates = @(
        'C:\Program Files\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe',  # 2022
        'C:\Program Files\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe',  # 2019
        'C:\Program Files\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe',  # 2017
        'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe'
    )
    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($found) { $sqlcmd = $found }
}

function Get-SqlInstanceName([string]$serverName) {
    if ($serverName -match '\\([^\\,]+)') {
        return $Matches[1]
    }

    return 'MSSQLSERVER'
}

function Get-SqlServiceName([string]$instanceName) {
    if ($instanceName -ieq 'MSSQLSERVER') {
        return 'MSSQLSERVER'
    }

    return "MSSQL`$$instanceName"
}

function Get-SqlInstanceRegistryPath([string]$instanceName) {
    $instanceNamesPath = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
    $instanceNames = Get-ItemProperty -Path $instanceNamesPath -ErrorAction Stop
    $instanceId = $instanceNames.$instanceName

    if ([string]::IsNullOrWhiteSpace($instanceId)) {
        throw "Could not resolve SQL instance id for instance '$instanceName'."
    }

    return "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\MSSQLServer"
}

function Wait-ForSqlReady([string]$serverName, [string]$sqlcmdPath, [int]$maxWaitSeconds = 180) {
    Write-Host "  Waiting for SQL Server to be ready..." -ForegroundColor Gray

    $interval = 5
    $waited = 0

    while ($waited -lt $maxWaitSeconds) {
        try {
            $result = & $sqlcmdPath -S $serverName -E -Q "SELECT 1" 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  SQL Server is ready." -ForegroundColor Gray
                return
            }
        } catch {}

        Write-Host "  Still waiting... ($waited s)" -ForegroundColor DarkGray
        Start-Sleep -Seconds $interval
        $waited += $interval
    }

    throw "SQL Server at $serverName did not become ready within $maxWaitSeconds seconds."
}

function Ensure-SqlMixedMode([string]$serverName) {
    $instanceName = Get-SqlInstanceName $serverName
    $serviceName = Get-SqlServiceName $instanceName
    $registryPath = Get-SqlInstanceRegistryPath $instanceName
    $currentMode = (Get-ItemProperty -Path $registryPath -Name LoginMode -ErrorAction SilentlyContinue).LoginMode

    if ($currentMode -eq 2) {
        Write-Host "  SQL authentication mode already enabled." -ForegroundColor Gray
        return
    }

    Write-Host "  Enabling SQL Server mixed authentication mode..." -ForegroundColor Gray
    Set-ItemProperty -Path $registryPath -Name LoginMode -Value 2

    Write-Host "  Restarting SQL service $serviceName..." -ForegroundColor Gray
    Restart-Service -Name $serviceName -Force -ErrorAction Stop
    Wait-ForSqlReady -serverName $serverName -sqlcmdPath $sqlcmd
}

Wait-ForSqlReady -serverName $SqlServer -sqlcmdPath $sqlcmd
Ensure-SqlMixedMode -serverName $SqlServer

# -- Create database, login, and user -----------------------------------------
$sql = @"
USE master;
GO

-- Drop database if it is in an inconsistent state (previous failed install).
-- Inconsistent = VMS exists but the Users table is missing, meaning migrations
-- never completed. A fresh drop ensures a clean slate.
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'VMS')
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [VMS].sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo')
    )
    BEGIN
        PRINT 'VMS database exists but Users table is missing -- dropping for clean install.';
        ALTER DATABASE [VMS] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [VMS];
        PRINT 'Dropped inconsistent VMS database.';
    END
    ELSE
        PRINT 'VMS database exists and schema is intact -- preserving data.';
END
GO

-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'VMS')
BEGIN
    CREATE DATABASE [VMS];
    PRINT 'Database VMS created.';
END
GO

-- Create SQL login if it does not exist
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'vms_app')
BEGIN
    CREATE LOGIN [vms_app]
        WITH PASSWORD = '$EscapedSqlPassword',
             CHECK_POLICY = OFF,
             CHECK_EXPIRATION = OFF;
    PRINT 'Login vms_app created.';
END
ELSE
BEGIN
    -- Update password in case of reinstall
    ALTER LOGIN [vms_app] WITH PASSWORD = '$EscapedSqlPassword', CHECK_POLICY = OFF;
    PRINT 'Login vms_app already exists -- password updated.';
END
GO

-- Map login to database user
USE [VMS];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'vms_app')
BEGIN
    CREATE USER [vms_app] FOR LOGIN [vms_app];
    PRINT 'User vms_app created.';
END
ELSE
    PRINT 'User vms_app already exists.';
GO

ALTER ROLE [db_owner] ADD MEMBER [vms_app];
PRINT 'Role db_owner granted.';
GO
"@

$tmpSql = [System.IO.Path]::GetTempFileName() + '.sql'
$sql | Set-Content $tmpSql -Encoding UTF8

Write-Host "  Running SQL setup script..." -ForegroundColor Gray
$output = & $sqlcmd -S $SqlServer -E -i $tmpSql 2>&1
Remove-Item $tmpSql -Force

if ($LASTEXITCODE -ne 0) {
    Write-Host $output
    throw "SQL setup failed with exit code $LASTEXITCODE"
}
Write-Host $output -ForegroundColor DarkGray

Write-Host "  Verifying SQL login for vms_app..." -ForegroundColor Gray
$verifyOutput = & $sqlcmd -S $SqlServer -U 'vms_app' -P $SqlPassword -Q "SELECT 1" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $verifyOutput
    throw "SQL login verification failed for vms_app."
}

Write-Host "[SQL] Database setup complete." -ForegroundColor Green
