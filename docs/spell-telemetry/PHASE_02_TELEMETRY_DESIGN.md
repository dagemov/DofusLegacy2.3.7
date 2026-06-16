# Fase 2 — Diseño de telemetría spell/effect (implementado Phase 3)

**Fecha:** 2026-06-16  
**Rama:** `feature/spell-effect-telemetry-phase-1`  
**Estado:** Implementado — observación only, sin cambios de balance  
**Principio:** flags off en producción por defecto

## Arquitectura acordada

`SpellEffectTelemetry` es **fachada especializada** sobre `CombatTelemetry` (mismo canal `spell-casts-*.jsonl`, schema `spell-effect-telemetry-v1`).

---

## Capas obligatorias

| Código | Nombre | Eventos principales |
| --- | --- | --- |
| A | AI | `AiSpellCandidate`, `AiSpellRejected`, `AiSpellSelected` |
| V | Validation | `SpellCastAttempt`, `SpellValidationResult` |
| M | Target mask | `EffectTargetsResolved` |
| F | Formula | `DamageComputed`, `DamageApplied` |
| H | Handler | `EffectHandlerResult`, `SummonAttempt/Result` |
| B | Buff lifecycle | `BuffApplied`, `DelayedEffect*`, `BuffTriggered` |
| BD | Spell data | `SpellEffectPlanned` |

Guía operativa: [PHASE_04_LAB_ANALYSIS_GUIDE.md](./PHASE_04_LAB_ANALYSIS_GUIDE.md)

---

## 1. Objetivos

## Capas obligatorias

| Código | Nombre | Eventos principales |
| --- | --- | --- |
| A | AI | `AiSpellCandidate`, `AiSpellRejected`, `AiSpellSelected` |
| V | Validation | `SpellCastAttempt`, `SpellValidationResult` |
| M | Target mask | `EffectTargetsResolved` |
| F | Formula | `DamageComputed`, `DamageApplied` |
| H | Handler | `EffectHandlerResult`, `SummonAttempt/Result` |
| B | Buff lifecycle | `BuffApplied`, `DelayedEffect*`, `BuffTriggered` |
| BD | Spell data | `SpellEffectPlanned` |

Ver guía operativa: [PHASE_04_LAB_ANALYSIS_GUIDE.md](./PHASE_04_LAB_ANALYSIS_GUIDE.md)

## 1. Objetivos

1. Intento de cast (permitido/rechazado + razón)
2. Efectos del spell (raw data + crítico vs normal)
3. Objetivos seleccionados y filtrados por mask
4. Outcome real (daño, buff, poison, summon, nada + razón)
5. Summons, DOT/glyph/trap, buffs/castigos — eventos dedicados

**No duplicar:** unificar en schema v2 JSONL; `FightCombatLogger` puede delegar o emitir mirror texto para consola lab.

---

## 2. Arquitectura propuesta

```text
┌─────────────────────────────────────────────────────────────┐
│  SpellEffectTelemetry (new, static/facade)                  │
│  - Enabled via SpellEffectTelemetryEnabled                  │
│  - Writes spell-effects-YYYYMMDD.jsonl                      │
│  - Schema: spell-effect-telemetry-v1                        │
└───────────────┬─────────────────────────────────────────────┘
                │ called from
    ┌───────────┼───────────┬──────────────┬────────────────┐
    ▼           ▼           ▼              ▼                ▼
CanCastSpell  CastSpell  EffectDispatcher  InflictDamage   MonsterAttackAI
GetAffected   RollCrit   SpellEffectHandler AddBuff        Summon.cs
Actors        UseAP      Glyph/Trap         TriggerBuffs
```

### 2.1 Clase nueva (implementación Phase 3)

**Archivo propuesto:** `Sunshine.WorldServer/Game/Fights/Telemetry/SpellEffectTelemetry.cs`

Responsabilidades:

- Resolver flags y path (`SpellEffectTelemetryLogDirectory` default `{combat logs}/spell-effects/`)
- Serializar JSONL con campos tipados (no solo `detail=` string)
- Correlación via `correlationId` = `{fightId}-{turnId}-{monotonicSeq}`
- Thread-safe append (mismo patrón `CombatTelemetry`)

### 2.2 Relación con sistemas existentes

| Sistema | Acción |
| --- | --- |
| `CombatTelemetry` | Mantener turn flow; opcionalmente añadir `correlationId` en SpellCastStarted |
| `FightCombatLogger` | Lab console — llamar `SpellEffectTelemetry` desde mismos hooks o deprecar gradualmente campos duplicados |
| `CombatTelemetryAnalyzer` | Extender con parser `spell-effects-*.jsonl` + reportes por capa |

---

## 3. Schema JSONL v1

**Archivo:** `spell-effects-YYYYMMDD-HHmmss.jsonl`  
**schemaVersion:** `spell-effect-telemetry-v1`

