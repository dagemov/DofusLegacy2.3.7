[CmdletBinding()]
param(
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$SshKey = "",
    [string]$DbNameHint = "sunshine-db",
    [string]$RemoteBackupDir = "/root/backups/sunshine",
    [string]$Tables = "items accounts worlds_characters characters characters_items characters_spells characters_stats npcs npcs_items",
    [switch]$ConfirmBackup
)

$ErrorActionPreference = "Stop"
if (-not $SshKey) {
    $downloadKey = Join-Path $env:USERPROFILE "Downloads\keys\private_key_sebas.pem"
    $fallbackKey = Join-Path $env:USERPROFILE "Downloads\private_key_sebas.pem"
    if (Test-Path $downloadKey) {
        $SshKey = $downloadKey
    }
    elseif (Test-Path $fallbackKey) {
        $SshKey = $fallbackKey
    }
}

if (-not $SshKey -or -not (Test-Path $SshKey)) {
    throw "SSH key not found. Pass -SshKey with a local non-tracked PEM file."
}

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)

function Invoke-Remote {
    param([Parameter(Mandatory = $true)][string]$Command)

    & ssh @sshArgs $Command
    if ($LASTEXITCODE -ne 0) {
        throw "SSH failed while executing: $Command"
    }
}

function Invoke-RemoteBashScript {
    param([Parameter(Mandatory = $true)][string]$Script)

    ($Script -replace "`r`n", "`n") | & ssh @sshArgs "bash -s"
    if ($LASTEXITCODE -ne 0) {
        throw "SSH bash script failed."
    }
}

$containers = Invoke-Remote -Command "docker ps -a --format '{{.Names}}'"
$dbContainer = $containers | Where-Object { $_ -like "*$DbNameHint*" } | Select-Object -First 1
if (-not $dbContainer) {
    throw "No DB container matching '$DbNameHint' was detected."
}

Write-Output "DB target detected:"
Write-Output "  Container: $dbContainer"
Write-Output "  Remote dir: $RemoteBackupDir"
Write-Output "  Tables: $Tables"

if (-not $ConfirmBackup) {
    Write-Warning "Safe mode: no backup created. Use -ConfirmBackup to execute the dump."
    exit 0
}

$remoteScript = @'
set -euo pipefail
mkdir -p '__REMOTE_BACKUP_DIR__'
stamp=$(date -u +%Y%m%dT%H%M%SZ)
file='__REMOTE_BACKUP_DIR__'/sunshine-pre-restart-$stamp.sql
docker exec '__DB_CONTAINER__' sh -lc 'exec mariadb-dump --single-transaction --quick -uroot -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE" __TABLES__' > "$file"
if [ ! -s "$file" ]; then
  echo "Backup file is empty: $file" >&2
  exit 1
fi
bytes=$(stat -c%s "$file")
printf 'BACKUP_FILE=%s\n' "$file"
printf 'BACKUP_BYTES=%s\n' "$bytes"
'@

$remoteScript = $remoteScript.Replace('__REMOTE_BACKUP_DIR__', $RemoteBackupDir).Replace('__DB_CONTAINER__', $dbContainer).Replace('__TABLES__', $Tables)

Invoke-RemoteBashScript -Script $remoteScript
