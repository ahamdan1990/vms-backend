<#
.SYNOPSIS
    Generates or preserves deployment secrets, updates appsettings.Production.json,
    and writes the React runtime config.js for same-origin IIS deployment.
    Runs BEFORE database setup so the installer-provided SQL password is available to
    03-setup-sql.ps1.
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

function Ensure-JsonObject([psobject]$Object, [string]$Name) {
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $Object.$Name) {
        $child = [pscustomobject]@{}
        Set-JsonProperty $Object $Name $child
        return $child
    }

    return $Object.$Name
}

function New-RandomBase64([int]$bytes = 64) {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $buf = [byte[]]::new($bytes)
    $rng.GetBytes($buf)
    return [Convert]::ToBase64String($buf)
}

function New-RandomPassword {
    $chars  = 'abcdefghijklmnopqrstuvwxyz'
    $upper  = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
    $digits = '0123456789'
    $spec   = '!@#$%^&*'
    $all    = $chars + $upper + $digits + $spec
    $rng    = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $buf    = [byte[]]::new(20)
    $rng.GetBytes($buf)
    $pwd = ($buf | ForEach-Object { $all[$_ % $all.Length] }) -join ''
    $pwd = $upper[($buf[0] % 26)] + $digits[($buf[1] % 10)] + $spec[($buf[2] % 8)] + $pwd.Substring(3)
    return $pwd
}

