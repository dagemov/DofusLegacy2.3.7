# MySQL Local Navicat Connection

This repository can currently expose two different MySQL servers on the same machine:

- Docker MariaDB for Sunshine, published on `127.0.0.1:${MYSQL_PUBLISH_PORT}`
- an optional XAMPP `mysqld` listener that may answer on `localhost:3306`

For Sunshine runtime consistency, the official local database is the Docker-published endpoint generated from `.env`.

## Official Sunshine runtime connection

- Connection Name: `DofusLegacy Sunshine Local`
- Host: `127.0.0.1`
- Port: `3306` by default, or the value of `MYSQL_PUBLISH_PORT` in `.env`
- User: `sunshine`
- Password: the value of `MYSQL_APP_PASSWORD` in `.env`
- Database: `sunshine`

This is the same endpoint written into the host runtime `Database.xml` by:

```powershell
.\scripts\sync-env-to-config.ps1
```

## Why not `localhost`

On this workstation, `localhost:3306` can resolve to the XAMPP `mysqld` process while `127.0.0.1:3306` resolves to Docker's published Sunshine MariaDB container.

That means:

- `127.0.0.1` is the official Sunshine runtime target
- `localhost` may point to a different `sunshine` schema copy

Use `127.0.0.1` in Navicat if you want to inspect the same database that Sunshine uses.

## Quick verification in Navicat

After connecting, run:

```sql
SELECT CURRENT_USER() AS user_name, @@hostname AS server_name, @@port AS port_in_use;
SHOW TABLES LIKE 'worlds';
```

Expected for the official runtime database:

- `user_name` is `sunshine@%` or another dedicated Sunshine runtime user
- `port_in_use` matches the Docker-published MySQL port
- the `worlds` table exists

## Docker checks

```powershell
docker ps
docker port sunshine-db
netstat -ano | findstr :3306
```

Expected:

- container `sunshine-db` is running
- Docker publishes `127.0.0.1:3306->3306/tcp` unless `.env` changes the host port

## XAMPP checks

```powershell
netstat -ano | findstr :3306
Get-Process -Name mysqld
```

If XAMPP is also listening on `0.0.0.0:3306` or `[::]:3306`, do not use `localhost` for Sunshine runtime validation.

## Regenerate the runtime config

To keep Sunshine pointed at the Docker MariaDB instance:

```powershell
.\scripts\sync-env-to-config.ps1
```

That writes:

- `Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0\Config.xml`
- `Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0\Database.xml`

The generated `Database.xml` should contain:

```txt
Database = sunshine
Hostname = 127.0.0.1
Port = 3306
Username = sunshine
Password = <value from .env>
```

## If Navicat connects to the wrong server

1. Change the host from `localhost` to `127.0.0.1`
2. Confirm the port matches `MYSQL_PUBLISH_PORT`
3. Re-run `.\scripts\sync-env-to-config.ps1`
4. Re-test with `SELECT CURRENT_USER(), @@hostname, @@port`
