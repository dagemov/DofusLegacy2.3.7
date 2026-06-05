# Validación VPS — Fase 3 engine-fix

| Campo | Valor |
|-------|--------|
| Fecha | 2026-05-30 |
| Rama | `develop-build` |
| Commit | `5a27f8f` (Fase 3 + fix csproj `a471096`) |
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

## Resultado (2026-06-05 17:16 UTC)

| Criterio | Estado |
|----------|--------|
| Build Docker | **OK** (~50 s) |
| Runtime boot | **OK** — READY en 38.75 s |
| EffectsLoader | **162** efectos cargados |
| Puertos 2450/5557 | **OK** |
| FightCombatLogger | **OK** — `FIGHT_COMBAT_LOG_ENABLED=true` en contenedor; ruta `docker/logs/fights/` escribible |
| Logs combate previo | **No** — test anterior a fix (runtime `:ro` + env sobrescrito a `false`) |
| Logs combate post-fix | **OK** — `docker/logs/fights/1.log` (fight=1): SOCKET, CAST, DISPATCH, DAMAGE |
| Cliente in-game | **Pendiente** — checklist humano en `validation-checklist.md` |
| Restauración prod | **Pendiente** — develop-build activo para test |

## Commits incluidos

| Commit | Capa |
|--------|------|
| `c8a05af` | docs scaffold |
| `e85d26d` | DOT HpSteal |
| `b0a7b5f` | Effect_Kill |
| `d7529d6` | Punishment |
| `8b32ee9` | Summons |
| `c646296` | FightCombatLogger |
