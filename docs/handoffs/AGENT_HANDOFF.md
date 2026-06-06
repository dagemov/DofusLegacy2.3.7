# Agent Handoff

Generated: `2026-06-06`

## Deploy Gate — VPS Combat Telemetry

| Campo | Valor |
| --- | --- |
| Rama | **`feature/items-sets-visibility-and-vps-combat-telemetry`** |
| Deploy `sunshine-server` + telemetría | **OK** (2026-06-06) |
| Backup DB VPS | `/root/backups/sunshine/sunshine-pre-restart-20260606T153909Z.sql` (2.6 MB) |
| Smoke JSONL | **PENDING_OPERATOR** |
| Phase 3 ReadyChecker | **BLOQUEADA** |

### Validación VPS (automática)

```txt
FIGHT_TELEMETRY_ENABLED=true
CombatTelemetryEnabled=true
/app/logs/combat/ — directorio creado, vacío hasta combates
sunshine-server Up
```

### Operador — siguiente

```powershell
# 1 combate smoke en cliente → luego:
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
start Infrastructure\temporal-artifacts\combat-telemetry\report.html

# Sesión 30–50 combates → disable:
$env:CONFIRM_RESTART="1"
.\infrastructure\artifacts\combat-health\disable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
```

Docs: [combat-vps-telemetry-deploy-gate.md](../combat-sanitization/combat-vps-telemetry-deploy-gate.md), [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md)

### Fixes deploy (rama)

- `deploy-vps.ps1` — `Invoke-SshBash`, `-SunshineOnly`
- `docker/Dockerfile` — CRLF entrypoint
- `backup-before-restart.ps1`, `backup-vps-state.ps1` — bash pipe

### Items/sets sprint (histórico)

- Sets CRUD: OK (cherry-pick)
- Jalato publish: **READY_FOR_OPERATOR_PUBLISH**
