# Fase 4 — Escenarios de prueba

Cada escenario incluye: **ID**, **capa del motor**, archivos Sunshine, referencia Rollback, pasos de reproducción, logs esperados y estado inicial **PENDING**.

Convención `auditoria:` según [PHASE-DELIVERY-TEMPLATE.md](../PHASE-DELIVERY-TEMPLATE.md).

---

## 1. Venenos / DOT (capa: `HpSteal` + `TriggerFightBuffs`)

| ID | Escenario | Capa | Caso jugador (solo test) |
|----|-----------|------|--------------------------|
| V-01 | Tick daño al **inicio de turno** del portador | DOT / `TURN_BEGIN` | Cil, veneno raza, robo HP con `Duration > 0` |
| V-02 | Robo 50% PV al caster cada tick | DOT / cura | Mismo |
| V-03 | **Fin de turno** / decremento duración buff DOT | `TriggerBuff` + `DecrementsAllBuffsDuration` | Veneno multi-turno |
| V-04 | **Muerte por veneno** (PV → 0 por ticks acumulados) | `InflictDamage` → `TryKillIfNoHealth` | Cualquier DOT letal |

### Hilos de código (game)

| Archivo | Responsabilidad |
|---------|-----------------|
| `Game/Effects/Spells/Damages/HpSteal.cs` | Si `Duration != 0` → `TriggerBuff` `TURN_BEGIN` |
| `Game/Actors/Fighters/FightActor.cs` | `TriggerFightBuffs(TURN_BEGIN)` en `StartTurn` |
| `Game/Fights/Buffs/Customs/TriggerBuff.cs` | Callback tick |
| `Game/Fights/Diagnostics/FightCombatLogger.cs` | Eventos `TRIGGER`, `DAMAGE` |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Damages/StealHpEffectHandler.cs
ruta/actual/Game/Effects/Spells/Damages/HpSteal.cs
LINEAS: 17-41
Módulo: game
Evidencia: confirmado en diff (Fase 3)
```

### Pasos repro (V-01 / V-02)

1. Pelea PvM o PvP con personaje que aplique robo HP / veneno con duración > 0.
2. Aplicar efecto; verificar que el daño **no** sea solo instantáneo al cast.
3. Pasar turnos del portador; cada inicio de turno debe aplicar daño + cura 50% al caster.
4. Revisar `docker/logs/fights/{fightId}.log`: `TRIGGER type=TURN_BEGIN`, `DAMAGE`.

### Logs esperados

```
event=TRIGGER type=TURN_BEGIN effect=Effect_StealHP*
event=DAMAGE ...
```

---

## 2. Invocaciones (capa: `Summon` / `SummonedMonster`)

| ID | Escenario | Capa | Flags plantilla |
|----|-----------|------|-----------------|
| I-01 | **Sacrificada** (buff + IA summon) | `Sacrifice` + `Summon` | `Sacrifice.cs` |
| I-02 | **Hinchable** / bomba | `Summon` + `BombManager` | `Effect_SummonBomb` |
| I-03 | **Árbol** estático (sin turno IA) | `SummonedStaticMonster` | `CanPlay=false` |
| I-04 | **Doble** | `Double.cs` | `Effect_Double` |
| I-05 | **Suicida** fin de turno | `DiesAtTurnEnd` | `UseSummonSlot=false` |
| I-06 | Caso jugador Golosón | Misma capa que I-05 | **No** fix por `spellId` |

### Hilos de código (game)

| Archivo | Responsabilidad |
|---------|-----------------|
| `Game/Effects/Spells/Summon/Summon.cs` | Creación summon; rama `SummonedStaticMonster` |
| `Game/Actors/Fighters/SummonedStaticMonster.cs` | `CanPlayTurn => false` |
| `Game/Actors/Fighters/SummonedMonster.cs` | `DiesAtTurnEnd`, `Die()` |
| `Game/Actors/Fighters/FightActor.cs` | `StartTurn` skip IA estática; `EndTurn` suicide |
| `Game/Effects/Spells/Summon/Double.cs` | Doble Sadida |
| `Game/Effects/Spells/Buffs/Sacrifice.cs` | Sacrificio |

```text
auditoria:
ruta/rollback/Game/Fights/Fighters/SummonedStaticMonster.cs
ruta/actual/Game/Actors/Fighters/SummonedStaticMonster.cs
LINEAS: Rollback 7-14 / Sunshine 1-18
Módulo: game
```

### Pasos repro (I-03)

1. Invocar monstruo con `CanPlay=false` en plantilla (árbol bloqueador).
2. Cuando le toque turno en timeline: debe **saltar** IA y pasar turno sin actuar.
3. El summon permanece en mapa hasta muerte por daño.

### Pasos repro (I-05 / I-06)

1. Invocar monstruo con `UseSummonSlot=false` y `CanPlay=true`.
2. Turno del summon: ejecuta IA (ataque).
3. Al **fin de su turno**: `Die()` automático.
4. Log: `event=SUMMON_DIE`.

---

## 3. Castigos Sacrógrito (capa: `PunishmentBuff` + `Effect_Punishment`)

Todos son variantes del **mismo efecto** `Effect_Punishment`; un solo fix de capa (Fase 3).

| ID | Escenario | Caso prueba (`SpellIdEnum`) | Stat vía `Effect.DiceNum` |
|----|-----------|----------------------------|---------------------------|
| P-01 | Castigo **Osado** | `BoldPunishment` | Fuerza |
| P-02 | Castigo **Ágil** | `NimblePunishment` | Agilidad |
| P-03 | Castigo **Forzado** | `ForcedPunishment` | Según dados efecto |
| P-04 | Castigo **Espiritual** | `SpiritualPunishment` | Inteligencia |
| P-05 | Tope por **ronda** (`DiceFace`) | Cualquiera de arriba | No superar cap por ronda |
| P-06 | Daño % castigo (`PunishmentDamage`) | Hechizos daño castigo | Capa separada de buff reactivo |

### Hilos de código (game)

| Archivo | Responsabilidad |
|---------|-----------------|
| `Game/Effects/Spells/Buffs/PunishmentBoost.cs` | Aplica `PunishmentBuff`; stat desde `Effect.DiceNum` |
| `Game/Fights/Buffs/Spells/PunishmentBuff.cs` | `OnDamaged`; tope ronda `_boostThisRound` / `PerRoundCap` |
| `Game/Actors/Fighters/FightActor.cs` | `InflictDamage` → `OnDamaged` + `AFTER_ATTACKED` |
| `Game/Effects/Spells/Damages/PunishmentDamage.cs` | Daño % directo (no confundir con buff) |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Buffs/PunishmentEffectHandler.cs
ruta/actual/Game/Fights/Buffs/Spells/PunishmentBuff.cs
LINEAS: Rollback 19-47 / Sunshine OnDamaged
Módulo: game
```

