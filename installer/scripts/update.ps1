<#
.SYNOPSIS
    Applies a packaged in-place VMS upgrade with backup, verification, and rollback.

.DESCRIPTION
    This script is intended to be run from the upgrade package selected by
    apply-update.ps1. It preserves runtime configuration, backs up the VMS
    database, snapshots the current application files, deploys the new build,
    verifies application startup through /health, and rolls back both files and
    database if the upgraded application fails to start cleanly.

.PARAMETER InstallPath
    Root install path of the existing VMS deployment (for example C:\VMS\app).

.PARAMETER PackagePath
    Path to the extracted upgrade package root or the .zip package file.

.EXAMPLE
    .\update.ps1 -InstallPath "C:\VMS\app" -PackagePath "C:\Temp\VMS-Upgrade-1.2.0.zip"
#>
param(
    [Parameter(Mandatory)][string]$InstallPath,
    [Parameter(Mandatory)][string]$PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:LogPaths = New-Object System.Collections.Generic.List[string]
$script:PackageExtractPath = $null
$script:ManagedExcludedDirectories = @(
    'logs',
    'uploads',
    'backups',
    '_vms_install',
    'wwwroot\uploads'
)
$script:PreservedRelativeFiles = @(
    'appsettings.Production.json',
    'wwwroot\config.js'
)
$script:DeploymentManifestRelativePath = '_vms_install\deployment-manifest.json'

function Add-LogPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $directory = Split-Path -Parent $resolvedPath
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    if (-not $script:LogPaths.Contains($resolvedPath)) {
        $script:LogPaths.Add($resolvedPath)
    }
}

function Write-Log(
    [string]$Message,
    [ConsoleColor]$Color = [ConsoleColor]::Gray
) {
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color

    foreach ($path in $script:LogPaths) {
        Add-Content -Path $path -Value $line
    }
}

function Assert-Path([string]$Path, [string]$Message) {
    if (-not (Test-Path $Path)) {
        throw $Message
    }
}

function Resolve-SqlCmdPath {
    $sqlcmd = Get-Command 'sqlcmd.exe' -ErrorAction SilentlyContinue
    if ($sqlcmd) {
        return $sqlcmd.Source
    }

    $candidates = @(
        'C:\Program Files\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\150\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\140\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe',
        'C:\Program Files\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw 'sqlcmd.exe was not found. Install SQL Server command-line tools before running the upgrade.'
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

function Get-SqlServiceAccount([string]$serverName) {
    $instanceName = Get-SqlInstanceName $serverName
    $serviceName = Get-SqlServiceName $instanceName

    try {
        $service = Get-CimInstance Win32_Service -Filter "Name='$serviceName'" -ErrorAction Stop
        if (-not [string]::IsNullOrWhiteSpace($service.StartName)) {
            return $service.StartName
        }
    } catch {
        Write-Log "Could not read SQL Server service account for ${serviceName}: $($_.Exception.Message)" DarkYellow
    }

    return 'NT AUTHORITY\NETWORK SERVICE'
}

function Grant-DirectoryAccess([string]$Path, [string]$Identity) {
    if ([string]::IsNullOrWhiteSpace($Identity)) {
        return
    }

    try {
        $acl = Get-Acl $Path
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $Identity,
            'Modify',
            'ContainerInherit,ObjectInherit',
            'None',
            'Allow'
        )
        $acl.SetAccessRule($rule)
        Set-Acl -Path $Path -AclObject $acl
        Write-Log "Granted $Identity modify access on $Path"
    } catch {
        Write-Log "Could not grant $Identity access on ${Path}: $($_.Exception.Message)" DarkYellow
    }
}

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [int[]]$AllowedExitCodes = @(0),
        [Parameter(Mandatory)][string]$Description
    )

    Write-Log $Description Cyan
    $output = & $FilePath @Arguments 2>&1
    $exitCode = $LASTEXITCODE

    foreach ($line in @($output)) {
        $text = [string]$line
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            Write-Log "  $text" DarkGray
        }
    }

    if ($AllowedExitCodes -notcontains $exitCode) {
        throw "$Description failed with exit code $exitCode."
    }
}

