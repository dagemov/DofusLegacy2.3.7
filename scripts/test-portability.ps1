[CmdletBinding()]
param(
    [string]$EnvPath,
    [string]$ComposeDirectory,
    [string[]]$ComposeFiles = @("docker-compose.yml", "docker-compose.local.yml"),
    [switch]$IncludeRestartTest
)

if (-not $EnvPath) {
    $EnvPath = Join-Path $PSScriptRoot "..\\.env"
}

if (-not $ComposeDirectory) {
    $ComposeDirectory = Join-Path $PSScriptRoot "..\\docker"
}

function Read-EnvFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Env file not found: $Path"
    }

    $values = @{}
    foreach ($rawLine in Get-Content $Path) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -lt 1) {
            continue
        }

        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1).Trim()
        $values[$key] = $value
    }

    return $values
}

function Get-EnvValue {
    param(
        [hashtable]$Values,
        [string]$Key,
        [string]$DefaultValue
    )

    if ($Values.ContainsKey($Key) -and -not [string]::IsNullOrWhiteSpace($Values[$Key])) {
        return $Values[$Key]
    }

    return $DefaultValue
}

function Add-Result {
    param(
        [string]$Id,
        [string]$Scenario,
        [string]$Status,
        [string]$Detail,
        [bool]$Critical = $false
    )

    $script:Results.Add([pscustomobject]@{
        Id = $Id
        Scenario = $Scenario
        Status = $Status
        Detail = $Detail
        Critical = $Critical
    }) | Out-Null
}

function Invoke-DockerCapture {
    param([string[]]$Arguments)

    $output = & docker @Arguments 2>&1 | Out-String
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = $output.Trim()
    }
}

function Invoke-ComposeCapture {
    param([string[]]$Arguments)

    $allArguments = @("compose")
    foreach ($file in $ComposeFiles) {
        $allArguments += @("-f", $file)
    }
    $allArguments += $Arguments

    Push-Location $ComposeDirectory
    try {
        $output = & docker @allArguments 2>&1 | Out-String
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output = $output.Trim()
        }
    }
    finally {
        Pop-Location
    }
}

function Read-BigEndianInt {
    param(
        [byte[]]$Buffer,
        [int]$Offset
    )

    return [BitConverter]::ToInt32([byte[]]@(
        $Buffer[$Offset + 3],
        $Buffer[$Offset + 2],
        $Buffer[$Offset + 1],
        $Buffer[$Offset]
    ), 0)
}

function Read-Packet {
    param(
        [string]$Host,
        [int]$Port
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connect = $client.BeginConnect($Host, $Port, $null, $null)
        if (-not $connect.AsyncWaitHandle.WaitOne(5000)) {
            throw "Connection timeout"
        }

        $client.EndConnect($connect)
        $stream = $client.GetStream()
        $stream.ReadTimeout = 5000

        $header = New-Object byte[] 5
        $read = $stream.Read($header, 0, 2)
        if ($read -ne 2) {
            throw "Incomplete header"
        }

        $packetHeader = ($header[0] -shl 8) -bor $header[1]
        $lengthBytes = $packetHeader -band 0x03
        if ($lengthBytes -gt 0) {
            $read = $stream.Read($header, 2, $lengthBytes)
            if ($read -ne $lengthBytes) {
                throw "Incomplete length"
            }
        }

        $payloadLength = switch ($lengthBytes) {
            0 { 0 }
            1 { $header[2] }
            2 { ($header[2] -shl 8) -bor $header[3] }
            3 { ($header[2] -shl 16) -bor ($header[3] -shl 8) -bor $header[4] }
        }

        $payload = New-Object byte[] $payloadLength
        $offset = 0
        while ($offset -lt $payloadLength) {
            $count = $stream.Read($payload, $offset, $payloadLength - $offset)
            if ($count -le 0) {
                throw "Connection closed before full payload"
            }

            $offset += $count
        }

        return [pscustomobject]@{
            MessageId = ($packetHeader -shr 2)
            LengthBytes = $lengthBytes
            Payload = $payload
        }
    }
    finally {
        $client.Dispose()
    }
}

function Test-ProtocolHandshake {
    param(
        [string]$Host,
        [int]$Port,
        [int]$ExpectedVersion
    )

    $packet = Read-Packet -Host $Host -Port $Port
    if ($packet.MessageId -ne 1) {
        throw "Expected ProtocolRequired (1), got $($packet.MessageId)."
    }

    if ($packet.Payload.Length -lt 8) {
        throw "ProtocolRequired payload is too short."
    }

    $requiredVersion = Read-BigEndianInt -Buffer $packet.Payload -Offset 0
    $currentVersion = Read-BigEndianInt -Buffer $packet.Payload -Offset 4

    if ($requiredVersion -ne $ExpectedVersion -or $currentVersion -ne $ExpectedVersion) {
        throw "Expected protocol $ExpectedVersion, got required=$requiredVersion current=$currentVersion."
    }
}

