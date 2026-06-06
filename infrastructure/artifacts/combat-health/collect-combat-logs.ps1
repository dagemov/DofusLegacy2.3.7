#Requires -Version 5.1
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$SourceDirectory = "",
    [string]$Label = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceDirectory)) {
    $SourceDirectory = Join-Path $RepoRoot "Infrastructure\logs\combat"
}

if (-not (Test-Path $SourceDirectory)) {
    Write-Warning "Directorio de logs no existe: $SourceDirectory"
    Write-Warning "FightTelemetry aún no implementado en Sunshine o no hubo sesiones."
    exit 0
}

$timestamp = if ($Label) { $Label } else { Get-Date -Format "yyyyMMdd-HHmmss" }
$destRoot = Join-Path $RepoRoot "Infrastructure\temporal-artifacts\combat-logs\local\$timestamp"
New-Item -ItemType Directory -Force -Path $destRoot | Out-Null

$files = Get-ChildItem -Path $SourceDirectory -Recurse -File -ErrorAction SilentlyContinue
if ($files.Count -eq 0) {
    Write-Warning "No hay archivos en $SourceDirectory"
    exit 0
}

foreach ($file in $files) {
    $relative = $file.FullName.Substring($SourceDirectory.Length).TrimStart("\", "/")
    $target = Join-Path $destRoot $relative
    $parent = Split-Path $target -Parent
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Copy-Item -Path $file.FullName -Destination $target -Force
}

Write-Host "Logs archivados en: $destRoot ($($files.Count) archivos)"
