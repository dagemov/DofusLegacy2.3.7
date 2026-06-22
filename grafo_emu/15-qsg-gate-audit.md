# #15 — QSG Gate: Auditoría de deuda técnica (pre-Fase 14)

## §0 — ALCANCE Y VERIFICACIÓN DE DATOS

### Archivos inspeccionados

| Archivo | Estado | Conteo |
|---------|--------|--------|
| `prototype/nodes.jsonl` | PRESENTE | 37 nodos |
| `prototype/edges.jsonl` | PRESENTE | 44 aristas |
| `prototype/traverse.mjs` | PRESENTE | reconstructor sin filtros |
| `prototype/truth-deltas.jsonl` | **AUSENTE EN DATASET** | — |
| `prototype/decoder-validations.jsonl` | **AUSENTE EN DATASET** | — |

### Campos y tipos NOT PRESENT en dataset

| Elemento | Estado |
|----------|--------|
| `truth_state` en aristas | **NOT PRESENT** |
| `confidence_final` en aristas | **NOT PRESENT** |
| `edge_status` en aristas | **NOT PRESENT** (solo `status` en 6 aristas) |
| Nodos tipo `DecoderValidation` | **NOT PRESENT** |
| Nodos tipo `TruthDelta` | **NOT PRESENT** |
| Nodos tipo `ConflictResolution` | **NOT PRESENT** |
| Nodos tipo `QueryFeedbackSignal` | **NOT PRESENT** |
| Edges hacia/desde `QueryFeedbackSignal` | **NOT PRESENT** |

### Reglas de clasificación proxy (solo campos observables)

| Condición observable | `proxy_class` |
|----------------------|---------------|
| `provenance.source === "LOG"` | TRUTH/OBSERVED |
| `status === disputed\|candidate\|ref-only` OR `confidence < 0.6` | UNCERTAIN |
| Resto (BD, CODE, MCP2, DERIVED, MCP2+LOG, CODE+LOG) | DERIVED |

---

## §1 — TEST 1: TRUTH COVERAGE

**Seeds evaluados (10 existentes en dataset):** `spell:189`, `spell:196`, `item:12116`, `item:288`, `npc:1053`, `npc:449`, `npc:488`, `quest:3`, `spelllevel:941`, `spelllevel:976`

| Seed | Path TRUTH/OBSERVED | Dominante vs DERIVED | Edge evidence |
|------|---------------------|----------------------|---------------|
| spell:189 | Sí | Sí (LOG conf=1.0 > BD disputed e002 conf=0.3) | `e003` USES_EFFECT, `e007` OBSERVED_IN |
| spell:196 | Sí | Sí (LOG conf=1.0 > e102 conf=0.2) | `e103`, `e104` USES_EFFECT, `e106` OBSERVED_IN |
| item:12116 | No | N/A | sin aristas LOG; solo `e202` HAS_TYPE (BD conf=0.6) |
| item:288 | No | N/A | sin aristas salientes; entrante `e306` status=ref-only |
| npc:1053 | No | N/A | solo `e201` SELLS (BD conf=1.0) |
| npc:449 | No | N/A | sin aristas salientes; entrante `e307` (BD conf=0.7) |
| npc:488 | No | N/A | sin aristas salientes; entrante `e308` (BD conf=0.7) |
| quest:3 | No | N/A | `e301`–`e305` HAS_STEP (BD conf=1.0) |
| spelllevel:941 | No | N/A | solo `e002` PARSED_EFFECT status=disputed conf=0.3 |
| spelllevel:976 | No | N/A | solo `e102` PARSED_EFFECT status=disputed conf=0.2 |

**truth_coverage_ratio** = 2 / 10 = **0.2**

```json
{
  "truth_coverage_ratio": 0.2,
  "missing_truth_seeds": [
    "item:12116",
    "item:288",
    "npc:1053",
    "npc:449",
    "npc:488",
    "quest:3",
    "spelllevel:941",
    "spelllevel:976"
  ],
  "risk_level": "HIGH"
}
```

---

## §2 — TEST 2: DERIVED CONTAMINATION

### Clasificación de las 44 aristas

