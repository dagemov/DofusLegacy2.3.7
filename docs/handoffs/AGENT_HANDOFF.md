# Agent Handoff — Combat Sanitization / VPS Telemetry Active

Generated: `2026-06-06`  
Rama: **`feature/items-sets-visibility-and-vps-combat-telemetry`**

## Estado VPS (actual)

```txt
sunshine-server UP
servidor funcional
cliente conecta — CONFIRMADO por operador
telemetría ON
operador realizando combates reales AHORA
```

| Campo | Valor |
| --- | --- |
| VPS | `174.138.35.107` |
| Auth / World | `2450` / `5557` |
| `WORLD_PUBLIC_HOST` | `174.138.35.107` |
| `worlds.Id=18` | `174.138.35.107:5557` |
| Telemetría path | `/app/logs/combat/` |
| SSH key | `SSH/private_key_sebas.pem` (no `.ppk`) |
| Phase 3 ReadyChecker | **BLOQUEADA** — esperar análisis de logs |

## Incidentes cerrados (2026-06-06)

### 1 — Items ObjectEffect → crash boot

Items `12618–12622`: `UPDATE items SET Effects=0x30303030`.  
Doc: [vps-telemetry-deploy-connection-incident.md](../combat-sanitization/vps-telemetry-deploy-connection-incident.md)

### 2 — Puertos/hosts legacy

`.env` corregido a `2450`/`5557`/`174.138.35.107`. Commit `a1b6c3e`.  
Doc: [vps-client-port-host-diagnostic.md](../combat-sanitization/vps-client-port-host-diagnostic.md)

## Acción inmediata — cuando terminen los combates

**NO apagar telemetría antes de recolectar.**

### Paso 1 — Recolectar y analizar

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
start Infrastructure\temporal-artifacts\combat-telemetry\report.html
```

Revisar:

```txt
Infrastructure/temporal-artifacts/combat-telemetry/report.md
Infrastructure/temporal-artifacts/combat-telemetry/report.json
```

Crear tras análisis: `docs/combat-sanitization/combat-vps-telemetry-analysis-YYYYMMDD.md`

### Paso 2 — Apagar telemetría (después de collect)

```powershell
$env:CONFIRM_RESTART="1"
.\infrastructure\artifacts\combat-health\disable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
```

### Preguntas que el análisis debe responder

```txt
1. ¿Hay combat-turn-flow-*.jsonl reales?
2. ¿Hay spell-casts-*.jsonl reales?
3. ¿Cuántos combates capturados?
4. ¿Hay TimerElapsed ~35000ms?
5. ¿Llega GameFightTurnReadyMessage?
6. ¿Cuánto tarda AiStarted -> AiFinished?
7. ¿Cuánto tarda AiFinished -> NextTurnStarted?
8. ¿Turno humano antes de fin animación enemiga?
9. ¿Spells fallidos?
10. ¿Effects fallidos?
```

## Decisión Phase 3 (tras logs)

| Evidencia | Rama / acción |
| --- | --- |
| Hand-off roto confirmado | `feature/combat-readychecker-phase3` — portar ReadyChecker / TryAdvanceTurn desde `RollBlackServer\2.0.0\Rollback` |
| Logs ambiguos | `feature/combat-telemetry-phase2b` — más telemetría spell/AI/timers |
| Sin reproducción | Documentar *Phase 3 bloqueada por falta de reproducción* |

## Prohibido ahora

```txt
no tocar lógica de turnos sin analizar logs
no ReadyChecker / IA / spells / summons fixes
no items/admin changes
no reiniciar VPS en bucle
no docker compose down -v
no commitear logs pesados
```

## Scripts

```txt
infrastructure/artifacts/combat-health/enable-vps-combat-telemetry.ps1
infrastructure/artifacts/combat-health/disable-vps-combat-telemetry.ps1
infrastructure/artifacts/combat-health/collect-vps-combat-logs.ps1
infrastructure/artifacts/combat-health/analyze-combat-telemetry.ps1
infrastructure/artifacts/combat-health/fix-vps-client-ports.ps1
```

## Cierre del gate (pendiente)

- [ ] Logs reales descargados del VPS
- [ ] Analyzer → `report.md` / `report.html`
- [ ] Decisión Phase 3 vs 2B con evidencia
- [ ] Telemetría apagada al final (o documentar si sigue ON)
- [ ] Handoff actualizado

## Docs gate

- [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md)
- [combat-vps-telemetry-deploy-gate.md](../combat-sanitization/combat-vps-telemetry-deploy-gate.md)
