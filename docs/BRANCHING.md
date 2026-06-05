# Flujo de ramas — DofusLegacy 2.3.7

Convención acordada para desarrollo en este repositorio.

## Ramas

| Rama | Uso |
|------|-----|
| `main` | Producción / releases estables (sin PR en pipeline efectos Fase 5) |
| `develop` | **Integración y desarrollo** — **único destino de PRs** en origin |
| `feature/*` | Entregable por fase (docs y/o código) → PR a `develop` |
| **`develop-compile`** | **Solo local** — acumula merges de cada fase; compile gate Docker |
| **`develop-build`** | **Solo local / VPS** — sandbox runtime; **no existe en origin** |

## Política origin (Fase 5)

- En GitHub **solo se envían PRs hacia `develop`**.
- **`origin/develop-build` eliminada** (2026-05-30). VPS test (`/opt/dofus-2.0.0-build`) hace checkout de **`develop`**.
- Release `develop` → `main`: acuerdo futuro del equipo, fuera del pipeline efectos Fase 5.

## Flujo habitual

1. Crear o usar `feature/nombre` desde `develop`.
2. Abrir **PR → `develop`** (no directo a `main`).
3. Tras revisión, merge en `develop`.
4. Tests runtime en sandbox local `develop-build` o VPS con código de `develop`.
5. Cuando el equipo acuerde release: **PR `develop` → `main`** (fuera de alcance Fase 5).

## Ramas de referencia

| Rama | Estado |
|------|--------|
| `develop` | Integración (Fases 1–4 mergeadas; PR #14–#18) |
| `develop-compile` | **Local** — compile gate |
| `develop-build` | **Local/VPS** — sin rama remota |
| `feature/effects-audit-phase1` | Fase 1 docs (cerrada, PR #14) |
| `feature/effects-catalog-phase2` | Fase 2 catálogo (PR #16) |
| `feature/effects-engine-fix-phase3` | Fase 3 fixes motor (PR #17) |
| `feature/effects-validation-phase4` | Fase 4 validación (PR #18 mergeado) |
| `feature/effects-integration-phase5` | Fase 5 integración (PR #19) |

## Fase 5 — Integración

1. Auditar PRs pipeline: `base=develop`.
2. Eliminar `origin/develop-build` si existía.
3. Merge PR #18 (Fase 4) en `develop`.
4. `feature/effects-integration-phase5` → docs en `docs/effects-integration-phase5/`.
5. Compile gate en `develop-compile`.
6. PR → `develop` (fin del flujo en origin).
7. Regression checklist en VPS test (checkout `develop`).

Ver [effects-integration-phase5/README.md](./effects-integration-phase5/README.md).

## Fase 4 — Validación funcional

1. Merge Fase 3 en `develop` (PR #17).
2. `develop-compile`: `git merge develop` + `docker compose build sunshine` → compile OK.
3. `feature/effects-validation-phase4` → docs en `docs/effects-validation-phase4/`.
4. Tests in-game en VPS `/opt/dofus-2.0.0-build` (checkout **`develop`**); completar `validation-results.md`.
5. PR → `develop` (PR #18 mergeado).

Ver [effects-validation-phase4/README.md](./effects-validation-phase4/README.md).

## Test VPS (sandbox)

1. Backup `sunshine-server` en VPS
2. `docker stop sunshine-server` (si libera puertos)
3. En `/opt/dofus-2.0.0-build`: `git checkout develop && git pull origin develop`
4. `docker compose build sunshine && up -d sunshine`
5. Validar puertos **2450** / **5557**
6. Restaurar prod desde `/opt/dofus-2.0.0` si aplica

Ver [combat-fix-philosophy.md](./combat-fix-philosophy.md) y [PHASE-DELIVERY-TEMPLATE.md](./PHASE-DELIVERY-TEMPLATE.md).

## Comandos rápidos

```powershell
git checkout develop
git pull origin develop

git checkout develop-compile
git merge develop
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

## Registros compile / VPS

- [20260605-develop-compile-phase4-dad4332.md](./vps-build-validation/20260605-develop-compile-phase4-dad4332.md)
- Fase 5: ver `docs/vps-build-validation/` tras compile gate