function Invoke-Robocopy {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [switch]$Mirror,
        [string[]]$ExcludeDirectories = @(),
        [string[]]$ExcludeFiles = @()
    )

    if (-not (Test-Path $Destination)) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }

    $arguments = @($Source, $Destination)
    if ($Mirror) {
        $arguments += '/MIR'
    } else {
        $arguments += '/E'
    }

    $arguments += @('/COPY:DAT', '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')

    if ($ExcludeDirectories.Count -gt 0) {
        $arguments += '/XD'
        $arguments += $ExcludeDirectories
    }

    if ($ExcludeFiles.Count -gt 0) {
        $arguments += '/XF'
        $arguments += $ExcludeFiles
    }

    Write-Log "Robocopy $Source -> $Destination" Cyan
    $output = & robocopy @arguments 2>&1
    $exitCode = $LASTEXITCODE

    foreach ($line in @($output)) {
        $text = [string]$line
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            Write-Log "  $text" DarkGray
        }
    }

    if ($exitCode -ge 8) {
        throw "Robocopy failed with exit code $exitCode while syncing $Source to $Destination."
    }
}

function Normalize-RelativePath([string]$RelativePath) {
    if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        return ''
    }

    return $RelativePath.Replace('/', '\').TrimStart('\')
}

function Resolve-ManagedPath([string]$RootPath, [string]$RelativePath) {
    $resolvedRoot = [System.IO.Path]::GetFullPath($RootPath).TrimEnd('\')
    $combinedPath = if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        $resolvedRoot
    } else {
        Join-Path $resolvedRoot (Normalize-RelativePath $RelativePath)
    }

    $resolvedPath = [System.IO.Path]::GetFullPath($combinedPath)
    if ($resolvedPath -ne $resolvedRoot -and -not $resolvedPath.StartsWith($resolvedRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Resolved path escaped install root: $RelativePath"
    }

    return $resolvedPath
}

function Test-IsManagedPathExcluded([string]$RelativePath) {
    $normalized = (Normalize-RelativePath $RelativePath).ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $true
    }

    foreach ($preservedFile in $script:PreservedRelativeFiles) {
        if ($normalized -eq $preservedFile.ToLowerInvariant()) {
            return $true
        }
    }

    foreach ($excludedDirectory in $script:ManagedExcludedDirectories) {
        $excludedPrefix = $excludedDirectory.ToLowerInvariant()
        if ($normalized -eq $excludedPrefix -or $normalized.StartsWith($excludedPrefix + '\')) {
            return $true
        }
    }

    return $false
}

function Get-ManagedRelativeFilePaths([string]$RootPath) {
    if (-not (Test-Path $RootPath)) {
        return @()
    }

    $resolvedRoot = [System.IO.Path]::GetFullPath($RootPath)

    return Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File | ForEach-Object {
        $relativePath = [System.IO.Path]::GetRelativePath($resolvedRoot, $_.FullName)
        $normalizedPath = Normalize-RelativePath $relativePath
        if (-not (Test-IsManagedPathExcluded $normalizedPath)) {
            $normalizedPath
        }
    } | Sort-Object -Unique
}

function Get-DeploymentManifestPath([string]$CurrentInstallPath) {
    return Resolve-ManagedPath -RootPath $CurrentInstallPath -RelativePath $script:DeploymentManifestRelativePath
}

function Load-DeploymentManifest([string]$CurrentInstallPath) {
    $manifestPath = Get-DeploymentManifestPath -CurrentInstallPath $CurrentInstallPath
    if (-not (Test-Path $manifestPath)) {
        Write-Log 'No prior deployment manifest found. Bootstrapping managed file list from the current install.' DarkYellow
        return Get-ManagedRelativeFilePaths -RootPath $CurrentInstallPath
    }

    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
    $files = @($manifest.files)

    return $files | ForEach-Object { Normalize-RelativePath ([string]$_) } | Where-Object {
        -not (Test-IsManagedPathExcluded $_)
    } | Sort-Object -Unique
}

function Save-DeploymentManifest([string]$CurrentInstallPath, [string]$Version, [string[]]$Files) {
    $manifestPath = Get-DeploymentManifestPath -CurrentInstallPath $CurrentInstallPath
    $manifestDirectory = Split-Path -Parent $manifestPath
    if (-not (Test-Path $manifestDirectory)) {
        New-Item -ItemType Directory -Path $manifestDirectory -Force | Out-Null
    }

    $manifest = [ordered]@{
        version      = $Version
        generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
        files        = @($Files | ForEach-Object { Normalize-RelativePath $_ } | Sort-Object -Unique)
    }

    $manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding UTF8
    Write-Log "Updated deployment manifest: $manifestPath"
}

function Remove-ObsoleteManagedFiles(
    [string]$CurrentInstallPath,
    [string[]]$PreviousFiles,
    [string[]]$CurrentFiles
) {
    $obsoleteFiles = @($PreviousFiles | Where-Object { $CurrentFiles -notcontains $_ })
    if ($obsoleteFiles.Count -eq 0) {
        Write-Log 'No obsolete deployed files were detected.' DarkGray
        return
    }

    foreach ($relativePath in $obsoleteFiles) {
        if (Test-IsManagedPathExcluded $relativePath) {
            continue
        }

        $resolvedPath = Resolve-ManagedPath -RootPath $CurrentInstallPath -RelativePath $relativePath
        if (Test-Path -LiteralPath $resolvedPath) {
            Remove-Item -LiteralPath $resolvedPath -Force -ErrorAction Stop
            Write-Log "Removed obsolete deployed file $relativePath"
        }
    }

    $obsoleteDirectories = $obsoleteFiles | ForEach-Object {
        Normalize-RelativePath (Split-Path -Parent $_)
    } | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_) -and -not (Test-IsManagedPathExcluded $_)
    } | Sort-Object -Unique

    foreach ($relativeDirectory in ($obsoleteDirectories | Sort-Object { $_.Length } -Descending)) {
        $resolvedDirectory = Resolve-ManagedPath -RootPath $CurrentInstallPath -RelativePath $relativeDirectory
        if (-not (Test-Path -LiteralPath $resolvedDirectory -PathType Container)) {
            continue
        }

        $remainingChildren = @(Get-ChildItem -LiteralPath $resolvedDirectory -Force)
        if ($remainingChildren.Count -eq 0) {
            Remove-Item -LiteralPath $resolvedDirectory -Force -ErrorAction Stop
            Write-Log "Removed empty deployed directory $relativeDirectory"
        }
    }
}

