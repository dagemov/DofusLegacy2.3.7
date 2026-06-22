#Requires -Version 5.1
<#
.SYNOPSIS
  Verifica integridad del cliente Dofus antes de probar tiendas NPC / .tiendas.

.PARAMETER GameRoot
  Carpeta raíz del cliente (contiene DofusInvoker.swf, ui/, data/).

.PARAMETER Layer
  Capa del pipeline: L0 (unico soportado).

.PARAMETER RepoRoot
  Raíz del repo (default: padre de client-src).
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [ValidateSet("L0")]
    [string]$Layer = "L0",

    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
if (-not $RepoRoot) {
    $RepoRoot = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { Split-Path (Get-Location) -Parent }
}
$script:FailCount = 0
$script:WarnCount = 0

function Write-Ok([string]$Msg) { Write-Host "[OK]  $Msg" -ForegroundColor Green }
function Write-Fail([string]$Msg) {
    Write-Host "[FAIL] $Msg" -ForegroundColor Red
    $script:FailCount++
}
function Write-Warn([string]$Msg) {
    Write-Host "[WARN] $Msg" -ForegroundColor Yellow
    $script:WarnCount++
}

function Get-FileMd5([string]$Path) {
    if (-not (Test-Path $Path)) { return $null }
    return (Get-FileHash -Algorithm MD5 -Path $Path).Hash.ToUpperInvariant()
}

function Test-ManifestFile([string]$Root, [object]$Entry) {
    $rel = $Entry.path -replace '/', '\'
    $full = Join-Path $Root $rel
    if (-not (Test-Path $full)) {
        Write-Fail "Missing: $rel"
        return
    }
    $md5 = Get-FileMd5 $full
    if ($Entry.md5 -and $md5 -ne $Entry.md5.ToUpperInvariant()) {
        Write-Fail "$rel MD5 mismatch (disk=$md5 expected=$($Entry.md5))"
        return
    }
    if ($Entry.size) {
        $len = (Get-Item $full).Length
        if ($len -ne [int]$Entry.size) {
            Write-Warn "$rel size $len (manifest $($Entry.size))"
        }
    }
    Write-Ok "$rel"
}

