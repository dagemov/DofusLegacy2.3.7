#Requires -Version 5.1
<#
.SYNOPSIS
  Activa telemetría de combate en VPS (sunshine-server) bajo demanda.
.NOTES
  - Usa -DryRun para auditar sin cambios.
  - Restart real requiere CONFIRM_RESTART=1.
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$VpsHost = $(if ($env:VPS_HOST) { $env:VPS_HOST } else { "174.138.35.107" }),
    [string]$SshUser = $(if ($env:SSH_USER) { $env:SSH_USER } else { "root" }),
    [Parameter(Mandatory = $true)]
    [string]$SshKey,
    [string]$RemoteRoot = "/opt/dofus-2.0.0",
    [string]$ContainerName = "sunshine-server",
    [string]$RemoteLogDir = "/app/logs/combat",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)
$envFile = "$RemoteRoot/.env"

Write-Host "=== Enable VPS Combat Telemetry ==="
Write-Host "Host: $VpsHost | Container: $ContainerName | Log: $RemoteLogDir | DryRun: $($DryRun.IsPresent)"

$lines = @(
    "FIGHT_TELEMETRY_ENABLED=true",
    "FIGHT_TELEMETRY_LOG_DIRECTORY=$RemoteLogDir",
    "COMBAT_TELEMETRY_WRITE_TURN_FLOW=true",
    "COMBAT_TELEMETRY_WRITE_SPELL_CASTS=true"
)

if ($DryRun) {
    Write-Host "[DryRun] Variables a escribir en $envFile :"
    $lines | ForEach-Object { Write-Host "  $_" }
    Write-Host "[DryRun] mkdir -p $RemoteLogDir dentro de $ContainerName"
    Write-Host "[DryRun] Restart requiere CONFIRM_RESTART=1"
    exit 0
}

foreach ($line in $lines) {
    $key = $line.Split('=')[0]
    $value = $line.Substring($key.Length + 1)
    $cmd = "ENV_FILE='$envFile'; touch `"`$ENV_FILE`"; if grep -q '^${key}=' `"`$ENV_FILE`"; then sed -i 's|^${key}=.*|${key}=${value}|' `"`$ENV_FILE`"; else echo '${key}=${value}' >> `"`$ENV_FILE`"; fi"
    & ssh @sshArgs $cmd
    if ($LASTEXITCODE -ne 0) { throw "Falló al escribir ${key} en VPS." }
}

& ssh @sshArgs "docker exec $ContainerName mkdir -p $RemoteLogDir $RemoteLogDir/spell-casts"
& ssh @sshArgs "grep -E '^(FIGHT_TELEMETRY_|COMBAT_TELEMETRY_)' '$envFile' || true"

if ($env:CONFIRM_RESTART -ne '1') {
    Write-Warning "Variables actualizadas. Define CONFIRM_RESTART=1 y re-ejecuta, o usa restart-world-safe.ps1 -ConfirmRestart"
    exit 0
}

$restartScript = Join-Path $RepoRoot "scripts\vps\restart-world-safe.ps1"
& $restartScript -SshKey $SshKey -VpsHost $VpsHost -SshUser $SshUser -WorldNameHint $ContainerName -ConfirmRestart
Write-Host "Telemetría activada en VPS."
