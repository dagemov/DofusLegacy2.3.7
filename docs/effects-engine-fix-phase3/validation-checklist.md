# Fase 3 — Checklist de validación

## Entorno

| Campo | Valor |
|-------|--------|
| Rama | `develop-build` |
| Path VPS | `/opt/dofus-2.0.0-build` |
| Puertos | 2450 / 5557 |
| Logger | `FIGHT_COMBAT_LOG_ENABLED=true` en `.env` |
| Ruta logs VPS | `docker/logs/fights/{fightId}.log` (host) = `/app/logs/fights/` (contenedor) |

## Por categoría

### DOT / robo HP (commit `e85d26d`)

- [x] Código: `Duration != 0` → `TriggerBuff` `TURN_BEGIN`
- [x] Código: dispatch en `StartTurn`
- [ ] In-game: veneno tick por turno
- [ ] In-game: robo 50% al caster cada tick
- [ ] Log: `TRIGGER type=TURN_BEGIN` + `DAMAGE`

### Muerte instantánea (commit `b0a7b5f`)

- [x] Código: handler `Effect_Kill` registrado
- [ ] In-game: glifo/trampa kill mata al pisar celda
- [ ] Log: `event=KILL`

### Castigos (commit `d7529d6`)

- [x] Código: tope por ronda `DiceFace`
- [x] Código: stat desde `Effect.DiceNum` (sin `SpellIdEnum`)
- [ ] In-game: bonus al recibir daño
- [ ] In-game: tope por ronda visible

### Invocaciones (commit `8b32ee9`)

- [x] Código: `SummonedStaticMonster` (`CanPlay=false`)
- [x] Código: `DiesAtTurnEnd` (`UseSummonSlot=false`)
- [ ] In-game: bloqueadora estática sin turno IA
- [ ] In-game: suicida muere al fin de turno
- [ ] Log: `SUMMON_DIE`

### Logger (commit `c646296`)

- [x] Código: `FightCombatLogger` + hooks
- [ ] VPS: archivo `runtime/logs/fights/{id}.log`
- [ ] VPS: eventos cast/socket en pelea de prueba

## Lectura de logs

```
[2026-05-30 12:00:00.000] fight=42 event=CAST caster=1 spell=180 cell=256
[2026-05-30 12:00:00.100] fight=42 event=DISPATCH caster=1 spell=180 effect=Effect_StealHPWater duration=3
[2026-05-30 12:00:05.000] fight=42 event=TRIGGER type=TURN_BEGIN buff=3 target=2 effect=Effect_StealHPWater
[2026-05-30 12:00:05.050] fight=42 event=DAMAGE src=1 tgt=2 amount=35 school=Water
[2026-05-30 12:00:05.100] fight=42 event=SOCKET msg=GameActionFightSpellCastMessage recipients=2
```

## Restaurar prod

Tras sesión: `docker stop` contenedor build → `docker compose up` desde `/opt/dofus-2.0.0/docker`.
