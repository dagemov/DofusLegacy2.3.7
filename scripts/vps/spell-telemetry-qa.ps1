[CmdletBinding()]
param(
    [ValidateSet("status", "backup", "activate", "deactivate", "collect", "verify")]
    [string]$Action = "status",
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$SshKey = "",
    [string]$RemotePath = "/opt/dofus-2.0.0",
    [string]$LocalCollectDir = "",
    [string]$RunDate = ""
)

$ErrorActionPreference = "Stop"
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
if (-not $SshKey) {
    $SshKey = Join-Path $repoRoot "SSH\private_key_sebas.pem"
}
if (-not $LocalCollectDir) {
    $stamp = if ($RunDate) { $RunDate } else { Get-Date -Format "yyyyMMdd" }
    $LocalCollectDir = Join-Path $repoRoot "Infrastructure\temporal-artifacts\combat-telemetry\vps-run-$stamp"
}
$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "StrictHostKeyChecking=accept-new", "-o", "BatchMode=yes", $sshTarget)

function Invoke-Ssh {
    param([Parameter(Mandatory = $true)][string]$Command)
    & ssh @sshArgs $Command
    if ($LASTEXITCODE -ne 0) { throw "SSH failed: $Command" }
}

function Invoke-ScpFrom {
    param(
        [Parameter(Mandatory = $true)][string]$RemoteSource,
        [Parameter(Mandatory = $true)][string]$LocalDestination
    )
    New-Item -ItemType Directory -Force -Path $LocalDestination | Out-Null
    & scp @("-i", $SshKey, "-o", "StrictHostKeyChecking=accept-new", "-r", "${sshTarget}:${RemoteSource}", $LocalDestination)
    if ($LASTEXITCODE -ne 0) { throw "SCP failed: $RemoteSource" }
}

$qaEnvBlock = @"
# --- spell telemetry QA window (temporary) ---
SPELL_EFFECT_TELEMETRY_ENABLED=true
COMBAT_HEALTH_LAB=1
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
FIGHT_TELEMETRY_ENABLED=false
# Mirror humano OFF durante QA (JSONL es fuente principal)
FIGHT_COMBAT_LOG_ENABLED=false
"@

switch ($Action) {
    "status" {
        Invoke-Ssh @"
set -eu
echo '=== git/deploy hint ==='
test -f $RemotePath/.deploy-commit && cat $RemotePath/.deploy-commit || echo 'no .deploy-commit file'
echo '=== telemetry env in .env ==='
grep -E 'SPELL_EFFECT|COMBAT_HEALTH|FIGHT_TELEMETRY|FIGHT_COMBAT' $RemotePath/.env 2>/dev/null || echo '(none)'
echo '=== container env ==='
docker inspect sunshine-server --format '{{range .Config.Env}}{{println .}}{{end}}' | grep -E 'SPELL_EFFECT|COMBAT_HEALTH|FIGHT_TELEMETRY|FIGHT_COMBAT' || echo '(none)'
echo '=== spell-casts on host ==='
ls -la $RemotePath/logs/combat/spell-casts/ 2>/dev/null || echo 'host dir missing'
echo '=== spell-casts in container ==='
docker exec sunshine-server ls -la /app/logs/combat/spell-casts/ 2>/dev/null || echo 'container dir missing'
docker exec sunshine-server sh -c 'grep -aq SpellEffectTelemetry /app/Sunshine.dll && echo DLL_HAS_SpellEffectTelemetry || echo DLL_MISSING_SpellEffectTelemetry'
"@
    }
    "backup" {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        Invoke-Ssh @"
set -eu
mkdir -p $RemotePath/backups/spell-telemetry-$stamp
cp -a $RemotePath/.env $RemotePath/backups/spell-telemetry-$stamp/.env
cp -a $RemotePath/docker/docker-compose.vps.yml $RemotePath/backups/spell-telemetry-$stamp/docker-compose.vps.yml 2>/dev/null || true
echo backup=$RemotePath/backups/spell-telemetry-$stamp
"@
    }
    "activate" {
        Invoke-Ssh @"
set -eu
ENV_FILE=$RemotePath/.env
touch `"`$ENV_FILE`"
grep -v -E '^(SPELL_EFFECT_TELEMETRY_ENABLED|COMBAT_HEALTH_LAB|FIGHT_TELEMETRY_LOG_DIRECTORY|FIGHT_TELEMETRY_ENABLED|FIGHT_COMBAT_LOG_ENABLED)=' `"`$ENV_FILE`" > `"`$ENV_FILE.tmp`" || true
cat >> `"`$ENV_FILE.tmp`" <<'EOF'
$qaEnvBlock
EOF
mv `"`$ENV_FILE.tmp`" `"`$ENV_FILE`"
mkdir -p $RemotePath/logs/combat/spell-casts
cd $RemotePath/docker
docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml up -d sunshine
docker exec sunshine-server ls -la /app/logs/combat/spell-casts/
echo 'QA window activated. Run combats, then: spell-telemetry-qa.ps1 -Action collect'
"@
    }
    "deactivate" {
        Invoke-Ssh @"
set -eu
ENV_FILE=$RemotePath/.env
grep -v -E '^(SPELL_EFFECT_TELEMETRY_ENABLED|COMBAT_HEALTH_LAB|FIGHT_TELEMETRY_LOG_DIRECTORY|FIGHT_TELEMETRY_ENABLED)=' `"`$ENV_FILE`" > `"`$ENV_FILE.tmp`" || true
mv `"`$ENV_FILE.tmp`" `"`$ENV_FILE`"
cd $RemotePath/docker
docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml up -d sunshine
echo 'Spell telemetry QA vars removed. FIGHT_COMBAT_LOG_* unchanged — set manually if needed.'
"@
    }
    "collect" {
        Invoke-ScpFrom -RemoteSource "$RemotePath/logs/combat/spell-casts/" -LocalDestination $LocalCollectDir
        Write-Output "Collected to: $LocalCollectDir"
        Get-ChildItem -Path $LocalCollectDir -Recurse -Filter "*.jsonl" | Select-Object FullName, Length, LastWriteTime
    }
    "verify" {
        Invoke-Ssh @"
docker exec sunshine-server sh -c 'grep -aq SpellEffectTelemetry /app/Sunshine.dll && echo OK_DLL || echo FAIL_DLL'
docker exec sunshine-server printenv SPELL_EFFECT_TELEMETRY_ENABLED COMBAT_HEALTH_LAB FIGHT_TELEMETRY_LOG_DIRECTORY FIGHT_COMBAT_LOG_ENABLED 2>/dev/null || true
"@
    }
}
