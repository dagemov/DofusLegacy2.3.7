#Requires -Version 5.1
<#
.SYNOPSIS
  Descarga un dump enfocado de sunshine desde VPS y lo guarda en db-snapshots/ (gitignored).
.NOTES
  No commitea dumps. Restauración local es manual o con herramienta preferida del operador.
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$VpsHost = $(if ($env:VPS_HOST) { $env:VPS_HOST } else { "174.138.35.107" }),
    [string]$SshUser = $(if ($env:SSH_USER) { $env:SSH_USER } else { "root" }),
    [Parameter(Mandatory = $true)]
    [string]$SshKey,
    [string]$RemoteBackupDir = "/root/backups/sunshine-focused",
    [string]$DbContainer = "sunshine-db",
    [string]$DbName = "sunshine",
    [string]$LocalDbName = "sunshine_lab",
    [switch]$DownloadOnly
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

$snapshotDir = Join-Path $PSScriptRoot "db-snapshots"
New-Item -ItemType Directory -Force -Path $snapshotDir | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$localDump = Join-Path $snapshotDir "sunshine-lab-$timestamp.sql"
$remoteDump = "/tmp/sunshine-lab-$timestamp.sql"

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "StrictHostKeyChecking=accept-new")

Write-Host "Creando dump remoto en VPS..."
$dumpCmd = @"
docker exec $DbContainer mysqldump -u root --single-transaction --routines --triggers $DbName > $remoteDump 2>/dev/null || mysqldump -u root --single-transaction $DbName > $remoteDump
"@
& ssh @sshArgs $sshTarget $dumpCmd
if ($LASTEXITCODE -ne 0) { throw "mysqldump remoto falló." }

Write-Host "Descargando dump → $localDump"
& scp @sshArgs "${sshTarget}:$remoteDump" $localDump
if ($LASTEXITCODE -ne 0) { throw "scp falló." }

& ssh @sshArgs $sshTarget "rm -f $remoteDump" | Out-Null

Write-Host "Dump guardado: $localDump"
Write-Host ""
Write-Host "Restauración local sugerida (MariaDB/MySQL local):"
Write-Host "  CREATE DATABASE IF NOT EXISTS $LocalDbName CHARACTER SET utf8mb4;"
Write-Host "  mysql -u root -p $LocalDbName < `"$localDump`""
Write-Host ""
Write-Host "Luego apunta appsettings.Development.local.json a $LocalDbName en 127.0.0.1."

if ($DownloadOnly) { exit 0 }

Write-Host ""
Write-Host "NOTA: restauración automática no incluida — evita sobrescribir DB local sin confirmación."
