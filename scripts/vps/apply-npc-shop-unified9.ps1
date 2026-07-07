#Requires -Version 5.1
<#
.SYNOPSIS
  Applies npc-shop-unified9-apply.sql (prices + catalog) on VPS sunshine-db.
#>
param(
    [string]$VpsHost = "34.46.208.124"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$key = Join-Path $repoRoot "SSH\private_key_sebas.pem"
$sqlFile = Join-Path $repoRoot "database\patches\npc-shop-unified9-apply.sql"
$remoteSql = "/tmp/npc-shop-unified9-apply.sql"

if (-not (Test-Path $key)) { throw "SSH key not found: $key" }
if (-not (Test-Path $sqlFile)) { throw "SQL patch not found: $sqlFile" }

Write-Host "Applying unified9 shop prices on VPS..." -ForegroundColor Cyan
scp -O -i $key $sqlFile "root@${VpsHost}:${remoteSql}"
ssh -i $key -o BatchMode=yes -o StrictHostKeyChecking=accept-new "root@$VpsHost" "docker cp ${remoteSql} sunshine-db:/tmp/npc-shop-unified9-apply.sql && docker exec sunshine-db bash -c 'mariadb -uroot -pchange-me-root sunshine < /tmp/npc-shop-unified9-apply.sql' && docker exec sunshine-db mariadb -uroot -pchange-me-root sunshine -N -e 'SELECT ni.NpcId, ni.Item, ni.Price FROM npcs_items ni WHERE ni.Item=12116' && docker restart sunshine-server"

Write-Host "Done. Restart server triggered; verify in-game prices after relog." -ForegroundColor Green