function Resolve-PackageRoot([string]$Path) {
    $resolvedPath = (Resolve-Path -Path $Path -ErrorAction Stop).Path

    if (Test-Path $resolvedPath -PathType Leaf) {
        if ([System.IO.Path]::GetExtension($resolvedPath) -ne '.zip') {
            throw "Unsupported package file: $resolvedPath. Provide a .zip package or extracted upgrade folder."
        }

        $extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vms-upgrade-" + [guid]::NewGuid().ToString('N'))
        Expand-Archive -Path $resolvedPath -DestinationPath $extractRoot -Force
        $script:PackageExtractPath = $extractRoot
        Write-Log "Extracted upgrade package to $extractRoot"
        return $extractRoot
    }

    return $resolvedPath
}

function Get-UpgradeManifest([string]$PackageRoot) {
    $manifestPath = Join-Path $PackageRoot 'manifest.json'
    Assert-Path $manifestPath "Upgrade manifest not found at $manifestPath."

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.packageType -ne 'VMSUpgradePackage') {
        throw "Unsupported package type '$($manifest.packageType)'."
    }

    $appPath = Join-Path $PackageRoot 'app'
    $supportScriptsPath = Join-Path $PackageRoot 'support-scripts'

    Assert-Path $appPath "Upgrade package is missing the app payload at $appPath."
    Assert-Path $supportScriptsPath "Upgrade package is missing the support scripts at $supportScriptsPath."

    return [pscustomobject]@{
        Manifest           = $manifest
        Version            = [string]$manifest.version
        CreatedUtc         = [string]$manifest.createdUtc
        AppPath            = $appPath
        SupportScriptsPath = $supportScriptsPath
    }
}

