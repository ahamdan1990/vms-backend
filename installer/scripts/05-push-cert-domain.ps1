<#
.SYNOPSIS
    Pushes the VMS HTTPS certificate to remote Windows machines.

.DESCRIPTION
    Uses PowerShell Remoting to import the certificate into
    Cert:\LocalMachine\Root on every target machine.

.PARAMETER ConfigFile
    Optional path to the installer config file. If omitted, the script uses its
    own _vms_install folder to locate the certificate.

.PARAMETER Computers
    Explicit list of computer names or IPs to target.

.PARAMETER AllDomainComputers
    Query Active Directory for all enabled computer accounts and push to all of
    them. Requires the ActiveDirectory module.

.PARAMETER CertFile
    Override the certificate path. Defaults to <script folder>\vms-cert.cer.
#>
param(
    [string]$ConfigFile,
    [string[]]$Computers,
    [switch]$AllDomainComputers,
    [string]$CertFile
)
$ErrorActionPreference = 'Stop'

$appPath = Split-Path $PSScriptRoot -Parent
if ($ConfigFile) {
    if (-not (Test-Path $ConfigFile)) {
        throw "Config file not found at '$ConfigFile'."
    }

    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
    if ($config.AppPath) {
        $appPath = [string]$config.AppPath
    }
}

$resolvedCert = if ($CertFile) {
    $CertFile
} else {
    Join-Path $PSScriptRoot 'vms-cert.cer'
}

if (-not (Test-Path $resolvedCert)) {
    throw "Certificate not found at '$resolvedCert'. Run 04-configure-iis.ps1 first."
}

$certBytes = [System.IO.File]::ReadAllBytes($resolvedCert)
Write-Host "[CertPush] Using certificate: $resolvedCert" -ForegroundColor Cyan
Write-Host "           Size: $($certBytes.Length) bytes" -ForegroundColor Gray

if ($AllDomainComputers) {
    Write-Host "[CertPush] Querying Active Directory for all enabled computers..." -ForegroundColor Cyan
    if (-not (Get-Module -ListAvailable -Name ActiveDirectory)) {
        throw "ActiveDirectory module not found.`nInstall it with: Add-WindowsCapability -Online -Name Rsat.ActiveDirectory.DS-LDS.Tools~~~~0.0.1.0"
    }

    Import-Module ActiveDirectory -ErrorAction Stop
    $Computers = Get-ADComputer -Filter { Enabled -eq $true } |
        Select-Object -ExpandProperty Name
    Write-Host "           Found $($Computers.Count) domain computers." -ForegroundColor Gray
}

if (-not $Computers -or $Computers.Count -eq 0) {
    throw "No target computers specified. Use -Computers or -AllDomainComputers."
}

Write-Host "[CertPush] Pushing to $($Computers.Count) computer(s)..." -ForegroundColor Cyan

$succeeded = [System.Collections.Generic.List[string]]::new()
$failed = [System.Collections.Generic.List[string]]::new()

$scriptBlock = {
    param([byte[]]$CertData)

    $tempFile = [System.IO.Path]::GetTempFileName() + '.cer'
    [System.IO.File]::WriteAllBytes($tempFile, $CertData)

    try {
        $imported = Import-Certificate -FilePath $tempFile -CertStoreLocation 'Cert:\LocalMachine\Root'
        "Installed: $($imported.Subject) [$($imported.Thumbprint.Substring(0,8))...]"
    }
    finally {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
}

foreach ($computer in $Computers) {
    if ($computer -ieq $env:COMPUTERNAME) {
        Write-Host "  SKIP  : $computer (this is the VMS server, already trusted)" -ForegroundColor DarkGray
        continue
    }

    try {
        $result = Invoke-Command -ComputerName $computer `
            -ScriptBlock $scriptBlock `
            -ArgumentList (,$certBytes) `
            -ErrorAction Stop

        Write-Host "  OK    : $computer - $result" -ForegroundColor Green
        $succeeded.Add($computer)
    }
    catch {
        $reason = $_.Exception.Message -replace '\r?\n', ' '
        Write-Host "  FAILED: $computer - $reason" -ForegroundColor Red
        $failed.Add($computer)
    }
}

Write-Host ""
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host " Results: $($succeeded.Count) succeeded, $($failed.Count) failed" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed computers:" -ForegroundColor Yellow
    $failed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    Write-Host ""
    Write-Host "Common causes and fixes:" -ForegroundColor Yellow
    Write-Host "  WinRM not enabled   : run winrm quickconfig on the target as Administrator" -ForegroundColor Yellow
    Write-Host "  Firewall blocking   : open TCP 5985 or 5986 on the target" -ForegroundColor Yellow
    Write-Host "  Insufficient rights : re-run this script as Domain Admin" -ForegroundColor Yellow
    Write-Host "  Machine offline     : retry later; skipped machines are listed above" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative for failed machines: copy these files from the server and run as Admin:" -ForegroundColor Yellow
    Write-Host "  $(Join-Path $appPath '_vms_install\install-cert.ps1')" -ForegroundColor White
    Write-Host "  $(Join-Path $appPath '_vms_install\vms-cert.cer')" -ForegroundColor White
}

if ($succeeded.Count -gt 0) {
    Write-Host ""
    Write-Host "Certificate trusted on $($succeeded.Count) machine(s)." -ForegroundColor Green
}
