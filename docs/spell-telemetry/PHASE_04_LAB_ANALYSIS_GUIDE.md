# Fase 4 — Guía de análisis en lab (spell/effect telemetry)

**Canal único:** `CombatTelemetry` → `logs/combat/spell-casts/spell-casts-*.jsonl`  
**Schema:** `spell-effect-telemetry-v1` (campo `layer` presente)  
**Fachada:** `SpellEffectTelemetry` (no logger separado)

---

## 1. Activar / desactivar

### Lab local (recomendado)

```powershell
$env:COMBAT_HEALTH_LAB = "1"
$env:SPELL_EFFECT_TELEMETRY_ENABLED = "true"
# Opcional: turn flow completo
$env:FIGHT_TELEMETRY_ENABLED = "true"
$env:FIGHT_TELEMETRY_LOG_DIRECTORY = "Infrastructure/logs/combat"
```

En `Config.xml`:

```xml
<CombatTelemetryEnabled>true</CombatTelemetryEnabled>
<SpellEffectTelemetryEnabled>true</SpellEffectTelemetryEnabled>
<CombatTelemetryLogDirectory>Infrastructure/logs/combat</CombatTelemetryLogDirectory>
```

### Desactivar (producción / default)

```powershell
Remove-Item Env:SPELL_EFFECT_TELEMETRY_ENABLED -ErrorAction SilentlyContinue
Remove-Item Env:COMBAT_HEALTH_LAB -ErrorAction SilentlyContinue
```

O en config: `SpellEffectTelemetryEnabled=false` (default).

**Importante:** con flags off, early-return sin alloc — cero impacto en combate.

---

## 2. Capas de diagnóstico

| Capa | Código | Pregunta que responde |
| --- | --- | --- |
| **A** | AI decision | ¿El mob consideró / rechazó / eligió el spell? |
| **V** | Validation | ¿`CanCastSpell` permitió el cast? ¿Por qué no? |
| **M** | Target mask | ¿Qué fighters entraron en zona vs filtrados por mask? |
| **F** | Formula | ¿Qué daño se calculó vs aplicó (roll, boost, resist)? |
| **H** | Handler | ¿El handler corrió? ¿Summon/Dispatch OK? |
| **B** | Buff lifecycle | ¿DOT/trigger/castigo se programó, tickó, expiró? |
| **BD** | Spell data | ¿Qué dice BD (dados, mask, zone, critical list)? |

---

## 3. Flujo de lectura por combate

1. Buscar `correlationId` del cast (`42-3-107-1`).
2. Seguir en orden:
   - `SpellCastAttempt` → `SpellValidationResult` → `SpellCastResolved`
   - `SpellEffectPlanned` (BD) por cada effect
   - `EffectTargetsResolved` (M)
   - `EffectHandlerResult` (H)
   - `DamageComputed` / `DamageApplied` (F)
   - `DelayedEffect*` / `BuffTriggered` (B)
   - `SummonAttempt` / `SummonResult` (H)
3. Para mobs: filtrar `AiSpellCandidate` / `AiSpellRejected` / `AiSpellSelected`.

---

## 4. Árbol de decisión por síntoma

### Látigo (30) no lanza

| Log observado | Capa | Interpretación |
| --- | --- | --- |
| `SpellValidationResult reasonCode=CELL_NOT_FREE` | **V** | Celda vacía/ocupada vs `NeedTakenCell` |
| `NOT_IN_ZONE` / `NOT_ENOUGH_AP` | **V** | Alcance/AP/history |
| Cast OK + `EffectTargetsResolved` vacío | **M** | Mask/zone no incluye summon |
| Handler OK, sin damage/heal | **H** | Handler whip específico |

### Sacrificada (233) mata caster

| Log | Capa |
| --- | --- |
| `EffectTargetsResolved` Kill incluye Sadida/caster | **M** |
| Kill en targets enemigo OK pero `DamageApplied` full HP loss | **H** (`Kill.cs`) |
| Solo `Effect_DamageAir` sin `Effect_Kill` en Planned | **BD** |

### Veneno no baja vida

| Log | Capa |
| --- | --- |
| Sin `DelayedEffectScheduled` | **H/BD** (Duration=0 o handler) |
| Scheduled sin `DelayedEffectTick` | **B** (StartTurn trigger) |
| Tick con `amount=0` | **F** |
| `DelayedEffectExpired reasonCode=duration` sin tick | **B** confirmado |

### Rekop (114) crítico < normal

| Log | Capa |
| --- | --- |
| `SpellCastResolved critical=CRITICAL_HIT` + `SpellEffectPlanned` dados menores | **BD** |
| Dados OK, `DamageComputed rolledAmount` bajo | **F** |
| `rolledAmount` OK, `finalDamage` bajo | **F** (resist/armor) |

### Ira Yopuka (159) 2ª baja poco

| Log | Capa |
| --- | --- |
| `formulaNotes=IopWrath:first_cast_state51` en 2ª cast | **H** (state no cargó) |
| `IopWrath:charged_cast_diceNum+80` pero `rolledAmount` ~400 | **F/BD** |
| `customHandlerType` ausente + Generic path | **H** (ColereHandler ID mismatch observacional) |

### Desinvocación (46) / Sacrificio Muñequero (198) en jugador

| Log | Capa |
| --- | --- |
| `includedTargets` con `isSummon=false` | **M** |
| Targets OK, wrong outcome | **H** |

### Boss no invoca (Snifter Cell, Cil, Dragocerdo)

| Log | Capa |
| --- | --- |
| Solo `AiSpellRejected NOT_IN_ZONE` | **A+V** |
| `AiSpellCandidate` sin Selected | **A** (prioridad lista spells) |
| Selected + `SummonFailedReason` | **H** |
| Sin `AiSpell*` para spell summon | **A** (spell no en grade / categoría IA) |

### Castigo Sacrógrito no suma stats

| Log | Capa |
| --- | --- |
| Sin `BuffApplied PunishmentBuff` | **H/BD** |
| `BuffTriggered bonusAppliedToStats=false` | **B** |
| Triggered true, `statsSnapshot` sin cambio | **B/H** |

---

## 5. Analyzer

```powershell
dotnet run --project infrastructure/scripts/CombatTelemetryAnalyzer -- `
  --input "Infrastructure/logs/combat" `
  --output "Infrastructure/temporal-artifacts/combat-telemetry/report.md"
```

Genera adicionalmente: `spell-effect-layer-report.md` con conteos por capa y top `reasonCode`.

Ejemplo con sample:

```powershell
dotnet run --project infrastructure/scripts/CombatTelemetryAnalyzer -- `
  --input "docs/spell-telemetry/examples" `
  --output "Infrastructure/temporal-artifacts/combat-telemetry/sample-report.md"
```

---

## 6. Ejemplos incluidos

Ver [`examples/sample-spell-effects.jsonl`](./examples/sample-spell-effects.jsonl):

1. Látigo rechazado (`CELL_NOT_FREE`, layer V)
2. Boss summon probe rechazado (`NOT_IN_ZONE`, layer A/V)
3. Veneno programado pero expirado sin tick (layer B)

---

## Referencias

- [PHASE_02_TELEMETRY_DESIGN.md](./PHASE_02_TELEMETRY_DESIGN.md)
- [PHASE_03_MANUAL_TEST_MATRIX.md](./PHASE_03_MANUAL_TEST_MATRIX.md)
- [combat-log-schema.md](../combat-sanitization/combat-log-schema.md)
