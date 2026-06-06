# Deploy Gate — VPS Combat Telemetry

**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**Fecha:** 2026-06-06  
**Estado gate:** **PARCIAL** — deploy + telemetría OK; incidente conexión **RESUELTO** (2026-06-06); smoke JSONL **PENDING_OPERATOR**

## Resumen ejecutivo

| Criterio | Resultado |
| --- | --- |
| Backup antes de deploy | **OK** (DB VPS + inventario) |
| `sunshine-server` con `CombatTelemetry` | **OK** (rebuild 2026-06-06) |
| enable/disable scripts | **OK** |
| Variables telemetría activas | **OK** |
| Smoke combate → JSONL | **PENDING_OPERATOR** |
| 30–50 combates | **NO INICIADO** |
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

Directorio en contenedor: `/app/logs/combat/` (creado; vacío hasta combates).

> Nota: la guía menciona `/var/log/sunshine/combat`; en Docker el path efectivo es **`/app/logs/combat`**.

## Paso 5 — Smoke combate

**No ejecutado por agente** (requiere cliente + operador).

Tras 1 combate PvM, validar:

```txt
/app/logs/combat/combat-turn-flow-*.jsonl
/app/logs/combat/spell-casts/spell-casts-*.jsonl
```

Recolectar:

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
start Infrastructure\temporal-artifacts\combat-telemetry\report.html
```

## Paso 6–7 — Sesión 30–50 combates

**Bloqueado** hasta smoke OK.

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

## Post-sesión operador

1. Confirmar login cliente (`174.138.35.107:2450` / world `5557`) — ver [vps-client-port-host-diagnostic.md](./vps-client-port-host-diagnostic.md).
2. 1 combate smoke → verificar `.jsonl`.
3. Si OK → matriz [combat-vps-test-matrix.md](./combat-vps-test-matrix.md).
4. `disable-vps-combat-telemetry.ps1` al terminar.
5. Collect + analyzer + cerrar gate.
