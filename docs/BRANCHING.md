# Flujo de ramas — DofusLegacy 2.3.7

Convención acordada para desarrollo en este repositorio.

## Ramas

| Rama | Uso |
|------|-----|
| `main` | Producción / releases estables |
| `develop` | **Integración** — destino de PRs `feature/*` (recreada desde `main`) |
| `feature/*` | Entregable por fase → PR a `develop` |
| **`develop-compile`** | **Solo local** — compile gate Docker |
| **`develop-build`** | **Solo local / VPS** — sandbox runtime; **no existe en origin** |

## Política origin (2026-06-05)

| Regla | Detalle |
|-------|---------|
| PRs en GitHub | Solo hacia **`develop`** |
| Merge | **Solo manual** por el equipo — nunca por agente/script |
| `develop` | Recreada desde `main` @ `1f998cd` (reset pipeline efectos) |
| `origin/develop-build` | Eliminada — no pushear |
| `develop` → `main` | Fuera de alcance hasta acuerdo del equipo |

## PRs abiertas — pipeline efectos

| PR | Fase | Head | Base | Estado |
|----|------|------|------|--------|
| [#21](https://github.com/dagemov/DofusLegacy2.3.7/pull/21) | 1 | `feature/effects-audit-phase1` | `develop` | **abierta** |
| [#22](https://github.com/dagemov/DofusLegacy2.3.7/pull/22) | 2 | `feature/effects-catalog-phase2` | `develop` | **abierta** |
| [#23](https://github.com/dagemov/DofusLegacy2.3.7/pull/23) | 3 | `feature/effects-engine-fix-phase3` | `develop` | **abierta** |
| [#24](https://github.com/dagemov/DofusLegacy2.3.7/pull/24) | 4 | `feature/effects-validation-phase4` | `develop` | **abierta** |
| [#25](https://github.com/dagemov/DofusLegacy2.3.7/pull/25) | 5 | `feature/effects-integration-phase5` | `develop` | **abierta** |

PRs históricas #14–#18 y #19–#20: cerradas (algunas mergeadas en el `develop` anterior). El trabajo vive en las PRs #21–#25.

**Orden de merge recomendado (manual):** #21 → #22 → #23 → #24 → #25.

## Flujo habitual

1. `develop` = línea de integración (hoy igual a `main` hasta merges manuales).
2. Crear `feature/*` desde `develop`.
3. Abrir **PR → `develop`** — dejar **abierta** hasta revisión.
4. Merge **solo** cuando el equipo apruebe (en orden 1→5 para el pipeline efectos).
5. Tests runtime en sandbox `develop-build` o VPS `/opt/dofus-2.0.0-build`.

## Reset de `develop` (ejecutado 2026-06-05)

```powershell
git push origin --delete develop
git push origin 1f998cd:refs/heads/develop
```

VPS sync:

```bash
cd /opt/dofus-2.0.0-build
git fetch origin +refs/heads/develop:refs/remotes/origin/develop
git reset --hard origin/develop
```

## Test VPS (sandbox)

1. Backup `sunshine-server` si aplica
2. Checkout `develop` en `/opt/dofus-2.0.0-build`
3. `docker compose build sunshine && up -d sunshine`
4. Puertos **2450** / **5557**

**Nota:** Tras el reset, `develop` no incluye fixes Fase 3 hasta merge manual de PR #23.

Ver [effects-integration-phase5/README.md](./effects-integration-phase5/README.md).

## Comandos rápidos

```powershell
git fetch origin
git checkout develop
git reset --hard origin/develop

git checkout develop-compile
git merge develop
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```
