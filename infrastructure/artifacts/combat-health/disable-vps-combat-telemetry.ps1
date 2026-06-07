#Requires -Version 5.1
<#
.SYNOPSIS
  Desactiva telemetría de combate en VPS. No borra logs.
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$VpsHost = $(if ($env:VPS_HOST) { $env:VPS_HOST } else { "174.138.35.107" }),
    [string]$SshUser = $(if ($env:SSH_USER) { $env:SSH_USER } else { "root" }),
    [Parameter(Mandatory = $true)]
    [string]$SshKey,
    [string]$RemoteRoot = "/opt/dofus-2.0.0",
    [string]$ContainerName = "sunshine-server",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)
$envFile = "$RemoteRoot/.env"

Write-Host "=== Disable VPS Combat Telemetry ==="
Write-Host "DryRun: $($DryRun.IsPresent)"

if ($DryRun) {
    Write-Host "[DryRun] FIGHT_TELEMETRY_ENABLED=false en $envFile"
    if ($env:CONFIRM_RESTART -ne '1') {
        Write-Host "[DryRun] Restart omitido (CONFIRM_RESTART=1 requerido)."
    }
    exit 0
}

$cmd = "ENV_FILE='$envFile'; touch `"`$ENV_FILE`"; if grep -q '^FIGHT_TELEMETRY_ENABLED=' `"`$ENV_FILE`"; then sed -i 's|^FIGHT_TELEMETRY_ENABLED=.*|FIGHT_TELEMETRY_ENABLED=false|' `"`$ENV_FILE`"; else echo 'FIGHT_TELEMETRY_ENABLED=false' >> `"`$ENV_FILE`"; fi; grep -E '^(FIGHT_TELEMETRY_|COMBAT_TELEMETRY_)' `"`$ENV_FILE`" || true"
& ssh @sshArgs $cmd
if ($LASTEXITCODE -ne 0) { throw "Falló disable remoto." }

if ($env:CONFIRM_RESTART -ne '1') {
    Write-Warning "Telemetría desactivada en .env. Reinicia world con CONFIRM_RESTART=1."
    exit 0
}

$restartScript = Join-Path $RepoRoot "scripts\vps\restart-world-safe.ps1"
& $restartScript -SshKey $SshKey -VpsHost $VpsHost -SshUser $SshUser -WorldNameHint $ContainerName -ConfirmRestart
Write-Host "Telemetría desactivada. Descarga logs antes de limpiar el VPS."
