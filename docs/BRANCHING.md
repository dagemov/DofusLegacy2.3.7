# Flujo de ramas — DofusLegacy 2.3.7

Convención acordada para desarrollo en este repositorio.

## Ramas

| Rama | Uso |
|------|-----|
| `main` | Producción / releases estables |
| `develop` | **Integración y desarrollo** — destino de PRs de features |
| `feature/*` | Entregable por fase (docs y/o código) → PR a `develop` |
| **`develop-compile`** | **Solo local** — acumula merges de cada fase para pruebas |
| **`develop-build`** | Remoto + **VPS** — validación compile/runtime (`/opt/dofus-2.0.0-build`) |

## Flujo habitual

1. Crear o usar `feature/nombre` desde `develop`.
2. Abrir **PR → `develop`** (no directo a `main`).
3. Tras revisión, merge en `develop`.
4. Cuando el equipo acuerde release: **PR `develop` → `main`**.

## Ramas de referencia

| Rama | Estado |
|------|--------|
| `develop` | Integración (Fase 1–3 mergeadas; PR #14, #16, #17) |
| `develop-compile` | **Local** — compile gate; merge acumulado de `develop` |
| `develop-build` | Test VPS — ver [vps-build-validation/](./vps-build-validation/) |
| `feature/effects-audit-phase1` | Fase 1 docs (cerrada) |
| `feature/effects-catalog-phase2` | Fase 2 catálogo (PR #16) |
| `feature/effects-engine-fix-phase3` | Fase 3 fixes motor (PR #17 mergeado) |
| `feature/effects-validation-phase4` | Fase 4 validación funcional (escenarios + resultados) |

## Fase 4 — Validación funcional

1. Merge Fase 3 en `develop` (PR #17).
2. `develop-compile`: `git merge develop` + `docker compose build sunshine` → compile OK.
3. `feature/effects-validation-phase4` → docs en `docs/effects-validation-phase4/`.
4. Tests in-game en `develop-build` (VPS); completar `validation-results.md`.
5. PR → `develop`.

Ver [effects-validation-phase4/README.md](./effects-validation-phase4/README.md).

## Test VPS (develop-build)

1. Backup `sunshine-server` en VPS
2. `docker stop sunshine-server`
3. Build/up desde `/opt/dofus-2.0.0-build` (rama `develop-build`)
4. Validar puertos **2450** / **5557**
5. Restaurar prod desde `/opt/dofus-2.0.0`

Ver [combat-fix-philosophy.md](./combat-fix-philosophy.md) y [PHASE-DELIVERY-TEMPLATE.md](./PHASE-DELIVERY-TEMPLATE.md).

## Comandos rápidos

```powershell
git checkout develop
git pull origin develop

git checkout feature/effects-audit-phase1
git pull origin feature/effects-audit-phase1
```

## develop-compile (compile gate)

```powershell
git checkout develop-compile
git merge develop
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

Registro: [20260605-develop-compile-phase4-dad4332.md](./vps-build-validation/20260605-develop-compile-phase4-dad4332.md).
