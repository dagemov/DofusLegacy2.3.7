# Agent Handoff — Combat ReadyChecker Phase 3

Generated: `2026-06-07`

## Massive devp integration sync (2026-06-07)

| Área | Estado |
| --- | --- |
| **Items Builder** | **COMPLETE** / pending operator publish only |
| **Sets Builder** | **COMPLETE** — PR [#34](https://github.com/dagemov/DofusLegacy2.3.7/pull/34), [#35](https://github.com/dagemov/DofusLegacy2.3.7/pull/35) |
| **Client Publication** | **COMPLETE** / operator controlled publish (ya en `devp`) |
| **Combat Telemetry** | **ACTIVE** — PR [#36](https://github.com/dagemov/DofusLegacy2.3.7/pull/36)–[#38](https://github.com/dagemov/DofusLegacy2.3.7/pull/38) |
| **ReadyChecker** | **PASS functional** — residual timers classified; PR [#39](https://github.com/dagemov/DofusLegacy2.3.7/pull/39) (conflictos con PR #32) |
| **Spell Builder** | PR [#40](https://github.com/dagemov/DofusLegacy2.3.7/pull/40) — API + Angular read-only |
| **`main`** | **Intacta** — integración solo vía `devp` |

**Next:** Combat Phase 4 Spell/Summon telemetry analysis

Detalle completo: [massive-devp-sync-20260607.md](../integration/massive-devp-sync-20260607.md)

---

## PR #39 + PR #32 merge resolution

| Campo | Valor |
| --- | --- |
| Estado | **`DONE` local** — push + actualizar PR #39 pendiente |
| Build | `Sunshine.csproj` **OK** |
| Preservado #32 | IA monstruos, castigos, summons (`DiesAtTurnEnd`, Roublabot), `FightCombatLogger`, Sacrifice/DOT |
| Integrado #39 | ReadyChecker, `TryAdvanceTurn`, telemetría, `SetReadyForNextTurn` |

Detalle: [pr39-readychecker-merge-resolution.md](../integration/pr39-readychecker-merge-resolution.md)

**QA pendiente:** VPS smoke post-merge antes de merge a `devp`. **No merge a `main`.**

---

## Phase 3.1 — Analyzer polish + timer classification

| Campo | Valor |
| --- | --- |
| Estado | **`DONE`** |
| Tipo | `analyzer + documentación` (sin lógica de combate) |

- `readyCheckerStartCount` **145** (alias `ReadyCheckerStarted` corregido)
- 11 `TimerElapsed` clasificados — rescate ~35 s jugador activo, no fallo ReadyChecker
- **Phase 3:** PASS WITH MINOR RESIDUAL TIMERS
- **Siguiente:** Phase 4 Spell/Summon telemetry analysis

---

## Estado VPS

```txt
sunshine-server UP — ReadyChecker build desplegado
puertos 2450/5557 OK (corregidos post-rebuild)
telemetría OFF (post-QA 2026-06-07)
smoke 47 combates: PASS operador
```

| Campo | Valor |
| --- | --- |
| VPS | `174.138.35.107` |
| Backup DB | `/root/backups/sunshine/sunshine-pre-restart-20260607T015107Z.sql` |
| Backup inventario | `backups/vps/20260606-215057/` |
| `.env` backup | `/opt/dofus-2.0.0/.env.bak-phase3qa-20260607` |
| Telemetría | **OFF** |
| ReadyChecker | **ON** |

## Collect (referencia)

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
```

Logs QA: `Infrastructure/temporal-artifacts/combat-logs/vps/20260607-113152/`

## Rollback rápido

```txt
CombatReadyCheckerEnabled=false + restart sunshine
```

## Cierre Phase 3 QA

- [x] Deploy sunshine OK
- [x] TCP 2450/5557 OK
- [x] 47 combates smoke
- [x] Collect + métricas
- [x] Telemetría OFF post-QA
- [x] Phase 3.1 analyzer + clasificación timers
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
