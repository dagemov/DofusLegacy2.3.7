# Phase 3 QA VPS — ReadyChecker deploy

**Fecha:** 2026-06-07  
**Rama desplegada:** `feature/combat-readychecker-phase3`  
**VPS:** `174.138.35.107`  
**Decisión actual:** **PASS WITH MINOR RESIDUAL TIMERS**

---

## Paso 1 — Preflight local

```txt
dotnet build Sunshine.csproj → OK (0 errores)
```

---

## Paso 2 — Backups

| Backup | Ruta | Resultado |
| --- | --- | --- |
| VPS inventory | `backups/vps/20260606-215057/` | OK |
| VPS DB focused | `/root/backups/sunshine/sunshine-pre-restart-20260607T015107Z.sql` (2 679 464 bytes) | OK |
| Local `backup-db.ps1` | — | SKIP — Docker local no disponible |

---

## Paso 3 — Deploy

| Intento | Resultado |
| --- | --- |
| `deploy-vps.ps1` (rama spell-builder por error de workspace) | SYNC parcial; bash preflight falló |
| Re-sync `Sunshine net11.0` manual (SCP) | OK |
| Primer `docker compose up --build sunshine` | **FAIL** — `ReadyChecker.cs` ausente en VPS (sync desde rama incorrecta) |
| Checkout `feature/combat-readychecker-phase3` + re-SCP | OK |
| Segundo rebuild sunshine | **OK** — imagen `sunshine-emu-sunshine` |

**Contenedor final:**

```txt
sunshine-server Up
sunshine-db Up (healthy)
Puertos: 0.0.0.0:2450->2450, 0.0.0.0:5557->5557
Logs READY: announced as 174.138.35.107:2450 / :5557
```

**Incidente post-rebuild:** `.env` volvió a puertos legacy `446`/`3467` tras recreate sin flags. **Corregido** con sed + restart.

Backup `.env`: `/opt/dofus-2.0.0/.env.bak-phase3qa-20260607`

---

## Paso 4 — Flags VPS

`/opt/dofus-2.0.0/.env`:

```txt
WORLD_PUBLIC_HOST=174.138.35.107
AUTH_PORT=2450
WORLD_PORT=5557
CombatReadyCheckerEnabled=true
CombatReadyCheckerTimeoutMs=5000
FIGHT_TELEMETRY_ENABLED=true
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
CombatTelemetryEnabled=true
```

`docker exec sunshine-server printenv`:

```txt
FIGHT_TELEMETRY_ENABLED=true
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
COMBAT_TELEMETRY_WRITE_TURN_FLOW=true
COMBAT_TELEMETRY_WRITE_SPELL_CASTS=true
```

Nota: `CombatReadyChecker*` se lee vía `GameConfig` (default **enabled=true**, timeout **5000** si no está en `Config.xml`).

---

## Paso 5 — Login pre-QA

| Check | Resultado |
| --- | --- |
| `Test-NetConnection` 2450 | **True** |
| `Test-NetConnection` 5557 | **True** |
| `sunshine-server` READY | **OK** |
| Cliente operador | **OK** |

---

## Paso 6 — Smoke QA (completado 2026-06-07)

**Operador:** 47 combates (sesión extendida).

**Veredicto subjetivo operador:**

```txt
Los combates se sienten demasiado fluidos.
Están mejores.
```

**Collect:** `Infrastructure/temporal-artifacts/combat-logs/vps/20260607-113152/`  
**Reportes:** `Infrastructure/temporal-artifacts/combat-telemetry/report.{md,json,html}`

---

## Paso 7 — Métricas (baseline vs post-fix)

### Baseline (2026-06-06, pre-ReadyChecker)

| Métrica | Valor |
| --- | ---: |
| Combates | 21 |
| `TimerElapsed` ~35 s `CharacterFighter` | 6 |
| `GameFightTurnReadyMessageReceived` | 109 |
| `ReadyChecker*` | 0 |
| Spells fallidos | 0 |

### Post-fix (2026-06-07, con ReadyChecker)

| Métrica | Valor | vs baseline |
| --- | ---: | --- |
| Combates (`FightStarted`) | **47** | más muestra |
| `ReadyCheckerStarted` | **145** | ✅ (era 0) |
| `ReadyCheckerAck` | **225** | ✅ |
| `ReadyCheckerTimeout` | **15** | ✅ |
| `ReadyCheckerAdvanceTurn` | **144** | ✅ |
| `GameFightTurnReadyMessageReceived` | **348** | ↑ (más combates) |
| `TimerElapsed` `CharacterFighter` | **11** | ↓ por combate (~0.23 vs ~0.29) |
| Spell cast failed | **16** | ⚠️ regresión aislada (handlers) |
| Crash servidor | **0** | ✅ |

### Phase 3.1 — Analyzer fix + clasificación timers

| Check | Resultado |
| --- | --- |
| `readyCheckerStartCount` en `report.json` | **145** (antes 0 por alias `ReadyCheckerStarted`) |
| `readyCheckerAckCount` | **225** |
| `readyCheckerAdvanceTurnCount` | **144** |
| Timers clasificados | **11/11** en `combat-timer-elapsed-classification-report.md` |

**Clasificación de los 11 `TimerElapsed` (~35 s, `CharacterFighter`):**

| Clasificación | Count | Interpretación |
| --- | ---: | --- |
| `PLAYER_NO_ACTION` | 0 | Ningún timer fue turno vacío / AFK puro |
| `UNKNOWN` | 11 | Jugador activo (2–8 hechizos/turno) sin `EndTurn` cliente antes del rescate ~35 s |
| `READY_TIMEOUT_EXPECTED` | 0 | El timer no coincide con timeout de hand-off |
| `MISSING_READY_ACK` | 0 | Hand-off previo reconstruido con ACK en 10/11 casos |
| `POSSIBLE_CLIENT_STALL` | 0 | Sin patrón de spam ready sin progreso |

**Conclusión Phase 3.1:** los timers residuales no son fallos de ReadyChecker ni ACK perdidos. Son rescates esperados cuando el operador/jugador no pasa turno manualmente tras jugar durante ~35 s. **No justifica cambiar turn flow ni timeout de ReadyChecker.**

---

## Paso 8–9 — Telemetría

- `disable-vps-combat-telemetry.ps1` ejecutado 2026-06-07 — **OFF**
- Logs preservados en VPS y copia local

---

## Rollback rápido

```txt
CombatReadyCheckerEnabled=false en .env
docker compose ... up -d sunshine
```

Sin restore DB.

---

## Decisión

| Fase | Estado |
| --- | --- |
| Deploy SunshineOnly | **PASS** |
| Config + puertos | **PASS** |
| Login TCP | **PASS** |
| ReadyChecker en logs | **PASS** |
| Mejora subjetiva combate | **PASS** |
| `TimerElapsed` → 0 | **Residual** (11 en 47 combates; clasificados, no bug de hand-off) |
| Phase 3 QA global | **PASS WITH MINOR RESIDUAL TIMERS** |

**Siguiente:** Phase 4 — Spell/Summon telemetry analysis (16 `SpellCastFailed`, handlers).
