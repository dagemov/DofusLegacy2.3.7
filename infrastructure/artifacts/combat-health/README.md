# Combat Health Lab (temporal)

Entorno local documentado para diagnosticar combate. **No es fuente de verdad** — los fixes van al repo oficial vía PR.

## Documentación

- [combat-health-lab-plan.md](../../../docs/combat-sanitization/combat-health-lab-plan.md)
- [combat-system-audit.md](../../../docs/combat-sanitization/combat-system-audit.md)
- [combat-turn-flow-comparison.md](../../../docs/combat-sanitization/combat-turn-flow-comparison.md)
- [combat-telemetry-plan.md](../../../docs/combat-sanitization/combat-telemetry-plan.md)

## Scripts

| Script | Uso |
| --- | --- |
| `run-local-combat-lab.ps1` | Build + lanza Sunshine apuntando a DB lab |
| `sync-vps-db-snapshot.ps1` | Descarga dump VPS y restaura en MariaDB local |
| `collect-combat-logs.ps1` | Archiva logs locales |
| `collect-vps-combat-logs.ps1` | Descarga logs de combate del VPS |
| `analyze-combat-telemetry.ps1` | Genera `report.md`, `report.json`, `report.html` |
| `enable-vps-combat-telemetry.ps1` | Activa telemetría en VPS (`-DryRun`, `CONFIRM_RESTART=1`) |
| `disable-vps-combat-telemetry.ps1` | Desactiva telemetría en VPS |

## Carpetas gitignored

```txt
db-snapshots/   # dumps SQL — nunca commit
logs/           # copias locales de sesión
local-client/   # copia cliente opcional
```

Telemetría del servidor (cuando esté implementada):

```txt
Infrastructure/logs/combat/
```

## Inicio rápido

```powershell
# 1. Snapshot DB (requiere SSH key local, no commitear)
.\sync-vps-db-snapshot.ps1 -SshKey "..\..\..\SSH\private_key_sebas.pem"

# 2. Configurar appsettings.Development.local.json (gitignored) → sunshine_lab

# 3. Servidor local con telemetría
.\run-local-combat-lab.ps1

# 4. Tras combate de prueba
.\collect-combat-logs.ps1
.\analyze-combat-telemetry.ps1
```

## Referencia antigua

Analizador y logs de ejemplo:

```txt
C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback\Infrastructure\scripts\CombatTelemetryAnalyzer
C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback\Infrastructure\logs\combat
```

## Prohibido

- Apuntar el lab a DB producción VPS para pruebas destructivas
- Commitear `.sql`, `.log`, claves PEM
- Dejar este artifact como destino final de código de combate