function Test-TcpOpen {
    param(
        [string]$Host,
        [int]$Port
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connect = $client.BeginConnect($Host, $Port, $null, $null)
        if (-not $connect.AsyncWaitHandle.WaitOne(5000)) {
            throw "Connection timeout"
        }

        $client.EndConnect($connect)
    }
    finally {
        $client.Dispose()
    }
}

$envValues = Read-EnvFile -Path $EnvPath
$Results = [System.Collections.Generic.List[object]]::new()

$authPort = [int](Get-EnvValue -Values $envValues -Key "AUTH_PORT" -DefaultValue "2450")
$worldPort = [int](Get-EnvValue -Values $envValues -Key "WORLD_PORT" -DefaultValue "5557")
$mysqlPublishPort = [int](Get-EnvValue -Values $envValues -Key "MYSQL_PUBLISH_PORT" -DefaultValue "3306")
$protocolVersion = [int](Get-EnvValue -Values $envValues -Key "PROTOCOL_VERSION" -DefaultValue "1375")
$worldPublicHost = Get-EnvValue -Values $envValues -Key "WORLD_PUBLIC_HOST" -DefaultValue "127.0.0.1"
$remoteUser = Get-EnvValue -Values $envValues -Key "MYSQL_REMOTE_USER" -DefaultValue ""
$remotePassword = Get-EnvValue -Values $envValues -Key "MYSQL_REMOTE_PASSWORD" -DefaultValue ""
$mysqlDatabase = Get-EnvValue -Values $envValues -Key "MYSQL_DATABASE" -DefaultValue "sunshine"

try {
    Test-ProtocolHandshake -Host "127.0.0.1" -Port $authPort -ExpectedVersion $protocolVersion
    Add-Result -Id "T1" -Scenario "Auth local" -Status "PASS" -Detail "ProtocolRequired matched $protocolVersion on 127.0.0.1:$authPort." -Critical $true
}
catch {
    Add-Result -Id "T1" -Scenario "Auth local" -Status "FAIL" -Detail $_.Exception.Message -Critical $true
}

try {
    Test-TcpOpen -Host "127.0.0.1" -Port $worldPort
    Add-Result -Id "T2" -Scenario "World local" -Status "PASS" -Detail "TCP open on 127.0.0.1:$worldPort." -Critical $true
}
catch {
    Add-Result -Id "T2" -Scenario "World local" -Status "FAIL" -Detail $_.Exception.Message -Critical $true
}

if ([string]::IsNullOrWhiteSpace($remoteUser) -or [string]::IsNullOrWhiteSpace($remotePassword)) {
    Add-Result -Id "T3" -Scenario "MySQL host local" -Status "FAIL" -Detail "MYSQL_REMOTE_USER or MYSQL_REMOTE_PASSWORD is missing." -Critical $true
}
else {
    $t3 = Invoke-DockerCapture -Arguments @(
        "run", "--rm", "mariadb:10.11",
        "mariadb-admin",
        "--connect-timeout=5",
        "-h", "host.docker.internal",
        "-P", "$mysqlPublishPort",
        "-u", $remoteUser,
        "-p$remotePassword",
        "ping",
        "--silent"
    )

    if ($t3.ExitCode -eq 0) {
        Add-Result -Id "T3" -Scenario "MySQL host local" -Status "PASS" -Detail "Remote user answered through host.docker.internal:$mysqlPublishPort." -Critical $true
    }
    else {
        Add-Result -Id "T3" -Scenario "MySQL host local" -Status "FAIL" -Detail $t3.Output -Critical $true
    }
}

$t4 = Invoke-DockerCapture -Arguments @(
    "exec", "sunshine-server",
    "sh", "-lc",
    'mariadb-admin ping -h db -P 3306 -u"$MYSQL_APP_USER" -p"$MYSQL_APP_PASSWORD" --skip-ssl --silent'
)

if ($t4.ExitCode -eq 0) {
    Add-Result -Id "T4" -Scenario "MySQL red interna" -Status "PASS" -Detail "sunshine-server can ping db:3306 with app credentials." -Critical $true
}
else {
    Add-Result -Id "T4" -Scenario "MySQL red interna" -Status "FAIL" -Detail $t4.Output -Critical $true
}

$t5 = Invoke-DockerCapture -Arguments @(
    "exec", "sunshine-db",
    "sh", "-lc",
    'mariadb -N -uroot -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE" -e "SELECT Address, Port FROM worlds WHERE Id = 18;"'
)

