[CmdletBinding()]
param(
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$SshKey = "",
    [string]$RemotePath = "/opt/dofus-2.0.0",
    [switch]$SkipSync,
    [switch]$StackOnly,
    [switch]$TraefikOnly
)

$ErrorActionPreference = "Stop"
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
if (-not $SshKey) {
    $SshKey = Join-Path $repoRoot "SSH\private_key_sebas.pem"
}
$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "StrictHostKeyChecking=accept-new", $sshTarget)

function Invoke-Ssh {
    param([Parameter(Mandatory = $true)][string]$Command)
    & ssh @sshArgs $Command
    if ($LASTEXITCODE -ne 0) {
        throw "SSH command failed: $Command"
    }
}

function Invoke-Scp {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )
    & scp @("-i", $SshKey, "-o", "StrictHostKeyChecking=accept-new", "-r", $Source, "${sshTarget}:${Destination}")
    if ($LASTEXITCODE -ne 0) {
        throw "SCP failed: $Source -> $Destination"
    }
}

if (-not $SkipSync) {
    Write-Output "Creating remote directory $RemotePath ..."
    Invoke-Ssh "mkdir -p $RemotePath"

    $syncItems = @(
        "docker",
        "database",
        "runtime",
        "Sunshine net11.0",
        "OneLauncher/OneLauncher.Api",
        "RollblackLegacy.Auth",
        "RollblackLegacy.Website",
        "RollblackLegacy.Website.Application",
        "RollblackLegacy.Website.Infrastructure",
        "RollblackLegacy.Website.Contracts",
        "RollblackLegacy.Website.Domain",
        ".env.example",
        "README.md"
    )

    foreach ($item in $syncItems) {
        $localPath = Join-Path $repoRoot $item
        if (-not (Test-Path $localPath)) {
            Write-Warning "Skipping missing path: $item"
            continue
        }
        Write-Output "Syncing $item ..."
        Invoke-Scp -Source $localPath -Destination "$RemotePath/"
    }

    $envPath = Join-Path $repoRoot ".env"
    if (Test-Path $envPath) {
        Invoke-Scp -Source $envPath -Destination "$RemotePath/.env"
    }
    else {
        Write-Warning "No local .env found. Copy .env.example to .env on the VPS before starting containers."
    }
}

if ($TraefikOnly) {
    Invoke-Ssh "cd $RemotePath/docker && docker compose -f docker-compose-traefik.yml up -d"
    exit 0
}

if (-not $StackOnly) {
    Invoke-Ssh @"
set -eu
if ! command -v docker >/dev/null 2>&1; then
  curl -fsSL https://get.docker.com | sh
fi
docker network inspect traefik_web >/dev/null 2>&1 || docker network create traefik_web
if command -v ufw >/dev/null 2>&1; then
  ufw allow 22/tcp || true
  ufw allow 80/tcp || true
  ufw allow 443/tcp || true
  ufw allow 2450/tcp || true
  ufw allow 5557/tcp || true
fi
"@
}

$composeCmd = @"
cd $RemotePath/docker && docker compose --env-file ../.env \
  -f docker-compose.yml \
  -f docker-compose.vps.yml \
  -f docker-compose-onelauncher-api.yml \
  -f docker-compose-website.yml \
  up -d --build && \
docker compose -f docker-compose-traefik.yml up -d
"@

Invoke-Ssh $composeCmd
Write-Output "Deploy finished. Verify: docker ps on $VpsHost"
