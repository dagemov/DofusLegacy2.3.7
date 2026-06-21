#Requires -Version 5.1
<#
.SYNOPSIS
  Restaura TradeCenter baseline L0 en el kit (sin recompilar TradeCenter).
#>
param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path $RepoRoot).Path
$kitTc = Join-Path $RepoRoot "client-src\kits\layer-L0\ui\Ankama_TradeCenter\TradeCenter.swf"
$baselineTc = Join-Path $RepoRoot "ui\Ankama_TradeCenter\TradeCenter.swf.bak-pre-L0"
$kitVerInfo = Join-Path $RepoRoot "client-src\kits\layer-L0\data\Launcher\VerInfo.rec"
$gameVerInfo = Join-Path $RepoRoot "data\Launcher\VerInfo.rec"

if (-not (Test-Path $baselineTc)) { throw "Missing baseline TradeCenter: $baselineTc" }

Copy-Item $baselineTc $kitTc -Force
Copy-Item $baselineTc (Join-Path $RepoRoot "ui\Ankama_TradeCenter\TradeCenter.swf") -Force

function Update-VerInfoEntry([string]$Path, [string]$RelForward, [string]$FilePath) {
    if (-not (Test-Path $Path)) { return }
    $md5 = (Get-FileHash $FilePath -Algorithm MD5).Hash.ToLowerInvariant()
    $size = (Get-Item $FilePath).Length
    $needle = $RelForward -replace '\\', '/'
    $lines = Get-Content $Path
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -like "$needle,*" -or $lines[$i] -like "ui/$needle,*" -or $lines[$i] -like "ui\$needle,*") {
            $prefix = if ($lines[$i].StartsWith("ui/") -or $lines[$i].StartsWith("ui\")) { "ui/Ankama_TradeCenter/TradeCenter.swf" } else { $needle }
            if ($lines[$i] -match '^ui[/\\]Ankama_TradeCenter[/\\]TradeCenter\.swf,') {
                $lines[$i] = "ui/Ankama_TradeCenter/TradeCenter.swf,$md5,$size"
            }
            break
        }
    }
    Set-Content -Path $Path -Value $lines -Encoding UTF8
}

foreach ($rel in @("ui/Ankama_TradeCenter/TradeCenter.swf")) {
    Update-VerInfoEntry $kitVerInfo $rel $kitTc
    Update-VerInfoEntry $gameVerInfo $rel $kitTc
}

$md5 = (Get-FileHash $kitTc -Algorithm MD5).Hash.ToUpperInvariant()
Write-Host "TradeCenter baseline restored: $md5 ($((Get-Item $kitTc).Length) bytes)"
