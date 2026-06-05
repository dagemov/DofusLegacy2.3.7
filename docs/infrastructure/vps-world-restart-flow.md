# VPS World Restart Flow

## Snapshot

- Date: `2026-06-02`
- Status: `DOCUMENTED / NOT EXECUTED`

## Important current blocker

Historical SSH access was previously confirmed for:

- `root@174.138.35.107`

Current result on this machine during stabilization:

```txt
Permission denied (publickey)
```

So this document describes the safe intended flow, but the live VPS audit and restart remain blocked until SSH access is restored.

## Goal

Restart only the World runtime after cache-sensitive admin changes, without restarting the whole VPS and without touching the database service.

## Scripts

Prepared scripts:

- `scripts/vps/restart-world-safe.ps1`
- `scripts/vps/restart-world-safe.sh`

Both scripts are intentionally safe by default:

- they discover likely `world` or `sunshine` runtimes first
- they print the detected target
- they do nothing unless explicit confirmation is provided
- they tail the last logs after restart

## Expected runtime audit

When SSH access is restored, audit with:

```bash
docker ps
docker compose ls
systemctl list-units | grep -i sunshine
ps aux | grep -i sunshine
```

Questions to answer live:

- Does World run in Docker?
- Does Auth run in Docker?
- Does Website/Admin API run in Docker?
- What is the exact World container or service name?

## Safe PowerShell usage

Audit only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\restart-world-safe.ps1
```

Restart only after explicit authorization:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vps\restart-world-safe.ps1 -ConfirmRestart
```

## Safe shell usage

Audit only:

```bash
bash ./scripts/vps/restart-world-safe.sh
```

Restart only after explicit authorization:

```bash
CONFIRM_RESTART=1 bash ./scripts/vps/restart-world-safe.sh
```

## Safety rules

- do not restart MySQL
- do not reboot the VPS
- do not restart all containers blindly
- do not run the real restart without operator approval
- keep the final selected target visible in logs or console output

## Recommended operator flow

1. Save the admin change.
2. Confirm whether the affected data is cached by World.
3. Run the audit mode first.
4. Verify the detected target is truly the World runtime.
5. Run the restart only with explicit approval.
6. Review the last 50 log lines.
7. Re-test the affected item or spell.
