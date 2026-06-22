#Requires -Version 5.1
<#
.SYNOPSIS
  Parchea DofusInvoker.swf (solo ExchangeManagementFrame) para sync precio tienda NPC.
  No toca TradeCenter.swf.
#>
param(
    [string]$GameRoot = (Join-Path (Split-Path $PSScriptRoot -Parent) ""),

    [switch]$SkipPreflight
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$GameRoot = (Resolve-Path $GameRoot).Path

$ffdecJar = Join-Path $RepoRoot "ffdec\ffdec.jar"
$baselineInvoker = Join-Path $RepoRoot "client-src\kits\layer-L0\DofusInvoker.swf.baseline-DFFED0C8"
$patchAs = Join-Path $RepoRoot "client-src\patches\com\ankamagames\dofus\logic\game\common\frames\ExchangeManagementFrame.as"
$className = "com.ankamagames.dofus.logic.game.common.frames.ExchangeManagementFrame"
$kitInvoker = Join-Path $RepoRoot "client-src\kits\layer-L0\DofusInvoker.swf"
$outInvoker = Join-Path $GameRoot "DofusInvoker.swf"
$kitVerInfo = Join-Path $RepoRoot "client-src\kits\layer-L0\data\Launcher\VerInfo.rec"
$verInfo = Join-Path $GameRoot "data\Launcher\VerInfo.rec"

function Write-Step([string]$Msg) { Write-Host "`n>> $Msg" -ForegroundColor Cyan }

function Update-VerInfoEntry([string]$Path, [string]$RelForward, [string]$FilePath, [switch]$LeadingSlash) {
    if (-not (Test-Path $Path)) { return }
    $md5 = (Get-FileHash $FilePath -Algorithm MD5).Hash.ToLowerInvariant()
    $size = (Get-Item $FilePath).Length
    $needle = ($RelForward -replace '\\', '/').TrimStart('/')
    $prefix = if ($LeadingSlash) { "/$needle" } else { $needle }
    $lines = Get-Content $Path
    $updated = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match [regex]::Escape($needle) -and $line -match ',[0-9a-f]{32},') {
            if ($LeadingSlash -and -not $line.StartsWith('/')) { continue }
            if (-not $LeadingSlash -and $line.StartsWith('/')) { continue }
            $lines[$i] = "$prefix,$md5,$size"
            $updated = $true
        }
    }
    if (-not $updated) { $lines += "$prefix,$md5,$size" }
    Set-Content -Path $Path -Value $lines -Encoding UTF8
    Write-Host "VerInfo: $prefix -> $md5 ($size bytes)"
}

if (-not (Test-Path $ffdecJar)) { throw "Missing ffdec: $ffdecJar" }
if (-not (Test-Path $baselineInvoker)) {
    $src = Join-Path $RepoRoot "client-src\kits\layer-L0\DofusInvoker.swf"
    if (-not (Test-Path $src)) { throw "Missing baseline invoker backup" }
    Copy-Item $src $baselineInvoker -Force
}
if (-not (Test-Path $patchAs)) { throw "Missing patch: $patchAs" }

Write-Step "FFDec -replace $className"
$tmpOut = "$kitInvoker.build"
if (Test-Path $tmpOut) { Remove-Item $tmpOut -Force }
& java -jar $ffdecJar -replace $baselineInvoker $tmpOut $className $patchAs
if ($LASTEXITCODE -ne 0) { throw "FFDec -replace failed ($LASTEXITCODE)" }

Copy-Item $tmpOut $kitInvoker -Force
Copy-Item $tmpOut $outInvoker -Force
Remove-Item $tmpOut -Force

$newMd5 = (Get-FileHash $kitInvoker -Algorithm MD5).Hash
Write-Host "DofusInvoker.swf MD5: $newMd5  size: $((Get-Item $kitInvoker).Length)"

Update-VerInfoEntry $kitVerInfo "DofusInvoker.swf" $kitInvoker -LeadingSlash
Update-VerInfoEntry $verInfo "DofusInvoker.swf" $outInvoker -LeadingSlash

if (-not $SkipPreflight) {
    Write-Step "Preflight L0"
    & (Join-Path $PSScriptRoot "preflight-client.ps1") -GameRoot $GameRoot -Layer L0
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "`nInvoker patch OK. Kit: client-src/kits/layer-L0/`n" -ForegroundColor Green
