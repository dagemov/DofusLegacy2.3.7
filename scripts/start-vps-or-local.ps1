[CmdletBinding()]
param(
    [string]$VpsHost = "174.138.35.107",
    [switch]$Build,
    [switch]$StartTunnel,
    [switch]$ForceLocal,
    [switch]$BootstrapFromRollback,
    [string]$EnvPath,
    [string]$FallbackConfigPath,
    [string]$RollbackDumpPath
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $EnvPath) {
    $EnvPath = Join-Path $repoRoot ".env"
}

if (-not $FallbackConfigPath) {
    $FallbackConfigPath = Join-Path $env:USERPROFILE "Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0\Config.xml"
}

if (-not $RollbackDumpPath) {
    $RollbackDumpPath = Join-Path $env:USERPROFILE "Downloads\RollBackShushine\sunshine.sql"
}

function Read-KeyValueFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $values = @{}
    foreach ($line in Get-Content -Path $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed -split "=", 2
        if ($parts.Count -ne 2) {
            continue
        }

        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        if ($key) {
            $values[$key] = $value
        }
    }

    return $values
}

function Test-TcpPort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetHost,
        [Parameter(Mandatory = $true)]
        [int]$Port,
        [int]$TimeoutMs = 2000
    )

    try {
        $client = [System.Net.Sockets.TcpClient]::new()
        $asyncResult = $client.BeginConnect($TargetHost, $Port, $null, $null)
        $connected = $asyncResult.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        if (-not $connected -or -not $client.Connected) {
            return $false
        }

        $client.EndConnect($asyncResult) | Out-Null
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($client) {
            $client.Dispose()
        }
    }
}

function Resolve-ServerSettings {
    if (Test-Path $EnvPath) {
        $envValues = Read-KeyValueFile -Path $EnvPath
        return @{
            Source = ".env"
            AuthPort = [int]$envValues["AUTH_PORT"]
            WorldPort = [int]$envValues["WORLD_PORT"]
            ProtocolVersion = [int]$envValues["PROTOCOL_VERSION"]
            PublicHost = $envValues["WORLD_PUBLIC_HOST"]
        }
    }

    if (Test-Path $FallbackConfigPath) {
        $configValues = Read-KeyValueFile -Path $FallbackConfigPath
        return @{
            Source = "rollback-config"
            AuthPort = [int]$configValues["AuthPort"]
            WorldPort = [int]$configValues["WorldPort"]
            ProtocolVersion = [int]$configValues["ProtocolVersion"]
            PublicHost = $configValues["WorldIp"]
        }
    }

    throw "Unable to resolve server settings from .env or fallback config."
}

function Initialize-EnvFromRollback {
    if (Test-Path $EnvPath) {
        return
    }

    $examplePath = Join-Path $repoRoot ".env.example"
    if (-not (Test-Path $examplePath)) {
        throw "Missing $examplePath."
    }

    if (-not (Test-Path $FallbackConfigPath)) {
        throw "Missing fallback config at $FallbackConfigPath."
    }

    $configValues = Read-KeyValueFile -Path $FallbackConfigPath
    $lines = Get-Content -Path $examplePath
    $rewritten = foreach ($line in $lines) {
        if ($line -match "^WORLD_PUBLIC_HOST=") {
            "WORLD_PUBLIC_HOST=127.0.0.1"
        }
        elseif ($line -match "^AUTH_PORT=") {
            "AUTH_PORT=$($configValues["AuthPort"])"
        }
        elseif ($line -match "^WORLD_PORT=") {
            "WORLD_PORT=$($configValues["WorldPort"])"
        }
        elseif ($line -match "^PROTOCOL_VERSION=") {
            "PROTOCOL_VERSION=$($configValues["ProtocolVersion"])"
        }
        elseif ($line -match "^AUTH_PUBLISH_HOST=") {
            "AUTH_PUBLISH_HOST=127.0.0.1"
        }
        elseif ($line -match "^WORLD_PUBLISH_HOST=") {
            "WORLD_PUBLISH_HOST=127.0.0.1"
        }
        else {
            $line
        }
    }

    $rewritten | Set-Content -Path $EnvPath -Encoding UTF8
}

function Copy-RollbackDump {
    $targetDump = Join-Path $repoRoot "database\sunshine.sql"
    if (-not (Test-Path $RollbackDumpPath)) {
        throw "Missing rollback dump at $RollbackDumpPath."
    }

    Copy-Item -Path $RollbackDumpPath -Destination $targetDump -Force
}

function Reset-LocalComposeState {
    $composeDir = Join-Path $repoRoot "docker"
    Push-Location $composeDir
    try {
        & docker compose --env-file $EnvPath -f docker-compose.yml -f docker-compose.local.yml down -v
    }
    finally {
        Pop-Location
    }
}

$settings = Resolve-ServerSettings
$authOpen = Test-TcpPort -TargetHost $VpsHost -Port $settings.AuthPort
$worldOpen = Test-TcpPort -TargetHost $VpsHost -Port $settings.WorldPort

Write-Output "Probe source: $($settings.Source)"
Write-Output "VPS host: $VpsHost"
Write-Output "Auth port $($settings.AuthPort): $(if ($authOpen) { 'OPEN' } else { 'CLOSED' })"
Write-Output "World port $($settings.WorldPort): $(if ($worldOpen) { 'OPEN' } else { 'CLOSED' })"

if (-not $ForceLocal -and $authOpen -and $worldOpen) {
    Write-Output "The VPS is already serving auth and world traffic. Local Docker start skipped."

    if ($StartTunnel) {
        & (Join-Path $PSScriptRoot "vps-tunnel.ps1") -Mode start -VpsHost $VpsHost -AuthPort $settings.AuthPort -WorldPort $settings.WorldPort
    }

    return
}

Write-Output "The VPS is not fully reachable for gameplay. Falling back to the local Docker stack."
& (Join-Path $PSScriptRoot "vps-tunnel.ps1") -Mode stop | Out-Null

if ($BootstrapFromRollback) {
    Initialize-EnvFromRollback
    Copy-RollbackDump
    Reset-LocalComposeState
}

if (-not (Test-Path $EnvPath)) {
    throw "Missing $EnvPath. Run again with -BootstrapFromRollback or create .env first."
}

$setupScript = Join-Path $PSScriptRoot "setup.ps1"
if ($Build) {
    & $setupScript -Mode local -Build
}
else {
    & $setupScript -Mode local
}

if ($LASTEXITCODE -ne 0) {
    throw "Local Docker startup failed."
}