function Test-DmCoherence([string]$DmPath, [string]$ModuleRoot) {
    if (-not (Test-Path $DmPath)) {
        Write-Fail "Missing .dm: $DmPath"
        return
    }
    [xml]$dm = Get-Content $DmPath -Encoding UTF8
    foreach ($ui in $dm.module.uis.ui) {
        $file = [string]$ui.file
        $xmlPath = Join-Path $ModuleRoot ($file -replace '/', '\')
        if (-not (Test-Path $xmlPath)) {
            Write-Fail ".dm references missing XML: $file"
        }
    }
    Write-Ok ".dm XML references"
}

function Test-DmPatterns([string]$DmPath, [string[]]$MustContain, [string[]]$MustNotContain) {
    $text = Get-Content $DmPath -Raw
    foreach ($p in $MustContain) {
        if ($text -notmatch [regex]::Escape($p)) {
            Write-Fail ".dm must contain: $p"
        } else {
            Write-Ok ".dm contains $p"
        }
    }
    foreach ($p in $MustNotContain) {
        if ($text -match [regex]::Escape($p)) {
            Write-Fail ".dm must NOT contain: $p"
        } else {
            Write-Ok ".dm excludes $p"
        }
    }
}

function Test-TradeCenterSwf([string]$SwfPath, [object]$Checks, [string]$SourceDir) {
    if (-not (Test-Path $SwfPath)) {
        Write-Fail "Missing TradeCenter.swf"
        return
    }

    $ffdecJar = Join-Path $RepoRoot "ffdec\ffdec.jar"
    $exportDir = Join-Path $env:TEMP ("tc_preflight_" + [guid]::NewGuid().ToString("N"))
    $exported = $false

    if ((Test-Path $ffdecJar) -and $Checks) {
        New-Item -ItemType Directory -Force -Path $exportDir | Out-Null
        $java = Get-Command java -ErrorAction SilentlyContinue
        if ($java) {
            & java -jar $ffdecJar -export script $exportDir $SwfPath 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) { $exported = $true }
        } else {
            Write-Warn "java not in PATH - skipping FFDec export checks"
        }
    }

    if ($exported) {
        $estateForm = Get-ChildItem -Path $exportDir -Recurse -Filter "EstateForm.as" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($estateForm) {
            $efText = Get-Content $estateForm.FullName -Raw
            if ($Checks.estateFormMustNotImport -and $efText -match [regex]::Escape($Checks.estateFormMustNotImport)) {
                Write-Fail "ui.EstateForm imports $($Checks.estateFormMustNotImport) - module will not load"
            } else {
                Write-Ok "ui.EstateForm clean (no $($Checks.estateFormMustNotImport))"
            }
        }
        foreach ($cls in $Checks.requireClasses) {
            $found = Get-ChildItem -Path $exportDir -Recurse -Filter ($cls.Split('.')[-1] + ".as") -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match [regex]::Escape($cls.Replace('.', '\')) -or $_.DirectoryName -match [regex]::Escape($cls.Replace('.', '\')) }
            if (-not $found) {
                $alt = Get-ChildItem -Path $exportDir -Recurse -Filter "TradeCenter.as" -ErrorAction SilentlyContinue
                if ($cls -eq "TradeCenter" -and $alt) { Write-Ok "class TradeCenter" }
                elseif ($cls -eq "ui.StockNpcStore") {
                    $sn = Get-ChildItem -Path $exportDir -Recurse -Filter "StockNpcStore.as" -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($sn) { Write-Ok "class ui.StockNpcStore" } else { Write-Fail "Missing class $cls in SWF export" }
                }
                else { Write-Fail "Missing class $cls in SWF export" }
            } else {
                Write-Ok "class $cls"
            }
        }
        Remove-Item -Recurse -Force $exportDir -ErrorAction SilentlyContinue
    }

    $srcRoot = Join-Path $RepoRoot "client-src\tradecenter\scripts\scripts"
    if ((Test-Path $srcRoot) -and $Checks.requireSourcePatterns) {
        $tc = Join-Path $srcRoot "TradeCenter.as"
        $sn = Join-Path $srcRoot "ui\StockNpcStore.as"
        $blob = ""
        if (Test-Path $tc) { $blob += Get-Content $tc -Raw }
        if (Test-Path $sn) { $blob += Get-Content $sn -Raw }
        foreach ($pat in $Checks.requireSourcePatterns) {
            if ($blob -notmatch [regex]::Escape($pat)) {
                Write-Fail "Source missing pattern: $pat (layer $Layer)"
            } else {
                Write-Ok "Source contains $pat"
            }
        }
        if ($Checks.mustNotContain) {
            foreach ($pat in $Checks.mustNotContain) {
                if ($blob -match [regex]::Escape($pat)) {
                    Write-Fail "Source must not contain: $pat"
                } else {
                    Write-Ok "Source excludes $pat"
                }
            }
        }
    }
}

function Get-MergedManifest([string]$LayerName) {
    $path = Join-Path $PSScriptRoot "manifests\layer-$LayerName.json"
    if (-not (Test-Path $path)) { return $null }
    $m = Get-Content $path -Raw | ConvertFrom-Json
    if ($m.extends) {
        $parent = Get-MergedManifest $m.extends
        if ($parent) {
            if ($parent.files -and -not $m.files) { $m | Add-Member -NotePropertyName files -NotePropertyValue @() -Force }
            if ($parent.files) {
                $m.files = @($parent.files) + @($m.files)
            }
            if ($parent.dmMustNotContain -and -not $m.dmMustNotContain) {
                $m | Add-Member -NotePropertyName dmMustNotContain -NotePropertyValue $parent.dmMustNotContain -Force
            }
            if ($parent.tradeCenterChecks -and -not $m.tradeCenterChecks) {
                $m | Add-Member -NotePropertyName tradeCenterChecks -NotePropertyValue $parent.tradeCenterChecks -Force
            }
        }
    }
    return $m
}

# --- main ---
$GameRoot = (Resolve-Path $GameRoot).Path
$manifestPath = Join-Path $PSScriptRoot "manifests\layer-$Layer.json"
if (-not (Test-Path $manifestPath)) {
    Write-Fail "Manifest not found: $manifestPath"
    exit 1
}

$manifest = Get-MergedManifest $Layer
$leafPath = Join-Path $PSScriptRoot "manifests\layer-$Layer.json"
$leaf = Get-Content $leafPath -Raw | ConvertFrom-Json
if ($leaf.tradeCenterChecks) {
    $manifest.tradeCenterChecks = $leaf.tradeCenterChecks
    $l0Path = Join-Path $PSScriptRoot "manifests\layer-L0.json"
    if (Test-Path $l0Path) {
        $l0 = Get-Content $l0Path -Raw | ConvertFrom-Json
        if ($l0.tradeCenterChecks.estateFormMustNotImport -and -not $manifest.tradeCenterChecks.estateFormMustNotImport) {
            $manifest.tradeCenterChecks | Add-Member -NotePropertyName estateFormMustNotImport -NotePropertyValue $l0.tradeCenterChecks.estateFormMustNotImport -Force
        }
        if ($l0.tradeCenterChecks.requireClasses -and -not $manifest.tradeCenterChecks.requireClasses) {
            $manifest.tradeCenterChecks | Add-Member -NotePropertyName requireClasses -NotePropertyValue $l0.tradeCenterChecks.requireClasses -Force
        }
    }
}
Write-Host "`n=== Preflight layer $Layer ===" -ForegroundColor Cyan
Write-Host "GameRoot: $GameRoot`n"

foreach ($entry in $manifest.files) {
    if ($entry.md5) {
        Test-ManifestFile $GameRoot $entry
    }
}

$tcModule = Join-Path $GameRoot "ui\Ankama_TradeCenter"
$dmPath = Join-Path $tcModule "Ankama_TradeCenter.dm"
$swfPath = Join-Path $tcModule "TradeCenter.swf"

Test-DmCoherence $dmPath $tcModule

if ($manifest.dmMustNotContain) {
    Test-DmPatterns $dmPath @() $manifest.dmMustNotContain
}
if ($manifest.dmMustContain) {
    Test-DmPatterns $dmPath $manifest.dmMustContain @()
}

if ($manifest.tradeCenterChecks) {
    Test-TradeCenterSwf $swfPath $manifest.tradeCenterChecks (Join-Path $RepoRoot "client-src\tradecenter\scripts\scripts")
}

$hashMetas = Join-Path $GameRoot "ui\hash.metas"
if (Test-Path $hashMetas) {
    Write-Ok "ui/hash.metas present"
} else {
    Write-Warn "ui/hash.metas missing - update after SWF changes if launcher validates modules"
}

$verInfo = Join-Path $GameRoot "data\Launcher\VerInfo.rec"
if (Test-Path $verInfo) {
    Write-Ok "VerInfo.rec present"
} else {
    Write-Warn "VerInfo.rec missing"
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Failures: $script:FailCount  Warnings: $script:WarnCount"
if ($script:FailCount -gt 0) {
    Write-Host "Result: FAIL - fix before testing in game`n" -ForegroundColor Red
    exit 1
}
Write-Host "Result: OK - safe to test in game`n" -ForegroundColor Green
exit 0
