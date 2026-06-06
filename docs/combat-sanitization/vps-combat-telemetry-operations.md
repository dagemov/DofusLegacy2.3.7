# Operaciones — telemetría de combate en VPS

**Host:** `174.138.35.107`  
**Clave SSH:** `SSH/private_key_sebas.pem` (gitignored)  
**Contenedor world:** `sunshine-server`  
**Logs JSONL:** `/app/logs/combat/` (dentro del contenedor)

## Prerrequisitos

1. Imagen `sunshine-server` desplegada con `CombatTelemetry.cs` (rama sprint o `feature/combat-telemetry-phase2`).
2. Backup antes de cambios: `CONFIRM_BACKUP=1` + `scripts/vps/backup-before-restart.sh`.
3. **No** `docker compose down -v`. **No** borrar volúmenes.

## Activar telemetría

```powershell
# Auditar (sin cambios)
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 `
  -SshKey "SSH\private_key_sebas.pem" -DryRun

# Aplicar + reiniciar world
$env:CONFIRM_RESTART = "1"
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 `
  -SshKey "SSH\private_key_sebas.pem"
```

Variables escritas en `/opt/dofus-2.0.0/.env`:

```txt
FIGHT_TELEMETRY_ENABLED=true
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
COMBAT_TELEMETRY_WRITE_TURN_FLOW=true
COMBAT_TELEMETRY_WRITE_SPELL_CASTS=true
```

## Sesión de prueba (30–50 combates)

Ver [combat-vps-test-matrix.md](./combat-vps-test-matrix.md).

## Desactivar telemetría

```powershell
$env:CONFIRM_RESTART = "1"
.\infrastructure\artifacts\combat-health\disable-vps-combat-telemetry.ps1 `
  -SshKey "SSH\private_key_sebas.pem"
```

No borra logs. Desactiva solo nuevas escrituras tras restart.

## Descargar y analizar (sin PuTTY)

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 `
  -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer

notepad Infrastructure\temporal-artifacts\combat-telemetry\report.md
start Infrastructure\temporal-artifacts\combat-telemetry\report.html
```

Salida local (gitignored):

```txt
Infrastructure/temporal-artifacts/combat-logs/vps/YYYYMMDD-HHMMSS/
Infrastructure/temporal-artifacts/combat-telemetry/report.md
Infrastructure/temporal-artifacts/combat-telemetry/report.json
Infrastructure/temporal-artifacts/combat-telemetry/report.html
```

## Bash equivalente

```bash
./scripts/vps/enable-combat-telemetry.sh          # dry-run
CONFIRM_RESTART=1 ./scripts/vps/enable-combat-telemetry.sh --execute
./scripts/vps/collect-combat-logs.sh
```

## Prohibiciones

```txt
no logs eternamente activos en producción
no commitear .jsonl ni reportes generados
no reiniciar sin CONFIRM_RESTART=1
no tocar sunshine-db
```
