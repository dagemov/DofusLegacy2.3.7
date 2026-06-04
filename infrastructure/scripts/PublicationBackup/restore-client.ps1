#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupId,
    [string]$RepoRoot = "",
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

$backupDir = Join-Path $RepoRoot "backups\client\$BackupId"
$manifestPath = Join-Path $backupDir "manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Backup no encontrado: $backupDir"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$restoreTarget = Join-Path $RepoRoot "Infrastructure\staging-client\client-restore-sandbox"
$confirm = $env:CONFIRM_RESTORE -eq "1"
$dryRun = -not $Execute

Write-Host "Restore client (sandbox only - NO Client2.3.7 real)"
Write-Host "  Backup: $backupDir"
Write-Host "  Target sandbox: $restoreTarget"
Write-Host "  Mode: $(if ($dryRun) { 'dry-run' } else { 'execute' })"
Write-Host "  CONFIRM_RESTORE: $confirm"

foreach ($file in $manifest.files) {
    $source = Join-Path $backupDir ($file.relativePath -replace "/", "\")
    $dest = Join-Path $restoreTarget ($file.relativePath -replace "/", "\")
    Write-Host "  $($file.relativePath) -> $dest"
}

if ($dryRun) {
    Write-Host "Dry-run completo. Para ejecutar: -Execute y `$env:CONFIRM_RESTORE='1'"
    exit 0
}

if (-not $confirm) {
    throw "Execute requiere CONFIRM_RESTORE=1. No se restauró nada."
}

foreach ($file in $manifest.files) {
    $source = Join-Path $backupDir ($file.relativePath -replace "/", "\")
    $dest = Join-Path $restoreTarget ($file.relativePath -replace "/", "\")
    $destParent = Split-Path $dest -Parent
    New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $dest -Force
}

Write-Host "Restore sandbox OK: $restoreTarget"