### 3.1 Campos comunes (todos los eventos)

| Campo | Tipo | Descripción |
| --- | --- | --- |
| `schemaVersion` | string | `spell-effect-telemetry-v1` |
| `timestampUtc` | ISO-8601 | |
| `event` | string | Nombre evento |
| `correlationId` | string | `{fightId}-{turnId}-{seq}` |
| `fightId` | int | |
| `turnId` | string | `{round}-{actorId}` |
| `threadId` | int | |

### 3.2 Eventos de cast

#### `SpellCastAttempt`

Emitido al entrar a `CastSpell` (antes de validación) o al recibir cast IA.

| Campo | Tipo |
| --- | --- |
| `casterId`, `casterName`, `casterType`, `casterTeam` | |
| `casterCell` | short |
| `spellId`, `spellLevel`, `spellName` | opcional name desde template |
| `targetCell` | short |
| `targetFighterId` | int? |
| `apBefore` | int |
| `isCriticalRoll` | pending until roll |
| `source` | `Player` / `AI` / `Mark` / `Trigger` |

#### `SpellValidationResult`

Emitido al salir de `CanCastSpell`.

| Campo | Tipo |
| --- | --- |
| `allowed` | bool |
| `resultCode` | string (`SpellCastResult` enum) |
| `rejectLayer` | `Turn` / `AP` / `Cell` / `State` / `Zone` / `History` / `BombLimit` |
| `detail` | string opcional (ej. `needFreeCell=true cellOccupied=true`) |
| `lineOfSight` | bool? si se añade check |
| `inZone` | bool |
| `historyState` | string opcional (recast count) |

#### `SpellCastCompleted`

| Campo | Tipo |
| --- | --- |
| `critical` | `NORMAL` / `CRITICAL_HIT` / `CRITICAL_FAIL` |
| `apAfter` | int |
| `handlerPath` | `Custom` / `Generic` |
| `customHandlerType` | string? |
| `effectCount` | int |
| `durationMs` | long |

### 3.3 Eventos de efecto

#### `SpellEffectPlanned`

Por cada `Effect` en lista normal/critical **antes** de dispatch.

| Campo | Tipo |
| --- | --- |
| `effectId`, `effectName` | |
| `diceNum`, `diceFace`, `value` | |
| `duration`, `delay` | |
| `targetMask` | int (raw) |
| `targetMaskResolved` | string humano (`ALLY_ALL`, …) |
| `zoneShape`, `zoneSize`, `zoneMinSize` | |
| `isCriticalEffect` | bool |

#### `EffectTargetsResolved`

Post `GetAffectedActors`.

| Campo | Tipo |
| --- | --- |
| `effectId` | |
| `targetCell` | |
| `candidatesInZone` | int |
| `targets[]` | array `{fighterId, name, type, team, cell, isSummon, ownerId, includedByMask, hpBefore}` |
| `filteredOut[]` | `{fighterId, reason}` — fighters en celda pero excluidos por mask |

#### `EffectHandlerResult`

Post `Apply()` (o catch).

| Campo | Tipo |
| --- | --- |
| `effectId` | |
| `handlerType` | CLR name |
| `outcome` | `Applied` / `HandlerMissing` / `NoTargets` / `Exception` |
| `error` | string? |

### 3.4 Eventos de outcome

#### `DamageComputed`

Pre aplicación resistencias.

| Campo | Tipo |
| --- | --- |
| `sourceId`, `targetId` | |
| `effectId`, `spellId` | |
| `school` | |
| `diceNum`, `diceFace`, `fixedBonus` | |
| `rolledAmount` | int |
| `formulaNotes` | string? (ej. `IopWrathState51 +80`) |

#### `DamageApplied`

| Campo | Tipo |
| --- | --- |
| `finalAmount` | |
| `hpBefore`, `hpAfter` | |
| `wasPoison`, `wasShielded` | |
| `resistanceApplied` | bool |

#### `HealApplied`, `BuffApplied`, `StateChanged`

Análogos con campos mínimos documentados en matriz Phase 4.

### 3.5 Poison / glyph / trap / delayed

#### `DelayedEffectScheduled`

| Campo | Tipo |
| --- | --- |
| `kind` | `DOT` / `HOT` / `TriggerBuff` / `Glyph` / `Trap` |
| `creatorId`, `carrierId` | |
| `effectId`, `spellId` | |
| `expectedTrigger` | `TURN_BEGIN` / `TURN_END` / `MOVE` / `MARK` |
| `expectedTurnRound` | int? |
| `durationRemaining` | short |
| `expectedDamageMin`, `expectedDamageMax` | |

#### `DelayedEffectTick`

