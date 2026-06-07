[CmdletBinding()]
param(
    [string]$FileList = "",
    [string]$RemoveList = "",
    [switch]$BuildOnly,
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$RemotePath = "/opt/dofus-2.0.0-build",
    [int]$Tail = 50
)

$ErrorActionPreference = "Stop"
$Files = @($FileList -split ';' | Where-Object { $_ -ne '' })
$Remove = @($RemoveList -split ';' | Where-Object { $_ -ne '' })
Write-Output ("Files to sync: " + ($Files -join ' | '))
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$key = Join-Path $repoRoot "SSH\private_key_sebas.pem"
$sshTarget = "${SshUser}@${VpsHost}"
$sshBase = @("-i", $key, "-o", "StrictHostKeyChecking=accept-new", "-o", "ConnectTimeout=20", "-o", "ServerAliveInterval=10", "-o", "ServerAliveCountMax=6", "-o", "BatchMode=yes")

Push-Location $repoRoot
try {
    if (-not $BuildOnly -and $Files.Count -gt 0) {
        # Ensure all remote parent directories exist (single ssh call).
        $dirs = @{}
        foreach ($f in $Files) {
            $idx = $f.LastIndexOf('/')
            if ($idx -gt 0) { $dirs["$RemotePath/" + $f.Substring(0, $idx)] = $true }
        }
        if ($dirs.Count -gt 0) {
            $mkdirArg = ($dirs.Keys | ForEach-Object { "'$_'" }) -join " "
            & ssh @sshBase $sshTarget "mkdir -p $mkdirArg && echo MKDIR_OK"
            if ($LASTEXITCODE -ne 0) { throw "remote mkdir failed" }
        }
        # Copy each file directly to its destination path (handles spaces via remote quoting).
        foreach ($f in $Files) {
            $remote = "$RemotePath/$f"
            # -O forces the legacy SCP protocol; the SFTP protocol intermittently stalls on large files.
            # Legacy protocol passes the remote path through a shell, so quote it (paths contain spaces).
            & scp -O @sshBase $f "${sshTarget}:'$remote'"
            if ($LASTEXITCODE -ne 0) { throw "scp failed for $f" }
        }
        Write-Output "SYNC_OK"
    }

    if ($Remove.Count -gt 0) {
        $rmList = ($Remove | ForEach-Object { "'$RemotePath/$_'" }) -join " "
        & ssh @sshBase $sshTarget "rm -f $rmList && echo REMOVE_OK"
        if ($LASTEXITCODE -ne 0) { throw "remote remove failed" }
    }

    $buildCmd = "cd '$RemotePath/docker' && DOCKER_BUILDKIT=1 docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml build sunshine 2>&1 | tail -$Tail"
    & ssh @sshBase $sshTarget $buildCmd
    if ($LASTEXITCODE -ne 0) { throw "BUILD GATE FAILED (exit $LASTEXITCODE)" }
}
finally {
    Pop-Location
}
