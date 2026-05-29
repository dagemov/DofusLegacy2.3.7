# Docker MySQL and Navicat Connection

This repository publishes the MariaDB container to the Windows host through the loopback adapter. For local tools and a host-run Sunshine process, prefer `127.0.0.1` over `localhost`.

## Active local development connection

- Connection Name: `DofusLegacy Docker MySQL`
- Host: `127.0.0.1`
- Port: `3306` by default, or the value of `MYSQL_PUBLISH_PORT` from `.env`
- User: `sunshine`
- Password: the value of `MYSQL_APP_PASSWORD` from `.env`
- Database: `sunshine`

The root user also exists for container administration, but the application flow should use the dedicated `sunshine` user defined by `.env`.

## Why `127.0.0.1` instead of `localhost`

- `docker-compose.local.yml` publishes MariaDB explicitly on `127.0.0.1:${MYSQL_PUBLISH_PORT}:3306`
- the host-run Sunshine executable reaches Docker through the published host port, not through the internal Docker DNS name `db`
- using `127.0.0.1` avoids ambiguity with host name resolution and keeps the connection aligned with the Docker publish rule

## Navicat fields

- Connection Name: `DofusLegacy Docker MySQL`
- Host: `127.0.0.1`
- Port: `3306`
- User Name: `sunshine`
- Password: the value from `.env`
- Database / Initial Schema: `sunshine`

If you need an admin session for schema inspection, use:

- User Name: `root`
- Password: the value of `MYSQL_ROOT_PASSWORD` from `.env`

## Validate the published port

```powershell
docker ps
docker port sunshine-db
netstat -ano | findstr :3306
```

Expected result:

- the container is named `sunshine-db`
- `3306/tcp` is published as `127.0.0.1:3306` unless `.env` changes the host port

## Validate from inside the container

```powershell
docker exec -it sunshine-db mysql -uroot -p
```

Then verify the application database and user:

```sql
SHOW DATABASES;
SELECT User, Host FROM mysql.user ORDER BY User, Host;
```

## Generate the host-side Sunshine config

When running `Sunshine` directly from Visual Studio or `bin\Debug\net11.0`, generate `Config.xml` and `Database.xml` from `.env` first:

```powershell
.\scripts\sync-env-to-config.ps1
```

That command writes the live files into:

```txt
Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0
```

The generated `Database.xml` uses:

```txt
Hostname = 127.0.0.1
Port = 3306
Username = sunshine
Password = <MYSQL_APP_PASSWORD>
```

## If Navicat cannot connect

Check these points in order:

1. `docker ps` shows `sunshine-db` as `healthy`
2. `docker port sunshine-db` shows a published `127.0.0.1` binding
3. no other service is already occupying the published port
4. the password in Navicat matches `.env`
5. the MariaDB volume was not initialized with older credentials
