<#
.SYNOPSIS
    Builds the complete VMS Windows installer (.exe).

.DESCRIPTION
    1. Publishes the .NET 9 API
    2. Builds the React frontend
    3. Assembles the dist/ folder
    4. Verifies all prerequisites are present in prereqs/
    5. Compiles the Inno Setup installer

.PARAMETER Version
    Version number to embed in the installer filename. Default: "1.0.0"

.PARAMETER SkipFrontend
    Skip npm build (useful if React build is already current).

.PARAMETER SkipApi
    Skip dotnet publish (useful if API publish is already current).

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version "2.1.0"
    .\build-installer.ps1 -SkipFrontend

.PREREQUISITES
    Before running, download these files into .\prereqs\:

    1. .NET 9 Hosting Bundle
       URL: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
       File: dotnet-hosting-9.0.x-win.exe   (find the "Hosting Bundle" installer)

    2. Redis for Windows (tporadowski port)
       URL: https://github.com/tporadowski/redis/releases
       File: Redis-x64-5.0.14.msi

    3. SQL Server 2022 Express (full offline installer)
       URL: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
            Click "Express" -> "Download Media" -> "Express Advanced" package
       File: SQLEXPR_x64_ENU.exe

    4. Inno Setup 6 must be installed:
       URL: https://jrsoftware.org/isdl.php
       Default install path: C:\Program Files (x86)\Inno Setup 6\
#>