| proxy_class | Count | Ratio |
|-------------|-------|-------|
| TRUTH/OBSERVED | 8 | 0.18 |
| DERIVED | 30 | 0.68 |
| UNCERTAIN | 6 | 0.14 |

**TRUTH/OBSERVED ids:** `e003`, `e007`, `e008`, `e015`, `e103`, `e104`, `e106`, `e107`

**UNCERTAIN ids:** `e002`, `e016`, `e020`, `e102`, `e114`, `e306`

**DERIVED domina TRUTH:** 30 > 8 → **sí**

### Conflictos por dominio (observable)

| Dominio | Conflicto observable |
|---------|---------------------|
| spell | `e002` PARSED_EFFECT→effect:99 vs `e003` USES_EFFECT→effect:182; `e004` CONTRADICTS |
| spell | `e102` PARSED_EFFECT→effect:138 vs `e103`/`e104` USES_EFFECT; `e105` CONTRADICTS |
| npc | `e201` SELLS — solo BD, cero aristas LOG |
| quest | `e301`–`e308` — solo BD, cero aristas LOG |
| item | `e202` HAS_TYPE conf=0.6; `e306`→item:288 ref-only |

```json
{
  "derived_dominance": true,
  "contaminated_queries": [
    "spell:189 — e002 PARSED_EFFECT coexiste con e003 USES_EFFECT sin campo truth_state",
    "spell:196 — e102 PARSED_EFFECT coexiste con e103/e104 USES_EFFECT",
    "quest:3 — e301-e305 HAS_STEP solo BD",
    "npc:1053 — e201 SELLS solo BD",
    "item:12116 — e202 HAS_TYPE solo BD conf=0.6"
  ],
  "risk_level": "HIGH"
}
```

---

## §3 — TEST 3: DECODER CONSISTENCY

**DecoderValidation nodes:** **NOT PRESENT**

**Patrones observados:**

| Patrón | Edge ids |
|--------|----------|
| PARSED_EFFECT vs USES_EFFECT | `e002` vs `e003`; `e102` vs `e103` |
| status=disputed | `e002`, `e102` |
| CONTRADICTS sin supresión de disputed | `e004`, `e105` documentan; `e002`/`e102` persisten |

```json
{
  "invalid_decoder_leaks": [
    {
      "decoder": "naive-hex-parse (provenance.method en e002, e102)",
      "leaking_edges": ["e002", "e102"],
      "note": "status=disputed; CONTRADICTS e004/e105 no eliminan aristas disputed; DecoderValidation NOT PRESENT"
    }
  ],
  "severity": "HIGH"
}
```

---

## §4 — TEST 4: QUERY STABILITY

**Query simulado:** `"why spell 189 behaves differently"`

**Motor QEM:** **NOT PRESENT**

**traverse.mjs:** determinístico — 3 ejecuciones `node traverse.mjs spell:189` producen el mismo árbol (incluye ramas `e001`→`e002` DERIVED/UNCERTAIN y `e003` TRUTH/OBSERVED sin preferencia).

| Run | Path funcional spell→effect observable | Variación |
|-----|----------------------------------------|-----------|
| 1 | Ambas ramas visibles: HAS_LEVEL→PARSED_EFFECT (e002) y USES_EFFECT (e003) | — |
| 2 | Idem | 0 |
| 3 | Idem | 0 |

```json
{
  "unstable_queries": [],
  "avg_variance": 0.0,
  "risk_level": "MEDIUM"
}
```

---

## §5 — TEST 5: UNCERTAIN GRAPH OVERFLOW

### Aristas (44)

| proxy_class | Count | Ratio |
|-------------|-------|-------|
| TRUTH/OBSERVED | 8 | 0.18 |
| DERIVED | 30 | 0.68 |
| UNCERTAIN | 6 | 0.14 |

### Nodos (37)

| proxy_class | Count | Ratio |
|-------------|-------|-------|
| UNCERTAIN | 5 | 0.14 |
| Otros | 32 | 0.86 |

**Nodos UNCERTAIN:** `effect:99` (disputed), `effect:138` (disputed), `item:288` (ref-only), `itemtype:16` (client-side), `deploy:pending` (candidate conf=0.0)