if ($t5.ExitCode -eq 0 -and $t5.Output) {
    $columns = $t5.Output -split "\s+"
    if ($columns.Length -ge 2 -and $columns[0] -eq $worldPublicHost -and [int]$columns[1] -eq $worldPort) {
        Add-Result -Id "T5" -Scenario "Worlds en BD" -Status "PASS" -Detail "worlds.Id=18 matches $worldPublicHost:$worldPort." -Critical $true
    }
    else {
        Add-Result -Id "T5" -Scenario "Worlds en BD" -Status "FAIL" -Detail "Expected $worldPublicHost:$worldPort, got '$($t5.Output)'." -Critical $true
    }
}
else {
    Add-Result -Id "T5" -Scenario "Worlds en BD" -Status "FAIL" -Detail $t5.Output -Critical $true
}

if ([string]::IsNullOrWhiteSpace($remoteUser) -or [string]::IsNullOrWhiteSpace($remotePassword)) {
    Add-Result -Id "T6" -Scenario "Contenedor a contenedor" -Status "SKIP" -Detail "Remote credentials are missing."
}
else {
    $t6 = Invoke-DockerCapture -Arguments @(
        "run", "--rm", "--network", "red-emu2", "mariadb:10.11",
        "mariadb-admin",
        "--connect-timeout=5",
        "-h", "db",
        "-P", "3306",
        "-u", $remoteUser,
        "-p$remotePassword",
        "ping",
        "--silent"
    )

    if ($t6.ExitCode -eq 0) {
        Add-Result -Id "T6" -Scenario "Contenedor a contenedor" -Status "PASS" -Detail "Disposable client reached db:3306 on red-emu2."
    }
    else {
        Add-Result -Id "T6" -Scenario "Contenedor a contenedor" -Status "FAIL" -Detail $t6.Output
    }
}

$authPublishHost = Get-EnvValue -Values $envValues -Key "AUTH_PUBLISH_HOST" -DefaultValue "127.0.0.1"
$worldPublishHost = Get-EnvValue -Values $envValues -Key "WORLD_PUBLISH_HOST" -DefaultValue "127.0.0.1"
if ($authPublishHost -ne "0.0.0.0" -and $worldPublishHost -ne "0.0.0.0") {
    Add-Result -Id "T7" -Scenario "LAN" -Status "SKIP" -Detail "Publish hosts are not exposed on 0.0.0.0."
}
else {
    $lanIp = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } |
        Select-Object -First 1 -ExpandProperty IPAddress

    if (-not $lanIp) {
        Add-Result -Id "T7" -Scenario "LAN" -Status "SKIP" -Detail "No LAN IPv4 address detected."
    }
    else {
        try {
            Test-TcpOpen -Host $lanIp -Port $authPort
            Test-TcpOpen -Host $lanIp -Port $worldPort
            Add-Result -Id "T7" -Scenario "LAN" -Status "PASS" -Detail "TCP open on $lanIp for auth/world."
        }
        catch {
            Add-Result -Id "T7" -Scenario "LAN" -Status "FAIL" -Detail $_.Exception.Message
        }
    }
}

if ($IncludeRestartTest) {
    $down = Invoke-ComposeCapture -Arguments @("down")
    $up = Invoke-ComposeCapture -Arguments @("up", "-d")
    $recheck = Invoke-DockerCapture -Arguments @(
        "exec", "sunshine-db",
        "sh", "-lc",
        'mariadb -N -uroot -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE" -e "SELECT Address, Port FROM worlds WHERE Id = 18;"'
    )

    if ($down.ExitCode -eq 0 -and $up.ExitCode -eq 0 -and $recheck.ExitCode -eq 0 -and $recheck.Output -match [regex]::Escape($worldPublicHost)) {
        Add-Result -Id "T8" -Scenario "Reinicio portable" -Status "PASS" -Detail "Compose restart preserved db_data and worlds.Id=18."
    }
    else {
        Add-Result -Id "T8" -Scenario "Reinicio portable" -Status "FAIL" -Detail ($down.Output, $up.Output, $recheck.Output -join [Environment]::NewLine)
    }
}
else {
    Add-Result -Id "T8" -Scenario "Reinicio portable" -Status "SKIP" -Detail "Run with -IncludeRestartTest to execute compose down/up."
}

foreach ($result in $Results) {
    $color = switch ($result.Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        default { "Yellow" }
    }

    Write-Host ("[{0}] {1} {2} :: {3}" -f $result.Status, $result.Id, $result.Scenario, $result.Detail) -ForegroundColor $color
}

$criticalFailures = $Results | Where-Object { $_.Critical -and $_.Status -eq "FAIL" }
if ($criticalFailures) {
    exit 1
}
