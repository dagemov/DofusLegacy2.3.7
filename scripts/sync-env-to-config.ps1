[CmdletBinding()]
param(
    [string]$EnvPath,
    [string]$OutputDirectory,
    [ValidateSet("host", "docker")]
    [string]$DatabaseTarget = "host"
)

if (-not $EnvPath) {
    $EnvPath = Join-Path $PSScriptRoot "..\\.env"
}

if (-not $OutputDirectory) {
    if ($DatabaseTarget -eq "host") {
        $OutputDirectory = Join-Path $PSScriptRoot "..\\Sunshine net11.0\\Sunshine net11.0\\bin\\Debug\\net11.0"
    }
    else {
        $OutputDirectory = Join-Path $PSScriptRoot "..\\config\\generated"
    }
}

function Read-EnvFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Env file not found: $Path"
    }

    $values = @{}
    foreach ($rawLine in Get-Content $Path) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -lt 1) {
            continue
        }

        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        $values[$key] = $value
    }

    return $values
}

function Get-EnvValue {
    param(
        [hashtable]$Values,
        [string]$Key,
        [string]$DefaultValue
    )

    if ($Values.ContainsKey($Key) -and -not [string]::IsNullOrWhiteSpace($Values[$Key])) {
        return $Values[$Key]
    }

    return $DefaultValue
}

function Normalize-HostValue {
    param(
        [string]$Value,
        [string]$DefaultValue
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $DefaultValue
    }

    if ($Value.Equals("localhost", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "127.0.0.1"
    }

    return $Value
}

$envValues = Read-EnvFile -Path $EnvPath
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$configPath = Join-Path $OutputDirectory "Config.xml"
$databasePath = Join-Path $OutputDirectory "Database.xml"
$databaseHost = if ($DatabaseTarget -eq "docker") {
    Get-EnvValue -Values $envValues -Key "MYSQL_HOST" -DefaultValue "db"
}
else {
    Normalize-HostValue -Value (Get-EnvValue -Values $envValues -Key "MYSQL_PUBLISH_HOST" -DefaultValue "127.0.0.1") -DefaultValue "127.0.0.1"
}
$databasePort = if ($DatabaseTarget -eq "docker") {
    Get-EnvValue -Values $envValues -Key "MYSQL_PORT" -DefaultValue "3306"
}
else {
    Get-EnvValue -Values $envValues -Key "MYSQL_PUBLISH_PORT" -DefaultValue "3306"
}

@"
# Sunshine configuration generated from .env
AuthIp=0.0.0.0
AuthPort=$(Get-EnvValue -Values $envValues -Key "AUTH_PORT" -DefaultValue "2450")
WorldIp=0.0.0.0
WorldPort=$(Get-EnvValue -Values $envValues -Key "WORLD_PORT" -DefaultValue "5557")
ProtocolVersion=$(Get-EnvValue -Values $envValues -Key "PROTOCOL_VERSION" -DefaultValue "1375")

RateXp=$(Get-EnvValue -Values $envValues -Key "RATE_XP" -DefaultValue "3")
RateDrop=$(Get-EnvValue -Values $envValues -Key "RATE_DROP" -DefaultValue "1")
RateJobXp=$(Get-EnvValue -Values $envValues -Key "RATE_JOB_XP" -DefaultValue "5")
RateMountXp=$(Get-EnvValue -Values $envValues -Key "RATE_MOUNT_XP" -DefaultValue "1")
RateKamas=$(Get-EnvValue -Values $envValues -Key "RATE_KAMAS" -DefaultValue "2")

AutoSaveInterval=$(Get-EnvValue -Values $envValues -Key "AUTO_SAVE_INTERVAL" -DefaultValue "5")
"@ | Set-Content -Path $configPath -Encoding ASCII

@"
Database Sunshine

Database = $(Get-EnvValue -Values $envValues -Key "MYSQL_DATABASE" -DefaultValue "sunshine")
Hostname = $databaseHost
Port = $databasePort
Username = $(Get-EnvValue -Values $envValues -Key "MYSQL_APP_USER" -DefaultValue "sunshine")
Password = $(Get-EnvValue -Values $envValues -Key "MYSQL_APP_PASSWORD" -DefaultValue "change-me-app")
"@ | Set-Content -Path $databasePath -Encoding ASCII

Write-Host "Generated $configPath"
Write-Host "Generated $databasePath for $DatabaseTarget access."
