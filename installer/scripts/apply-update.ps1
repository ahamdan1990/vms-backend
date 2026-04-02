<#
.SYNOPSIS
    Interactive launcher for packaged VMS upgrades.

.DESCRIPTION
    This script is installed under _vms_install and is intended to be launched
    from the Start menu shortcut or upgrade-vms.bat. It self-elevates if needed,
    prompts the operator for an upgrade package when one is not provided, and
    then executes the upgrade engine bundled inside that package.
#>
param(
    [string]$PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ExtractedPackageRoot = $null

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal $identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Start-ElevatedSelf([string]$SelectedPackagePath) {
    $arguments = @(
        '-NoProfile',
        '-STA',
        '-ExecutionPolicy', 'Bypass',
        '-File', "`"$PSCommandPath`""
    )

    if (-not [string]::IsNullOrWhiteSpace($SelectedPackagePath)) {
        $arguments += @('-PackagePath', "`"$SelectedPackagePath`"")
    }

    $process = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList $arguments `
        -Verb RunAs `
        -Wait `
        -PassThru

    exit $process.ExitCode
}

function Select-UpgradePackage {
    Add-Type -AssemblyName System.Windows.Forms

    $fileDialog = New-Object System.Windows.Forms.OpenFileDialog
    $fileDialog.Title = 'Select VMS upgrade package'
    $fileDialog.Filter = 'VMS Upgrade Package (*.zip)|*.zip|All files (*.*)|*.*'
    $fileDialog.Multiselect = $false

    if ($fileDialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        return $fileDialog.FileName
    }

    $folderDialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $folderDialog.Description = 'Select an extracted VMS upgrade package folder'
    $folderDialog.ShowNewFolderButton = $false

    if ($folderDialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        return $folderDialog.SelectedPath
    }

    throw 'No upgrade package was selected.'
}

function Resolve-UpgradeRoot([string]$SelectedPackagePath) {
    $resolvedPath = (Resolve-Path -Path $SelectedPackagePath -ErrorAction Stop).Path

    if (Test-Path $resolvedPath -PathType Leaf) {
        if ([System.IO.Path]::GetExtension($resolvedPath) -ne '.zip') {
            throw "Unsupported package file: $resolvedPath"
        }

        $extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vms-upgrade-launch-" + [guid]::NewGuid().ToString('N'))
        Expand-Archive -Path $resolvedPath -DestinationPath $extractRoot -Force
        $script:ExtractedPackageRoot = $extractRoot
        return $extractRoot
    }

    return $resolvedPath
}

try {
    if (-not (Test-IsAdministrator)) {
        Start-ElevatedSelf -SelectedPackagePath $PackagePath
    }

    if ([string]::IsNullOrWhiteSpace($PackagePath)) {
        $PackagePath = Select-UpgradePackage
    }

    $installPath = Split-Path -Parent $PSScriptRoot
    $packageRoot = Resolve-UpgradeRoot -SelectedPackagePath $PackagePath
    $manifestPath = Join-Path $packageRoot 'manifest.json'
    $packageUpdateScript = Join-Path $packageRoot 'support-scripts\update.ps1'

    if (-not (Test-Path $manifestPath)) {
        throw "Upgrade manifest not found at $manifestPath"
    }

    if (-not (Test-Path $packageUpdateScript)) {
        throw "Package upgrade script not found at $packageUpdateScript"
    }

    Write-Host "Applying VMS upgrade from $PackagePath" -ForegroundColor Cyan
    & $packageUpdateScript -InstallPath $installPath -PackagePath $packageRoot
} catch {
    Write-Host "Upgrade launcher failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    if ($script:ExtractedPackageRoot -and (Test-Path $script:ExtractedPackageRoot)) {
        Remove-Item -Path $script:ExtractedPackageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
