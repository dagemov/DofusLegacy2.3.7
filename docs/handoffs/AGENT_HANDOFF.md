# Agent Handoff

Generated: `2026-06-06`

## Estado VPS — conexión restaurada

| Campo | Valor |
| --- | --- |
| Rama | **`feature/items-sets-visibility-and-vps-combat-telemetry`** |
| Incidente | Cliente *"Conexión al servidor fracasó"* tras rebuild telemetría |
| Clasificación | **`RESTORED_WITH_TELEMETRY_ON`** |
| Causa | Items Admin (`12618–12622`) con `Effects` ObjectEffect → crash `ItemsLoader` |
| Fix | `Effects='0000'` en 5 filas + `docker restart sunshine-server` |
| Backup hex roto | `Infrastructure/temporal-artifacts/combat-telemetry/broken-items-effects-backup.txt` |
| DB backup VPS | `/root/backups/sunshine/sunshine-pre-restart-20260606T153909Z.sql` (intacto, no usado) |

### Validación automática (2026-06-06)

```txt
sunshine-server Up — READY 100%
Auth 0.0.0.0:446 / World 0.0.0.0:3467
FIGHT_TELEMETRY_ENABLED=true
TcpTestSucceeded 446, 3467 desde Windows
```

### Operador — siguiente

1. **Confirmar login cliente** (servidor READY; si falla, revisar IP cliente vs `WORLD_PUBLIC_HOST=127.0.0.1` en `.env`).
2. Re-aplicar effects Jalato/Gay cuando exista fix codec Admin↔runtime.
3. 1 combate smoke → `collect-vps-combat-logs.ps1 -RunAnalyzer`.
4. Phase 3 ReadyChecker sigue **BLOQUEADA** hasta JSONL real.

Docs incidente: [vps-telemetry-deploy-connection-incident.md](../combat-sanitization/vps-telemetry-deploy-connection-incident.md)  
Deploy gate: [combat-vps-telemetry-deploy-gate.md](../combat-sanitization/combat-vps-telemetry-deploy-gate.md)

### Deuda técnica abierta

- `ItemsLoader` usa `EffectManager.GetEffects(string)` (legacy); Admin escribe `ObjectEffectSerializer` — ver [items-builder-effects-serialization-audit.md](../admin-tools/items-builder/items-builder-effects-serialization-audit.md).
- `WORLD_PUBLIC_HOST=127.0.0.1` en VPS `.env` — valorar `174.138.35.107` si el cliente depende de anuncio auth.

### Histórico sprint (rama)

- Sets CRUD, telemetría scripts, deploy `-SunshineOnly`: OK
- Jalato publish: **READY_FOR_OPERATOR_PUBLISH** (effects vaciados temporalmente en VPS)
