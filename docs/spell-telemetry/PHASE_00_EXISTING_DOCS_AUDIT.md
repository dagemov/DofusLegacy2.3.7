# Fase 0 — Auditoría documental existente (hechizos / combate / telemetría)

**Fecha:** 2026-06-16  
**Repo:** `DofusLegacy2.3.7`  
**Estado:** Lectura obligatoria completada — **no implementar fixes de balance hasta telemetría spell-level**

---

## 1. Inventario de documentación relevante

### 1.1 Combate y telemetría (macro sanitization)

| Documento | Contenido | Relevancia spell audit |
| --- | --- | --- |
| [combat-system-audit.md](../combat-sanitization/combat-system-audit.md) | Mapa Sunshine vs Rollback; gaps ReadyChecker, telemetría, IA | **Alta** — arquitectura base |
| [combat-telemetry-plan.md](../combat-sanitization/combat-telemetry-plan.md) | Plan Phase 1–5; spell-casts en Phase 4 | **Alta** — roadmap previo |
| [combat-telemetry-phase2.md](../combat-sanitization/combat-telemetry-phase2.md) | `CombatTelemetry` JSONL implementado | **Alta** — baseline existente |
| [combat-log-schema.md](../combat-sanitization/combat-log-schema.md) | Schema `combat-telemetry-phase2-jsonl-1` | **Alta** — extender, no reemplazar |
| [combat-readychecker-phase3.md](../combat-sanitization/combat-readychecker-phase3.md) | ReadyChecker + flags | Media — turn flow, no spells |
| [combat-fix-philosophy.md](../combat-fix-philosophy.md) | Fix por capa, no por hechizo | **Crítica** — regla de equipo |
| [combat/README.md](../combat/README.md) | Índice fases combat sanitization | Navegación |
| [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md) | Gate PvM real pendiente operador | Media |
| [vps-combat-telemetry-operations.md](../combat-sanitization/vps-combat-telemetry-operations.md) | Ops VPS logs | Media — deploy controlado |

### 1.2 Motor de efectos (auditoría + fixes)

| Documento | Contenido | Relevancia |
| --- | --- | --- |
| [effects-audit-phase1/effect-engine-overview.md](../effects-audit-phase1/effect-engine-overview.md) | Pipeline cast/dispatch/buffs/summons vs Rollback | **Alta** |
| [effects-audit-phase1/affected-systems.md](../effects-audit-phase1/affected-systems.md) | Sistemas afectados por diff | Alta |
| [effects-catalog-phase2/execution-pipeline.md](../effects-catalog-phase2/execution-pipeline.md) | Etapas 1–6 del pipeline | **Alta** |
| [effects-catalog-phase2/effect-categories.md](../effects-catalog-phase2/effect-categories.md) | Taxonomía DOT/buff/summon/trap | **Alta** |
| [effects-catalog-phase2/effect-id-mapping.md](../effects-catalog-phase2/effect-id-mapping.md) | EffectId → handler | Alta |
| [effects-engine-fix-phase3/README.md](../effects-engine-fix-phase3/README.md) | Commits Fase 3 por capa | **Alta** — fixes ya hechos |
| [effects-engine-fix-phase3/root-cause-analysis.md](../effects-engine-fix-phase3/root-cause-analysis.md) | Causa raíz DOT/Kill/Punishment/Summon | **Alta** |
| [effects-validation-phase4/test-scenarios.md](../effects-validation-phase4/test-scenarios.md) | Escenarios V/I/P/G manual | **Alta** — base matriz pruebas |
| [effects-validation-phase4/validation-results.md](../effects-validation-phase4/validation-results.md) | Estado PENDING mayoría | Alta |

### 1.3 Fixes documentados por síntoma

| Documento | Síntoma | Estado doc |
| --- | --- | --- |
| [fix-cura-sacrificada-combate.md](../fix-cura-sacrificada-combate.md) | Néctar cura enemigos; Sacrificada `Effect_Kill` mata rival; slot invocación | Fix documentado (jun 2026) |
| [patch-integration-changelog.md](../patch-integration-changelog.md) | PR #32 — DOT/HOT, IA, rates, handlers | Histórico |
| [server-rates/pr32-inspection.md](../server-rates/pr32-inspection.md) | Rates XP/drop/arma; `SpellHistory` | Media — no daño spell |

### 1.4 Integración / PRs

