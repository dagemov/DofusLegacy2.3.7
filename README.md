# DofusLegacy2.3.7

Docker portability for Sunshine EMU now lives around a single root `.env` and an explicit Docker network named `red-emu2`.

## Current repo shape

- Sunshine source code is currently stored in `Sunshine net11.0/Sunshine net11.0`.
- Runtime payload for Docker is expected in `runtime/`.
- MariaDB bootstrap SQL is expected in `database/sunshine.sql`.
- The Dofus client and Uplauncher stay outside the Sunshine image.

## Connection matrix

| Surface | Hostname / Address | Port | Purpose |
|---|---|---:|---|
| Internal Docker | `db` | `3306` | MariaDB for Sunshine and sibling containers |
| Internal Docker | `sunshine` | `${AUTH_PORT}` | Auth server inside `red-emu2` |
| Internal Docker | `sunshine` | `${WORLD_PORT}` | World server inside `red-emu2` |
| Host published | `${MYSQL_PUBLISH_HOST}` | `${MYSQL_PUBLISH_PORT}` | Optional MySQL admin access |
| Host published | `${AUTH_PUBLISH_HOST}` | `${AUTH_PORT}` | Auth exposure on the Docker host |
| Host published | `${WORLD_PUBLISH_HOST}` | `${WORLD_PORT}` | World exposure on the Docker host |
| Game client view | `${WORLD_PUBLIC_HOST}` | `${WORLD_PORT}` | Address returned through `worlds.Id=18` |

## Required files

1. Copy `.env.example` to `.env` and fill the real operator values.
2. Replace `database/sunshine.sql` with the real Sunshine MariaDB dump.
3. Verify `runtime/maps`, `runtime/d2os` and `runtime/data` are populated.

## Commands

Validate compose:

```powershell
Set-Location .\docker
docker compose --env-file ..\.env config
```

Start locally with loopback-only publishing:

```powershell
Set-Location .\docker
docker compose --env-file ..\.env -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

Start on a VPS with public publishing:

```powershell
Set-Location .\docker
docker compose --env-file ..\.env -f docker-compose.yml -f docker-compose.vps.yml up -d --build
```

Run the portability matrix:

```powershell
.\scripts\test-portability.ps1
```

Probe the VPS first, create a localhost tunnel when it is alive, and only fall back to local Docker when the VPS game ports are down:

```powershell
.\scripts\start-vps-or-local.ps1 -StartTunnel
```

Bootstrap a local fallback from the rollback config and dump if the VPS ports are down:

```powershell
.\scripts\start-vps-or-local.ps1 -Build -BootstrapFromRollback
```

## Scripts

- `scripts/setup.ps1`: validates runtime payload, checks compose and starts the requested override.
- `scripts/start-vps-or-local.ps1`: probes the VPS game ports and starts local Docker only when the remote auth/world ports are down.
- `scripts/sync-env-to-config.ps1`: generates host-side `Config.xml` and `Database.xml` from `.env`.
- `scripts/validate-torrent.ps1`: verifies the runtime payload required by Sunshine.
- `scripts/vps-tunnel.ps1`: starts or stops a localhost TCP forwarder for the VPS when SSH is unavailable.
- `scripts/test-portability.ps1`: executes T1-T8 and returns a non-zero exit code on critical failures.

## Migration from `sunshine-net`

If an old network named `sunshine-net` still exists and no active container uses it, remove it before standardizing on `red-emu2`:

```powershell
docker network ls
docker network rm sunshine-net
```

## Important notes

- `docker/entrypoint.sh` generates `Config.xml` and `Database.xml` at runtime; manual config mounts are no longer required.
- The current Sunshine project targets `net11.0` and the Docker build therefore uses `.NET 11 preview` images as of May 27, 2026.
- Secrets remain out of Git because `.env` is ignored.
