# VPS Controlled Restart

## Snapshot

- Date: `2026-06-02`
- Host target: `174.138.35.107`
- Expected host name: `RollBlackLegacy`
- Scope: restart only the World runtime after item-template deployment

## Current status

SSH restart is now executable from this workstation when using a local non-tracked key copy.

The live rollout on `2026-06-03` used:

```txt
C:\Users\Hombr\Downloads\keys\private_key_sebas.pem
```

That key must remain local-only and must not be copied into the repo.

## Safe goal

Restart only the World runtime.

Do not:

- reboot the VPS
- restart MySQL
- delete containers
- run `docker compose down -v`
- remove volumes

## Existing safe scripts

- `scripts/vps/restart-world-safe.ps1`
- `scripts/vps/restart-world-safe.sh`

Both scripts were corrected during the live rollout to:

- prefer `sunshine-server` as the default target hint
- discover Docker targets with `docker ps -a` so a stopped World container is still selected for restart instead of accidentally picking `sunshine-db`

## Audit-first commands

PowerShell audit only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\restart-world-safe.ps1
```

Shell audit only:

```bash
bash ./scripts/vps/restart-world-safe.sh
```

## Real restart commands

PowerShell real restart:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\restart-world-safe.ps1 -ConfirmRestart
```

Shell real restart:

```bash
CONFIRM_RESTART=1 bash ./scripts/vps/restart-world-safe.sh
```

## Focused DB backup before restart window

The restart window should be paired with a minimal SQL backup of affected tables only:

```bash
mysqldump --single-transaction sunshine items accounts worlds_characters characters characters_items > dofus_tester_backup.sql
```

Adjust credentials locally; do not commit them.

## Recommended deployment order

1. Confirm SSH access is restored.
2. Run audit mode first and confirm the detected target is really World.
3. Apply the item template patch.
4. Apply the account role patch.
5. Stop or restart only World in the controlled window.
6. Apply the inventory grant patch while target characters are offline.
7. Start or confirm World back up.
8. Validate login and inventory with `sebcos1`.

## Last validated live result

- Target container: `sunshine-server`
- Database container left untouched: `sunshine-db`
- Public game ports recovered after restart:
  - `2450/tcp`
  - `5557/tcp`
- No `docker compose down -v`
- No volume deletion
- No MySQL container restart as part of the final controlled World restart
