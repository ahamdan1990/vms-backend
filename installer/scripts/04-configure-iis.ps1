<#
.SYNOPSIS
    Creates the IIS app pool and website, generates a self-signed SSL certificate,
    configures HTTPS bindings for the supported hostnames, aligns environment
    variables in web.config, grants file-system permissions to the app-pool
    identity, and opens firewall ports 80 and 443.
#>
param(
    [Parameter(Mandatory)][string]$ConfigFile
)
$ErrorActionPreference = 'Stop'

function Set-JsonProperty([psobject]$Object, [string]$Name, $Value) {
    if ($null -eq $Object.PSObject.Properties[$Name]) {
        $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    } else {
        $Object.$Name = $Value
    }
}

function Set-EnvVar([xml]$doc, $parent, [string]$name, [string]$value) {
    $existing = $parent.SelectNodes("environmentVariable[@name='$name']")
    foreach ($node in $existing) {
        $parent.RemoveChild($node) | Out-Null
    }

    $el = $doc.CreateElement('environmentVariable')
    $el.SetAttribute('name', $name)
    $el.SetAttribute('value', $value)
    $parent.AppendChild($el) | Out-Null
}

function Remove-HostnameSslBinding([string]$hostName) {
    if ([string]::IsNullOrWhiteSpace($hostName)) {
        return
    }

    netsh http delete sslcert hostnameport="$hostName`:443" | Out-Null
}

function Remove-IpSslBinding([string]$ipAddress) {
    if ([string]::IsNullOrWhiteSpace($ipAddress)) {
        return
    }

    netsh http delete sslcert ipport="$ipAddress`:443" | Out-Null
}

function Get-LocalIpv4Addresses {
    try {
        return Get-NetIPAddress -AddressFamily IPv4 -ErrorAction Stop |
            Where-Object {
                $_.IPAddress -and
                $_.IPAddress -notlike '169.254.*'
            } |
            Select-Object -ExpandProperty IPAddress -Unique
    } catch {
        return @('127.0.0.1')
    }
}

function Normalize-HostValue([string]$hostValue) {
    if ([string]::IsNullOrWhiteSpace($hostValue)) {
        return $null
    }

    $trimmed = $hostValue.Trim()
    $parsedIp = $null
    if ([System.Net.IPAddress]::TryParse($trimmed, [ref]$parsedIp)) {
        return $trimmed
    }

    return $trimmed.ToLowerInvariant()
}

function Invoke-NetshOrThrow([string[]]$Arguments, [string]$failureMessage) {
    $output = & netsh @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $details = ($output | Out-String).Trim()
        if ([string]::IsNullOrWhiteSpace($details)) {
            throw $failureMessage
        }

        throw "$failureMessage`n$details"
    }
}

function Enable-FirefoxEnterpriseRoots {
    $policyPath = 'HKLM:\SOFTWARE\Policies\Mozilla\Firefox\Certificates'
    New-Item -Path $policyPath -Force | Out-Null
    New-ItemProperty -Path $policyPath `
        -Name 'ImportEnterpriseRoots' `
        -PropertyType DWord `
        -Value 1 `
        -Force | Out-Null
}

$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$AppPath = $config.AppPath
$PublicUrl = if ($config.PublicUrl) { [string]$config.PublicUrl } else { [string]$config.ServerUrl }
$PublicUri = [System.Uri]$PublicUrl
$PublicHost = $PublicUri.Host
$DeploymentMode = if ($config.DeploymentMode) { [string]$config.DeploymentMode } else { 'Server' }
$RedisConnection = if ([string]::IsNullOrWhiteSpace($config.RedisConnection)) { '127.0.0.1:6379' } else { [string]$config.RedisConnection }
$SupportedHosts = if ($config.SupportedHosts) { @($config.SupportedHosts) } else { @('localhost', '127.0.0.1', $env:COMPUTERNAME, $PublicHost) + (Get-LocalIpv4Addresses) }
$SupportedHosts = $SupportedHosts |
    ForEach-Object { Normalize-HostValue $_ } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Sort-Object -Unique

$dnsHosts = $SupportedHosts |
    Where-Object {
        $parsedIp = $null
        -not [System.Net.IPAddress]::TryParse($_, [ref]$parsedIp)
    } |
    Select-Object -Unique

$ipHosts = $SupportedHosts |
    Where-Object {
        $parsedIp = $null
        [System.Net.IPAddress]::TryParse($_, [ref]$parsedIp)
    } |
    Select-Object -Unique