param(
    [string]$Version       = '1.0.0',
    [switch]$SkipFrontend,
    [switch]$SkipApi,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$ScriptDir   = $PSScriptRoot
$RepoRoot    = Split-Path -Parent $ScriptDir
$BackendDir  = Join-Path $RepoRoot 'vms-backend'
$FrontendDir = Join-Path $RepoRoot 'vms-frontend'
$DistAppDir  = Join-Path $ScriptDir 'dist\app'
$UpgradeStageDir = Join-Path $ScriptDir 'dist\upgrade-package'
$PrereqsDir  = Join-Path $ScriptDir 'prereqs'
$OutputDir   = Join-Path $ScriptDir 'output'
$InnoSetup   = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'

function Write-Step([string]$Msg) {
    $sep = '=' * 60
    Write-Host ''
    Write-Host $sep      -ForegroundColor DarkGray
    Write-Host "  $Msg"  -ForegroundColor Cyan
    Write-Host $sep      -ForegroundColor DarkGray
}

function Assert-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

# --- 0. Preflight checks ---
Write-Step 'Preflight checks'

Assert-Command 'dotnet'
Assert-Command 'node'
Assert-Command 'npm'

if (-not (Test-Path $InnoSetup)) {
    throw "Inno Setup 6 not found at: $InnoSetup`n  Download from: https://jrsoftware.org/isdl.php"
}
Write-Host "  dotnet:    $(dotnet --version)" -ForegroundColor Gray
Write-Host "  node:      $(node --version)"   -ForegroundColor Gray
Write-Host "  npm:       $(npm --version)"    -ForegroundColor Gray
Write-Host '  InnoSetup: found'               -ForegroundColor Gray

# --- 1. Clean dist ---
Write-Step 'Cleaning dist/'
if (Test-Path $DistAppDir) { Remove-Item $DistAppDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistAppDir | Out-Null
Write-Host '  Cleaned: dist/' -ForegroundColor Gray

# --- 2. Publish .NET API ---
if (-not $SkipApi) {
    Write-Step 'Publishing .NET 9 API'
    Push-Location $BackendDir
    try {
        dotnet publish 'VisitorManagementSystem.Api.csproj' `
            --configuration Release `
            --runtime win-x64 `
            --self-contained false `
            --output $DistAppDir `
            /p:PublishSingleFile=false `
            /p:DebugType=None `
            /p:DebugSymbols=false

        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
        Write-Host "  Published to: $DistAppDir" -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host '  Skipping .NET publish (-SkipApi)' -ForegroundColor Yellow
}

# --- 3. Build React frontend ---
if (-not $SkipFrontend) {
    Write-Step 'Building React frontend'
    Push-Location $FrontendDir
    try {
        $env:REACT_APP_API_URL  = ''
        $env:GENERATE_SOURCEMAP = 'false'
        $env:NODE_ENV           = 'production'
        $env:CI                 = 'false'   # prevent react-scripts from treating warnings as errors

        # Use npx react-scripts directly — avoids the Unix-syntax env var prefix
        # in the package.json "build" script which fails on Windows cmd.exe
        npx react-scripts build
        if ($LASTEXITCODE -ne 0) { throw "react-scripts build failed (exit $LASTEXITCODE)" }

        $wwwrootDest = Join-Path $DistAppDir 'wwwroot'
        New-Item -ItemType Directory -Force -Path $wwwrootDest | Out-Null
        Copy-Item -Recurse -Force 'build\*' $wwwrootDest
        Write-Host "  Built and copied to: $wwwrootDest" -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host '  Skipping React build (-SkipFrontend)' -ForegroundColor Yellow
}

# --- 4. Remove runtime-only content from packaged output ---
Write-Step 'Sanitizing dist/'
$runtimeOnlyDirs = @(
    (Join-Path $DistAppDir 'logs'),
    (Join-Path $DistAppDir 'uploads'),
    (Join-Path $DistAppDir 'backups'),
    (Join-Path $DistAppDir 'wwwroot\uploads')
)

foreach ($dir in $runtimeOnlyDirs) {
    if (Test-Path $dir) {
        Remove-Item -Path $dir -Recurse -Force
        Write-Host "  Removed runtime directory: $dir" -ForegroundColor Gray
    }
}

# --- 5. Verify published output ---
Write-Step 'Verifying dist/'
$requiredFiles = @(
    'VisitorManagementSystem.Api.exe',
    'appsettings.json',
    'appsettings.Production.json',
    'web.config',
    'wwwroot\index.html',
    'wwwroot\config.js'
)
$allOk = $true
foreach ($f in $requiredFiles) {
    $full = Join-Path $DistAppDir $f
    if (Test-Path $full) {
        Write-Host "  OK: $f" -ForegroundColor Green
    } else {
        Write-Host "  MISSING: $f" -ForegroundColor Red
        $allOk = $false
    }
}
if (-not $allOk) { throw 'One or more required output files are missing. Check the build output above.' }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# --- 5. Build upgrade package ---
Write-Step 'Building upgrade package'
if (Test-Path $UpgradeStageDir) { Remove-Item $UpgradeStageDir -Recurse -Force }

$upgradeAppDir = Join-Path $UpgradeStageDir 'app'
$upgradeScriptsDir = Join-Path $UpgradeStageDir 'support-scripts'
New-Item -ItemType Directory -Force -Path $upgradeAppDir | Out-Null
New-Item -ItemType Directory -Force -Path $upgradeScriptsDir | Out-Null

Copy-Item -Path (Join-Path $DistAppDir '*') -Destination $upgradeAppDir -Recurse -Force
Copy-Item -Path (Join-Path $ScriptDir 'scripts\*') -Destination $upgradeScriptsDir -Recurse -Force

$manifest = [ordered]@{
    packageType = 'VMSUpgradePackage'
    version     = $Version
    createdUtc  = (Get-Date).ToUniversalTime().ToString('o')
}

$manifestPath = Join-Path $UpgradeStageDir 'manifest.json'
$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

$upgradeZip = Join-Path $OutputDir "VMS-Upgrade-$Version.zip"
if (Test-Path $upgradeZip) { Remove-Item $upgradeZip -Force }

Compress-Archive -Path (Join-Path $UpgradeStageDir '*') -DestinationPath $upgradeZip -CompressionLevel Optimal
Write-Host "  Upgrade package : $upgradeZip" -ForegroundColor Green

# --- 6. Verify prerequisites / compile installer ---
$outputExe = $null
if (-not $SkipInstaller) {
    Write-Step 'Verifying prerequisites in prereqs/'
    $prereqs = @{
        'dotnet-hosting-9.0.14-win.exe' = 'https://dotnet.microsoft.com/en-us/download/dotnet/9.0  (Hosting Bundle)'
        'Redis-x64-5.0.14.1.msi'        = 'https://github.com/tporadowski/redis/releases'
        'SQLEXPRADV_x64_ENU.exe'         = 'https://www.microsoft.com/en-us/sql-server/sql-server-downloads  (Express Advanced)'
    }
    $missingPrereqs = @()
    foreach ($file in $prereqs.Keys) {
        $path = Join-Path $PrereqsDir $file
        if (Test-Path $path) {
            $mb = [math]::Round((Get-Item $path).Length / 1MB, 1)
            Write-Host "  OK ($mb MB): $file" -ForegroundColor Green
        } else {
            Write-Host "  MISSING: $file" -ForegroundColor Red
            Write-Host "    Download: $($prereqs[$file])" -ForegroundColor DarkGray
            $missingPrereqs += $file
        }
    }

if ($missingPrereqs.Count -gt 0) {
        Write-Host "`n  Cannot build installer -- download the missing files listed above." -ForegroundColor Red
        throw 'Missing prerequisite files.'
    }

    Write-Step 'Compiling Inno Setup installer'

    & $InnoSetup `
        "/DMyAppVersion=$Version" `
        /O"$OutputDir" `
        (Join-Path $ScriptDir 'vms-setup.iss')

    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed (exit $LASTEXITCODE)" }

    $outputExe = Join-Path $OutputDir "VMS-Setup-$Version.exe"
}

# --- Done ---
$upgradeMb = [math]::Round((Get-Item $upgradeZip).Length / 1MB, 1)
$line      = '=' * 60

Write-Host ''
Write-Host $line                        -ForegroundColor Green
Write-Host '  BUILD COMPLETE'           -ForegroundColor Green
Write-Host $line                        -ForegroundColor Green
if ($outputExe) {
    $mb = [math]::Round((Get-Item $outputExe).Length / 1MB, 1)
    Write-Host "  Installer      : $outputExe"  -ForegroundColor White
    Write-Host "  Installer size : $mb MB"      -ForegroundColor White
} else {
    Write-Host '  Installer      : skipped (-SkipInstaller)' -ForegroundColor White
}
Write-Host "  Upgrade package: $upgradeZip"    -ForegroundColor White
Write-Host "  Package size   : $upgradeMb MB"  -ForegroundColor White
Write-Host $line                        -ForegroundColor Green
