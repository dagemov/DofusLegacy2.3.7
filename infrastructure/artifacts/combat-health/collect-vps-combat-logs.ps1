#Requires -Version 5.1
<#
.SYNOPSIS
  Descarga logs JSONL de combate del VPS y opcionalmente ejecuta el analyzer.
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$VpsHost = $(if ($env:VPS_HOST) { $env:VPS_HOST } else { "174.138.35.107" }),
    [string]$SshUser = $(if ($env:SSH_USER) { $env:SSH_USER } else { "root" }),
    [Parameter(Mandatory = $true)]
    [string]$SshKey,
    [string]$ContainerName = "sunshine-server",
    [string]$ContainerLogDir = "/app/logs/combat",
    [string]$HostLogDir = "",
    [switch]$DryRun,
    [switch]$RunAnalyzer
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$destRoot = Join-Path $RepoRoot "Infrastructure\temporal-artifacts\combat-logs\vps\$timestamp"
$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)

Write-Host "=== Collect VPS Combat Logs ==="
Write-Host "Destino local: $destRoot"
Write-Host "DryRun: $($DryRun.IsPresent)"

if ($DryRun) {
    Write-Host "[DryRun] Intentaría docker cp ${ContainerName}:${ContainerLogDir}/. -> $destRoot"
    if (-not [string]::IsNullOrWhiteSpace($HostLogDir)) {
        Write-Host "[DryRun] Fallback scp desde host: $HostLogDir"
    }
    if ($RunAnalyzer) {
        Write-Host "[DryRun] Ejecutaría analyze-combat-telemetry.ps1 sobre $destRoot"
    }
    exit 0
}

New-Item -ItemType Directory -Force -Path $destRoot | Out-Null

$dockerCheck = & ssh @sshArgs "docker ps --format '{{.Names}}' | grep -x '$ContainerName' || true"
if ($dockerCheck -match $ContainerName) {
    $remoteHasLogs = & ssh @sshArgs "docker exec $ContainerName sh -c 'test -d $ContainerLogDir && ls -1 $ContainerLogDir/*.jsonl 2>/dev/null | head -1 || true'"
    if (-not [string]::IsNullOrWhiteSpace($remoteHasLogs)) {
        $remoteTmp = "/tmp/combat-collect-$timestamp"
        Write-Host "Descargando desde contenedor: $ContainerLogDir"
        & ssh @sshArgs "rm -rf '$remoteTmp' && docker cp ${ContainerName}:${ContainerLogDir} '$remoteTmp'"
        if ($LASTEXITCODE -ne 0) { throw "docker cp remoto falló." }
        & scp -r @sshArgs "${sshTarget}:${remoteTmp}/." $destRoot
        if ($LASTEXITCODE -ne 0) { throw "scp falló." }
        & ssh @sshArgs "rm -rf '$remoteTmp'" | Out-Null
    }
}

if ((Get-ChildItem -Path $destRoot -Recurse -File -ErrorAction SilentlyContinue).Count -eq 0 -and $HostLogDir) {
    Write-Host "Fallback host path: $HostLogDir"
    & scp -r @sshArgs "${sshTarget}:${HostLogDir}/*" $destRoot
}

$fileCount = (Get-ChildItem -Path $destRoot -Recurse -File -ErrorAction SilentlyContinue).Count
if ($fileCount -eq 0) {
    Write-Warning "No se descargaron archivos. ¿Telemetría activa y hubo combates?"
    exit 1
}

Write-Host "Descarga completa: $fileCount archivos en $destRoot"

if ($RunAnalyzer) {
    $analyzeScript = Join-Path $PSScriptRoot "analyze-combat-telemetry.ps1"
    & $analyzeScript -RepoRoot $RepoRoot -InputDirectory $destRoot
}
