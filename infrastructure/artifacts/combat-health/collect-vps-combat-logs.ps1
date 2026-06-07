#Requires -Version 5.1
<#
.SYNOPSIS
  Descarga logs de combate del VPS a temporal-artifacts (gitignored).
.NOTES
  Solo usar tras activar telemetría en VPS beta con aprobación explícita.
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$VpsHost = $(if ($env:VPS_HOST) { $env:VPS_HOST } else { "174.138.35.107" }),
    [string]$SshUser = $(if ($env:SSH_USER) { $env:SSH_USER } else { "root" }),
    [Parameter(Mandatory = $true)]
    [string]$SshKey,
    [string]$RemoteLogDir = "/opt/dofus-2.0.0/Infrastructure/logs/combat"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$destRoot = Join-Path $RepoRoot "Infrastructure\temporal-artifacts\combat-logs\vps\$timestamp"
New-Item -ItemType Directory -Force -Path $destRoot | Out-Null

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "StrictHostKeyChecking=accept-new")

Write-Host "Comprobando directorio remoto: $RemoteLogDir"
$check = & ssh @sshArgs $sshTarget "test -d '$RemoteLogDir' && echo OK || echo MISSING"
if ($check -notmatch "OK") {
    Write-Warning "Directorio remoto no encontrado. Ajusta -RemoteLogDir o activa telemetría en VPS."
    exit 1
}

Write-Host "Descargando logs → $destRoot"
& scp -r @sshArgs "${sshTarget}:${RemoteLogDir}/*" $destRoot
if ($LASTEXITCODE -ne 0) { throw "scp falló." }

$fileCount = (Get-ChildItem -Path $destRoot -Recurse -File).Count
Write-Host "Descarga completa: $fileCount archivos en $destRoot"
