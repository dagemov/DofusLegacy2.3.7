#Requires -Version 5.1
<#
.SYNOPSIS
  Gate deploy: backup VPS, sync Sunshine con telemetría, rebuild sunshine-server, enable telemetry.
.NOTES
  Usa scripts oficiales del repo. No ejecuta docker compose down -v.
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$SshKey = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path "SSH\private_key_sebas.pem"),
    [switch]$SkipBackup,
    [switch]$SkipDeploy,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

Write-Host "=== Deploy Gate — VPS Combat Telemetry ==="
Write-Host "Rama local: $(git -C $RepoRoot branch --show-current)"
Write-Host "DryRun: $($DryRun.IsPresent)"

if (-not $SkipBackup) {
    Write-Host "`n[1/4] Backup VPS inventory..."
    if ($DryRun) {
        Write-Host "[DryRun] CONFIRM_BACKUP=1 backup-vps-state.ps1"
    }
    else {
        $env:CONFIRM_BACKUP = "1"
        & (Join-Path $RepoRoot "infrastructure\scripts\PublicationBackup\backup-vps-state.ps1") -SshKey $SshKey
        & (Join-Path $RepoRoot "scripts\vps\backup-before-restart.ps1") -SshKey $SshKey -ConfirmBackup
    }
}

if (-not $SkipDeploy) {
    Write-Host "`n[2/4] Deploy (sync + docker compose build)..."
    if ($DryRun) {
        Write-Host "[DryRun] scripts\deploy-vps.ps1 -SshKey $SshKey"
    }
    else {
        & (Join-Path $RepoRoot "scripts\deploy-vps.ps1") -SshKey $SshKey -SkipSync -StackOnly -SunshineOnly
    }
}

Write-Host "`n[3/4] Enable telemetry..."
if ($DryRun) {
    & (Join-Path $PSScriptRoot "enable-vps-combat-telemetry.ps1") -SshKey $SshKey -DryRun
}
else {
    $env:CONFIRM_RESTART = "1"
    & (Join-Path $PSScriptRoot "enable-vps-combat-telemetry.ps1") -SshKey $SshKey
}

Write-Host "`n[4/4] Validación SSH..."
$validateCmd = "docker exec sunshine-server printenv | grep -E 'FIGHT_TELEMETRY|COMBAT_TELEMETRY' || true; docker exec sunshine-server ls -lah /app/logs/combat 2>/dev/null || true"
if ($DryRun) {
    Write-Host "[DryRun] ssh validate: $validateCmd"
}
else {
    & ssh -i $SshKey -o BatchMode=yes -o StrictHostKeyChecking=accept-new "root@174.138.35.107" $validateCmd
}

Write-Host "`nPost-deploy: 1 combate smoke → collect-vps-combat-logs.ps1 -RunAnalyzer"
