[CmdletBinding()]
param(
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$SshKey = "",
    [int]$Tail = 80
)

$ErrorActionPreference = "Stop"
if (-not $SshKey) {
    $downloadKey = Join-Path $env:USERPROFILE "Downloads\keys\private_key_sebas.pem"
    if (Test-Path $downloadKey) { $SshKey = $downloadKey }
}
if (-not $SshKey -or -not (Test-Path $SshKey)) {
    throw "SSH key not found. Pass -SshKey with your local PEM file."
}

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)

$outPath = & ssh @sshArgs "bash /opt/dofus-2.0.0-build/scripts/qa-npc-logs.sh"
Write-Output "Log file: $outPath"
Write-Output "--- last $Tail lines ---"
& ssh @sshArgs "tail -n $Tail '$outPath'"
