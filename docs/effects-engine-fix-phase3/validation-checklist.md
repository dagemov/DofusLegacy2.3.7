# Fase 3 — Checklist de validación

> Esqueleto — marcar en commit #7 tras test VPS.

## Entorno

- Rama: `develop-build` @ VPS `/opt/dofus-2.0.0-build`
- Puertos: 2450 / 5557
- Logs: `runtime/logs/fights/{fightId}.log` con `FIGHT_COMBAT_LOG_ENABLED=true`

## Por categoría

### DOT / robo HP (commit #1)

- [ ] Veneno: tick cada turno del portador
- [ ] Robo multi-turno: daño + cura 50% al caster por tick
- [ ] Log: evento `TRIGGER` `TURN_BEGIN` + `DAMAGE`

### Muerte instantánea (commit #2)

- [ ] Glifo/trampa con `Effect_Kill`: pisar celda mata
- [ ] Log: evento `KILL`

### Castigos (commit #3)

- [ ] `Effect_Punishment`: bonus al recibir daño
- [ ] Tope por ronda (`DiceFace`) respetado
- [ ] Log: `PUNISHMENT` o `DAMAGE` + variación stats

### Invocaciones (commit #4)

- [ ] Invocación estática (`CanPlay=false`): no ejecuta IA
- [ ] Suicida (`UseSummonSlot=false`): muere al fin de su turno
- [ ] Log: `SUMMON_DIE` / fin de turno

### Logger (commit #6)

- [ ] Archivo por `fightId` creado
- [ ] Cast + `EffectId` registrados
- [ ] Fan-out socket en mensajes clave

## Lectura de logs

```
[timestamp] fight=123 event=CAST spell=42 effect=StealHPWater duration=3
[timestamp] fight=123 event=TRIGGER type=TURN_BEGIN buff=7 target=456
[timestamp] fight=123 event=DAMAGE src=456 tgt=789 amount=42
[timestamp] fight=123 event=SOCKET msg=GameActionFightSpellCastMessage recipients=2
```

## Restaurar prod

Tras sesión: stop build → `compose up` desde `/opt/dofus-2.0.0` (ver docs VPS).