function Get-InstalledVersion([string]$CurrentInstallPath) {
    $exePath = Join-Path $CurrentInstallPath 'VisitorManagementSystem.Api.exe'
    if (-not (Test-Path $exePath)) {
        return 'unknown'
    }

    $versionInfo = (Get-Item $exePath).VersionInfo
    if (-not [string]::IsNullOrWhiteSpace($versionInfo.ProductVersion)) {
        return $versionInfo.ProductVersion
    }

    if (-not [string]::IsNullOrWhiteSpace($versionInfo.FileVersion)) {
        return $versionInfo.FileVersion
    }

    return 'unknown'
}

function Get-DatabaseInfo([string]$CurrentInstallPath) {
    $settingsPath = Join-Path $CurrentInstallPath 'appsettings.Production.json'
    Assert-Path $settingsPath "Production settings file not found at $settingsPath."

    $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $connectionString = [string]$settings.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "ConnectionStrings:DefaultConnection is missing from $settingsPath."
    }

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    if ([string]::IsNullOrWhiteSpace($builder.DataSource)) {
        throw 'Could not determine SQL Server data source from the production connection string.'
    }

    return [pscustomobject]@{
        DataSource      = $builder.DataSource
        DatabaseName    = if ([string]::IsNullOrWhiteSpace($builder.InitialCatalog)) { 'VMS' } else { $builder.InitialCatalog }
        ConnectionString = $connectionString
    }
}

