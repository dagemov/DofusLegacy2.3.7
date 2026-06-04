#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [int]$TargetItemId = 12617
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

function Get-LatestBackupUtc {
    param([string]$CategoryRoot)
    if (-not (Test-Path $CategoryRoot)) { return $null }
    $latest = Get-ChildItem -Directory $CategoryRoot -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Select-Object -First 1
    if ($null -eq $latest) { return $null }
    $manifest = Join-Path $latest.FullName "manifest.json"
    if (-not (Test-Path $manifest)) { return $latest.Name }
    try {
        $json = Get-Content $manifest -Raw | ConvertFrom-Json
        return [datetimeoffset]::Parse($json.createdAtUtc)
    }
    catch {
        return $null
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Resolve-RepoRoot (Join-Path $scriptDir "..\..\..")
}

$laneDir = Join-Path $RepoRoot "Infrastructure\staging-client\publish-lane"
New-Item -ItemType Directory -Path $laneDir -Force | Out-Null

$packageRel = "Infrastructure/staging-client/publication-package-phase3c/$TargetItemId"
$packageDir = Join-Path $RepoRoot ($packageRel -replace "/", "\")
$validationPath = Join-Path $packageDir "validation-report.json"

$blocking = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$nextSteps = [System.Collections.Generic.List[string]]::new()

$validationStatus = $null
$lastValidationUtc = $null
if (Test-Path $validationPath) {
    try {
        $validation = Get-Content $validationPath -Raw | ConvertFrom-Json
        $validationStatus = $validation.ValidationStatus
        $lastValidationUtc = [datetimeoffset]::Parse($validation.CheckedAt)
        if ($validation.BlockingReasons) {
            $blocking.AddRange(@($validation.BlockingReasons))
        }
    }
    catch {
        $warnings.Add("No se pudo leer validation-report.json.")
    }
}

$clientBackupUtc = Get-LatestBackupUtc (Join-Path $RepoRoot "backups\client")
$dbBackupUtc = Get-LatestBackupUtc (Join-Path $RepoRoot "backups\db")

$packageValid = $validationStatus -in @("READY_FOR_CONTROLLED_PUBLISH", "VALID_STAGING_PACKAGE")
if (-not (Test-Path $packageDir)) {
    $laneStatus = "NEEDS_VALIDATION"
    $blocking.Add("Paquete staging no encontrado en $packageRel.")
}
elseif (-not $packageValid) {
    $laneStatus = "NEEDS_VALIDATION"
    $blocking.Add("ValidationStatus actual: $(if ($validationStatus) { $validationStatus } else { '(sin reporte)' }).")
}
elseif ($null -eq $clientBackupUtc) {
    $laneStatus = "NEEDS_BACKUP"
    $blocking.Add("No existe backup cliente en backups/client/.")
}
elseif ($blocking.Count -gt 0) {
    $laneStatus = "BLOCKED"
}
else {
    $laneStatus = "READY"
}

if ($null -eq $dbBackupUtc) {
    $warnings.Add("No existe backup DB en backups/db/ (recomendado antes de publish).")
}

$nextSteps.Add("Publicación real sigue bloqueada (ProductionPublishBlocked=true).")
if ($laneStatus -eq "READY") {
    $nextSteps.Add("Phase 5+: aplicar patch solo en copia backup del cliente tras aprobación explícita.")
}
else {
    $nextSteps.Add("Ejecutar backup-client y backup-db con CONFIRM_BACKUP=1, luego re-evaluar lane.")
}

$state = @{
    PublishLaneStatus               = $laneStatus
    TargetItemId                    = $TargetItemId
    StagingPackagePath              = $packageRel
    LastEvaluatedAtUtc              = (Get-Date).ToUniversalTime().ToString("o")
    LastValidationUtc               = if ($lastValidationUtc) { $lastValidationUtc.ToString("o") } else { $null }
    LastValidationStatus            = $validationStatus
    LastClientBackupUtc             = if ($clientBackupUtc) { $clientBackupUtc.ToString("o") } else { $null }
    LastDbBackupUtc                 = if ($dbBackupUtc) { $dbBackupUtc.ToString("o") } else { $null }
    RequiresClientBackupBeforePublish = $true
    ProductionPublishBlocked        = $true
    BlockingReasons                 = $blocking
    Warnings                        = $warnings
    NextManualSteps                 = $nextSteps
    Pipeline                        = @("publication-package", "backup-validation", "patch-validation", "ready-to-publish")
}

$lanePath = Join-Path $laneDir "lane-state.json"
$state | ConvertTo-Json -Depth 6 | Set-Content -Path $lanePath -Encoding UTF8
Write-Host "Publish lane: $laneStatus -> $lanePath"