$subjectHost = $PublicHost
$parsedPublicIp = $null
if ([System.Net.IPAddress]::TryParse($PublicHost, [ref]$parsedPublicIp)) {
    $subjectHost = @($dnsHosts | Select-Object -First 1)[0]
}

if ([string]::IsNullOrWhiteSpace($subjectHost)) {
    $subjectHost = $env:COMPUTERNAME
}

Write-Host "[IIS] Configuring IIS site, app pool, and HTTPS..." -ForegroundColor Cyan
Write-Host "  Canonical URL : $PublicUrl" -ForegroundColor Gray
Write-Host "  DNS hosts     : $($dnsHosts -join ', ')" -ForegroundColor Gray
Write-Host "  IP hosts      : $($ipHosts -join ', ')" -ForegroundColor Gray
Write-Host "  Deployment    : $DeploymentMode" -ForegroundColor Gray

Import-Module WebAdministration

# Stop Default Web Site if it is holding port 80
$defaultSite = Get-WebSite -Name 'Default Web Site' -ErrorAction SilentlyContinue
if ($defaultSite -and $defaultSite.State -eq 'Started') {
    $on80 = $defaultSite.Bindings.Collection |
        Where-Object { $_.bindingInformation -like '*:80:' }
    if ($on80) {
        Write-Host "  Stopping Default Web Site (port 80 conflict)..." -ForegroundColor Gray
        Stop-WebSite -Name 'Default Web Site'
        Set-ItemProperty 'IIS:\Sites\Default Web Site' -Name serverAutoStart -Value $false
    }
}

# App pool
if (Test-Path 'IIS:\AppPools\VMSAppPool') {
    Write-Host "  App pool VMSAppPool already exists -- reconfiguring..." -ForegroundColor Gray
    try { Stop-WebAppPool -Name 'VMSAppPool' } catch {}
} else {
    New-WebAppPool -Name 'VMSAppPool' | Out-Null
    Write-Host "  Created app pool: VMSAppPool" -ForegroundColor Gray
}

Set-ItemProperty 'IIS:\AppPools\VMSAppPool' -Name managedRuntimeVersion          -Value ''
Set-ItemProperty 'IIS:\AppPools\VMSAppPool' -Name enable32BitAppOnWin64          -Value $false
Set-ItemProperty 'IIS:\AppPools\VMSAppPool' -Name processModel.identityType      -Value 'ApplicationPoolIdentity'
Set-ItemProperty 'IIS:\AppPools\VMSAppPool' -Name startMode                      -Value 'AlwaysRunning'
Set-ItemProperty 'IIS:\AppPools\VMSAppPool' -Name recycling.periodicRestart.time -Value '00:00:00'

# Website
if (Test-Path 'IIS:\Sites\VMS') {
    Write-Host "  Removing existing VMS site before reconfiguration..." -ForegroundColor Gray
    try { Stop-WebSite -Name 'VMS' } catch {}
    Remove-WebSite -Name 'VMS'
}

$websiteParams = @{
    Name = 'VMS'
    PhysicalPath = $AppPath
    ApplicationPool = 'VMSAppPool'
    Port = 80
}

New-Website @websiteParams | Out-Null
Write-Host "  Created site: VMS (HTTP binding on port 80)" -ForegroundColor Gray

# Set environment variables in web.config
$webConfigPath = Join-Path $AppPath 'web.config'
if (-not (Test-Path $webConfigPath)) {
    throw "web.config not found at $webConfigPath -- ensure the API was published correctly."
}

[xml]$webConfig = Get-Content $webConfigPath -Encoding UTF8
$aspNetCore = $webConfig.SelectSingleNode('//aspNetCore')
if (-not $aspNetCore) {
    throw "Could not find <aspNetCore> element in web.config"
}

$envVars = $aspNetCore.SelectSingleNode('environmentVariables')
if (-not $envVars) {
    $envVars = $webConfig.CreateElement('environmentVariables')
    $aspNetCore.AppendChild($envVars) | Out-Null
}

Set-EnvVar $webConfig $envVars 'ASPNETCORE_ENVIRONMENT' 'Production'
Set-EnvVar $webConfig $envVars 'REDIS_CONNECTION' $RedisConnection

$webConfig.Save($webConfigPath)
Write-Host "  Updated web.config: ASPNETCORE_ENVIRONMENT=Production, REDIS_CONNECTION=$RedisConnection" -ForegroundColor Gray

# Self-signed certificate
Write-Host "[SSL] Generating self-signed certificate..." -ForegroundColor Cyan

