[CmdletBinding()]
param(
    [string]$VpsHost = "174.138.35.107",
    [string]$SshUser = "root",
    [string]$SshKey = "",
    [string]$WorldNameHint = "sunshine-server",
    [int]$Tail = 50,
    [switch]$ConfirmRestart
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if (-not $SshKey) {
    $SshKey = Join-Path $repoRoot "SSH\private_key_sebas.pem"
}

$sshTarget = "${SshUser}@${VpsHost}"
$sshArgs = @("-i", $SshKey, "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new", $sshTarget)

function Invoke-Remote {
    param([Parameter(Mandatory = $true)][string]$Command)

    & ssh @sshArgs $Command
    if ($LASTEXITCODE -ne 0) {
        throw "SSH fallo al ejecutar: $Command"
    }
}

$discoveryScript = @"
set -eu
if command -v docker >/dev/null 2>&1; then
  docker ps -a --format 'docker|{{.Names}}|{{.Image}}'
fi
if command -v systemctl >/dev/null 2>&1; then
  systemctl list-units --type=service --all --no-legend | sed 's/[[:space:]].*$//' | sed 's/^/systemd|/' | sed 's/$/|service/'
fi
"@

$lines = Invoke-Remote -Command $discoveryScript
$candidates = @()
foreach ($line in $lines) {
    if ($line -notmatch '^(docker|systemd)\|') {
        continue
    }

    $parts = $line -split '\|', 3
    if ($parts.Count -lt 3) {
        continue
    }

    $name = $parts[1]
    if ($name -match '(?i)world|sunshine') {
        $candidates += [pscustomobject]@{
            Kind = $parts[0]
            Name = $name
            Meta = $parts[2]
        }
    }
}

if ($candidates.Count -eq 0) {
    throw "No se detecto ningun candidate world/sunshine por SSH."
}

$selected = $candidates | Where-Object { $_.Name -match [regex]::Escape($WorldNameHint) } | Select-Object -First 1
if (-not $selected) {
    $selected = $candidates | Select-Object -First 1
}

Write-Output "Target detectado:"
Write-Output "  Kind: $($selected.Kind)"
Write-Output "  Name: $($selected.Name)"
Write-Output "  Meta: $($selected.Meta)"

if (-not $ConfirmRestart) {
    Write-Warning "Modo seguro: no se reinicio nada. Usa -ConfirmRestart para ejecutar el restart real."
    exit 0
}

if ($selected.Kind -eq "docker") {
    Invoke-Remote -Command "docker restart $($selected.Name) && docker logs --tail $Tail $($selected.Name)"
}
elseif ($selected.Kind -eq "systemd") {
    Invoke-Remote -Command "systemctl restart $($selected.Name) && journalctl -u $($selected.Name) -n $Tail --no-pager"
}
else {
    throw "Tipo de runtime no soportado: $($selected.Kind)"
}
