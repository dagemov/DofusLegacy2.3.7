# Deploy Gate — VPS Combat Telemetry

**Rama:** `feature/combat-readychecker-phase3`  
**Fecha deploy QA:** 2026-06-07  
**Estado gate:** **PARTIAL** — ReadyChecker desplegado; puertos OK; telemetría ON; **smoke 5 combates pendiente** ([combat-readychecker-phase3-vps-qa.md](./combat-readychecker-phase3-vps-qa.md))
**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**Fecha:** 2026-06-06  
**Estado gate:** **PARCIAL** — deploy + telemetría OK; login OK; **combates reales en curso**; collect/análisis **PENDING_POST_COMBAT**

## Resumen ejecutivo

| Criterio | Resultado |
| --- | --- |
| Backup antes de deploy | **OK** (DB VPS + inventario) |
| `sunshine-server` con `CombatTelemetry` | **OK** (rebuild 2026-06-06) |
| enable/disable scripts | **OK** |
| Variables telemetría activas | **OK** |
| Cliente conecta | **OK** (confirmado operador 2026-06-06) |
| Combates reales → JSONL | **EN CURSO** (operador) |
| Collect + analyzer local | **PENDING_POST_COMBAT** |
| Phase 3 ReadyChecker deploy | **OK** (2026-06-07) |
| Phase 3 QA smoke + métricas | **PENDIENTE** |
| Phase 3 ReadyChecker | **BLOQUEADA** |

## Paso 1 — Preflight local

| Build | Resultado |
| --- | --- |
| `Sunshine.csproj` | OK |
| `Admin.Api` (tras `Stop-Process Admin.Api`) | OK |
| `npm run build` | OK |
| `Sunshine.sln` completo | Bloqueado por lock VS/Admin.Api si proceso activo |

## Paso 2 — Backups documentados

| Backup | Ruta / detalle |
| --- | --- |
| VPS DB (focused) | `/root/backups/sunshine/sunshine-pre-restart-20260606T153909Z.sql` (**2 653 548 bytes**) |
| VPS inventory | `backups/vps/20260606-113833/` (inventario; script CRLF corregido en sesión) |
| `backup-db.ps1` local | No ejecutado — requiere `sunshine-db` local |

## Paso 3 — Deploy

| Acción | Resultado |
| --- | --- |
| Sync `Sunshine net11.0` + `docker/` | OK (SCP) |
| `deploy-vps.ps1` stack completo | **FALLÓ** — build `website` (`HeroBackgroundPath`) |
| Rebuild **solo** `sunshine` | **OK** |
| Incidente `entrypoint.sh` CRLF | **Corregido** — `sed` en Dockerfile + remoto |
| Contenedor final | `sunshine-server Up` — carga mundo OK |
| Incidente post-deploy | **RESUELTO** — crash Items/ObjectEffect; ver [vps-telemetry-deploy-connection-incident.md](./vps-telemetry-deploy-connection-incident.md) |

Comando usado (sunshine-only):

```bash
docker compose -f docker-compose.yml -f docker-compose.vps.yml up -d --build sunshine
```

Nuevo flag en `deploy-vps.ps1`: **`-SunshineOnly`**.

## Paso 4 — Telemetría activada

Variables en `/opt/dofus-2.0.0/.env` y `Config.xml`:

```txt
FIGHT_TELEMETRY_ENABLED=true
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
CombatTelemetryEnabled=true
CombatTelemetryWriteTurnFlow=true
CombatTelemetryWriteSpellCasts=true
```

Directorio en contenedor: `/app/logs/combat/` — **telemetría activa durante combates del operador**.

> Nota: la guía menciona `/var/log/sunshine/combat`; en Docker el path efectivo es **`/app/logs/combat`**.

## Paso 5 — Captura combate (en curso)

**Operador conectado y realizando combates** (2026-06-06). No apagar telemetría hasta recolectar.

Tras terminar sesión, validar en VPS o tras collect:

```txt
/app/logs/combat/combat-turn-flow-*.jsonl
/app/logs/combat/spell-casts/spell-casts-*.jsonl
```

Recolectar:

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
start Infrastructure\temporal-artifacts\combat-telemetry\report.html
```

## Paso 6–7 — Post-sesión

1. `collect-vps-combat-logs.ps1 -RunAnalyzer`
2. `combat-vps-telemetry-analysis-YYYYMMDD.md`
3. `disable-vps-combat-telemetry.ps1` con `CONFIRM_RESTART=1`
4. Decisión Phase 3 vs 2B en [combat-real-telemetry-gate.md](./combat-real-telemetry-gate.md)

## Correcciones de scripts (sesión)

| Script | Fix |
| --- | --- |
| `deploy-vps.ps1` | `Invoke-SshBash`, `-SunshineOnly` |
| `backup-before-restart.ps1` | pipe a `bash -s` (sin CRLF) |
| `backup-vps-state.ps1` | idem |
| `docker/Dockerfile` | `sed` CRLF en entrypoint |

## Decisión Phase 3

```txt
BLOQUEADA — sin JSONL de combate real aún.
```

Tras smoke + 30–50 combates: actualizar [combat-real-telemetry-gate.md](./combat-real-telemetry-gate.md).

## Incidente conexión (2026-06-06)

Tras rebuild, `sunshine-server` entró en crash loop (**exit 139**) al cargar Items — hex `Effects` en formato ObjectEffect (Admin) incompatible con `EffectManager.GetEffects(string)`.

**Fix aplicado:** `Effects='0000'` en items `12618–12622` + `docker restart sunshine-server`. Telemetría permanece **ON**. Clasificación: `RESTORED_WITH_TELEMETRY_ON`.

Detalle: [vps-telemetry-deploy-connection-incident.md](./vps-telemetry-deploy-connection-incident.md).

## Post-sesión operador (orden estricto)

1. ~~Login cliente~~ **OK** — `174.138.35.107:2450` / world `5557`.
2. Terminar combates (en curso).
3. **Collect primero** — `collect-vps-combat-logs.ps1 -RunAnalyzer` → `report.html`.
4. **Luego** `disable-vps-combat-telemetry.ps1` (`CONFIRM_RESTART=1`).
5. Documentar análisis + cerrar gate.

Incidentes resueltos: [vps-telemetry-deploy-connection-incident.md](./vps-telemetry-deploy-connection-incident.md), [vps-client-port-host-diagnostic.md](./vps-client-port-host-diagnostic.md).
