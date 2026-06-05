# Flujo de ramas — DofusLegacy 2.3.7

Convención acordada para desarrollo en este repositorio (migración pipeline efectos → **`devp`**, 2026-05-30).

## Ramas

| Rama | Uso |
|------|-----|
| `main` | Producción / releases estables |
| **`devp`** | **Integración** — destino de PRs `feature/*` (items builder + pipeline efectos) |
| `feature/*` | Entregable por fase → PR a **`devp`** |
| **`devp-compile`** | **Solo local** — compile gate Docker (no se pushea) |
| VPS test | `/opt/dofus-2.0.0-build` — checkout **`devp`** |

## Política origin

| Regla | Detalle |
|-------|---------|
| PRs en GitHub | Solo hacia **`devp`** |
| Merge | **Solo manual** por el equipo — nunca por agente/script |
| Cerrar PRs | **No** cerrar PRs del pipeline — deben permanecer **abiertas** en `devp` |
| `origin/develop` | **Eliminada** tras abrir PRs #26–#30 |
| `origin/develop-build` | Eliminada previamente — no pushear |
| `devp` → `main` | Fuera de alcance hasta acuerdo del equipo |

## PRs abiertas — pipeline efectos (→ `devp`)

| PR | Fase | Head | Base | Estado |
|----|------|------|------|--------|
| #26 (est.) | 1 | `feature/effects-audit-phase1` | **`devp`** | **abierta** |
| #27 | 2 | `feature/effects-catalog-phase2` | **`devp`** | **abierta** |
| #28 | 3 | `feature/effects-engine-fix-phase3` | **`devp`** | **abierta** |
| #29 | 4 | `feature/effects-validation-phase4` | **`devp`** | **abierta** |
| #30 | 5 | `feature/effects-integration-phase5` | **`devp`** | **abierta** |

PRs históricas #14–#25 (base `develop`): cerradas. El trabajo vive en las ramas `feature/*` recreadas desde `devp` @ `cf69aa1`.

**Orden de merge recomendado (manual):** #26 → #27 → #28 → #29 → #30.

## Flujo habitual

1. `devp` = línea de integración (base actual: `cf69aa1`, items builder PR #15).
2. Crear `feature/*` desde `devp`.
3. Abrir **PR → `devp`** — dejar **abierta** hasta revisión.
4. Merge **solo** cuando el equipo apruebe (en orden 1→5 para el pipeline efectos).
5. Tests runtime en VPS `/opt/dofus-2.0.0-build` con checkout **`devp`**.

## Tags de respaldo (pre-migración)

| Tag | Contenido |
|-----|-----------|
| `backup/effects-phase1-pre-devp` | Tip anterior `feature/effects-audit-phase1` |
| `backup/effects-phase5-pre-devp` | Tip anterior `feature/effects-integration-phase5` |

## Test VPS (sandbox)

1. Backup `sunshine-server` si aplica
2. Checkout **`devp`** en `/opt/dofus-2.0.0-build`
3. `docker compose build sunshine && up -d sunshine`
4. Puertos **2450** / **5557**

Ver [effects-integration-phase5/README.md](./effects-integration-phase5/README.md).

## Comandos rápidos

```powershell
git fetch origin
git checkout devp
git reset --hard origin/devp

git checkout devp-compile
git merge feature/effects-engine-fix-phase3
cd docker
docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

```bash
# VPS test
cd /opt/dofus-2.0.0-build
git fetch origin +refs/heads/devp:refs/remotes/origin/devp
git checkout devp
git reset --hard origin/devp
```
