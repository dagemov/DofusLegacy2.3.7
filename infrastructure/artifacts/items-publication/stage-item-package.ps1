#Requires -Version 5.1
<#
.SYNOPSIS
  Genera y valida package de publicación para uno o más ItemIds.
.EXAMPLE
  .\stage-item-package.ps1 -ItemIds 12620,12621,12622 -TemplateItemId 7754
#>
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    [Parameter(Mandatory = $true)]
    [int[]]$ItemIds,
    [int]$TemplateItemId = 7754,
    [string]$NameEs = "",
    [string]$NameEn = "",
    [string]$DescriptionEs = "",
    [string]$DescriptionEn = ""
)

$ErrorActionPreference = "Stop"
$pipelineProject = Join-Path $RepoRoot "Infrastructure\scripts\ClientItemPublicationPipeline\ClientItemPublicationPipeline.csproj"

if (-not (Test-Path $pipelineProject)) {
    throw "ClientItemPublicationPipeline no encontrado."
}

Push-Location $RepoRoot
try {
    foreach ($itemId in $ItemIds) {
        Write-Host "=== Item $itemId ==="
        $args = @(
            "run", "--project", $pipelineProject, "--",
            "dry-run", "--item-id", $itemId, "--template-item-id", $TemplateItemId
        )
        dotnet @args
        if ($LASTEXITCODE -ne 0) { throw "dry-run falló para $itemId" }

        $stageArgs = @(
            "run", "--project", $pipelineProject, "--",
            "stage-item-publication", "--item-id", $itemId, "--template-item-id", $TemplateItemId
        )
        if ($NameEs) { $stageArgs += @("--name-es", $NameEs) }
        if ($NameEn) { $stageArgs += @("--name-en", $NameEn) }
        dotnet @stageArgs
        if ($LASTEXITCODE -ne 0) { throw "stage-item-publication falló para $itemId" }

        dotnet run --project $pipelineProject -- validate-publication-package --item-id $itemId
        if ($LASTEXITCODE -ne 0) { throw "validate-publication-package falló para $itemId" }

        Write-Host "READY_FOR_CONTROLLED_PUBLISH: item $itemId"
        Write-Host "  Staging: Infrastructure/staging-client/publication-package-phase3c/$itemId/"
    }
}
finally {
    Pop-Location
}
