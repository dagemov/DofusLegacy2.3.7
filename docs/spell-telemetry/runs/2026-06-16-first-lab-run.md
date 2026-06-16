# First lab run — spell/effect telemetry (VPS QA)

**Estado:** `EN PROGRESO` — ventana QA abierta; combates pendientes de ejecución in-game  
**Fecha ventana:** 2026-06-16  
**Operador:** _pendiente_  
**Commit desplegado:** `4394b10` — Merge pull request #50 from dagemov/devp (`main`)  
**Commit telemetría:** `b6cc7f4` — Merge pull request #49 (spell/effect layered diagnostics)

---

## 1. Variables activadas

| Variable | Valor QA | Notas |
| --- | --- | --- |
| `SPELL_EFFECT_TELEMETRY_ENABLED` | `true` | Fuente principal JSONL |
| `COMBAT_HEALTH_LAB` | `1` | Lab mode |
| `FIGHT_TELEMETRY_LOG_DIRECTORY` | `/app/logs/combat` | Host: `/opt/dofus-2.0.0/logs/combat` |
| `FIGHT_TELEMETRY_ENABLED` | `false` | Turn-flow no requerido para spell layers |
| `FIGHT_COMBAT_LOG_ENABLED` | `false` | **Mirror OFF** — demasiado ruido; JSONL suficiente |

Backup previo: `/opt/dofus-2.0.0/backups/spell-telemetry-*`

---

## 2. Personajes / entorno

| Campo | Valor |
| --- | --- |
| Personajes QA | _pendiente — listar nicks_ |
| Nivel aprox. | _pendiente_ |
| Mapa / zona | _pendiente — ej. Incarnam arena, dungeon X_ |
| Tipo combate | PvM controlado / PvP voluntario / GM spawn |
| Observadores | |

---

## 3. Archivos JSONL recolectados

| Archivo | Ruta local | Combates | Bytes |
| --- | --- | --- | --- |
| _pendiente_ | `Infrastructure/temporal-artifacts/combat-telemetry/vps-run-20260616/` | | |

Ruta VPS origen: `/opt/dofus-2.0.0/logs/combat/spell-casts/spell-casts-*.jsonl`

Recolección:

```powershell
.\scripts\vps\spell-telemetry-qa.ps1 -Action collect -RunDate 20260616
```

---

## 4. Resumen analyzer

**Comando:**

```powershell
dotnet run --project infrastructure/scripts/CombatTelemetryAnalyzer -- `
  --input "Infrastructure/temporal-artifacts/combat-telemetry/vps-run-20260616" `
  --output "docs/spell-telemetry/runs/2026-06-16-layer-report.md"
```

**Resultado:** _pendiente — pegar conteos por layer/evento o enlace al reporte generado_

| Layer | Eventos | Validations failed | Handlers failed | Notes |
| --- | ---: | ---: | ---: | --- |
| A | | | | |
| V | | | | |
| M | | | | |
| F | | | | |
| H | | | | |
| B | | | | |
| BD | | | | |

---

## 5. Casos probados

### Bloque 1 — validación / target mask

| # | Caso | Spell | Target | fightId | correlationId | Resultado esperado | Resultado real | Capa rota |
| --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| 1.1 | Látigo Osa | 30 | invocación aliada | | | V OK, M incluye summon | _pendiente_ | |
| 1.2 | Látigo Osa | 30 | invocación enemiga | | | V OK, M filtra según mask | _pendiente_ | |
| 1.3 | Látigo Osa | 30 | jugador/monstruo normal | | | V OK | _pendiente_ | |
| 1.4 | Desinvocación Anutrof | 46 | summon | | | H ejecuta unsummon | _pendiente_ | |
| 1.5 | Desinvocación Anutrof | 46 | no-summon | | | V/M rechaza | _pendiente_ | |
| 1.6 | Sacrificio Muñequero | 198 | summon | | | H + transfer | _pendiente_ | |
| 1.7 | Sacrificio Muñequero | 198 | jugador/monstruo | | | V/M rechaza o sin efecto | _pendiente_ | |

### Bloque 2 — handlers / delayed

| # | Caso | Spell | Notas | Capa rota |
| --- | --- | ---: | --- | --- |
| 2.1 | Sacrificada Sadida | 233 | transfer daño/curación | _pendiente_ | |
| 2.2 | Veneno Sram/Sadida | _id_ | B: DelayedEffectTick | _pendiente_ | |
| 2.3 | Trampa / glifo | _id_ | H + B | _pendiente_ / N/A | |

### Bloque 3 — fórmula

| # | Caso | Spell | Variante | Capa rota |
| --- | --- | ---: | --- | --- |
| 3.1 | Rekop | 114 | normal | _pendiente_ | |
| 3.2 | Rekop | 114 | crítico | _pendiente_ | |
| 3.3 | Ira Yopuka | 159 | 1.er lanzamiento | _pendiente_ | |
| 3.4 | Ira Yopuka | 159 | 2.º lanzamiento | _pendiente_ | |
| 3.5 | Engaño Sram | _id_ | normal / crítico | _pendiente_ | |

### Bloque 4 — buffs (Sagrógrito castigos)

| # | Caso | Eventos esperados | Stats before/after | Capa rota |
| --- | --- | --- | --- | --- |
| 4.1 | Castigo _nombre_ | BuffApplied, BuffTriggered | _pendiente_ | |

### Bloque 5 — IA / bosses

| # | Boss | Comportamiento | Capa rota |
| --- | --- | --- | --- |
| 5.1 | Dragocerdo | veneno / autokill | _pendiente_ | |
| 5.2 | Snifter Cell | invocación tortugas | _pendiente_ | |
| 5.3 | Cil | invocación arañas | _pendiente_ | |

---

## 6. Hallazgos clasificados por capa

### A — AI

| ID | Síntoma | Evidencia (correlationId / línea JSONL) | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | _fix / más logs / data audit_ |

### V — Validation

| ID | Síntoma | Evidencia | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | |

### M — Target mask

| ID | Síntoma | Evidencia | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | |

### F — Formula

| ID | Síntoma | Evidencia | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | |

### H — Handler

| ID | Síntoma | Evidencia | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | |

### B — Buff / tick lifecycle

| ID | Síntoma | Evidencia | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | |

### BD — Spell data

| ID | Síntoma | Evidencia | Repetible | Decisión |
| --- | --- | --- | --- | --- |
| | | | | |

---

## 7. Decisiones globales

| Pregunta | Respuesta |
| --- | --- |
| ¿Telemetría suficiente para diagnosticar? | _pendiente_ |
| ¿Abrir fix inmediato? | **No** hasta hallazgo claro y repetible |
| ¿Rama fix sugerida? | _ninguna aún_ |
| ¿Desactivar ventana QA? | Tras recolectar logs + analyzer |

---

## 8. Post-ventana

- [ ] `spell-telemetry-qa.ps1 -Action deactivate`
- [ ] Verificar sin nuevos JSONL tras combate de control
- [ ] Commit reporte + layer-report (sin JSONL pesados)
- [ ] PR docs → `devp`

---

## Referencias

- [PHASE_03_MANUAL_TEST_MATRIX.md](../PHASE_03_MANUAL_TEST_MATRIX.md)
- [PHASE_04_LAB_ANALYSIS_GUIDE.md](../PHASE_04_LAB_ANALYSIS_GUIDE.md)
- [VPS_QA_ACTIVATION_PLAN.md](../VPS_QA_ACTIVATION_PLAN.md)
- [MODULE_CLOSURE_PR49.md](../MODULE_CLOSURE_PR49.md)
