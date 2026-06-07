# Phase 3 QA VPS — ReadyChecker deploy

**Fecha:** 2026-06-07  
**Rama desplegada:** `feature/combat-readychecker-phase3`  
**VPS:** `174.138.35.107`  
**Decisión actual:** **PARTIAL** — deploy + login OK; **smoke combates pendiente operador**

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
| Cliente operador | **PENDIENTE CONFIRMACIÓN** |

---

## Paso 6 — Smoke QA (pendiente)

**Estado:** no ejecutado en esta sesión — requiere operador in-game.

Matriz mínima (5 combates):

| # | Escenario |
| ---: | --- |
| 1 | 1v1 simple sin invocaciones |
| 2 | Con invocaciones |
| 3 | Varios monstruos |
| 4 | Con aliado (si hay) |
| 5 | Escenario que antes mostraba espera ~35 s |

**Collect post-smoke:**

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
start Infrastructure\temporal-artifacts\combat-telemetry\report.html
```

---

## Paso 7 — Métricas (baseline vs post-fix)

### Baseline (2026-06-06, pre-ReadyChecker)

| Métrica | Valor |
| --- | ---: |
| Combates | 21 |
| `TimerElapsed` ~35 s `CharacterFighter` | 6 |
| `GameFightTurnReadyMessageReceived` | 109 |
| `ReadyChecker*` | 0 |

### Post-fix (pendiente collect)

| Métrica | Esperado | Actual |
| --- | --- | --- |
| `ReadyCheckerStarted` | > 0 | **PENDIENTE** |
| `ReadyCheckerAck` / `Timeout` | > 0 | **PENDIENTE** |
| `TimerElapsed` ~35 s jugador | 0 o mucho menor | **PENDIENTE** |
| Spells OK | sin regresión | **PENDIENTE** |
| Crash | no | **OK** (boot READY) |

---

## Paso 8–9 — Sesión ampliada / apagar telemetría

- Sesión 30–50 combates: **solo tras smoke PASS**
- `disable-vps-combat-telemetry.ps1`: **NO ejecutado** — telemetría **ON** para captura QA

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
| Smoke 5 combates + métricas | **PENDIENTE** |
| Phase 3 QA global | **PARTIAL** |

**Siguiente:** operador realiza 5 combates smoke → collect → actualizar esta doc con PASS/FAIL.
