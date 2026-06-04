#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$DbContainerName = "sunshine-db",
    [string]$DatabaseName = "sunshine"
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

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $RepoRoot "backups\db\$timestamp"
$dumpPath = Join-Path $backupRoot "sunshine.sql"
$confirm = $env:CONFIRM_BACKUP -eq "1"

$running = docker inspect -f "{{.State.Running}}" $DbContainerName 2>$null
if ($LASTEXITCODE -ne 0 -or $running -ne "true") {
    throw "Contenedor '$DbContainerName' no está en ejecución. Inicia docker local antes del backup DB."
}

Write-Host "Publication DB backup plan"
Write-Host "  Container: $DbContainerName"
Write-Host "  Database: $DatabaseName"
Write-Host "  Output: $dumpPath"
Write-Host "  CONFIRM_BACKUP: $confirm"

if (-not $confirm) {
    Write-Host "Modo seguro: no se ejecutó mysqldump. Usa `$env:CONFIRM_BACKUP='1'."
    exit 0
}

New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$rootPassword = docker exec $DbContainerName printenv MYSQL_ROOT_PASSWORD 2>$null
if ([string]::IsNullOrWhiteSpace($rootPassword)) {
    throw "MYSQL_ROOT_PASSWORD no disponible en el contenedor."
}

docker exec $DbContainerName mysqldump -uroot -p"$rootPassword" --single-transaction --skip-lock-tables $DatabaseName `
    | Set-Content -Path $dumpPath -Encoding UTF8

if (-not (Test-Path $dumpPath) -or (Get-Item $dumpPath).Length -eq 0) {
    throw "Dump vacío o ausente."
}

$sha = ([BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash([IO.File]::ReadAllBytes($dumpPath))) -replace "-", "").ToLowerInvariant()
$manifest = @{
    backupType   = "database-sunshine"
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    database     = $DatabaseName
    container    = $DbContainerName
    dumpFile     = "sunshine.sql"
    sha256       = $sha
    sizeBytes    = (Get-Item $dumpPath).Length
    production   = $false
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $backupRoot "manifest.json") -Encoding UTF8
"$sha  sunshine.sql" | Set-Content (Join-Path $backupRoot "checksums.sha256") -Encoding UTF8
Write-Host "Backup OK: $backupRoot"

& (Join-Path $scriptDir "update-publish-lane.ps1") -RepoRoot $RepoRoot