function Invoke-SqlFile([string]$SqlCmdPath, [string]$ServerName, [string]$SqlText, [string]$Description) {
    $scriptPath = [System.IO.Path]::GetTempFileName() + '.sql'
    try {
        $SqlText | Set-Content -Path $scriptPath -Encoding UTF8
        Invoke-ExternalCommand `
            -FilePath $SqlCmdPath `
            -Arguments @('-S', $ServerName, '-E', '-b', '-i', $scriptPath) `
            -Description $Description
    } finally {
        Remove-Item -Path $scriptPath -Force -ErrorAction SilentlyContinue
    }
}

function Backup-Database {
    param(
        [Parameter(Mandatory)][string]$SqlCmdPath,
        [Parameter(Mandatory)][string]$ServerName,
        [Parameter(Mandatory)][string]$DatabaseName,
        [Parameter(Mandatory)][string]$BackupDirectory,
        [Parameter(Mandatory)][string]$UpgradeId
    )

    if (-not (Test-Path $BackupDirectory)) {
        New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
    }

    $serviceAccount = Get-SqlServiceAccount -serverName $ServerName
    Grant-DirectoryAccess -Path $BackupDirectory -Identity $serviceAccount

    $backupFile = Join-Path $BackupDirectory "$DatabaseName-$UpgradeId.bak"
    $escapedPath = $backupFile.Replace("'", "''")
    $sql = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$escapedPath'
WITH INIT, FORMAT, CHECKSUM, STATS = 10;
GO
RESTORE VERIFYONLY FROM DISK = N'$escapedPath';
GO
"@

    Invoke-SqlFile `
        -SqlCmdPath $SqlCmdPath `
        -ServerName $ServerName `
        -SqlText $sql `
        -Description "Backing up database [$DatabaseName] to $backupFile"

    Assert-Path $backupFile "Database backup file was not created at $backupFile."
    Write-Log "Database backup completed: $backupFile" Green
    return $backupFile
}

function Restore-Database {
    param(
        [Parameter(Mandatory)][string]$SqlCmdPath,
        [Parameter(Mandatory)][string]$ServerName,
        [Parameter(Mandatory)][string]$DatabaseName,
        [Parameter(Mandatory)][string]$BackupFile
    )

    $escapedPath = $BackupFile.Replace("'", "''")
    $sql = @"
USE master;
ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$DatabaseName]
FROM DISK = N'$escapedPath'
WITH REPLACE, RECOVERY, STATS = 10;
ALTER DATABASE [$DatabaseName] SET MULTI_USER;
GO
"@

    Invoke-SqlFile `
        -SqlCmdPath $SqlCmdPath `
        -ServerName $ServerName `
        -SqlText $sql `
        -Description "Restoring database [$DatabaseName] from $BackupFile"

    Write-Log "Database restored from $BackupFile" Yellow
}

function Stop-VmsSite {
    Import-Module WebAdministration

    if (Test-Path 'IIS:\Sites\VMS') {
        $siteState = (Get-WebsiteState -Name 'VMS').Value
        if ($siteState -eq 'Started') {
            Write-Log 'Stopping IIS site VMS' Cyan
            Stop-Website -Name 'VMS' -ErrorAction Stop
        } else {
            Write-Log "IIS site VMS already stopped ($siteState)" DarkGray
        }
    }

    if (Test-Path 'IIS:\AppPools\VMSAppPool') {
        $appPoolState = (Get-WebAppPoolState -Name 'VMSAppPool').Value
        if ($appPoolState -eq 'Started') {
            Write-Log 'Stopping IIS app pool VMSAppPool' Cyan
            Stop-WebAppPool -Name 'VMSAppPool' -ErrorAction Stop
        } else {
            Write-Log "IIS app pool VMSAppPool already stopped ($appPoolState)" DarkGray
        }
    }

    Start-Sleep -Seconds 3
}

function Start-VmsSite {
    Import-Module WebAdministration

    if (Test-Path 'IIS:\AppPools\VMSAppPool') {
        $appPoolState = (Get-WebAppPoolState -Name 'VMSAppPool').Value
        if ($appPoolState -ne 'Started') {
            Write-Log 'Starting IIS app pool VMSAppPool' Cyan
            Start-WebAppPool -Name 'VMSAppPool'
        } else {
            Write-Log 'IIS app pool VMSAppPool already started' DarkGray
        }
    }

    if (Test-Path 'IIS:\Sites\VMS') {
        $siteState = (Get-WebsiteState -Name 'VMS').Value
        if ($siteState -ne 'Started') {
            Write-Log 'Starting IIS site VMS' Cyan
            Start-Website -Name 'VMS'
        } else {
            Write-Log 'IIS site VMS already started' DarkGray
        }
    }
}

function Restore-PreservedFiles([string]$SnapshotPath, [string]$CurrentInstallPath) {
    $preservedFiles = @(
        'appsettings.Production.json',
        'wwwroot\config.js'
    )

    foreach ($relativePath in $preservedFiles) {
        $sourcePath = Join-Path $SnapshotPath $relativePath
        if (-not (Test-Path $sourcePath)) {
            continue
        }

        $destinationPath = Join-Path $CurrentInstallPath $relativePath
        $destinationDirectory = Split-Path -Parent $destinationPath
        if (-not (Test-Path $destinationDirectory)) {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        }

        Copy-Item -Path $sourcePath -Destination $destinationPath -Force
        Write-Log "Restored preserved file $relativePath"
    }
}

function Test-UpgradeHealth([string]$HealthUrl, [int]$TimeoutSeconds = 180) {
    Write-Log "Verifying upgraded application at $HealthUrl" Cyan

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $previousCallback = [System.Net.ServicePointManager]::ServerCertificateValidationCallback
    $previousProtocol = [System.Net.ServicePointManager]::SecurityProtocol

    [System.Net.ServicePointManager]::SecurityProtocol =
        [System.Net.SecurityProtocolType]::Tls12 -bor
        [System.Net.SecurityProtocolType]::Tls11 -bor
        [System.Net.SecurityProtocolType]::Tls
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

    try {
        while ((Get-Date) -lt $deadline) {
            try {
                $response = Invoke-WebRequest `
                    -Uri ($HealthUrl + '?ts=' + [guid]::NewGuid().ToString('N')) `
                    -UseBasicParsing `
                    -TimeoutSec 15

                if ($response.StatusCode -eq 200) {
                    $payload = $response.Content | ConvertFrom-Json
                    if ($null -ne $payload.status) {
                        Write-Log "Health check succeeded with status '$($payload.status)'" Green
                        return
                    }
                }

                Write-Log "Health endpoint returned HTTP $($response.StatusCode); waiting for the app to become ready..." DarkYellow
            } catch {
                Write-Log "Health probe retry: $($_.Exception.Message)" DarkYellow
            }

            Start-Sleep -Seconds 5
        }
    } finally {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback
        [System.Net.ServicePointManager]::SecurityProtocol = $previousProtocol
    }

    throw "Application did not become healthy within $TimeoutSeconds seconds."
}

