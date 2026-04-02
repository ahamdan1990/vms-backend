<#
.SYNOPSIS
    Enables all required IIS Windows features for hosting the VMS application.
    Requires WebSockets for SignalR real-time notifications.
#>
$ErrorActionPreference = 'Stop'

Write-Host "[IIS] Enabling Windows features..." -ForegroundColor Cyan

$features = @(
    'IIS-WebServerRole',
    'IIS-WebServer',
    'IIS-CommonHttpFeatures',
    'IIS-StaticContent',
    'IIS-DefaultDocument',
    'IIS-DirectoryBrowsing',
    'IIS-HttpErrors',
    'IIS-Security',
    'IIS-RequestFiltering',
    'IIS-Performance',
    'IIS-HttpCompressionStatic',
    'IIS-HttpCompressionDynamic',
    'IIS-WebSockets',              # Required: SignalR real-time notifications
    'IIS-ApplicationDevelopment',
    'IIS-ISAPIExtensions',
    'IIS-ISAPIFilter',
    'IIS-ASPNET45',
    'IIS-ManagementConsole',
    'IIS-ManagementScriptingTools'
)

$anyInstalled = $false
foreach ($feature in $features) {
    $state = (Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue).State
    if ($state -ne 'Enabled') {
        Write-Host "  Enabling: $feature" -ForegroundColor Gray
        Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart | Out-Null
        $anyInstalled = $true
    } else {
        Write-Host "  Already enabled: $feature" -ForegroundColor DarkGray
    }
}

# Restart IIS to pick up any changes
Write-Host "[IIS] Restarting IIS..." -ForegroundColor Cyan
Start-Process -FilePath 'iisreset.exe' -ArgumentList '/restart' -Wait -NoNewWindow

Write-Host "[IIS] Features enabled successfully." -ForegroundColor Green
