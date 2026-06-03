# VPS Controlled Restart

## Snapshot

- Date: `2026-06-02`
- Host target: `174.138.35.107`
- Expected host name: `RollBlackLegacy`
- Scope: restart only the World runtime after item-template deployment

## Current blocker

Current SSH result from this machine:

```txt
root@174.138.35.107: Permission denied (publickey)
```

And the documented default key path does not currently exist in the repo:

```txt
SSH/private_key_sebas.pem
```

Because of that, the restart flow is documented and ready, but not executable yet from this workstation.

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