$InstallPath = [System.IO.Path]::GetFullPath($InstallPath)
Add-LogPath (Join-Path $InstallPath 'logs\vms-upgrade.log')

$upgradeId = Get-Date -Format 'yyyyMMdd-HHmmss'
$upgradeRoot = $null
$databaseBackupFile = $null
$appSnapshotPath = $null
$rollbackRequired = $false
$previousManagedFiles = @()
$packageManagedFiles = @()

try {
    Assert-Path $InstallPath "VMS installation was not found at $InstallPath."

    $packageRoot = Resolve-PackageRoot -Path $PackagePath
    $package = Get-UpgradeManifest -PackageRoot $packageRoot
    $currentVersion = Get-InstalledVersion -CurrentInstallPath $InstallPath
    $previousManagedFiles = @(Load-DeploymentManifest -CurrentInstallPath $InstallPath)
    $packageManagedFiles = @(Get-ManagedRelativeFilePaths -RootPath $package.AppPath)

    $installParent = Split-Path -Parent $InstallPath
    $upgradeRoot = Join-Path $installParent "backups\upgrade-$upgradeId"
    $databaseBackupDirectory = Join-Path $upgradeRoot 'database'
    $appSnapshotPath = Join-Path $upgradeRoot 'app-snapshot'

    New-Item -ItemType Directory -Path $databaseBackupDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $appSnapshotPath -Force | Out-Null
    Add-LogPath (Join-Path $upgradeRoot 'upgrade.log')

    Write-Log '============================================================' Green
    Write-Log 'Starting VMS in-place upgrade' Green
    Write-Log "Current version : $currentVersion"
    Write-Log "Target version  : $($package.Version)"
    Write-Log "Package created : $($package.CreatedUtc)"
    Write-Log "Install path    : $InstallPath"
    Write-Log "Upgrade log dir : $upgradeRoot"
    Write-Log "Managed files   : current=$($previousManagedFiles.Count), package=$($packageManagedFiles.Count)"
    Write-Log '============================================================' Green

    $databaseInfo = Get-DatabaseInfo -CurrentInstallPath $InstallPath
    $sqlcmdPath = Resolve-SqlCmdPath
    Write-Log "Detected SQL Server: $($databaseInfo.DataSource)"
    Write-Log "Detected database : $($databaseInfo.DatabaseName)"

    $databaseBackupFile = Backup-Database `
        -SqlCmdPath $sqlcmdPath `
        -ServerName $databaseInfo.DataSource `
        -DatabaseName $databaseInfo.DatabaseName `
        -BackupDirectory $databaseBackupDirectory `
        -UpgradeId $upgradeId

    Stop-VmsSite

    Invoke-Robocopy `
        -Source $InstallPath `
        -Destination $appSnapshotPath `
        -Mirror `
        -ExcludeDirectories @(
            (Join-Path $InstallPath 'logs'),
            (Join-Path $InstallPath 'uploads'),
            (Join-Path $InstallPath 'backups'),
            (Join-Path $InstallPath 'wwwroot\uploads')
        )

    Write-Log "Application snapshot completed: $appSnapshotPath" Green
    $rollbackRequired = $true

    Invoke-Robocopy `
        -Source $package.AppPath `
        -Destination $InstallPath `
        -ExcludeDirectories @(
            (Join-Path $package.AppPath 'logs'),
            (Join-Path $package.AppPath 'uploads'),
            (Join-Path $package.AppPath 'backups'),
            (Join-Path $package.AppPath '_vms_install'),
            (Join-Path $package.AppPath 'wwwroot\uploads')
        ) `
        -ExcludeFiles @(
            'appsettings.Production.json',
            'config.js'
        )

    Restore-PreservedFiles -SnapshotPath $appSnapshotPath -CurrentInstallPath $InstallPath
    Remove-ObsoleteManagedFiles `
        -CurrentInstallPath $InstallPath `
        -PreviousFiles $previousManagedFiles `
        -CurrentFiles $packageManagedFiles

    Invoke-Robocopy `
        -Source $package.SupportScriptsPath `
        -Destination (Join-Path $InstallPath '_vms_install') `
        -ExcludeDirectories @()

    Start-VmsSite
    Test-UpgradeHealth -HealthUrl 'https://localhost/health'
    Save-DeploymentManifest `
        -CurrentInstallPath $InstallPath `
        -Version $package.Version `
        -Files $packageManagedFiles

    Write-Log '============================================================' Green
    Write-Log 'VMS upgrade completed successfully.' Green
    Write-Log "Database backup : $databaseBackupFile"
    Write-Log "App snapshot    : $appSnapshotPath"
    Write-Log '============================================================' Green
} catch {
    Write-Log "Upgrade failed: $($_.Exception.Message)" Red

    if ($rollbackRequired) {
        Write-Log 'Starting rollback...' Yellow

        try {
            Stop-VmsSite

            if ($appSnapshotPath -and (Test-Path $appSnapshotPath)) {
                Invoke-Robocopy `
                    -Source $appSnapshotPath `
                    -Destination $InstallPath `
                    -Mirror `
                    -ExcludeDirectories @(
                        (Join-Path $appSnapshotPath 'logs'),
                        (Join-Path $appSnapshotPath 'uploads'),
                        (Join-Path $appSnapshotPath 'backups'),
                        (Join-Path $appSnapshotPath 'wwwroot\uploads')
                    )
                Write-Log 'Application files restored from snapshot.' Yellow
            }

            if ($databaseBackupFile -and (Test-Path $databaseBackupFile)) {
                $databaseInfo = Get-DatabaseInfo -CurrentInstallPath $InstallPath
                $sqlcmdPath = Resolve-SqlCmdPath
                Restore-Database `
                    -SqlCmdPath $sqlcmdPath `
                    -ServerName $databaseInfo.DataSource `
                    -DatabaseName $databaseInfo.DatabaseName `
                    -BackupFile $databaseBackupFile
            }

            Start-VmsSite
            Test-UpgradeHealth -HealthUrl 'https://localhost/health'
            Write-Log 'Rollback completed successfully. Previous version is back online.' Yellow
        } catch {
            Write-Log "Rollback failed: $($_.Exception.Message)" Red
            throw "Upgrade failed and rollback did not complete cleanly. Review $upgradeRoot\upgrade.log."
        }
    }

    throw
} finally {
    if ($script:PackageExtractPath -and (Test-Path $script:PackageExtractPath)) {
        Remove-Item -Path $script:PackageExtractPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}
