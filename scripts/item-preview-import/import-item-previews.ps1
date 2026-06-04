[CmdletBinding()]
param(
    [string]$SourceRoot = "C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\client\app\content\gfx\items",
    [string]$DestinationRoot = "",
    [string[]]$Categories = @("amuletos_png"),
    [int[]]$IncludeIconIds = @(1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012),
    [switch]$Apply,
    [switch]$ForceOverwrite,
    [string]$ReportPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if (-not $DestinationRoot) {
    $DestinationRoot = Join-Path $repoRoot "Angular-tools\Admin\RollblackLegacy.Admin.Angular\src\assets\item-previews\by-icon"
}

$approvedCategories = @{
    "amuletos_png" = "Aprobado"
    "sombreros" = "Aprobado"
    "capas" = "Aprobado"
    "botas" = "Aprobado"
    "Dofus" = "Aprobado"
    "mascotas" = "Aprobado"
}

$blockedCategories = @(
    "weapons",
    "weapon",
    "armas",
    "anillos",
    "cinturones"
)

function Test-BlockedCategory {
    param([string]$Category)

    return $blockedCategories -contains $Category
}

function Get-ReportLines {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    $lines = @(
        "# Item Preview Import Report",
        "",
        "Date: $(Get-Date -Format s)",
        "Mode: $(if ($Apply) { 'APPLY' } else { 'DRY_RUN' })",
        "SourceRoot: $SourceRoot",
        "DestinationRoot: $DestinationRoot",
        "Categories: $($Categories -join ', ')",
        "IncludeIconIds: $($IncludeIconIds -join ', ')",
        ""
    )

    foreach ($row in $Rows) {
        $lines += "- [$($row.Status)] IconId=$($row.IconId) Category=$($row.Category) Source=$($row.SourcePath) Destination=$($row.DestinationPath)"
    }

    return $lines
}

if (-not (Test-Path $SourceRoot)) {
    throw "SourceRoot no existe: $SourceRoot"
}

if (-not (Test-Path $DestinationRoot)) {
    throw "DestinationRoot no existe: $DestinationRoot"
}

$rows = New-Object System.Collections.Generic.List[object]

foreach ($category in $Categories) {
    if (Test-BlockedCategory -Category $category) {
        throw "La categoria '$category' esta bloqueada en esta fase."
    }

    if (-not $approvedCategories.ContainsKey($category)) {
        throw "La categoria '$category' no esta aprobada. Usa solo categorias curadas."
    }

    $categoryPath = Join-Path $SourceRoot $category
    if (-not (Test-Path $categoryPath)) {
        throw "La categoria '$category' no existe en $categoryPath"
    }

    $files = Get-ChildItem -Path $categoryPath -Filter *.png -File | Sort-Object Name
    foreach ($file in $files) {
        $iconId = 0
        if (-not [int]::TryParse([System.IO.Path]::GetFileNameWithoutExtension($file.Name), [ref]$iconId)) {
            continue
        }

        if ($IncludeIconIds.Count -gt 0 -and $IncludeIconIds -notcontains $iconId) {
            continue
        }

        $destinationPath = Join-Path $DestinationRoot $file.Name
        $status = "WOULD_COPY"

        if (Test-Path $destinationPath) {
            $status = if ($ForceOverwrite) { "WOULD_OVERWRITE" } else { "SKIP_EXISTS" }
        }

        if ($Apply) {
            if ($status -eq "SKIP_EXISTS") {
                $rows.Add([pscustomobject]@{
                    IconId = $iconId
                    Category = $category
                    Status = $status
                    SourcePath = $file.FullName
                    DestinationPath = $destinationPath
                }) | Out-Null
                continue
            }

            Copy-Item -LiteralPath $file.FullName -Destination $destinationPath -Force:$ForceOverwrite.IsPresent
            $status = if (Test-Path $destinationPath) { "COPIED" } else { "FAILED" }
        }

        $rows.Add([pscustomobject]@{
            IconId = $iconId
            Category = $category
            Status = $status
            SourcePath = $file.FullName
            DestinationPath = $destinationPath
        }) | Out-Null
    }
}

$lines = Get-ReportLines -Rows $rows
if (-not $ReportPath) {
    $ReportPath = Join-Path $repoRoot "docs\admin-tools\items-builder\reports\item-preview-import-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
}

$reportDirectory = Split-Path -Parent $ReportPath
if (-not (Test-Path $reportDirectory)) {
    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
}

$lines | Set-Content -Path $ReportPath -Encoding UTF8
$rows | Format-Table -AutoSize
Write-Output ""
Write-Output "Reporte generado en: $ReportPath"