| Campo | Tipo |
| --- | --- |
| `kind` | |
| `scheduledRound`, `actualRound` | |
| `executed` | bool |
| `damageExpected`, `damageFinal` | |
| `skipReason` | `Dead` / `BuffExpired` / `NotTriggered` |

#### `DelayedEffectExpired`

`executed=false` si nunca tickó.

### 3.6 Summons

#### `SummonAttempt`

| Campo | Tipo |
| --- | --- |
| `spellId`, `monsterTemplateId` | |
| `ownerId`, `team` | |
| `chosenCell`, `fallbackCell` | |
| `summonCount`, `summonLimit`, `usesSlot` | |

#### `SummonResult`

| Campo | Tipo |
| --- | --- |
| `success` | bool |
| `summonFighterId` | |
| `failReason` | `NoFreeCell` / `LimitReached` / `InvalidTemplate` / … |
| `aiType` | string? |

#### `AiSummonProbe` (boss/mob)

| Campo | Tipo |
| --- | --- |
| `monsterId`, `fighterId` | |
| `spellId` | |
| `probeResult` | `OK` / `CanCastSpell code` |
| `selected` | bool |

### 3.7 Buffs / castigos

#### `BuffApplied`

| Campo | Tipo |
| --- | --- |
| `buffId`, `buffKind` | |
| `sourceSpellId`, `effectId` | |
| `statAffected`, `valueCalculated` | |
| `duration`, `stackable` | |
| `triggerCondition` | |

#### `BuffTriggered`

| Campo | Tipo |
| --- | --- |
| `triggerType` | `AfterDamaged`, `TURN_BEGIN`, … |
| `fired` | bool |
| `bonusAppliedToStats` | bool |
| `statsSnapshot` | optional `{stat, before, after}` |

---

## 4. Flags de configuración

| Clave | Env | Default prod | Default lab |
| --- | --- | --- | --- |
| `SpellEffectTelemetryEnabled` | `SPELL_EFFECT_TELEMETRY_ENABLED` | `false` | `true` if `COMBAT_HEALTH_LAB=1` |
| `SpellEffectTelemetryLogDirectory` | `SPELL_EFFECT_TELEMETRY_DIR` | `{BaseDirectory}/logs/combat/spell-effects` | `Infrastructure/logs/combat/spell-effects` |
| `SpellEffectTelemetryLogTargets` | — | `true` | `true` |
| `SpellEffectTelemetryLogAiProbes` | — | `false` | `true` (verbose IA) |
| `SpellEffectTelemetryLogDamageDetail` | — | `true` | `true` |
| `SpellEffectTelemetryConsoleMirror` | — | `false` | `true` |

**Master switch:** si `SpellEffectTelemetryEnabled=false`, cero overhead (early return, sin alloc).

**Producción VPS:** solo activar en ventana de QA; recolectar con script existente `collect-vps-combat-logs.ps1` extendido.

---

## 5. Puntos de instrumentación (Phase 3)

| Orden commit | Archivo | Eventos |
| --- | --- | --- |
| 1 | `SpellEffectTelemetry.cs` | scaffolding + schema |
| 2 | `FightActor.CanCastSpell` | `SpellValidationResult` |
| 3 | `FightActor.CastSpell` | `SpellCastAttempt`, `SpellCastCompleted` |
| 4 | `EffectDispatcher` + `EffectManager.GetAffectedActors` | `SpellEffectPlanned`, `EffectTargetsResolved` |
| 5 | `Damage` / `InflictDamage` | `DamageComputed`, `DamageApplied` |
| 6 | `Heal`, buff adds, `TriggerBuff` | heal/buff events |
| 7 | `DamageOverTimeBuff`, `HpSteal` callback | `DelayedEffect*` |
| 8 | `Summon.cs` | `SummonAttempt/Result` |
| 9 | `MonsterAttackAI` | `AiSummonProbe` |
| 10 | `Glyph.cs` / `Trap.cs` | `DelayedEffectScheduled` + tick |

**Reglas implementación:**

- No cambiar return values ni branching de combate
- Try/catch alrededor de log only
- Commits atómicos por capa de eventos

---

## 6. Ejemplos de logs esperados (casos reportados)

### 6.1 Látigo Osamodas inválido (spell 30)

```json
{"event":"SpellCastAttempt","spellId":30,"targetCell":245,"casterType":"CharacterFighter","apBefore":6}
{"event":"SpellValidationResult","allowed":false,"resultCode":"CELL_NOT_FREE","rejectLayer":"Cell","detail":"needTakenCell=false needFreeCell=true"}
```

**Diagnóstico:** capa validación celda/mask — no handler.

### 6.2 Sacrificada mata caster (spell 233)