| Documento | Contenido |
| --- | --- |
| [integration/massive-devp-sync-20260607.md](../integration/massive-devp-sync-20260607.md) | PRs #32–#40, orden merge |
| [integration/pr39-readychecker-merge-resolution.md](../integration/pr39-readychecker-merge-resolution.md) | Merge #32 + #39; preservar `FightCombatLogger` |
| [handoffs/AGENT_HANDOFF.md](../handoffs/AGENT_HANDOFF.md) | Estado macro; next = Phase 4 spell/summon |

### 1.5 Admin / spell builder (datos, no runtime)

| Documento | Notas |
| --- | --- |
| [admin-tools/spell-builder/spell-builder-audit.md](../admin-tools/spell-builder/spell-builder-audit.md) | Editor admin spells |
| [admin-tools/spells-builder/README.md](../admin-tools/spells-builder/README.md) | Herramienta spells |

**No encontrado en repo:** `docs/informe-combate-osa-sadida.html` (referenciado en fix Sacrificada).

---

## 2. Fixes ya aplicados (código + docs)

### 2.1 PR #32 — Integración parche compañero (merge 2026-06-07)

Commits relevantes combate/hechizos:

| Commit | Área | Cambio |
| --- | --- | --- |
| `16c4a68` | DOT/HOT | `DamageOverTimeBuff`, `HealOverTimeBuff`, tick en `StartTurn` |
| `16c4a68` | Kill | Handler genérico `Effect_Kill` |
| `16c4a68` | Sacrifice | `SacrificeDamage` (Effect_109) |
| `2d18d32` | Summon | Reubicar celda libre, `Effect_185` |
| `64ebe26` | IA | `MonsterAttackAI` rewrite (PR #32) |
| `1864886` | Arma | Límite usos/turno |
| `36d0524` | Tackle | Placaje PA/PM |
| `c4664ba` | Rates | XP/drops combate |

### 2.2 Fase 3 effects-engine-fix (pre/post #32)

| Commit | Capa |
| --- | --- |
| `9bb4dc5` / `e85d26d` | DOT robo HP → `TriggerBuff` TURN_BEGIN |
| `61d5ae4` / `b0a7b5f` | `Effect_Kill` handler |
| `cc0e53f` / `d7529d6` | `PunishmentBuff` AfterDamaged |
| `38b8047` / `8b32ee9` | Invocaciones `Die()` fin turno |
| `cb85345` / `c646296` | `FightCombatLogger` develop-build |

### 2.3 Combat telemetry + ReadyChecker

| Commit | Entregable |
| --- | --- |
| `245746c`, `b59a97c` | `CombatTelemetry` JSONL |
| `ff832db` | `ReadyChecker` |
| `8c543ed`, `c23bee5` | Merge readychecker + patch32 |
| `c2e9c4f` | **Regresión:** bypass ReadyChecker — avance directo turno (bug `_turnEndStarted`) |
| `2336543` | Sacrificada path, poison DOT, combat logging |
| `cdc3000` | Fix Sacrificada Kill + curas aliadas |
| `d0450b3` | Fix contador invocaciones (`stats.summoner`) |

### 2.4 Telemetría hoy en código (sin nuevo PR)

Dos sistemas **paralelos**:

| Sistema | Archivo | Flag | Granularidad |
| --- | --- | --- | --- |
| `CombatTelemetry` | `Game/Fights/Telemetry/CombatTelemetry.cs` | `CombatTelemetryEnabled` / `FIGHT_TELEMETRY_ENABLED` | JSONL turn + spell coarse |
| `FightCombatLogger` | `Game/Fights/Diagnostics/FightCombatLogger.cs` | `FightCombatLogEnabled` / `FIGHT_COMBAT_LOG_ENABLED` | Texto por fightId, más detalle efecto/daño/buff |

**Instrumentación actual:**

- `FightActor.CastSpell` → `SpellCastStarted/Failed/Resolved` + `FightCombatLogger` CAST/CAST_FAIL
- `EffectDispatcher.Dispatch` → `EffectResolved/Failed` + DISPATCH
- `FightActor.InflictDamage/Heal/Kill` → DAMAGE/HEAL/KILL
- `Summon.cs` → SUMMON_CREATE/FAIL
- `DamageOverTimeBuff` / `HealOverTimeBuff` → BUFF_TICK
- `MonsterAttackAI` → `AiActionSelected` (spellId, cell)
- `Fight.cs` → turn flow, ReadyChecker events (código existe; EndTurn bypass parcial)

---

## 3. Módulos de código que tocan combate/hechizos

| Módulo | Ruta principal | Rol |
| --- | --- | --- |
| Cast + validación | `FightActor.cs` (`CastSpell`, `CanCastSpell`, `GetCastZone`) | AP, zona, history, critical roll |
| Cast custom | `Game/Spells/Casts/SpellCastManager.cs`, `Game/Spells/Casts/**` | Handlers por spellId (ej. `ColereHandler`) |
| Dispatch | `EffectDispatcher.cs` | Factory + Initialize + Apply |
| Registro handlers | `EffectsLoader.cs`, `[EffectHandler]` en `Game/Effects/Spells/**` | ~100 handlers |
| Objetivos / zona | `EffectManager.GetAffectedActors` | Target mask + zone shape |
| Daño | `Damages/DirectDamage.cs`, `Damage.cs`, `EffectDamageResolver` | Fórmula dados + crítico |
| DOT/veneno | `HpSteal.cs`, `DamageOverTimeBuff.cs`, `TriggerBuff.cs` | Ticks TURN_BEGIN |
| Buffs/castigos | `PunishmentBuff.cs`, `PunishmentDamage.cs`, `StatsBoost.cs` | AfterDamaged / stats |
| Invocación | `Summon/Summon.cs`, `SummonedMonster.cs`, `SummonedStaticMonster.cs` | Spawn, slot, IA |
| Muerte | `Others/Kill.cs` | Effect_Kill (incl. spell 233 special case) |
| Curas | `Heals/Heal.cs` | Filtro aliados (whitelist 192) |
| Marcas | `Marks/GlyphSpawn.cs`, `TrapSpawn.cs`, `Fights/Triggers/Glyph.cs` | Glifos/trampas |
| IA mobs | `MonsterAttackAI.cs`, `AIFighter.cs` | Selección spell/move |
| Boss | `Fights/Mechanics/FrigostBossMechanics.cs` | Hooks parciales |
| History/recast | `SpellHistory` (en fighter) | MaxCastPerTurn, relanzamiento |
| Rates | `GameRates.cs`, `config_rates_Server.txt` | XP/drop, no daño spell |

---

## 4. Riesgos de duplicar trabajo

| Riesgo | Mitigación |
| --- | --- |
| Crear **tercer** logger ad-hoc | Extender `CombatTelemetry` + unificar campos con `FightCombatLogger` vía adapter o schema v2 |
| Re-implementar DOT fix | Ya en `HpSteal` + `DamageOverTimeBuff`; telemetría debe **confirmar** si tick no corre o daño=0 |
| Parchear Sacrificada otra vez | `Kill.cs` spell 233 + `fix-cura-sacrificada-combate.md`; verificar regresión con logs antes de tocar |
| Re-portar ReadyChecker | Código existe pero **bypassed** en `FightActor.EndTurn` (c2e9c4f); no mezclar con spell audit |
| Duplicar analyzer | Reusar `Infrastructure/scripts/CombatTelemetryAnalyzer/` — extender parsers |
| Fix por spellId sin evidencia | Violación de [combat-fix-philosophy.md](../combat-fix-philosophy.md) — telemetría primero |

---

## 5. PRs y commits relevantes

| PR / rama | Tema | Estado |
| --- | --- | --- |
| **#32** `devp-patch-integration` | Parche combate, DOT, IA, rates, summons | **MERGED** 2026-06-07 |
| **#37** combat-sanitization-phase1-audit | Docs auditoría | PR abierto cadena combat |
| **#38** combat-telemetry-phase2 | CombatTelemetry JSONL | PR (conflictos históricos) |
| **#39** combat-readychecker-phase3 | ReadyChecker | Merge local; bypass parcial en código actual |
| **#40** spell-builder-api | Admin read-only spells | Paralelo |

Commits clave post-#32 (spell-specific):

```text
cdc3000 fix(combat): curas aliadas y explosión Sacrificada sin kill instantáneo
d0450b3 fix(combat): liberar cupo de invocaciones al morir muñecas
2336543 Fix Sacrificada summon path, poison DOT pipeline, and combat logging
c2e9c4f fix(combat): restaurar avance directo de turno (bypass ReadyChecker incompleto)
```

---

## 6. Mapeo síntomas reportados → hipótesis / capa (abiertas)

| Síntoma reportado | SpellId (enum) | Capa sospechosa | Evidencia previa | Hipótesis abierta |
| --- | ---: | --- | --- | --- |
| Sacrificada mata lanzador | 233 (`Sacrifice`) | `Kill.cs`, target mask, autodaño cast 189 | Fix doc jun 2026 | Regresión o `Effect_Kill` en caster equivocado; telemetría debe mostrar targetIds por efecto |
| Rekop rompe juego | 114 (`Rekop`) | `DirectDamage`, crítico, `Damage.GenerateDamages` | — | Crítico vs normal invertido; dados mal resueltos |
| Ira Yopuka 2ª ~400 | 159 (`IopsWrath`) | `DirectDamage` state 51 + `ColereHandler(143)` **ID mismatch** | Código: handler 143 vs spell 159 | Handler custom no engancha; escala +80 hardcodeada puede ser incorrecta |
| Venenos no bajan vida | varios `Effect_StealHP*` / DOT | `HpSteal`, `TriggerBuff`, `StartTurn` | Fix Fase 3 | Tick no registrado, buff no creado, o `InflictDamage` bloqueado |
| Látigo Osamodas no lanza | 30 (`Whip`) | `CanCastSpell`, target mask, `NeedTakenCell` | — | Validación rechaza cast; falta log de `SpellCastFailed` reason |
| Sacrificio Muñequero / Desinvocación | 198 (`DollySacrifice`), 46 (`Unsummoming`) | `GetAffectedActors` default ALL | — | Target mask no filtra invocaciones |
| Castigos Sacrógrito | `Effect_Punishment` | `PunishmentBuff`, stat from dice | Fix Fase 3 | Bonus no aplicado a stats reales |
| Engaño crítico < normal | Sram direct damage | Critical effects path, `RollCriticalDice` | — | Crítico usa `CriticalEffects` con dados distintos en BD |
| Snifter Cell / Cil / Dragocerdo no invocan | spells en BD monstruo | `MonsterAttackAI`, `CanCastSpell`, summon handler | IA rewrite #32 | IA no elige spell vs cast rechazado vs SUMMON_FAIL |
| Boss veneno/autokill | DOT + Kill | `DamageOverTimeBuff`, `Kill.cs` | — | Mismo pipeline jugador; distinguir AI vs effect |

**IDs útiles** (`SpellIdEnum`, índice 0 = Punch):

```text
Whip = 30
Unsummoning = 46
Rekop = 114
IopsWrath = 159
DollySacrifice = 198
Sacrifice (muñeca) = 233
```

---

## 7. Brecha entre telemetría existente y objetivo del audit

La telemetría Phase 2 responde **turn flow** y **cast coarse** (spellId, result OK/Fail).

**No responde aún** (requiere Phase spell-effect telemetry):

1. AP antes/después, cooldown/relaunch state en cast
2. Line of sight / range validation explícita
3. Raw effect data (diceNum, diceSide, target mask, zone, critical vs normal)
4. Lista de objetivos filtrados vs incluidos por mask
5. Daño calculado vs final, resistencias, estados
6. Correlación poison/glyph/trap: turno esperado vs real
7. Buff stat aplicado vs stats finales del fighter
8. IA: spells evaluados vs rechazados vs elegidos (boss summon)

---

## 8. Decisión de fase

| Fase | Acción | Estado |
| --- | --- | --- |
| 0 | Este documento | **DONE** |
| 1 | Mapa pipeline real | Ver [PHASE_01_SPELL_PIPELINE_MAP.md](./PHASE_01_SPELL_PIPELINE_MAP.md) |
| 2 | Diseño telemetría spell/effect | Ver [PHASE_02_TELEMETRY_DESIGN.md](./PHASE_02_TELEMETRY_DESIGN.md) |
| 3 | Implementación mínima (rama `feature/spell-effect-telemetry-phase-1`) | **PENDIENTE aprobación** |
| 4 | Matriz pruebas manuales | Ver [PHASE_03_MANUAL_TEST_MATRIX.md](./PHASE_03_MANUAL_TEST_MATRIX.md) |

**Regla:** ningún fix de balance hasta tener logs que clasifiquen capa (validación / mask / handler / fórmula / buff / AI).