$sanEntries = @(
    ($dnsHosts | ForEach-Object { "DNS=$_" })
    ($ipHosts | ForEach-Object { "IPAddress=$_" })
)

Write-Host "  Certificate subject  : CN=$subjectHost" -ForegroundColor Gray
Write-Host "  Certificate SANs     : $($sanEntries -join ', ')" -ForegroundColor Gray

Get-ChildItem 'Cert:\LocalMachine\My' |
    Where-Object { $_.FriendlyName -eq 'VMS HTTPS Certificate' } |
    ForEach-Object { Remove-Item $_.PSPath -Force -ErrorAction SilentlyContinue }

$cert = New-SelfSignedCertificate `
    -Type 'Custom' `
    -Subject "CN=$subjectHost" `
    -TextExtension @(
        '2.5.29.37={text}1.3.6.1.5.5.7.3.1',
        "2.5.29.17={text}$($sanEntries -join '&')"
    ) `
    -CertStoreLocation 'Cert:\LocalMachine\My' `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -FriendlyName 'VMS HTTPS Certificate'

Write-Host "  Certificate created: $($cert.Thumbprint)" -ForegroundColor Gray

Get-ChildItem 'Cert:\LocalMachine\Root' |
    Where-Object { $_.FriendlyName -eq 'VMS HTTPS Certificate' } |
    ForEach-Object { Remove-Item $_.PSPath -Force -ErrorAction SilentlyContinue }

$tempCer = [System.IO.Path]::GetTempFileName() + '.cer'
Export-Certificate -Cert "Cert:\LocalMachine\My\$($cert.Thumbprint)" -FilePath $tempCer | Out-Null
Import-Certificate -FilePath $tempCer -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null
Remove-Item $tempCer -Force
Write-Host "  Certificate trusted on this machine (LocalMachine Root)" -ForegroundColor Gray

Enable-FirefoxEnterpriseRoots
Write-Host "  Firefox policy : ImportEnterpriseRoots=1 (restart Firefox if it was already open)" -ForegroundColor Gray

Set-JsonProperty -Object $config -Name 'SslThumbprint' -Value $cert.Thumbprint
$config | ConvertTo-Json -Depth 10 | Set-Content $ConfigFile -Encoding UTF8

# Save certificate for domain/manual client distribution outside the web root
$certOutputPath = Join-Path $AppPath '_vms_install\vms-cert.cer'
Export-Certificate -Cert "Cert:\LocalMachine\My\$($cert.Thumbprint)" -FilePath $certOutputPath | Out-Null
Write-Host "  Certificate exported: _vms_install\\vms-cert.cer" -ForegroundColor Gray

$clientScript = @"
#Requires -RunAsAdministrator
param(
    [string]`$CertPath = (Join-Path `$PSScriptRoot 'vms-cert.cer')
)

`$ErrorActionPreference = 'Stop'

if (-not (Test-Path `$CertPath)) {
    throw "Certificate file not found: `$CertPath"
}

Import-Certificate -FilePath `$CertPath -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null
Write-Host 'Certificate installed successfully.' -ForegroundColor Green
Write-Host 'Clients can now trust the VMS HTTPS certificate on this machine.' -ForegroundColor Green
"@

$installPs1Path = Join-Path $AppPath '_vms_install\install-cert.ps1'
$clientScript | Set-Content $installPs1Path -Encoding UTF8
Write-Host "  Helper written : _vms_install\\install-cert.ps1" -ForegroundColor Gray

Remove-Item (Join-Path $AppPath 'wwwroot\vms-cert.cer') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $AppPath 'wwwroot\install-cert.ps1') -Force -ErrorAction SilentlyContinue

# HTTPS bindings
$legacyHosts = @($PublicHost, 'localhost', $env:COMPUTERNAME) | Select-Object -Unique
foreach ($legacyHost in ($legacyHosts + $dnsHosts | Select-Object -Unique)) {
    Remove-HostnameSslBinding $legacyHost
}
netsh http delete sslcert ipport=0.0.0.0:443 | Out-Null
foreach ($ipHost in ($ipHosts + (Get-LocalIpv4Addresses) + '127.0.0.1' | Select-Object -Unique)) {
    Remove-IpSslBinding $ipHost
}

$existingHttpsBindings = Get-WebBinding -Name 'VMS' -Protocol 'https' -ErrorAction SilentlyContinue
foreach ($binding in $existingHttpsBindings) {
    $parts = $binding.bindingInformation.Split(':')
    Remove-WebBinding -Name 'VMS' `
        -Protocol 'https' `
        -Port ([int]$parts[1]) `
        -IPAddress $parts[0] `
        -HostHeader $binding.HostHeader `
        -ErrorAction SilentlyContinue
}

foreach ($httpsHost in $dnsHosts) {
    New-WebBinding -Name 'VMS' -Protocol 'https' -Port 443 -IPAddress '*' -HostHeader $httpsHost -SslFlags 1 | Out-Null
    Invoke-NetshOrThrow `
        -Arguments @(
            'http', 'add', 'sslcert',
            "hostnameport=$httpsHost`:443",
            "certhash=$($cert.Thumbprint)",
            "appid={C7A2D4F1-83BE-4E9A-B6CD-12345678ABCD}",
            'certstorename=MY'
        ) `
        -failureMessage "Failed to add HTTPS certificate binding for host '$httpsHost'."
    Write-Host "  Added HTTPS binding for host: $httpsHost" -ForegroundColor Gray
}

foreach ($httpsIp in $ipHosts) {
    New-WebBinding -Name 'VMS' -Protocol 'https' -Port 443 -IPAddress $httpsIp -SslFlags 0 | Out-Null
    Invoke-NetshOrThrow `
        -Arguments @(
            'http', 'add', 'sslcert',
            "ipport=$httpsIp`:443",
            "certhash=$($cert.Thumbprint)",
            "appid={C7A2D4F1-83BE-4E9A-B6CD-12345678ABCD}",
            'certstorename=MY'
        ) `
        -failureMessage "Failed to add HTTPS certificate binding for IP '$httpsIp'."
    Write-Host "  Added HTTPS binding for IP: $httpsIp" -ForegroundColor Gray
}

# Launch shortcut
$openVmsPath = Join-Path $AppPath '_vms_install\open-vms.bat'
"@echo off`r`nstart `"`" `"$PublicUrl`"" | Set-Content $openVmsPath -Encoding ASCII
Write-Host "  Written: open-vms.bat (opens $PublicUrl)" -ForegroundColor Gray

# File-system permissions
$identity = 'IIS AppPool\VMSAppPool'

try {
    $acl = Get-Acl $AppPath
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'
    )
    $acl.SetAccessRule($rule)
    Set-Acl $AppPath $acl
    Write-Host "  Permissions: IIS AppPool\\VMSAppPool has full control on app root" -ForegroundColor Gray
} catch {
    Write-Warning "Could not grant app root permissions to IIS AppPool\\VMSAppPool automatically: $($_.Exception.Message)"
    Write-Warning "Continuing installation. Verify that the IIS app pool identity can read the application files if the site does not start."
}

