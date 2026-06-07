# Fase 5 — Notas de despliegue e integración

Procedimiento para cerrar el pipeline de efectos en **`devp`** (origin). No incluye release a `main` ni deploy prod VPS.

## Política de merge (obligatoria)

| Regla | Detalle |
|-------|---------|
| Subir PRs | Sí — visibles en GitHub |
| Merge automático | **No** — ni por agente ni por script |
| Cerrar PRs | **No** — las PR del pipeline deben permanecer **abiertas** en `devp` |
| Quién mergea | Solo el equipo, manualmente, tras revisión |

## 1. PRs abiertas — pipeline efectos (→ `devp`)

| Orden | PR | Head | Base | Estado |
|-------|-----|------|------|--------|
| 1 | #26 | `feature/effects-audit-phase1` | **`devp`** | **abierta** |
| 2 | #27 | `feature/effects-catalog-phase2` | **`devp`** | **abierta** |
| 3 | #28 | `feature/effects-engine-fix-phase3` | **`devp`** | **abierta** |
| 4 | #29 | `feature/effects-validation-phase4` | **`devp`** | **abierta** |
| 5 | #30 | `feature/effects-integration-phase5` | **`devp`** | **abierta** |

PRs #14–#25 (base `develop`): historial cerrado. Ramas recreadas desde `devp` @ `cf69aa1`.

**Regla:** ningún PR del pipeline con `base=main`.

### Verificación PRs abiertas

```powershell
gh pr list --state open --base devp --json number,headRefName,baseRefName
```

## 2. Migración `develop` → `devp` (ejecutada)

Metodología: cada `feature/effects-*-phaseN` = `devp` + commits de esa fase (cherry-pick, sin merge commits).

| Paso | Acción |
|------|--------|
| Backup | Tags `backup/effects-phase1-pre-devp`, `backup/effects-phase5-pre-devp` |
| Base | `origin/devp` @ `cf69aa1` |
| Compile gate | `devp-compile` merge Fase 3 → `docker compose build sunshine` **OK** |
| Force-push | 5 ramas `feature/*` a origin |
| Eliminar | `origin/develop` **después** de abrir PRs #26–#30 |

VPS sync:

```bash
cd /opt/dofus-2.0.0-build
git fetch origin +refs/heads/devp:refs/remotes/origin/devp
git checkout devp
git reset --hard origin/devp
```

## 3. Higiene origin

| Rama | Estado |
|------|--------|
| `origin/develop-build` | Eliminada previamente |
| `origin/develop` | Eliminar tras verificar PRs abiertas a `devp` |
| `origin/devp` | **Única** rama de integración |

### Tras migración

| Entorno | Acción |
|---------|--------|
| **Local compile** | `devp-compile` (no pushear) |
| **VPS test** | `/opt/dofus-2.0.0-build` checkout **`devp`** |

## 4. Compile gate (`devp-compile`)

```powershell
git checkout devp-compile
git merge feature/effects-engine-fix-phase3
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

| Criterio | Esperado |
|----------|----------|
| Exit code | 0 |
| Errores CS | Ninguno |
| Imagen | `sunshine-emu-sunshine:latest` |

Registros: `docs/vps-build-validation/20260530-develop-build-phase3-c646296.md`, `20260605-develop-compile-phase4-dad4332.md`.

## 5. Runtime gate (VPS test — no prod)

Path: `/opt/dofus-2.0.0-build`  
Rama: **`devp`**

1. Backup `sunshine-server`:
   - `/opt/backups/sunshine-server-YYYYMMDD-HHMM.json`
   - imagen `sunshine:prod-backup-YYYYMMDD-HHMM`
2. `docker stop sunshine-server` (liberar 2450 / 5557).
3. `git fetch && git checkout devp && git reset --hard origin/devp`.
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
5. Verificar en logs: EffectsLoader ~**162** efectos; puertos **2450** / **5557**.
6. Tras tests: restaurar prod desde `/opt/dofus-2.0.0/docker` si se detuvo el contenedor prod.

Ver [regression-checklist.md](./regression-checklist.md).

## 6. Variables de entorno

| Entorno | `FIGHT_COMBAT_LOG_ENABLED` | Logs combate |
|---------|---------------------------|--------------|
| VPS test (`devp`) | `true` | `docker/logs/fights/{fightId}.log` |
| Prod futuro (`/opt/dofus-2.0.0`) | `false` (recomendado) | deshabilitado |

## 7. Contenido integrado (vía PRs #26–#30)

### Código (Fase 3)

| Capa | Archivo principal |
|------|-------------------|
| DOT / robo HP | `Game/Effects/Spells/Damages/HpSteal.cs` |
| Kill instantáneo | `Game/Effects/Spells/Others/Kill.cs` |
| Castigos | `Game/Fights/Buffs/Spells/PunishmentBuff.cs` |
| Invocaciones | `Game/Actors/Fighters/SummonedStaticMonster.cs`, `Summon.cs` |
| Logger | `Game/Fights/Diagnostics/FightCombatLogger.cs` |

### Documentación y admin

- `docs/effects-audit-phase1/` … `docs/effects-integration-phase5/`
- `docs/vps-build-validation/`
- `docs/admin-commands.md`
- `docker/grant-admin-maestro-yaco.sql`

## 8. Rollback (referencia — prod futuro)

No ejecutar en Fase 5. Para deploy futuro en `/opt/dofus-2.0.0`:

1. `docker tag sunshine:current sunshine:prod-backup-YYYYMMDD-HHMM`
2. `git checkout <sha-anterior>` en el path prod
3. `docker compose … up -d --build sunshine`
4. Verificar puertos; si falla, `docker compose up` con imagen backup

## 9. Fuera de alcance

- PR `devp` → `main`
- `docker compose up` en `/opt/dofus-2.0.0` (prod)
- Fixes Ola 2 (`FrigostBossMechanics` completo, empujes)
