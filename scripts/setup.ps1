[CmdletBinding()]
param(
    [ValidateSet("local", "vps")]
    [string]$Mode = "local",
    [switch]$Build
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$composeDir = Join-Path $repoRoot "docker"
$envPath = Join-Path $repoRoot ".env"

if (-not (Test-Path $envPath)) {
    throw "Missing $envPath. Copy .env.example to .env and fill in the operator values first."
}

& (Join-Path $PSScriptRoot "validate-torrent.ps1")

Push-Location $composeDir
try {
    $composeFiles = @("--env-file", $envPath, "-f", "docker-compose.yml", "-f", "docker-compose.$Mode.yml")
    & docker compose @composeFiles config
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose config failed."
    }

    $upArgs = @("compose") + $composeFiles + @("up", "-d")
    if ($Build) {
        $upArgs += "--build"
    }

    & docker @upArgs
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up failed."
    }
}
finally {
    Pop-Location
}
