#Requires -Version 5.1
<#
.SYNOPSIS
  Alinea puertos auth/world y WORLD_PUBLIC_HOST del VPS con el cliente Dofus (2450/5557).
#>
param(
    [string]$VpsHost = $(if ($env:VPS_HOST) { $env:VPS_HOST } else { "174.138.35.107" }),
    [string]$SshUser = $(if ($env:SSH_USER) { $env:SSH_USER } else { "root" }),
    [Parameter(Mandatory = $true)]
    [string]$SshKey,
    [string]$RemoteRoot = "/opt/dofus-2.0.0",
    [string]$PublicHost = "174.138.35.107",
    [int]$AuthPort = 2450,
    [int]$WorldPort = 5557,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKey)) {
    throw "SSH key no encontrada: $SshKey"
}

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)

Write-Host "=== Fix VPS client ports ==="
Write-Host "Target: $PublicHost auth=$AuthPort world=$WorldPort"
Write-Host "DryRun: $($DryRun.IsPresent)"

$remoteScript = @"
set -eu
cd $RemoteRoot
cp .env .env.bak-port-fix-`$(date -u +%Y%m%dT%H%M%SZ)
sed -i 's/^WORLD_PUBLIC_HOST=.*/WORLD_PUBLIC_HOST=$PublicHost/' .env
sed -i 's/^AUTH_PORT=.*/AUTH_PORT=$AuthPort/' .env
sed -i 's/^WORLD_PORT=.*/WORLD_PORT=$WorldPort/' .env
grep -E '^WORLD_PUBLIC_HOST=|^AUTH_PORT=|^WORLD_PORT=' .env
cd docker
docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml up -d sunshine
docker ps --filter name=sunshine-server --format '{{.Names}} {{.Ports}} {{.Status}}'
"@

if ($DryRun) {
    Write-Host $remoteScript
    exit 0
}

($remoteScript -replace "`r`n", "`n") | & ssh @sshArgs "bash -s"
if ($LASTEXITCODE -ne 0) { throw "Falló fix remoto de puertos." }

Write-Host "Espera ~40s al boot READY, luego:"
Write-Host "  Test-NetConnection $VpsHost -Port $AuthPort"
Write-Host "  Test-NetConnection $VpsHost -Port $WorldPort"
