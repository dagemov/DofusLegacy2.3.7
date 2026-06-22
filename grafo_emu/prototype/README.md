# grafo_emu/prototype/ — Vertical Slice

Prototipo **mínimo** que valida el modelo de `grafo_emu/00..08` reconstruyendo cadenas
completas con **datos reales** del emulador. No es el grafo completo: es una rebanada
vertical (5 entidades) que atraviesa las 5 capas (L1→L5).

## Qué NO es esto

Sin Neo4j, Memgraph, GraphQL, MCP, embeddings, RAG ni vector DB. Solo:

- `nodes.jsonl` — 35 nodos (uno por línea, formato de `04-modelo-grafo.md`).
- `edges.jsonl` — 42 aristas (incluye el eje epistémico L5 y `CONTRADICTS`).
- `traverse.mjs` — reconstructor en Node puro (cero dependencias) que recorre el grafo.

## Cómo ejecutar

```bash
cd grafo_emu/prototype
node traverse.mjs              # recorre los 5 seeds
node traverse.mjs spell:196    # un seed concreto
```

Salida: árbol de cadenas con relación, **confianza** (`●●●`/`●●○`/`●○○`/`○○○`),
**fuente de procedencia** (BD/CODE/LOG/MCP2/DERIVED) y auditoría de integridad
(aristas colgantes + contradicciones estático↔observado).

## Entidades del slice

| Seed | Qué demuestra |
|------|---------------|
| `spell:189` (La Sacrifiée) | Cadena epistémica completa: Spell→Effect→Code→Fight→Finding→Bug→Test→Deploy + **CONTRADICTS** estático vs observado |
| `spell:196` (Vent Empoisonné) | DOT con `BUFF_TICK amount=0` → BUG-002; efecto estático (138) contradice observado (Effect_DamageNeutral 100) |
| `item:12116` (Coiffe du Glourséleste) | Item de catálogo; `HAS_TYPE` parcial (tipo vive en D2O cliente); **sin** observación en logs (isla L5) |
| `npc:1053` (Vendeur de Dofus) | `SELLS` explícito a item:12116 (precio 9.75M) vía `npcs_items` |
| `quest:3` (La discorde végétale) | `HAS_STEP` × 5 confirmado por doble vía (CSV + back-reference) |

## Procedencia de cada dato (verificable)

- **BD**: `database/sunshine.sql` (líneas citadas en `provenance.ref`).
- **CODE**: `Sunshine net11.0/.../EffectsEnum.cs`, `SacrifierHandler.cs`, `Summon.cs`.
- **LOG**: VPS `/opt/dofus-2.0.0/logs/fights/1.log` (extraído por grep, contadores reales).
- **MCP2**: `mcp/diagnostics/signature-matcher.js`, `mcp/knowledge/schema.js`, `mcp/test/eval-battery.js`.

Ver el análisis completo en `grafo_emu/09-vertical-slice-validation.md`.

## Fase 13.5 — interpretación de verdad (runtime)

```bash
node truth-interpret.mjs          # resumen truth_state / conflict_pairs
node truth-interpret.mjs --json   # snapshot completo para QSG constrained mode
```

No modifica `nodes.jsonl` ni `edges.jsonl`. Ver `grafo_emu/13.5-truth-minimal-materialization-layer.md`.

## MCP World Control v1 — tests de validación

```bash
cd grafo_emu/prototype/world-control
node run-all.mjs              # 5 tests → stdout + last-run.json
```

Ver informe: `grafo_emu/16-mcp-world-control-v1-design.md`.

## Action Discovery Layer — MCP World Mining v1

```bash
cd grafo_emu/prototype/world-control
node run-mining.mjs           # pipeline mining → stdout + mining-last-run.json
```

Ver informe: `grafo_emu/17-action-discovery-layer-mcp-world-mining-v1.md`.

## System Discovery & World Operating Model

```bash
cd grafo_emu/prototype/world-model
node run-discovery.mjs        # Phases A–G + TEST 1–8 → world-model-last-run.json
```

Ver informe: `grafo_emu/18-system-discovery-world-operating-model.md`.

## World Semantic Model — Phase 19

```bash
cd grafo_emu/prototype/world-semantic
node run-semantic.mjs           # concepts + consistency + benchmark → world-semantic-last-run.json
```

Ver informe: `grafo_emu/19-world-semantic-model.md`.

## Relationship Recovery Layer — Phase 20

```bash
cd grafo_emu/prototype/world-relations
node run-relations.mjs           # recover edges + benchmark rerun → world-relations-last-run.json
```

Ver informe: `grafo_emu/20-relationship-recovery-layer.md`.

## Semantic Causality and Edge Weighting — Phase 21

```bash
cd grafo_emu/prototype/world-causal
node run-causal.mjs           # enrich edges + propagation + benchmark → world-causal-last-run.json
```

Ver informe: `grafo_emu/21-semantic-causal-layer.md`.

## MCP Execution Kernel v1 — Phase 22 (simulation)

```bash
cd grafo_emu/prototype/mcp-execution-sim
node run-execution-sim.mjs    # intent → plan → validate → stub execute/sync (read-only)
```

Ver informe: `grafo_emu/22-mcp-execution-kernel-v1.md`.

## Execution Bridge v1 — Phase 23 (controlled VPS writes)

```bash
cd grafo_emu/prototype/mcp-execution-bridge
node run-execution-bridge.mjs    # dry-run + TEST 1-8 → mcp-execution-bridge-last-run.json
node run-execution-bridge.mjs --intent modify_item --target item:519 \
  --fields-file out/vps-test-fields.json --confirm   # live write (SSH required)
```

Ver informe: `grafo_emu/23-execution-bridge-v1.md`.

## Runtime Sync Layer — Phase 24 (Graph ↔ Runtime consistency)

```bash
cd grafo_emu/prototype/runtime-sync
node run-runtime-sync.mjs    # preflight + diff + consistency + TEST 1-8 → runtime-sync-last-run.json
```

Ver informe: `grafo_emu/24-runtime-synchronization-layer.md`.

## World Transaction Model v1 — Phase 25

```bash
cd grafo_emu/prototype/world-transaction
node run-world-transaction.mjs    # Cases A-D + TEST 1-8 → world-transaction-last-run.json
```

Ver informe: `grafo_emu/25-world-transaction-model-v1.md`.

## MCP Tool Contract v1 — Phase 26

```bash
cd grafo_emu/prototype/mcp-tool-contract
node run-contract-validation.mjs    # preflight F21-F25 + CASE A-D + TEST 1-8 → mcp-tool-contract-last-run.json
```

Ver informe: `grafo_emu/26-mcp-tool-contract-v1.md`.

## MCP Runtime Gateway v1 — Phase 27

```bash
cd grafo_emu/prototype/mcp-runtime-gateway
node run-gateway-validation.mjs    # preflight F26 + CASE A-D replay + TEST 1-8 → mcp-runtime-gateway-last-run.json
```

Ver informe: `grafo_emu/27-mcp-runtime-gateway-v1.md`.

## MCP World Agent v1 — Phase 28

```bash
cd grafo_emu/prototype/mcp-world-agent
node run-agent-simulation.mjs    # NL intent → agent loop → F27 gateway + TEST 1-8 → mcp-world-agent-last-run.json
```

Ver informe: `grafo_emu/28-mcp-world-agent-v1.md`.
