# Fase 4 — Matriz de pruebas manuales (spell telemetry)

**Fecha:** 2026-06-16  
**Prerequisito:** `SpellEffectTelemetryEnabled=true` (lab) o `FIGHT_COMBAT_LOG_ENABLED=true` (baseline actual)  
**Objetivo:** clasificar bugs por capa antes de fix

---

## Configuración lab

```powershell
$env:SPELL_EFFECT_TELEMETRY_ENABLED = "true"
$env:COMBAT_HEALTH_LAB = "1"
$env:FIGHT_TELEMETRY_ENABLED = "true"   # opcional turn flow
```

Logs: `Infrastructure/logs/combat/spell-casts/spell-casts-*.jsonl` (canal CombatTelemetry).

Guía de lectura: [PHASE_04_LAB_ANALYSIS_GUIDE.md](./PHASE_04_LAB_ANALYSIS_GUIDE.md)

---

## Leyenda capas

| Capa | Código clave | Síntoma en log |
| --- | --- | --- |
| **V** Validación | `CanCastSpell`, `SpellHistory` | `SpellValidationResult` / `CAST_FAIL reason=` |
| **M** Target mask | `EffectManager.GetAffectedActors` | `EffectTargetsResolved` — wrong types in targets |
| **H** Effect handler | `EffectDispatcher`, `*Handler.Apply` | `EffectHandlerResult` outcome / DISPATCH sin DAMAGE |
| **F** Fórmula | `Damage`, `DirectDamage`, `EffectDamageResolver` | `DamageComputed` dice vs `DamageApplied` gap |
| **B** Buff/trigger | `TriggerBuff`, `PunishmentBuff`, DOT | `DelayedEffectScheduled` sin `Tick` |
| **A** IA | `MonsterAttackAI` | `AiSummonProbe` — spell never selected |

---

## 1. Sadida

### 1.1 Sacrificada — explosión (spell 233)

| Campo | Valor |
| --- | --- |
| Caster | Muñeca La Sacrifiée (monstruo ~116) o Sadida invocador |
| Objetivo | Enemigo adyacente + Sadida fuera de zona kill |
| Spell ID | **233** (`Sacrifice`) |
| Invocación previa | **189** (`TheMadoll` / muñeca) |

**Resultado esperado (Dofus 2):**

- Daño en área al enemigo según dados air
- Muñeca muere (`Effect_Kill` solo en caster/muñeca)
- Sadida y enemigo sobreviven salvo daño air

**Logs esperados (Phase 3):**

```text
SpellCastAttempt spellId=233
SpellEffectPlanned effectId=Effect_DamageAir
SpellEffectPlanned effectId=Effect_Kill
EffectTargetsResolved (Kill) → solo fighterId muñeca
DamageApplied tgt=enemigo (parcial)
SUMMON_DIE / Kill target=muñeca
```

**Si bug (mata Sadida/lanzador):**

| Observación | Capa |
| --- | --- |
| Kill targets incluye Sadida/enemigo | **M** o **H** (`Kill.cs`) |
| Kill solo muñeca pero Sadida muere por DamageAir | **F** daño excesivo |
| No hay Effect_Kill en log | **H** handler no ejecutado |

---

### 1.2 Sacrificio Muñequero (spell 198)

| Campo | Valor |
| --- | --- |
| Caster | Sadida |
| Objetivo A | Invocación aliada (muñeca) |
| Objetivo B | Jugador aliado / monstruo normal |
| Spell ID | **198** (`DollySacrifice`) |

**Esperado:** efecto solo en invocaciones (transferencia/sacrificio según nivel BD).

**Logs:** `EffectTargetsResolved` — `isSummon=true` only.

| Bug | Capa |
| --- | --- |
| Jugador en targets con `includedByMask=true` | **M** |
| Targets correctos pero daño wrong | **H** / **F** |

---

### 1.3 Venenos Sadida (robo HP / DOT)

| Campo | Valor |
| --- | --- |
| Spells | Veneno raza (ej. `TheftofLife` 242, `Bramble` 188 con duration) |
| Objetivo | Enemigo |

**Esperado:** tick daño inicio turno enemigo × duración.

**Logs baseline (hoy):**

```text
event=BUFF_ADD kind=DOT / TriggerBuff
event=BUFF_TICK kind=DOT
event=DAMAGE
event=TRIGGER type=TURN_BEGIN
```

**Si no tickea:**

| Observación | Capa |
| --- | --- |
| No BUFF_ADD / DelayedEffectScheduled | **H** (Duration=0 en BD o handler) |
| BUFF_ADD pero no TRIGGER TURN_BEGIN | **B** StartTurn |
| TRIGGER pero amount=0 | **F** |

---

## 2. Osamodas — Látigo (spell 30)

| Campo | Valor |
| --- | --- |
| Caster | Osamodas |
| Target 1 | Invocación aliada |
| Target 2 | Invocación enemiga |
| Target 3 | Jugador/monstruo sin invocación |
| Spell ID | **30** (`Whip`) |

**Esperado:** buff/control sobre invocación según diseño oficial; cast válido en celda con summon.

**Si no lanza:**

```text
SpellValidationResult allowed=false resultCode=...
```

| resultCode | Capa |
| --- | --- |
| `CELL_NOT_FREE` / `NOT_IN_ZONE` | **V** |
| `HISTORY_ERROR` | **V** SpellHistory |
| Cast OK pero no targets | **M** |

---

## 3. Anutrof — Desinvocación (spell 46)

| Campo | Valor |
| --- | --- |
| Caster | Anutrof |
| Target A | Invocación |
| Target B | Monstruo normal |
| Spell ID | **46** (`Unsummoning`) |

