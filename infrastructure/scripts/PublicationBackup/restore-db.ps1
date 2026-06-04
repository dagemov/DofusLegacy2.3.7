#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupId,
    [string]$RepoRoot = "",
    [string]$DbContainerName = "sunshine-db",
    [string]$DatabaseName = "sunshine",
    [switch]$Execute
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string]$Start)
    $directory = Get-Item -LiteralPath $Start
    while ($null -ne $directory) {
        if ((Test-Path (Join-Path $directory.FullName "Angular-tools\Admin")) -and (Test-Path (Join-Path $directory.FullName "docs"))) {
            return $directory.FullName
        }
        $directory = $directory.Parent
    }
    throw "No se pudo resolver la raíz del repo."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Resolve-RepoRoot (Join-Path $scriptDir "..\..\..")
}

$backupDir = Join-Path $RepoRoot "backups\db\$BackupId"
$dumpPath = Join-Path $backupDir "sunshine.sql"
if (-not (Test-Path $dumpPath)) {
    throw "Dump no encontrado: $dumpPath"
}

$confirm = $env:CONFIRM_RESTORE -eq "1"
$dryRun = -not $Execute

Write-Host "Restore DB (solo contenedor local $DbContainerName)"
Write-Host "  Backup: $backupDir"
Write-Host "  Dump: $dumpPath"
Write-Host "  Mode: $(if ($dryRun) { 'dry-run' } else { 'execute' })"
Write-Host "  CONFIRM_RESTORE: $confirm"
Write-Host "  Comando planificado: docker exec -i $DbContainerName mysql -uroot -p*** $DatabaseName < sunshine.sql"

if ($dryRun) {
    Write-Host "Dry-run completo. Para ejecutar: -Execute y CONFIRM_RESTORE=1"
    exit 0
}

if (-not $confirm) {
    throw "Execute requiere CONFIRM_RESTORE=1."
}

$running = docker inspect -f "{{.State.Running}}" $DbContainerName 2>$null
if ($running -ne "true") {
    throw "Contenedor $DbContainerName no está en ejecución."
}

$rootPassword = docker exec $DbContainerName printenv MYSQL_ROOT_PASSWORD
Get-Content $dumpPath -Raw | docker exec -i $DbContainerName mysql -uroot -p"$rootPassword" $DatabaseName
Write-Host "Restore DB local OK (sandbox/docker dev)."