function Get-LocalIpv4Addresses {
    try {
        return Get-NetIPAddress -AddressFamily IPv4 -ErrorAction Stop |
            Where-Object {
                $_.IPAddress -and
                $_.IPAddress -ne '127.0.0.1' -and
                $_.IPAddress -notlike '169.254.*'
            } |
            Select-Object -ExpandProperty IPAddress -Unique
    } catch {
        return @()
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

function Get-SupportedHosts([string]$primaryHost) {
    @(
        'localhost'
        '127.0.0.1'
        $env:COMPUTERNAME
        $primaryHost
        (Get-LocalIpv4Addresses)
    ) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { Normalize-HostValue $_ } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique
}

function Get-CanonicalPublicUrl([string]$rawUrl) {
    if ([string]::IsNullOrWhiteSpace($rawUrl)) {
        throw "ServerUrl is required."
    }

    $candidate = $rawUrl.Trim().TrimEnd('/')
    if ($candidate -notmatch '^[a-zA-Z][a-zA-Z0-9+\-.]*://') {
        $candidate = "https://$candidate"
    }

    $uri = [System.Uri]$candidate
    if (-not [string]::IsNullOrEmpty($uri.AbsolutePath) -and $uri.AbsolutePath -ne '/') {
        throw "Server URL must not contain a path. Use only the hostname or IP, for example https://localhost, https://server-name, or https://192.168.0.59."
    }

    if ($uri.Port -ne 80 -and $uri.Port -ne 443 -and $uri.IsDefaultPort -eq $false) {
        throw "Server URL must not use a custom port. The IIS installer configures ports 80 and 443 only."
    }

    return "https://$($uri.Host)"
}

function Get-ConnectionStringPassword([string]$connectionString) {
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        return $null
    }

    try {
        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
        if (-not [string]::IsNullOrWhiteSpace($builder.Password)) {
            return $builder.Password
        }
    } catch {
        # Fall back to a simple parser for legacy strings.
    }

    $segments = $connectionString -split ';'
    foreach ($segment in $segments) {
        $parts = $segment -split '=', 2
        if ($parts.Count -ne 2) {
            continue
        }

        $key = $parts[0].Trim()
        if ($key -ieq 'Password' -or $key -ieq 'Pwd') {
            return $parts[1].Trim()
        }
    }

    return $null
}

function New-SqlConnectionString([string]$serverName, [string]$databaseName, [string]$userName, [string]$password) {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source'] = $serverName
    $builder['Initial Catalog'] = $databaseName
    $builder['User ID'] = $userName
    $builder['Password'] = $password
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = $true
    $builder['Connect Timeout'] = 30
    return $builder.ConnectionString
}

function Write-RuntimeConfig([string]$AppPath, [string]$PublicUrl) {
    $configJs = @"
window.VMS_CONFIG = {
  apiUrl: '',
  canonicalUrl: '$PublicUrl'
};
"@

    $configJsPath = Join-Path $AppPath 'wwwroot\config.js'
    $configJs | Set-Content $configJsPath -Encoding UTF8
    Write-Host "  Written: wwwroot\config.js (same-origin API)" -ForegroundColor Gray
}

$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$AppPath = $config.AppPath
$ServerUrl = $config.ServerUrl
$RedisConnection = if ([string]::IsNullOrWhiteSpace($config.RedisConnection)) { '127.0.0.1:6379' } else { [string]$config.RedisConnection }
$SmtpHost = $config.SmtpHost
$SmtpPort = if ([string]::IsNullOrWhiteSpace([string]$config.SmtpPort)) { 587 } else { [int]$config.SmtpPort }
$SmtpUser = $config.SmtpUsername
$SmtpPass = $config.SmtpPassword
$SmtpFrom = $config.SmtpFromEmail
$requestedSqlPassword = if (
    $config.PSObject.Properties['SqlPassword'] -and
    -not [string]::IsNullOrWhiteSpace([string]$config.SqlPassword)
) {
    [string]$config.SqlPassword
} else {
    $null
}

$PublicUrl = Get-CanonicalPublicUrl $ServerUrl
$PublicUri = [System.Uri]$PublicUrl
$PublicHost = $PublicUri.Host
$DeploymentMode = if ($config.DeploymentMode) { [string]$config.DeploymentMode } elseif ($PublicHost -ieq 'localhost' -or $PublicHost -eq '127.0.0.1') { 'Standalone' } else { 'Server' }
$SupportedHosts = Get-SupportedHosts $PublicHost

Write-Host "[Config] Generating deployment configuration..." -ForegroundColor Cyan
Write-Host "  Canonical URL : $PublicUrl" -ForegroundColor Gray
Write-Host "  Deployment    : $DeploymentMode" -ForegroundColor Gray
Write-Host "  Supported hosts: $($SupportedHosts -join ', ')" -ForegroundColor Gray

$settingsPath = Join-Path $AppPath 'appsettings.Production.json'
if (-not (Test-Path $settingsPath)) {
    throw "appsettings.Production.json not found at: $settingsPath"
}

$settingsRaw = Get-Content $settingsPath -Raw
$isUpgrade = $settingsRaw -notmatch '#\{[A-Z_]+\}#'
$settings = $settingsRaw | ConvertFrom-Json

$existingSqlPassword = if ($isUpgrade) {
    Get-ConnectionStringPassword $settings.ConnectionStrings.DefaultConnection
} else {
    $null
}

$sqlPassword = if ($requestedSqlPassword) {
    $requestedSqlPassword
} elseif ($existingSqlPassword) {
    $existingSqlPassword
} else {
    New-RandomPassword
}
$jwtSecret = if ($isUpgrade) { $settings.JWT.SecretKey } else { New-RandomBase64 64 }
$dpKey = if ($isUpgrade) { $settings.Security.EncryptionKeys.DataProtectionKey } else { New-RandomBase64 32 }
$dbEncKey = if ($isUpgrade) { $settings.Security.EncryptionKeys.DatabaseEncryptionKey } else { New-RandomBase64 32 }

Set-JsonProperty $config 'PublicUrl' $PublicUrl
Set-JsonProperty $config 'PublicHost' $PublicHost
Set-JsonProperty $config 'DeploymentMode' $DeploymentMode
Set-JsonProperty $config 'SupportedHosts' $SupportedHosts
Set-JsonProperty $config 'SqlPassword' $sqlPassword
Set-JsonProperty $config 'JwtSecret' $jwtSecret
Set-JsonProperty $config 'DpKey' $dpKey
Set-JsonProperty $config 'DbEncKey' $dbEncKey
$config | ConvertTo-Json -Depth 10 | Set-Content $ConfigFile -Encoding UTF8

$sqlServer = $config.SqlServer
$connStr = New-SqlConnectionString -serverName $sqlServer -databaseName 'VMS' -userName 'vms_app' -password $sqlPassword
$httpOrigin = "http://$PublicHost"
$httpsOrigin = "https://$PublicHost"

$connectionStrings = Ensure-JsonObject $settings 'ConnectionStrings'
$jwtSettings = Ensure-JsonObject $settings 'JWT'
$securitySettings = Ensure-JsonObject $settings 'Security'
$encryptionKeys = Ensure-JsonObject $securitySettings 'EncryptionKeys'
$emailSettings = Ensure-JsonObject $settings 'Email'
$corsSettings = Ensure-JsonObject $settings 'Cors'
$supportedOrigins = foreach ($supportedHost in $SupportedHosts) {
    "http://$supportedHost"
    "https://$supportedHost"
}

if (
    ($null -eq $emailSettings.PSObject.Properties['FromEmail'] -or [string]::IsNullOrWhiteSpace([string]$emailSettings.FromEmail)) -and
    $null -ne $emailSettings.PSObject.Properties['FromAddress'] -and
    -not [string]::IsNullOrWhiteSpace([string]$emailSettings.FromAddress)
) {
    Set-JsonProperty $emailSettings 'FromEmail' ([string]$emailSettings.FromAddress)
}

Set-JsonProperty $settings 'BaseUrl' $PublicUrl
Set-JsonProperty $connectionStrings 'DefaultConnection' $connStr
Set-JsonProperty $connectionStrings 'Redis' $RedisConnection
Set-JsonProperty $corsSettings 'AllowedOrigins' @($supportedOrigins | Select-Object -Unique)
Set-JsonProperty $securitySettings 'EnforceCanonicalHost' $false

if (-not $isUpgrade) {
    Set-JsonProperty $jwtSettings 'SecretKey' $jwtSecret
    Set-JsonProperty $encryptionKeys 'DataProtectionKey' $dpKey
    Set-JsonProperty $encryptionKeys 'DatabaseEncryptionKey' $dbEncKey
    Set-JsonProperty $emailSettings 'SmtpHost' $SmtpHost
    Set-JsonProperty $emailSettings 'SmtpPort' $SmtpPort
    Set-JsonProperty $emailSettings 'Username' $SmtpUser
    Set-JsonProperty $emailSettings 'Password' $SmtpPass
    Set-JsonProperty $emailSettings 'FromEmail' $SmtpFrom
    Set-JsonProperty $emailSettings 'EnableSending' (-not [string]::IsNullOrWhiteSpace([string]$SmtpHost))
}

$settings | ConvertTo-Json -Depth 20 | Set-Content $settingsPath -Encoding UTF8
Write-Host "  Written: appsettings.Production.json" -ForegroundColor Gray

Write-RuntimeConfig -AppPath $AppPath -PublicUrl $PublicUrl

$dirs = @('logs', 'uploads', 'Templates\Email', 'Templates\Reports', 'Templates\Import')
foreach ($d in $dirs) {
    $full = Join-Path $AppPath $d
    New-Item -ItemType Directory -Force -Path $full | Out-Null
}

try {
    $settingsAcl = Get-Acl $settingsPath
    $settingsAcl.SetAccessRuleProtection($true, $false)
    $adminRule  = New-Object System.Security.AccessControl.FileSystemAccessRule('BUILTIN\Administrators', 'FullControl', 'Allow')
    $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule('SYSTEM', 'FullControl', 'Allow')
    $settingsAcl.AddAccessRule($adminRule)
    $settingsAcl.AddAccessRule($systemRule)
    Set-Acl $settingsPath $settingsAcl
    Write-Host "  Secured: appsettings.Production.json (Administrators + SYSTEM)" -ForegroundColor Gray
} catch {
    Write-Warning "Could not harden appsettings.Production.json ACLs automatically: $($_.Exception.Message)"
    Write-Warning "Continuing installation. Verify file permissions manually if needed."
}

if ($isUpgrade) {
    Write-Host "[Config] Upgrade detected: preserved existing secrets and refreshed canonical URL, SQL connection, CORS, and runtime config." -ForegroundColor Yellow
} else {
    Write-Host "[Config] Fresh installation configuration completed." -ForegroundColor Green
}