```json
{"event":"SpellEffectPlanned","spellId":233,"effectId":"Effect_DamageAir","diceNum":31,"diceFace":50}
{"event":"EffectTargetsResolved","effectId":"Effect_DamageAir","targets":[{"fighterId":379,"includedByMask":true}]}
{"event":"SpellEffectPlanned","spellId":233,"effectId":"Effect_Kill"}
{"event":"EffectTargetsResolved","effectId":"Effect_Kill","targets":[{"fighterId":378,"includedByMask":true},{"fighterId":-1825,"includedByMask":true}]}
{"event":"DamageApplied","targetId":379,"finalAmount":48}
{"event":"DamageApplied","targetId":378,"finalAmount":9999,"formulaNotes":"Kill effect"}
```

**Diagnóstico:** si Kill lista incluye Sadida → capa `Kill.cs` / target mask, no fórmula daño.

### 6.3 Veneno no tickea

```json
{"event":"SpellEffectPlanned","effectId":"Effect_StealHPEarth","duration":3}
{"event":"BuffApplied","buffKind":"TriggerBuff","triggerCondition":"TURN_BEGIN"}
{"event":"DelayedEffectScheduled","kind":"TriggerBuff","expectedTrigger":"TURN_BEGIN","durationRemaining":3}
// ... silencio — no DelayedEffectTick
{"event":"DelayedEffectExpired","executed":false,"skipReason":"NotTriggered"}
```

**Diagnóstico:** capa trigger StartTurn — no handler veneno específico.

### 6.4 Rekop fórmula rota (spell 114)

```json
{"event":"SpellCastCompleted","spellId":114,"critical":"CRITICAL_HIT"}
{"event":"SpellEffectPlanned","isCriticalEffect":true,"diceNum":10,"diceFace":14}
{"event":"DamageComputed","rolledAmount":12,"diceNum":10,"diceFace":14}
{"event":"DamageApplied","finalAmount":12}
```

Comparar con cast NORMAL — si crítico usa dados menores → capa BD/critical effects path.

### 6.5 Ira del Yopuka segunda lanzada (spell 159)

```json
{"event":"SpellCastCompleted","spellId":159,"customHandlerType":null,"handlerPath":"Generic"}
{"event":"DamageComputed","formulaNotes":"IopWrathState51 +80","diceNum":95,"rolledAmount":412}
```

Segundo cast en misma pelea — verificar `State_51` en `StateChanged` y si `+80` aplicó.

Si `ColereHandler` nunca aparece → mismatch ID 143 vs 159.

### 6.6 Sacrificio Muñequero en jugador normal (spell 198)

```json
{"event":"EffectTargetsResolved","spellId":198,"targetMask":3840,"targets":[{"fighterId":379,"type":"CharacterFighter","isSummon":false,"includedByMask":true}]}
```

**Diagnóstico:** mask/default GetAffectedActors — no AI.

### 6.7 Boss no invoca (ej. Snifter Cell)

```json
{"event":"AiSummonProbe","monsterId":1234,"spellId":401,"probeResult":"NOT_IN_ZONE","selected":false}
{"event":"AiSummonProbe","monsterId":1234,"spellId":401,"probeResult":"OK","selected":true}
{"event":"SummonAttempt","spellId":401,"monsterTemplateId":456}
{"event":"SummonResult","success":false,"failReason":"NoFreeCell"}
```

O sin probes → IA nunca iteró spell summon (capa AI spell list).

---

## 7. Analyzer / operación

Extender `Infrastructure/scripts/CombatTelemetryAnalyzer/`:

- Input: `spell-effects-*.jsonl`
- Output: `spell-effect-report.md` con secciones:
  - Casts rechazados por reason
  - Effects sin targets
  - DOT scheduled vs ticked
  - Summon fail reasons
  - Damage outliers (critical < normal)

Script lab:

```powershell
$env:SPELL_EFFECT_TELEMETRY_ENABLED = "true"
$env:COMBAT_HEALTH_LAB = "1"
# pelea manual...
.\infrastructure\artifacts\combat-health\analyze-combat-telemetry.ps1 -IncludeSpellEffects
```

---

## 8. Criterios de aceptación Phase 3 (implementación)

- [ ] Build OK con flags off (zero regression)
- [ ] Un combate manual Sadida+Osa genera JSONL parseable
- [ ] Casos 6.1–6.7 producibles en lab (aunque bug persista)
- [ ] PR solo telemetría — sin cambios fórmulas/handlers balance
- [ ] Docs Phase 4 matriz alineada con eventos reales

---

## 9. Referencias

- [combat-log-schema.md](../combat-sanitization/combat-log-schema.md) — baseline Phase 2
- [PHASE_01_SPELL_PIPELINE_MAP.md](./PHASE_01_SPELL_PIPELINE_MAP.md)
- [PHASE_03_MANUAL_TEST_MATRIX.md](./PHASE_03_MANUAL_TEST_MATRIX.md)
