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

Important operational finding:

- `docker restart sunshine-server` does not call `ShutdownManager`
- the current world process does not implement a signal hook that triggers `ServersManager.Instance.Save()` on container stop
- the graceful save path is still `.save` or `.stop <seconds>`

That means a raw Docker restart is not a safe substitute for an application-level save.

## Existing safe scripts

- `scripts/vps/backup-before-restart.ps1`
- `scripts/vps/backup-before-restart.sh`
- `scripts/vps/restart-world-safe.ps1`
- `scripts/vps/restart-world-safe.sh`

The restart scripts were corrected during the live rollout to:

- prefer `sunshine-server` as the default target hint
- discover Docker targets with `docker ps -a` so a stopped World container is still selected for restart instead of accidentally picking `sunshine-db`
- resolve the SSH key from a local non-tracked file instead of a repo path

The backup scripts:

- target `sunshine-db` explicitly
- dump only the focused operational tables by default
- validate that the resulting SQL file is non-empty
- keep passwords out of tracked docs by reading container environment on the VPS

Validated operator note:

- on this Windows workstation, the PowerShell wrappers are the validated execution path
- the shell variants remain the POSIX equivalent, but on Windows they may require an explicit `SSH_KEY` export depending on the bash runner

## Pre-restart save and backup

Recommended order before any production restart:

1. Announce the window in-game:

```txt
.a Maintenance in 2 minutes. Please relog after restart.
```

2. Force a world save from a moderator-or-higher account:

```txt
.save
```

3. If a graceful stop window is desired, prefer the built-in scheduler instead of raw Docker first:

```txt
.stop 60
```

4. Create the focused DB backup.

5. Validate the dump file is non-empty.

6. Only then run the controlled World restart if it is still required.

## Audit-first commands

PowerShell audit only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\backup-before-restart.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\vps\restart-world-safe.ps1
```

Shell audit only:

```bash
bash ./scripts/vps/backup-before-restart.sh
bash ./scripts/vps/restart-world-safe.sh
```

## Real backup commands

PowerShell real backup:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\backup-before-restart.ps1 -ConfirmBackup
```

Shell real backup:

```bash
CONFIRM_BACKUP=1 bash ./scripts/vps/backup-before-restart.sh
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

## Focused DB backup scope

Default focused tables:

- `items`
- `accounts`
- `worlds_characters`
- `characters`
- `characters_items`
- `characters_spells`
- `characters_stats`
- `npcs`
- `npcs_items`

Override them only if the maintenance scope truly needs more.

## Recommended deployment order

1. Confirm SSH access is restored.
2. Run backup audit mode and restart audit mode first.
3. Announce the maintenance window in-game.
4. Run `.save`.
5. Run the focused backup with confirmation and keep the output path.
6. Validate the dump file is non-empty.
7. Apply the SQL patch or operational change while target characters are offline or while World is intentionally stopped.
8. Restart only `sunshine-server` if a restart is still required.
9. Validate public ports, login, and the specific account/item scenario.

## Last validated live result

- Target container: `sunshine-server`
- Database container left untouched: `sunshine-db`
- Public game ports recovered after restart:
  - `2450/tcp`
  - `5557/tcp`
- No `docker compose down -v`
- No volume deletion
- No MySQL container restart as part of the final controlled World restart

## Current recommendation for future windows

- Never treat `docker restart sunshine-server` as an implicit save.
- Always run `.save` first when the world is still reachable.
- Always take the focused DB dump before the restart window.
- Use `.stop <seconds>` when you want the save to happen inside the server lifecycle instead of only outside it.
