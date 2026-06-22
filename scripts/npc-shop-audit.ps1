# NPC shop audit: export catalogs, economy proposal, distribution plan, SQL patches.
# Usage:
#   .\scripts\npc-shop-audit.ps1
#   .\scripts\npc-shop-audit.ps1 -Source db
#   .\scripts\npc-shop-audit.ps1 -Source sql

param(
    [ValidateSet("auto", "db", "sql")]
    [string]$Source = "auto"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$script = Join-Path $repoRoot "tools\npc-shop-audit\run_npc_shop_audit.py"

if (-not (Test-Path $script)) {
    throw "Missing audit script: $script"
}

Push-Location $repoRoot
try {
    python $script --source $Source
    if ($LASTEXITCODE -ne 0) {
        throw "npc-shop-audit failed with exit code $LASTEXITCODE"
    }
    Write-Host ""
    Write-Host "Outputs:" -ForegroundColor Green
    Write-Host "  tools/npc-shop-audit/npc-shops-full.json"
    Write-Host "  tools/npc-shop-audit/items-by-category.json"
    Write-Host "  tools/npc-shop-audit/economy-proposal.json"
    Write-Host "  tools/npc-shop-audit/npc-distribution-plan.json"
    Write-Host "  tools/npc-shop-audit/npc-lag-report.md"
    Write-Host "  docs/npc-shop-distribution.md"
    Write-Host "  database/patches/npc-shop-redistribute-apply.sql"
}
finally {
    Pop-Location
}
