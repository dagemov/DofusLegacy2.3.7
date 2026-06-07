#Requires -Version 5.1
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$InputDirectory = "",
    [string]$OutputDirectory = "",
    [string]$RollbackRoot = $(if ($env:ROLLBACK_REFERENCE_ROOT) { $env:ROLLBACK_REFERENCE_ROOT } else { "C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback" })
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($InputDirectory)) {
    $InputDirectory = Join-Path $RepoRoot "Infrastructure\logs\combat"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot "Infrastructure\temporal-artifacts\combat-telemetry"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$localAnalyzer = Join-Path $RepoRoot "infrastructure\scripts\CombatTelemetryAnalyzer\CombatTelemetryAnalyzer.csproj"
$rollbackAnalyzer = Join-Path $RollbackRoot "Infrastructure\scripts\CombatTelemetryAnalyzer\CombatTelemetryAnalyzer.csproj"

$analyzerProject = $null
if (Test-Path $localAnalyzer) {
    $analyzerProject = $localAnalyzer
    Write-Host "Usando analizador local: $analyzerProject"
}
elseif (Test-Path $rollbackAnalyzer) {
    $analyzerProject = $rollbackAnalyzer
    Write-Host "Usando analizador de referencia Rollback: $analyzerProject"
}
else {
    throw "CombatTelemetryAnalyzer no encontrado. Migra el proyecto a infrastructure/scripts/ o define ROLLBACK_REFERENCE_ROOT."
}

if (-not (Test-Path $InputDirectory)) {
    throw "InputDirectory no existe: $InputDirectory. Ejecuta run-local-combat-lab.ps1 y al menos un combate PvM antes de analizar."
}

$logFiles = @(Get-ChildItem -Path $InputDirectory -Recurse -Include *.jsonl, *.log -File -ErrorAction SilentlyContinue)
if ($logFiles.Count -eq 0) {
    throw "No hay archivos .jsonl/.log en $InputDirectory. Captura combates reales antes del gate Phase 3."
}

$nonSampleFiles = @($logFiles | Where-Object { $_.Name -notmatch 'sample' })
if ($nonSampleFiles.Count -eq 0) {
    Write-Warning "Solo archivos *sample* detectados. Esto NO cierra el gate de telemetría real."
}
else {
    Write-Host "Archivos reales detectados: $($nonSampleFiles.Count) de $($logFiles.Count)"
}

$primaryReport = Join-Path $OutputDirectory "report.md"
$jsonReport = Join-Path $OutputDirectory "report.json"

Write-Host "Analizando: $InputDirectory"
Write-Host "Reporte principal: $primaryReport"

Push-Location $RepoRoot
try {
    dotnet run --project $analyzerProject -- --input $InputDirectory --output $primaryReport --json-output $jsonReport
    if ($LASTEXITCODE -ne 0) { throw "CombatTelemetryAnalyzer falló con código $LASTEXITCODE." }
}
finally {
    Pop-Location
}

$htmlReport = Join-Path $OutputDirectory "report.html"
if (Test-Path $primaryReport) {
    $md = Get-Content -Path $primaryReport -Raw -Encoding UTF8
    $escaped = [System.Net.WebUtility]::HtmlEncode($md)
    $htmlBody = $escaped -replace "`r?`n", "<br/>"
    @"
<!DOCTYPE html>
<html lang="es">
<head><meta charset="utf-8"/><title>Combat Telemetry Report</title>
<style>body{font-family:Segoe UI,sans-serif;margin:2rem;line-height:1.5}code{font-family:Consolas,monospace}</style>
</head>
<body><h1>Combat Telemetry Report</h1><p>Generado localmente — no commitear.</p><div>$htmlBody</div></body>
</html>
"@ | Set-Content -Path $htmlReport -Encoding UTF8
}

Write-Host "Informes generados en: $OutputDirectory"
Write-Host "  - report.md"
Write-Host "  - report.json"
Write-Host "  - report.html"
Write-Host "  - combat-turn-latency-analysis-report.md"
Write-Host "  - combat-turn-transition-phase2-report.md"
Write-Host "  - combat-timer-elapsed-classification-report.md"
Write-Host ""
Write-Host "Abrir en Windows: notepad `"$primaryReport`""
Write-Host "O navegador: start `"$htmlReport`""
