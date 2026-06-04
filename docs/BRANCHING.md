# Flujo de ramas — DofusLegacy 2.3.7

Convención acordada para desarrollo en este repositorio.

## Ramas

| Rama | Uso |
|------|-----|
| `main` | Producción / releases estables |
| `develop` | **Integración y desarrollo** — destino de PRs de features |
| `feature/*` | Trabajo acotado (ej. `feature/effects-audit-phase1` = auditoría Fase 1, solo docs) |

## Flujo habitual

1. Crear o usar `feature/nombre` desde `develop`.
2. Abrir **PR → `develop`** (no directo a `main`).
3. Tras revisión, merge en `develop`.
4. Cuando el equipo acuerde release: **PR `develop` → `main`**.

## Ramas de referencia (2026-05-30)

- `develop` @ `b5cb3e1` — base integración (website, Sunshine, stack VPS).
- `feature/effects-audit-phase1` — 1 commit por encima: `docs/effects-audit-phase1/` (Fase 1, sin parches de combate).

## Comandos rápidos

```powershell
git checkout develop
git pull origin develop

git checkout feature/effects-audit-phase1
git pull origin feature/effects-audit-phase1
```

## PR pendiente típico

`feature/effects-audit-phase1` → `develop` (documentación auditoría efectos).
