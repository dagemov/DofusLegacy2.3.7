# Fase 5 — Notas de despliegue e integración

Procedimiento para cerrar el pipeline de efectos en **`develop`** (origin). No incluye release a `main` ni deploy prod VPS.

## Política de merge (obligatoria)

| Regla | Detalle |
|-------|---------|
| Subir PRs | Sí — visibles en GitHub |
| Merge automático | **No** — ni por agente ni por script |
| Cerrar PRs | **No** — las PR del pipeline deben permanecer **abiertas** en `develop` |
| Quién mergea | Solo el equipo, manualmente, tras revisión |

Si `develop` quedó inconsistente, resetear desde `main` (ver sección 2) y reabrir PRs — **sin mergear** en la operación.

## 1. PRs abiertas — pipeline efectos (post-reset 2026-06-05)

| Orden | PR | Head | Base | Estado |
|-------|-----|------|------|--------|
| 1 | [#21](https://github.com/dagemov/DofusLegacy2.3.7/pull/21) | `feature/effects-audit-phase1` | `develop` | **abierta** |
| 2 | [#22](https://github.com/dagemov/DofusLegacy2.3.7/pull/22) | `feature/effects-catalog-phase2` | `develop` | **abierta** |
| 3 | [#23](https://github.com/dagemov/DofusLegacy2.3.7/pull/23) | `feature/effects-engine-fix-phase3` | `develop` | **abierta** |
| 4 | [#24](https://github.com/dagemov/DofusLegacy2.3.7/pull/24) | `feature/effects-validation-phase4` | `develop` | **abierta** |
| 5 | [#19](https://github.com/dagemov/DofusLegacy2.3.7/pull/19) | `feature/effects-integration-phase5` | `develop` | **abierta** |

PR #19 reabierta tras cierre accidental. PR #25 (duplicado) cerrada. PRs #14–#18: historial del `develop` anterior.

**Regla:** ningún PR del pipeline con `base=main` ni `base=develop-build`.

### Verificación PRs abiertas

```powershell
$credOut = "protocol=https`nhost=github.com`n`n" | git credential fill 2>$null
$token = ($credOut | Select-String '^password=(.+)$').Matches.Groups[1].Value
$headers = @{ Authorization = "Bearer $token"; Accept = 'application/vnd.github+json' }
Invoke-RestMethod -Uri "https://api.github.com/repos/dagemov/DofusLegacy2.3.7/pulls?state=open" -Headers $headers |
  ForEach-Object { "$($_.number) base=$($_.base.ref) head=$($_.head.ref)" }
```

## 2. Reset de `develop` desde `main` (ejecutado)

```powershell
git push origin --delete develop
git push origin 1f998cd:refs/heads/develop
git ls-remote --heads origin develop main   # mismo SHA
```

VPS:

```bash
cd /opt/dofus-2.0.0-build
git fetch origin +refs/heads/develop:refs/remotes/origin/develop
git reset --hard origin/develop
```

**Efecto:** `develop` === `main`. Fixes Fase 3 no están en `develop` hasta merge manual PR #23.

## 3. Higiene origin — `develop-build` eliminada

La rama remota `develop-build` no forma parte del flujo. Procedimiento ejecutado:

```powershell
git push origin --delete develop-build
git ls-remote --heads origin develop-build   # debe devolver vacío
```

### Tras el borrado

| Entorno | Acción |
|---------|--------|
| **Local** | Conservar rama `develop-build` como sandbox (`git branch`); **no** `git push` |
| **VPS test** | `/opt/dofus-2.0.0-build` hace checkout de **`develop`** |

```bash
cd /opt/dofus-2.0.0-build
git fetch origin
git checkout develop
git pull origin develop
```

## 4. Compile gate (tras merge manual PR #23)

```powershell
git checkout develop-compile
git merge develop
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

| Criterio | Esperado |
|----------|----------|
| Exit code | 0 |
| Errores CS | Ninguno |
| Imagen | `sunshine-emu-sunshine:latest` |

Registrar en `docs/vps-build-validation/YYYYMMDD-develop-compile-phase5-{sha}.md`.

## 5. Runtime gate (VPS test — no prod)

Path: `/opt/dofus-2.0.0-build`  
Rama: **`develop`** (no `develop-build` en origin)

1. Backup `sunshine-server`:
   - `/opt/backups/sunshine-server-YYYYMMDD-HHMM.json`
   - imagen `sunshine:prod-backup-YYYYMMDD-HHMM`
2. `docker stop sunshine-server` (liberar 2450 / 5557).
3. `git pull origin develop` en el path test.
4. Build y arranque:
   ```bash
   cd /opt/dofus-2.0.0-build/docker
   docker compose --env-file ../.env \
     -f docker-compose.yml \
     -f docker-compose.vps.yml \
     build sunshine
   docker compose --env-file ../.env \
     -f docker-compose.yml \
     -f docker-compose.vps.yml \
     up -d sunshine
   ```
5. Verificar en logs: EffectsLoader ~**161** efectos; puertos **2450** / **5557**.
6. Tras tests: restaurar prod desde `/opt/dofus-2.0.0/docker` si se detuvo el contenedor prod.

Ver [regression-checklist.md](./regression-checklist.md).

## 6. Variables de entorno

| Entorno | `FIGHT_COMBAT_LOG_ENABLED` | Logs combate |
|---------|---------------------------|--------------|
| VPS test (`develop`) | `true` | `docker/logs/fights/{fightId}.log` |
| Prod futuro (`/opt/dofus-2.0.0`) | `false` (recomendado) | deshabilitado |

## 7. Contenido pendiente de integrar (vía PRs #21–#25)

### Código (Fase 3)

| Capa | Archivo principal |
|------|-------------------|
| DOT / robo HP | `Game/Effects/Spells/Damages/HpSteal.cs` |
| Kill instantáneo | `Game/Effects/Spells/Others/Kill.cs` |
| Castigos | `Game/Fights/Buffs/Spells/PunishmentBuff.cs` |
| Invocaciones | `Game/Actors/Fighters/SummonedStaticMonster.cs`, `Summon.cs` |
| Logger | `Game/Fights/Diagnostics/FightCombatLogger.cs` |

### Documentación

- `docs/effects-audit-phase1/` … `docs/effects-integration-phase5/`
- `docs/vps-build-validation/`

## 8. Rollback (referencia — prod futuro)

No ejecutar en Fase 5. Para deploy futuro en `/opt/dofus-2.0.0`:

1. `docker tag sunshine:current sunshine:prod-backup-YYYYMMDD-HHMM`
2. `git checkout <sha-anterior>` en el path prod
3. `docker compose … up -d --build sunshine`
4. Verificar puertos; si falla, `docker compose up` con imagen backup

Patrón: [20260605-develop-build-4d12fde.md](../vps-build-validation/20260605-develop-build-4d12fde.md).

## 9. Fuera de alcance

- PR `develop` → `main`
- `docker compose up` en `/opt/dofus-2.0.0` (prod)
- Push de `develop-build` a origin
- Fixes Ola 2 (`FrigostBossMechanics` completo, empujes)
