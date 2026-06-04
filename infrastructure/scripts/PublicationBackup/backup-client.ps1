#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$ClientRoot = "",
    [string]$RepoRoot = "",
    [switch]$SkipLaneUpdate
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string]$Start)
    $directory = Get-Item -LiteralPath $Start
    while ($null -ne $directory) {
        $admin = Join-Path $directory.FullName "Angular-tools\Admin"
        $docs = Join-Path $directory.FullName "docs"
        if ((Test-Path $admin) -and (Test-Path $docs)) {
            return $directory.FullName
        }
        $directory = $directory.Parent
    }
    throw "No se pudo resolver la raíz del repo."
}

function Get-Sha256Hex {
    param([string]$Path)
    $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash([IO.File]::ReadAllBytes($Path))
    return ([BitConverter]::ToString($hash) -replace "-", "").ToLowerInvariant()
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Resolve-RepoRoot (Join-Path $scriptDir "..\..\..")
}

if ([string]::IsNullOrWhiteSpace($ClientRoot)) {
    $ClientRoot = Join-Path $RepoRoot "Client2.3.7"
}

$files = @(
    @{ Relative = "data/common/Items.d2o"; Source = Join-Path $ClientRoot "data\common\Items.d2o" },
    @{ Relative = "data/common/ItemSets.d2o"; Source = Join-Path $ClientRoot "data\common\ItemSets.d2o" },
    @{ Relative = "data/common/ItemTypes.d2o"; Source = Join-Path $ClientRoot "data\common\ItemTypes.d2o" },
    @{ Relative = "data/i18n/i18n_es.d2i"; Source = Join-Path $ClientRoot "data\i18n\i18n_es.d2i" },
    @{ Relative = "data/i18n/i18n_en.d2i"; Source = Join-Path $ClientRoot "data\i18n\i18n_en.d2i" }
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $RepoRoot "backups\client\$timestamp"
$confirm = $env:CONFIRM_BACKUP -eq "1"

Write-Host "Publication client backup plan"
Write-Host "  RepoRoot: $RepoRoot"
Write-Host "  ClientRoot (read-only source): $ClientRoot"
Write-Host "  Output: $backupRoot"
Write-Host "  CONFIRM_BACKUP: $confirm"

foreach ($entry in $files) {
    if (-not (Test-Path $entry.Source)) {
        throw "Archivo cliente no encontrado: $($entry.Source)"
    }
    Write-Host "  - $($entry.Relative) ($((Get-Item $entry.Source).Length) bytes)"
}

if (-not $confirm) {
    Write-Host "Modo seguro: no se copió nada. Usa `$env:CONFIRM_BACKUP='1' para ejecutar."
    exit 0
}

New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$checksumLines = New-Object System.Collections.Generic.List[string]
$checksumLines.Add("# SHA-256 client publication backup")

$manifestFiles = @()
foreach ($entry in $files) {
    $destDir = Join-Path $backupRoot ($entry.Relative -replace "/", "\")
    $destDir = Split-Path $destDir -Parent
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    $destPath = Join-Path $backupRoot ($entry.Relative -replace "/", "\")
    Copy-Item -LiteralPath $entry.Source -Destination $destPath -Force
    $sha = Get-Sha256Hex $destPath
    $checksumLines.Add("$sha  $($entry.Relative)")
    $manifestFiles += @{
        relativePath = $entry.Relative
        sha256       = $sha
        sizeBytes    = (Get-Item $destPath).Length
    }
}

$manifest = @{
    backupType     = "client-publication"
    createdAtUtc   = (Get-Date).ToUniversalTime().ToString("o")
    clientRootPath = $ClientRoot
    backupPath     = $backupRoot
    files          = $manifestFiles
    phase          = "4-controlled-lane"
    production     = $false
}
$manifestPath = Join-Path $backupRoot "manifest.json"
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath -Encoding UTF8
$checksumLines | Set-Content -Path (Join-Path $backupRoot "checksums.sha256") -Encoding UTF8

Write-Host "Backup OK: $backupRoot"

if (-not $SkipLaneUpdate) {
    & (Join-Path $scriptDir "update-publish-lane.ps1") -RepoRoot $RepoRoot
}
