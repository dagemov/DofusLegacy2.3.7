[CmdletBinding()]
param(
    [string]$VpsHost = "174.138.35.107",
    [string]$SshKey = "",
    [string]$RemoteUser = "sunshine_remote",
    [string]$RemotePassword = "",
    [string]$EnvPath = ""
)

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
if (-not $SshKey) { $SshKey = Join-Path $repoRoot "SSH\private_key_sebas.pem" }
if (-not $EnvPath) { $EnvPath = Join-Path $repoRoot ".env" }

function Get-EnvValue {
    param([string]$Key, [string]$Default)
    if (-not (Test-Path $EnvPath)) { return $Default }
    foreach ($line in Get-Content $EnvPath) {
        $t = $line.Trim()
        if ($t -and -not $t.StartsWith("#") -and $t -match "^$Key=") {
            return ($t -split "=", 2)[1].Trim()
        }
    }
    return $Default
}

if (-not $RemotePassword) {
    $RemotePassword = Get-EnvValue "MYSQL_REMOTE_PASSWORD" "change-me-remote"
}

$rootPassword = Get-EnvValue "MYSQL_ROOT_PASSWORD" "change-me-root"
$dbName = Get-EnvValue "MYSQL_DATABASE" "sunshine"

$escapedPass = $RemotePassword -replace "'", "''"
$sql = @"
CREATE USER IF NOT EXISTS '$RemoteUser'@'%' IDENTIFIED BY '$escapedPass';
ALTER USER '$RemoteUser'@'%' IDENTIFIED BY '$escapedPass';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, DROP, INDEX, ALTER, CREATE TEMPORARY TABLES, LOCK TABLES, EXECUTE, TRIGGER, REFERENCES ON ``$dbName``.* TO '$RemoteUser'@'%';
FLUSH PRIVILEGES;
"@

$sqlFile = Join-Path $env:TEMP "grant-navicat.sql"
Set-Content -Path $sqlFile -Value $sql -Encoding UTF8
scp -i $SshKey -o StrictHostKeyChecking=accept-new $sqlFile "root@${VpsHost}:/tmp/grant-navicat.sql" | Out-Null

$grantScript = Join-Path $repoRoot "docker\grant-navicat.sh"
scp -i $SshKey -o StrictHostKeyChecking=accept-new $grantScript "root@${VpsHost}:/tmp/grant-navicat.sh" | Out-Null

$fw = @"
if command -v ufw >/dev/null 2>&1; then ufw allow 3306/tcp || true; fi
docker cp /tmp/grant-navicat.sh sunshine-db:/tmp/grant-navicat.sh
docker exec sunshine-db sh /tmp/grant-navicat.sh
docker port sunshine-db 3306
"@

ssh -i $SshKey -o StrictHostKeyChecking=accept-new "root@$VpsHost" $fw
Write-Output "Navicat: Host=$VpsHost Port=3306 User=$RemoteUser Database=$dbName"