**Dominios críticos DERIVED-only:** quest (`e301`–`e308`), npc comercio (`e201`), item catálogo (`e202`)

**UNCERTAIN > 0.5:** **no** (edge ratio 0.14; node ratio 0.14)

```json
{
  "truth_ratio": 0.18,
  "derived_ratio": 0.68,
  "uncertain_ratio": 0.14,
  "risk_level": "MEDIUM"
}
```

---

## §6 — TEST 6: QUERY FEEDBACK EFFECTIVENESS

**QueryFeedbackSignal nodes:** **NOT PRESENT**

**QueryFeedbackSignal edges:** **NOT PRESENT**

**traverse.mjs:** no referencia a `QueryFeedbackSignal`, `qsignal`, ranking ni `stable_contracts`

```json
{
  "signal_effectiveness": 0.0,
  "ignored_signals": [],
  "risk_level": "HIGH"
}
```

---

## §7 — VEREDICTO GLOBAL

| Regla | Valor observable | BLOCK |
|-------|------------------|-------|
| truth_coverage < 0.6 | 0.2 | **SÍ** |
| derived_dominance = true | true | **SÍ** |
| decoder_leaks > 0 | 2 (`e002`, `e102`) | **SÍ** |
| uncertain_ratio > 0.5 | 0.14 | no |

```json
{
  "readiness_for_fase_14": false,
  "blocking_issues": [
    "truth_coverage_ratio 0.2 < 0.6 — 8/10 seeds sin arista saliente provenance.source=LOG",
    "derived_dominance true — 30/44 aristas proxy_class DERIVED vs 8 TRUTH/OBSERVED",
    "decoder_leaks: e002 y e102 status=disputed persisten; DecoderValidation NOT PRESENT",
    "truth_state NOT PRESENT en aristas — no hay capa TRUTH materializada en dataset",
    "truth-deltas.jsonl AUSENTE EN DATASET",
    "decoder-validations.jsonl AUSENTE EN DATASET",
    "QueryFeedbackSignal NOT PRESENT — signal_effectiveness 0.0"
  ],
  "critical_debt": [
    "GTL (Fase 13) no materializado: solo nodes.jsonl + edges.jsonl sin truth_state",
    "CONTRADICTS e004/e105 presentes sin nodos ConflictResolution",
    "traverse.mjs recorre todas las aristas salientes sin filtro TRUTH/DERIVED",
    "Dominios quest/npc/item: 100% aristas BD (cero LOG)"
  ],
  "recommendation": "FIX_REQUIRED"
}
```

---

## §8 — INVENTARIO DE ARISTAS CRÍTICAS

