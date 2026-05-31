[CmdletBinding()]
param(
    [string]$RuntimePath
)

if (-not $RuntimePath) {
    $RuntimePath = Join-Path $PSScriptRoot "..\\runtime"
}

$requiredFiles = @(
    (Join-Path $RuntimePath "maps\\maps0.d2p"),
    (Join-Path $RuntimePath "maps\\elements.ele"),
    (Join-Path $RuntimePath "d2os\\Breeds.d2o")
)

$missing = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missing += $file
    }
}

if ($missing.Count -gt 0) {
    Write-Host "[FAIL] Missing runtime files:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "[PASS] Runtime payload looks complete for Docker portability." -ForegroundColor Green