### Pasos repro (P-01 a P-05)

1. Sacrógrito lanza castigo (buff `Effect_Punishment`).
2. Recibir daño en el turno; verificar subida de stat en UI.
3. Seguir recibiendo daño en la **misma ronda** hasta tope `DiceFace`: no debe pasar el cap.
4. Nueva ronda: cap se reinicia.
5. Log: `DAMAGE` + variación stats (mensajes combate).

---

## 4. Bosses (capa: `FrigostBossMechanics` — **Ola 2**)

| ID | Escenario | Archivos | Expectativa Fase 4 |
|----|-----------|----------|-------------------|
| B-01 | Invocaciones **requeridas** por fase | `FrigostBossMechanics.cs` | PENDING / posible FAIL |
| B-02 | Estados **vulnerabilidad** | `AddState.cs`, `StateBuff.cs` | PENDING |
| B-03 | **IA fase 2** | `MonsterAttackAI.cs` vs Rollback `Brain.cs` | PENDING / gap documentado |

```text
auditoria:
ruta/rollback/Game/Fights/AI/Brain.cs
ruta/actual/Game/Fights/Mechanics/FrigostBossMechanics.cs
Módulo: game
Evidencia: inferido — sin fix Fase 3
```

### Pasos repro (B-01)

1. Pelea boss Frigost documentado en `FrigostBossMechanics`.
2. Llevar a fase que fuerza summon (`ResolveForcedSummonMonsterId`).
3. Verificar invocación correcta y transición de fase.