| id | src | rel | dst | conf | source | proxy_class |
|----|-----|-----|-----|------|--------|-------------|
| e001 | spell:189 | HAS_LEVEL | spelllevel:941 | 1.0 | BD | DERIVED |
| e002 | spelllevel:941 | PARSED_EFFECT | effect:99 | 0.3 | BD | UNCERTAIN |
| e003 | spell:189 | USES_EFFECT | effect:182 | 1.0 | LOG | TRUTH/OBSERVED |
| e004 | effect:99 | CONTRADICTS | effect:182 | 0.9 | DERIVED | DERIVED |
| e005 | effect:182 | HANDLED_BY | cstype:Summon | 0.8 | CODE | DERIVED |
| e006 | spell:189 | CAST_HANDLED_BY | cstype:SacrificeHandler | 0.85 | CODE | DERIVED |
| e007 | spell:189 | OBSERVED_IN | fight:1 | 1.0 | LOG | TRUTH/OBSERVED |
| e008 | fight:1 | GENERATED | logseq:189@fight1 | 1.0 | LOG | TRUTH/OBSERVED |
| e009 | logseq:189@fight1 | MATCHES | sig:SIG-001 | 0.9 | MCP2 | DERIVED |
| e010 | logseq:189@fight1 | EVIDENCES | finding:SUMMON_MUERTE_INSTANTANEA@189 | 0.9 | MCP2+LOG | DERIVED |
| e011 | sig:SIG-001 | SIGNALS | bug:BUG-001 | 1.0 | MCP2 | DERIVED |
| e012 | finding:SUMMON_MUERTE_INSTANTANEA@189 | ASSOCIATED_WITH | bug:BUG-001 | 0.9 | MCP2 | DERIVED |
| e013 | cstype:SacrificeHandler | EXPLAINS | finding:SUMMON_MUERTE_INSTANTANEA@189 | 0.8 | CODE+LOG | DERIVED |
| e014 | hypothesis:189 | EXPLAINS | finding:SUMMON_MUERTE_INSTANTANEA@189 | 0.8 | MCP2 | DERIVED |
| e015 | hypothesis:189 | SUPPORTED_BY | logseq:189@fight1 | 0.85 | LOG | TRUTH/OBSERVED |
| e016 | contract:189 | DERIVED_FROM | spelllevel:941 | 0.5 | MCP2 | UNCERTAIN |
| e017 | logseq:189@fight1 | VIOLATES | contract:189 | 0.6 | DERIVED | DERIVED |
| e018 | bug:BUG-001 | VALIDATED_BY | test:T1 | 1.0 | MCP2 | DERIVED |
| e019 | test:T1 | TARGETS | spell:189 | 1.0 | MCP2 | DERIVED |
| e020 | test:T1 | GUARDED_BY | deploy:pending | 0.0 | MCP2 | UNCERTAIN |
| e101 | spell:196 | HAS_LEVEL | spelllevel:976 | 1.0 | BD | DERIVED |
| e102 | spelllevel:976 | PARSED_EFFECT | effect:138 | 0.2 | BD | UNCERTAIN |
| e103 | spell:196 | USES_EFFECT | effect:100 | 1.0 | LOG | TRUTH/OBSERVED |
| e104 | spell:196 | USES_EFFECT | effect:89 | 1.0 | LOG | TRUTH/OBSERVED |
| e105 | effect:138 | CONTRADICTS | effect:100 | 0.9 | DERIVED | DERIVED |
| e106 | spell:196 | OBSERVED_IN | fight:1 | 1.0 | LOG | TRUTH/OBSERVED |
| e107 | fight:1 | GENERATED | logseq:196@fight1 | 1.0 | LOG | TRUTH/OBSERVED |
| e108 | logseq:196@fight1 | MATCHES | sig:SIG-002 | 0.9 | MCP2 | DERIVED |
| e109 | logseq:196@fight1 | EVIDENCES | finding:TICK_CERO@196 | 0.9 | MCP2+LOG | DERIVED |
| e110 | sig:SIG-002 | SIGNALS | bug:BUG-002 | 1.0 | MCP2 | DERIVED |
| e111 | finding:TICK_CERO@196 | ASSOCIATED_WITH | bug:BUG-002 | 0.9 | MCP2 | DERIVED |
| e112 | bug:BUG-002 | VALIDATED_BY | test:T2 | 1.0 | MCP2 | DERIVED |
| e113 | test:T2 | TARGETS | spell:196 | 1.0 | MCP2 | DERIVED |
| e114 | test:T2 | GUARDED_BY | deploy:pending | 0.0 | MCP2 | UNCERTAIN |
| e201 | npc:1053 | SELLS | item:12116 | 1.0 | BD | DERIVED |
| e202 | item:12116 | HAS_TYPE | itemtype:16 | 0.6 | BD | DERIVED |
| e301 | quest:3 | HAS_STEP | queststep:2 | 1.0 | BD | DERIVED |
| e302 | quest:3 | HAS_STEP | queststep:3 | 1.0 | BD | DERIVED |
| e303 | quest:3 | HAS_STEP | queststep:4 | 1.0 | BD | DERIVED |
| e304 | quest:3 | HAS_STEP | queststep:5 | 1.0 | BD | DERIVED |
| e305 | quest:3 | HAS_STEP | queststep:32 | 1.0 | BD | DERIVED |
| e306 | queststep:32 | REWARDS | item:288 | 0.6 | BD | UNCERTAIN |
| e307 | queststep:3 | INVOLVES_NPC | npc:449 | 0.7 | BD | DERIVED |
| e308 | queststep:5 | INVOLVES_NPC | npc:488 | 0.7 | BD | DERIVED |
