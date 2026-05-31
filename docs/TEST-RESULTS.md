# Test Results

Validation date: May 27, 2026 (America/New_York)

Validation context:

- The operator `.env` was not present in this checkout.
- A temporary local `.env` copied from `.env.example` was used only for validation and then removed.
- `database/sunshine.sql` remained the intentional placeholder shipped by this patch set.

Validated successfully:

- `scripts/validate-torrent.ps1`
  Result: `PASS`
- `docker compose -f docker-compose.yml -f docker-compose.local.yml config`
  Result: `PASS`
- `docker compose -f docker-compose.yml -f docker-compose.local.yml build sunshine`
  Result: `PASS`
- `docker compose -f docker-compose.yml -f docker-compose.local.yml up -d db`
  Result: `PASS`
- `red-emu2` creation and DNS inside the network
  Result: `PASS`
- Host-published MariaDB ping using `MYSQL_REMOTE_USER`
  Result: `PASS`
- Disposable container to `db:3306` on `red-emu2`
  Result: `PASS`
- Sunshine image entrypoint generation of `Config.xml`, `Database.xml`, `maps` and `d2os` symlinks
  Result: `PASS`

Validated and blocked exactly where expected:

- Full Sunshine process start inside Docker
  Result: `BLOCKED BY PLACEHOLDER SQL`
  Detail: after the Linux path fixes, Sunshine now reaches MariaDB successfully and then aborts on `Table 'sunshine.worlds' doesn't exist`.
  Detail: this confirms the remaining blocker is the missing real dump, not Compose wiring, not Docker networking, and not runtime path portability.

Portability matrix status in this checkout:

| Test | Status | Notes |
|---|---|---|
| T1 Auth local | Blocked | Requires real `worlds`/auth schema and a successful full Sunshine boot. |
| T2 World local | Blocked | Requires a successful full Sunshine boot. |
| T3 MySQL host local | Pass | Validated with a disposable MariaDB client against the published port. |
| T4 MySQL red interna | Pass | Validated from the Sunshine image against `db:3306`. |
| T5 Worlds en BD | Blocked | Placeholder SQL does not create `worlds`. |
| T6 Contenedor a contenedor | Pass | Validated on `red-emu2` with a disposable MariaDB client. |
| T7 LAN | Not executed | Local validation used loopback publishing only. |
| T8 Reinicio portable | Not executed | Deferred until the operator provides the real dump and `.env`. |

Recommended next validation after the operator adds the real assets:

1. Copy the real operator `.env` to the repo root.
2. Replace `database/sunshine.sql` with the real Sunshine dump.
3. Run `scripts/setup.ps1 -Mode local -Build`.
4. Run `scripts/test-portability.ps1`.

## Update: May 27, 2026 at 9:15 PM (America/New_York)

Validation context:

- The rollback config at `C:\Users\Hombr\Downloads\RollBackShushine\Sunshine net11.0\Sunshine net11.0\bin\Debug\net11.0\Config.xml` was used to resolve the live VPS ports.
- The rollback dump at `C:\Users\Hombr\Downloads\RollBackShushine\sunshine.sql` was used to validate the local emergency bootstrap path.

Validated successfully:

- VPS auth probe on `194.99.21.223:446`
  Result: `PASS`
- VPS world probe on `194.99.21.223:3467`
  Result: `PASS`
- SSH probe on common ports `22`, `2222`, `2200`, `2022`, `22222`
  Result: `FAIL`
  Detail: no SSH listener was reachable, so the repository now uses a localhost TCP forwarder instead of an SSH tunnel.
- `scripts/start-vps-or-local.ps1 -StartTunnel`
  Result: `PASS`
  Detail: final state is a running tunnel on `127.0.0.1:446` and `127.0.0.1:3467` toward `194.99.21.223`, with no local Docker containers left running.
- `scripts/start-vps-or-local.ps1 -ForceLocal -Build -BootstrapFromRollback`
  Result: `PASS`
  Detail: this path now bootstraps `.env`, copies the rollback dump into `database/sunshine.sql`, resets the local Docker volume, and brings up `sunshine-db` plus `sunshine-server` locally.
- Local emergency ports after the forced bootstrap
  Result: `PASS`
  Detail: `127.0.0.1:446` and `127.0.0.1:3467` both opened successfully while the local stack was up.

Fixes applied during this validation:

1. `scripts/setup.ps1` now passes `--env-file ..\.env` so Compose interpolation uses the root `.env` even when commands run from `docker/`.
2. `scripts/start-vps-or-local.ps1` now clears the local Compose volume during `-BootstrapFromRollback`, which forces MariaDB to re-import the real dump instead of keeping the earlier placeholder schema.
3. `scripts/vps-tunnel.ps1` plus `scripts/vps-tunnel-worker.js` now provide a user-space TCP tunnel for the live VPS when SSH is unavailable.
