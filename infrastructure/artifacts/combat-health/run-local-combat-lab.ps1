#Requires -Version 5.1
<#
.SYNOPSIS
  Prepara y lanza el emulador Sunshine en modo Combat Health Lab.
.NOTES
  - Usa DB local/lab (nunca producción VPS para pruebas destructivas).
  - Activa variables de telemetría (cuando FightTelemetry esté implementado).
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [string]$Configuration = "Debug",
    [switch]$BuildOnly,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$sunshineProject = Join-Path $RepoRoot "Sunshine net11.0\Sunshine net11.0\Sunshine.csproj"
$combatLogRoot = Join-Path $RepoRoot "Infrastructure\logs\combat"
$spellCastLogRoot = Join-Path $combatLogRoot "spell-casts"

if (-not (Test-Path $sunshineProject)) {
    throw "No se encontró Sunshine.csproj en: $sunshineProject"
}

New-Item -ItemType Directory -Force -Path $combatLogRoot | Out-Null
New-Item -ItemType Directory -Force -Path $spellCastLogRoot | Out-Null

$env:COMBAT_HEALTH_LAB = "1"
$env:FIGHT_TELEMETRY_ENABLED = if ($env:FIGHT_TELEMETRY_ENABLED) { $env:FIGHT_TELEMETRY_ENABLED } else { "true" }
$env:FIGHT_TELEMETRY_FILE_ENABLED = if ($env:FIGHT_TELEMETRY_FILE_ENABLED) { $env:FIGHT_TELEMETRY_FILE_ENABLED } else { "true" }
$env:FIGHT_TELEMETRY_LOG_DIRECTORY = if ($env:FIGHT_TELEMETRY_LOG_DIRECTORY) { $env:FIGHT_TELEMETRY_LOG_DIRECTORY } else { $combatLogRoot }

Write-Host "Combat Health Lab"
Write-Host "  RepoRoot:              $RepoRoot"
Write-Host "  Telemetry directory:   $env:FIGHT_TELEMETRY_LOG_DIRECTORY"
$labDbHost = if ($env:COMBAT_LAB_DB_HOST) { $env:COMBAT_LAB_DB_HOST } else { "(configurar en appsettings.Development.local.json)" }
Write-Host "  COMBAT_LAB_DB:         $labDbHost"

if (-not $SkipBuild) {
    Write-Host "Building Sunshine ($Configuration)..."
    dotnet build $sunshineProject -c $Configuration --no-restore 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        dotnet build $sunshineProject -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "dotnet build falló." }
    }
}

if ($BuildOnly) {
    Write-Host "BuildOnly — servidor no iniciado."
    exit 0
}

Write-Host "Iniciando Sunshine (Ctrl+C para detener)..."
Write-Host "Verifica que la connection string apunta a sunshine_lab, NO al VPS."
Push-Location (Split-Path $sunshineProject -Parent)
try {
    dotnet run --project $sunshineProject -c $Configuration --no-build
}
finally {
    Pop-Location
}
