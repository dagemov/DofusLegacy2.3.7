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
| `develop` | Integración (Fase 1 mergeada vía PR #14) |
| `develop-build` | Test VPS — ver [vps-build-validation/](./vps-build-validation/) |
| `feature/effects-audit-phase1` | Fase 1 docs (cerrada) |
| `feature/effects-catalog-phase2` | Fase 2 catálogo de efectos |

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

## PR pendiente típico

`feature/effects-audit-phase1` → `develop` (documentación auditoría efectos).