**Esperado:** desinvoca solo invocaciones.

| Bug | Capa |
| --- | --- |
| Monstruo normal en `EffectTargetsResolved` | **M** |
| Targets OK pero no muere summon | **H** Unsummon handler |

---

## 4. Zurcarák — Rekop (spell 114)

| Campo | Valor |
| --- | --- |
| Caster | Zurcarák |
| Modo | Normal y crítico forzado (equipo / stats crítico) |
| Spell ID | **114** (`Rekop`) |

**Esperado:** crítico ≥ normal (mismo perfil dados salvo bonus crítico BD).

**Logs:**

```text
SpellCastCompleted critical=NORMAL|CRITICAL_HIT
DamageComputed rolledAmount=X
```

| Bug | Capa |
| --- | --- |
| CRITICAL_HIT con dados menores en SpellEffectPlanned | **F** / datos BD |
| Dados OK pero finalAmount invertido | **F** InflictDamage |
| Monto absurdo (>10k) | **F** formula / dice resolver |

---

## 5. Yopuka — Ira (spell 159)

| Campo | Valor |
| --- | --- |
| Caster | Yopuka (Iop) |
| Secuencia | 1ª lanzada → 2ª lanzada misma pelea |
| Spell ID | **159** (`IopsWrath`) |

**Esperado:** 2ª lanza escala fuerte (carga / estado).

**Logs clave:**

```text
StateChanged state=51
DamageComputed formulaNotes=IopWrathState51
customHandlerType=ColereHandler (ideal) vs handlerPath=Generic
```

| Bug | Capa |
| --- | --- |
| 2ª cast sin State_51 / sin +80 | **H** DirectDamage o ColereHandler no engancha (ID 143 vs 159) |
| State OK pero ~400 daño | **F** dados BD / resolver |
| Handler 143 activo en spell distinto | config **H** |

---

## 6. Sacrógrito — Castigos

| Campo | Valor |
| --- | --- |
| Spells | Castigo con `Effect_Punishment` (varios niveles) |
| Trigger | Recibir daño con castigo activo |

**Esperado:** stat bonus acumula con tope por ronda (`DiceFace`).

**Logs:**

```text
BUFF_ADD kind=PunishmentBuff
BuffTriggered triggerType=AfterDamaged bonusAppliedToStats=true
statsSnapshot agilidad: before → after
```

| Bug | Capa |
| --- | --- |
| BuffTriggered fired=false | **B** |
| fired=true pero stats iguales | **B** PunishmentBuff |
| Bonus enorme sin tope | **F** cap per round |

---

## 7. Sram — venenos / trampas / Engaño

### 7.1 Veneno + trampa

Reusar casos V-01..V-04 de [effects-validation-phase4/test-scenarios.md](../effects-validation-phase4/test-scenarios.md).

### 7.2 Engaño crítico vs normal

| Campo | Valor |
| --- | --- |
| Caster | Sram full set |
| Spell | Engaño (localizar id en spell book — buscar `Bluff` 110 o similar daño) |
| Compare | Daño normal vs crítico |

**Esperado:** crítico ≥ normal.

| Bug | Capa |
| --- | --- |
| `critical=CRITICAL_HIT` + lower `diceNum` en CriticalEffects | **F**/BD |
| Mismo effect list pero rolledAmount lower | **F** GenerateDamages |

---

## 8. Boss / mobs

### 8.1 Dragocerdo — veneno / autokill

| Campo | Valor |
| --- | --- |
| Mob | Dragocerdo (template id desde bestiario) |
| Mecánica | DOT + posible kill spell |

**Logs IA + effect:**

```text
AiActionSelected spellId=...
DelayedEffectTick kind=DOT
DamageApplied
```

| Sin AiActionSelected spell veneno | **A** |
| Cast OK, no DOT scheduled | **H** |
| DOT scheduled, no tick | **B** |

---

### 8.2 Snifter Cell — invoca tortugas

| Campo | Valor |
| --- | --- |
| Mob | Snifter Cell |
| Esperado | Summon tortuga turno X |

**Logs:**

```text
AiSummonProbe spellId=<summon spell from monster grade>
SummonAttempt → SummonResult success=true
SUMMON_CREATE
```

| Probes todos NOT_IN_ZONE | **A** + **V** |
| OK probe pero SUMMON_FAIL | **H** Summon.cs |
| Sin probes | **A** spell no en lista IA |

---

### 8.3 Cil — invoca arañas + veneno

Combinar 8.2 + sección veneno. Cil usa DOT — verificar `TriggerBuff` vs `DamageOverTimeBuff` según effect id en BD.

---

## 9. Checklist sesión QA

Por cada fila probada anotar:

- [ ] fightId
- [ ] Archivo log generado
- [ ] Bug reproducido Sí/No
- [ ] Capa primary (V/M/H/F/B/A)
- [ ] Evento smoking gun (1 línea)

---

## 10. Orden recomendado de prueba

1. Rekop + Ira Yopuka (daño puro — **F**)
2. Látigo + Desinvocación + Sacrificio Muñequero (**M**)
3. Sacrificada 233 (**H** Kill)
4. Veneno Sadida/Sram + Cil (**B**)
5. Castigos Sacrógrito (**B**)
6. Boss summons (**A**)

---

## Referencias

- [PHASE_02_TELEMETRY_DESIGN.md](./PHASE_02_TELEMETRY_DESIGN.md) — ejemplos JSON
- [fix-cura-sacrificada-combate.md](../fix-cura-sacrificada-combate.md)
- [effects-validation-phase4/test-scenarios.md](../effects-validation-phase4/test-scenarios.md)
- [combat-fix-philosophy.md](../combat-fix-philosophy.md)
