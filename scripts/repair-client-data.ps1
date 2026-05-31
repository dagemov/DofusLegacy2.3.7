[CmdletBinding()]
param(
    [string]$ClientPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "Client2.3.7"),
    [string]$PatchBase = "https://sunshine-dofus.com/Uplauncher/patchfiles"
)

function Expand-GzipFile {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 2 -or $bytes[0] -ne 0x1f -or $bytes[1] -ne 0x8b) {
        return $false
    }
    $input = New-Object System.IO.MemoryStream(,$bytes)
    $gzip = New-Object System.IO.Compression.GZipStream($input, [System.IO.Compression.CompressionMode]::Decompress)
    $output = New-Object System.IO.MemoryStream
    $gzip.CopyTo($output)
    [System.IO.File]::WriteAllBytes($Path, $output.ToArray())
    return $true
}

$files = @(
    "data/common/data.meta",
    "data/i18n/data.meta"
)

foreach ($rel in $files) {
    $dest = Join-Path $ClientPath ($rel -replace '/', '\')
    $dir = Split-Path $dest -Parent
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $url = "$PatchBase/$rel"
    Write-Output "Downloading $url ..."
    Invoke-WebRequest -Uri $url -OutFile $dest
    if (Expand-GzipFile -Path $dest) {
        Write-Output "  Decompressed gzip -> $dest"
    }
    else {
        Write-Output "  Saved as-is -> $dest"
    }
}

Write-Output "Done. Restart Dofus.exe from $ClientPath"
