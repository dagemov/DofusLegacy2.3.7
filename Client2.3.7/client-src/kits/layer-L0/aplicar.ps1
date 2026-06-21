#Requires -Version 5.1
<#
.SYNOPSIS
  Copia el kit L0 completo al cliente destino (tienda + fix precio sin tocar TradeCenter baseline).
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot
)

$ErrorActionPreference = "Stop"
$KitRoot = Split-Path $PSScriptRoot -Parent
$RepoRoot = Split-Path $KitRoot -Parent
$GameRoot = (Resolve-Path $GameRoot).Path
$preflight = Join-Path $RepoRoot "client-src\preflight-client.ps1"

Write-Host "Kit L0 -> $GameRoot" -ForegroundColor Cyan

Copy-Item (Join-Path $KitRoot "DofusInvoker.swf") (Join-Path $GameRoot "DofusInvoker.swf") -Force
Copy-Item (Join-Path $KitRoot "ui\Ankama_TradeCenter\*") (Join-Path $GameRoot "ui\Ankama_TradeCenter\") -Recurse -Force
Copy-Item (Join-Path $KitRoot "data\Launcher\VerInfo.rec") (Join-Path $GameRoot "data\Launcher\VerInfo.rec") -Force

Write-Host "Copiado. Ejecutando preflight L0..." -ForegroundColor Cyan
& $preflight -GameRoot $GameRoot -Layer L0
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nListo. Prueba .tienda 1 en juego.`n" -ForegroundColor Green