**Nota:** Si FAIL, documentar en `validation-results.md` y planificar commit `fix(fights): hooks genericos boss` (Ola 2).

---

## 5. Mapas — casillas especiales

| ID | Escenario | Capa | Archivos |
|----|-----------|------|----------|
| M-01 | **Casilla de muerte** (glifo kill) | `Effect_Kill` | `Others/Kill.cs`, `FightActor.Kill()` |
| M-02 | Activación glifo al **pisar** celda | `TriggerMarks` / `MOVE` | `Fight.cs`, `Triggers/Glyph.cs` |
| M-03 | Trampa con kill interno | `Trap.cs`, `TrapSpawn.cs` | Mismo handler kill |
| M-04 | Glifo veneno / fin turno | `Glyph.cs` `TURN_BEGIN`/`TURN_END` | Lista IDs glifo Fase 1 |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Others/KillEffectHandler.cs
ruta/actual/Game/Effects/Spells/Others/Kill.cs
LINEAS: 1-18
Módulo: game
Evidencia: confirmado en diff (Fase 3)
```

### Pasos repro (M-01)

1. Colocar glifo/trampa con efecto `Effect_Kill` en celda.
2. Mover fighter a la celda (o activar por `MOVE`).
3. Fighter debe morir inmediatamente.
4. Log: `event=KILL`.

---

## 6. Empujes (capa: `Push` + secuencias — **gap Fase 3 / Ola 2**)

| ID | Escenario | Archivos | Gap conocido |
|----|-----------|----------|--------------|
| E-01 | **Colisión** entre fighters | `Moves/Push.cs` | — |
| E-02 | **Daño empuje** (bonus/reducción) | `Push.cs`, stats empuje | Rollback `PushEffectHandler` |
| E-03 | **Kill por colisión** en cadena | `Push` + `Effect_Kill` | `ActiveSequenceCount` / `ReadyChecker` |
| E-04 | Glifo disparado **durante** empuje | `Push.cs` `ShouldTriggerOnMove` | Orden secuencias |

```text
auditoria:
ruta/rollback/Game/Effects/Handlers/Spells/Movements/PushEffectHandler.cs
ruta/actual/Game/Effects/Spells/Moves/Push.cs
Módulo: game
Evidencia: confirmado en diff Fase 1 — secuencias pendientes Fase 3
```

### Pasos repro (E-01 / E-02)

1. Lanzar hechizo con empuje hacia fighter contra pared u otro fighter.
2. Verificar desplazamiento correcto en cliente.
3. Si hay daño por colisión: verificar cantidad coherente con stats empuje.

### Pasos repro (E-03)

1. Empujar hacia celda con glifo kill o fighter con 1 PV contra pared.
2. Verificar orden: empuje → daño colisión → muerte → sin turno colgado.
3. Si turno se cuelga: marcar FAIL y referenciar gap secuencias.

---

## Resumen por capa

| Capa | IDs escenario | Fix Fase 3 |
|------|---------------|------------|
| DOT / HpSteal | V-01–V-04 | Sí |
| Invocaciones | I-01–I-06 | Sí |
| Castigos | P-01–P-06 | Sí (buff); P-06 validar aparte |
| Bosses | B-01–B-03 | No (Ola 2) |
| Casillas kill | M-01–M-04 | Sí (M-01); M-04 con V-* |
| Empujes | E-01–E-04 | Parcial / Ola 2 |
