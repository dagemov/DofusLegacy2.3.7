# Validación VPS — Fase 3 engine-fix

| Campo | Valor |
|-------|--------|
| Fecha | 2026-05-30 |
| Rama | `develop-build` |
| Commit | `c646296` (Fase 3: commits #0–#6 + docs #7) |
| VPS | `174.138.35.107` |
| Path test | `/opt/dofus-2.0.0-build` |
| Path prod | `/opt/dofus-2.0.0` |

## Cambios Sunshine

6 commits de código: HpSteal DOT, Effect_Kill, Punishment, Summons, FightCombatLogger.

## Procedimiento

1. Backup imagen prod si sunshine activo desde prod
2. `docker stop sunshine-server` (liberar 2450/5557)
3. `cd /opt/dofus-2.0.0-build && git fetch && git checkout develop-build && git pull`
4. `cd docker && docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine`
5. `FIGHT_COMBAT_LOG_ENABLED=true docker compose … up -d sunshine`
6. Verificar logs: efectos cargados, puertos 2450/5557, pelea de prueba + `runtime/logs/fights/`
7. Restaurar prod al cerrar sesión

## Resultado

| Criterio | Estado |
|----------|--------|
| Build Docker | *(actualizar tras ejecución)* |
| Runtime boot | *(actualizar tras ejecución)* |
| EffectsLoader | *(actualizar tras ejecución)* |
| FightCombatLogger | *(actualizar tras ejecución)* |
| Cliente in-game | *(requiere test humano)* |
| Restauración prod | *(al cerrar sesión)* |

## Commits incluidos

| Commit | Capa |
|--------|------|
| `c8a05af` | docs scaffold |
| `e85d26d` | DOT HpSteal |
| `b0a7b5f` | Effect_Kill |
| `d7529d6` | Punishment |
| `8b32ee9` | Summons |
| `c646296` | FightCombatLogger |