$settingsPath = Join-Path $AppPath 'appsettings.Production.json'
if (Test-Path $settingsPath) {
    try {
        $settingsAcl = Get-Acl $settingsPath
        $poolRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $identity, 'Read', 'Allow'
        )
        $settingsAcl.AddAccessRule($poolRule)
        Set-Acl $settingsPath $settingsAcl
        Write-Host "  Secured: appsettings.Production.json (app pool read access added)" -ForegroundColor Gray
    } catch {
        Write-Warning "Could not grant appsettings.Production.json read access to IIS AppPool\\VMSAppPool automatically: $($_.Exception.Message)"
        Write-Warning "Continuing installation. Verify file permissions manually if the application cannot read production settings."
    }
}

# Firewall rules
foreach ($fw in @(@{Name='VMS-HTTP';Port=80}, @{Name='VMS-HTTPS';Port=443})) {
    if (-not (Get-NetFirewallRule -DisplayName $fw.Name -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $fw.Name -Direction Inbound `
            -Protocol TCP -LocalPort $fw.Port -Action Allow | Out-Null
        Write-Host "  Firewall: opened port $($fw.Port) ($($fw.Name))" -ForegroundColor Gray
    } else {
        Write-Host "  Firewall: port $($fw.Port) already open" -ForegroundColor DarkGray
    }
}

Start-WebAppPool -Name 'VMSAppPool'
Start-WebSite -Name 'VMS'
Start-Process -FilePath 'iisreset.exe' -ArgumentList '/restart' -Wait -NoNewWindow

if ($DeploymentMode -eq 'Server') {
    Write-Host "  Note: remote Windows clients must trust _vms_install\\vms-cert.cer or use an enterprise/public CA certificate." -ForegroundColor Yellow
}

Write-Host "[IIS] Configuration complete. VMS accessible at $PublicUrl" -ForegroundColor Green
