[CmdletBinding()]
param(
    [ValidateSet("start", "stop", "status")]
    [string]$Mode = "status",
    [string]$VpsHost = "174.138.35.107",
    [string]$ListenAddress = "127.0.0.1",
    [int]$AuthPort = 0,
    [int]$WorldPort = 0,
    [string]$EnvPath,
    [string]$FallbackConfigPath,
    [string]$StatePath,
    [string]$NodePath
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $EnvPath) {
    $EnvPath = Join-Path $repoRoot ".env"
}

if (-not $FallbackConfigPath) {
    $FallbackConfigPath = Join-Path $env:USERPROFILE "Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0\Config.xml"
}

if (-not $StatePath) {
    $StatePath = Join-Path $env:TEMP "sunshine-vps-tunnel.state.json"
}

function Read-KeyValueFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $values = @{}
    foreach ($line in Get-Content -Path $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed -split "=", 2
        if ($parts.Count -ne 2) {
            continue
        }

        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        if ($key) {
            $values[$key] = $value
        }
    }

    return $values
}

function Resolve-TunnelPorts {
    if ($AuthPort -gt 0 -and $WorldPort -gt 0) {
        return @{
            AuthPort = $AuthPort
            WorldPort = $WorldPort
        }
    }

    if (Test-Path $EnvPath) {
        $envValues = Read-KeyValueFile -Path $EnvPath
        return @{
            AuthPort = [int]$envValues["AUTH_PORT"]
            WorldPort = [int]$envValues["WORLD_PORT"]
        }
    }

    if (Test-Path $FallbackConfigPath) {
        $configValues = Read-KeyValueFile -Path $FallbackConfigPath
        return @{
            AuthPort = [int]$configValues["AuthPort"]
            WorldPort = [int]$configValues["WorldPort"]
        }
    }

    throw "Unable to resolve auth/world ports from .env or fallback config."
}

function Test-TcpPort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetHost,
        [Parameter(Mandatory = $true)]
        [int]$Port,
        [int]$TimeoutMs = 2000
    )

    try {
        $client = [System.Net.Sockets.TcpClient]::new()
        $asyncResult = $client.BeginConnect($TargetHost, $Port, $null, $null)
        $connected = $asyncResult.AsyncWaitHandle.WaitOne($TimeoutMs, $false)
        if (-not $connected -or -not $client.Connected) {
            return $false
        }

        $client.EndConnect($asyncResult) | Out-Null
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($client) {
            $client.Dispose()
        }
    }
}

function Test-CanListen {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ListenHost,
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse($ListenHost), $Port)
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($listener) {
            $listener.Stop()
        }
    }
}

function Read-State {
    if (-not (Test-Path $StatePath)) {
        return $null
    }

    try {
        return Get-Content -Path $StatePath -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Remove-State {
    if (Test-Path $StatePath) {
        Remove-Item -Path $StatePath -Force
    }
}

function Stop-RunningTunnel {
    $state = Read-State
    if (-not $state) {
        return $false
    }

    $process = Get-Process -Id $state.Pid -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $state.Pid -Force
    }

    Remove-State
    return $true
}

function Resolve-NodeBinary {
    if ($NodePath -and (Test-Path $NodePath)) {
        return $NodePath
    }

    $nodeCommand = Get-Command node.exe -ErrorAction SilentlyContinue
    if ($nodeCommand) {
        return $nodeCommand.Source
    }

    $codexNode = Join-Path $env:USERPROFILE ".cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
    if (Test-Path $codexNode) {
        return $codexNode
    }

    throw "Unable to locate Node.js. Pass -NodePath explicitly if needed."
}

switch ($Mode) {
    "start" {
        $portState = Resolve-TunnelPorts
        $AuthPort = $portState.AuthPort
        $WorldPort = $portState.WorldPort

        $existingState = Read-State
        if ($existingState) {
            $existingProcess = Get-Process -Id $existingState.Pid -ErrorAction SilentlyContinue
            $sameConfig = $existingState.VpsHost -eq $VpsHost -and
                [int]$existingState.AuthPort -eq $AuthPort -and
                [int]$existingState.WorldPort -eq $WorldPort

            if ($existingProcess -and $sameConfig) {
                Write-Output "Tunnel already running. PID=$($existingState.Pid) AUTH=$AuthPort WORLD=$WorldPort."
                break
            }

            Stop-RunningTunnel | Out-Null
        }

        foreach ($port in @($AuthPort, $WorldPort)) {
            if (-not (Test-TcpPort -TargetHost $VpsHost -Port $port)) {
                throw "Remote port $VpsHost`:$port is closed. Tunnel start aborted."
            }

            if (-not (Test-CanListen -ListenHost $ListenAddress -Port $port)) {
                throw "Local port $ListenAddress`:$port is already in use."
            }
        }

        $resolvedNodePath = Resolve-NodeBinary
        $workerScript = Join-Path $PSScriptRoot "vps-tunnel-worker.js"
        if (-not (Test-Path $workerScript)) {
            throw "Missing worker script at $workerScript."
        }

        $arguments = @(
            $workerScript,
            "--listen-address", $ListenAddress,
            "--vps-host", $VpsHost,
            "--auth-port", "$AuthPort",
            "--world-port", "$WorldPort"
        )

        $process = Start-Process -FilePath $resolvedNodePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
        Start-Sleep -Seconds 2

        foreach ($port in @($AuthPort, $WorldPort)) {
            if (-not (Test-TcpPort -TargetHost $ListenAddress -Port $port)) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                throw "Tunnel worker started but local port $ListenAddress`:$port did not open."
            }
        }

        $state = [ordered]@{
            Pid = $process.Id
            NodePath = $resolvedNodePath
            VpsHost = $VpsHost
            ListenAddress = $ListenAddress
            AuthPort = $AuthPort
            WorldPort = $WorldPort
            StartedAt = (Get-Date).ToString("o")
        }

        $state | ConvertTo-Json | Set-Content -Path $StatePath -Encoding UTF8
        Write-Output "Tunnel started. PID=$($process.Id) AUTH=$AuthPort WORLD=$WorldPort HOST=$VpsHost."
    }

    "stop" {
        if (Stop-RunningTunnel) {
            Write-Output "Tunnel stopped."
        }
        else {
            Write-Output "Tunnel was not running."
        }
    }

    "status" {
        $state = Read-State
        if (-not $state) {
            Write-Output "Tunnel is not running."
            break
        }

        $process = Get-Process -Id $state.Pid -ErrorAction SilentlyContinue
        if (-not $process) {
            Remove-State
            Write-Output "Tunnel state existed but the process was gone."
            break
        }

        Write-Output "Tunnel running. PID=$($state.Pid) AUTH=$($state.AuthPort) WORLD=$($state.WorldPort) HOST=$($state.VpsHost)."
    }
}
