#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$SshKey = "",
    [string]$RemotePath = "/opt/dofus-2.0.0"
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
if ([string]::IsNullOrWhiteSpace($SshKey)) {
    $candidate = Join-Path $RepoRoot "SSH\private_key_sebas.pem"
    if (Test-Path $candidate) { $SshKey = $candidate }
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $RepoRoot "backups\vps\$timestamp"
$confirm = $env:CONFIRM_BACKUP -eq "1"

Write-Host "VPS inventory backup plan"
Write-Host "  Host: $VpsHost"
Write-Host "  RemotePath: $RemotePath"
Write-Host "  Output: $backupRoot"
Write-Host "  CONFIRM_BACKUP: $confirm"

if ([string]::IsNullOrWhiteSpace($SshKey) -or -not (Test-Path $SshKey)) {
    throw "SSH key no encontrada. Define SSH_KEY o coloca SSH\private_key_sebas.pem (no commitear)."
}

if (-not $confirm) {
    Write-Host "Modo seguro: no se conectó por SSH. Usa `$env:CONFIRM_BACKUP='1'."
    exit 0
}

New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$sshTarget = "${SshUser}@${VpsHost}"
$remoteScript = @"
set -eu
echo '=== hostname ==='
hostname
echo '=== uptime ==='
uptime
echo '=== docker ps ==='
docker ps -a
echo '=== docker images ==='
docker images
echo '=== docker compose files ==='
ls -la $RemotePath/docker 2>/dev/null || true
echo '=== docker compose config (main stack) ==='
cd $RemotePath/docker && docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml -f docker-compose-onelauncher-api.yml -f docker-compose-website.yml config 2>/dev/null | head -n 120 || true
"@

($remoteScript -replace "`r`n", "`n") | ssh -i $SshKey -o BatchMode=yes -o StrictHostKeyChecking=accept-new $sshTarget "bash -s" `
    | Set-Content -Path (Join-Path $backupRoot "vps-inventory.txt") -Encoding UTF8

$manifest = @{
    backupType   = "vps-inventory"
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    vpsHost      = $VpsHost
    remotePath   = $RemotePath
    inventoryFile = "vps-inventory.txt"
    production   = $false
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $backupRoot "manifest.json") -Encoding UTF8
Write-Host "Inventory OK: $backupRoot"

& (Join-Path $scriptDir "update-publish-lane.ps1") -RepoRoot $RepoRoot
