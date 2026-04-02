<#
.SYNOPSIS
    Cleanly removes the VMS installation.
    Called by the Inno Setup uninstaller.
#>
param(
    [Parameter(Mandatory)][string]$AppPath,
    [switch]$RemoveDatabase
)

Write-Host "[Uninstall] Removing VMS components..." -ForegroundColor Cyan

# Stop and remove IIS site and app pool
Import-Module WebAdministration -ErrorAction SilentlyContinue

$httpsHosts = @()
if (Test-Path 'IIS:\Sites\VMS') {
    $httpsHosts = Get-WebBinding -Name 'VMS' -Protocol 'https' -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty HostHeader -Unique
    Stop-WebSite -Name 'VMS' -ErrorAction SilentlyContinue
    Remove-WebSite -Name 'VMS' -ErrorAction SilentlyContinue
    Write-Host "  Removed IIS site: VMS" -ForegroundColor Gray
}

if (Test-Path 'IIS:\AppPools\VMSAppPool') {
    Stop-WebAppPool -Name 'VMSAppPool' -ErrorAction SilentlyContinue
    Remove-WebAppPool -Name 'VMSAppPool' -ErrorAction SilentlyContinue
    Write-Host "  Removed app pool: VMSAppPool" -ForegroundColor Gray
}

foreach ($host in $httpsHosts) {
    if (-not [string]::IsNullOrWhiteSpace($host)) {
        netsh http delete sslcert hostnameport="$host`:443" | Out-Null
        Write-Host "  Removed SSL binding for host: $host" -ForegroundColor Gray
    }
}
netsh http delete sslcert ipport=0.0.0.0:443 | Out-Null

Get-ChildItem 'Cert:\LocalMachine\My' -ErrorAction SilentlyContinue |
    Where-Object { $_.FriendlyName -eq 'VMS HTTPS Certificate' } |
    ForEach-Object {
        Remove-Item $_.PSPath -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed certificate from LocalMachine\\My: $($_.Thumbprint)" -ForegroundColor Gray
    }

Get-ChildItem 'Cert:\LocalMachine\Root' -ErrorAction SilentlyContinue |
    Where-Object { $_.FriendlyName -eq 'VMS HTTPS Certificate' } |
    ForEach-Object {
        Remove-Item $_.PSPath -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed certificate from LocalMachine\\Root: $($_.Thumbprint)" -ForegroundColor Gray
    }

# Remove firewall rules
$rulesToRemove = @('VMS-HTTP', 'VMS-HTTPS')
foreach ($rule in $rulesToRemove) {
    Remove-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue
    Write-Host "  Removed firewall rule: $rule" -ForegroundColor Gray
}

# Optionally drop the database
if ($RemoveDatabase) {
    Write-Host "  Dropping VMS database..." -ForegroundColor Yellow
    $sql = @"
USE master;
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'VMS')
BEGIN
    ALTER DATABASE [VMS] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [VMS];
    PRINT 'Database VMS dropped.';
END
IF EXISTS (SELECT name FROM sys.server_principals WHERE name = 'vms_app')
BEGIN
    DROP LOGIN [vms_app];
    PRINT 'Login vms_app dropped.';
END
"@
    try {
        $tmp = [System.IO.Path]::GetTempFileName() + '.sql'
        $sql | Set-Content $tmp -Encoding UTF8
        sqlcmd -S '.\SQLEXPRESS' -E -i $tmp 2>&1 | Write-Host
        Remove-Item $tmp -Force
    } catch {
        Write-Warning "Could not drop database: $_"
    }
}

Write-Host "[Uninstall] Done." -ForegroundColor Green
