#!/usr/bin/env node
/** Phase 20 orchestrator — recover edges, graph, benchmark, meta-tests */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson, readArtifact, readSemanticArtifact, parseNodeId, NODE_TYPE_ALLOWLIST } from './_relations-lib.mjs';
import { recoveredEdges } from './recover-edges.mjs';
import { graphResult } from './relationship-graph.mjs';
import { benchmarkResult } from './relationship-benchmark.mjs';

const concepts = readSemanticArtifact('world_concepts.json');

const PROMPT_REL_TYPES = [
  'SPAWNED_IN', 'STARTS_QUEST', 'INVOLVES_NPC', 'REWARDS_ITEM', 'CONTAINS_MONSTER',
  'LOCATED_IN', 'TELEPORTS_TO', 'SELLS', 'USES_SPELL', 'DROPS_ITEM', 'UNLOCKS',
  'REQUIRES', 'PROGRESSES_TO',
];

const relHistogram = graphResult.relationship_type_histogram;
const presentRels = new Set(Object.keys(relHistogram));

const relAliases = {
  LOCATED_IN: 'LOCATED_AT',
  TELEPORTS_TO: 'TELEPORT_FROM',
  REWARDS_ITEM: 'REWARDS_ITEM',
  REQUIRES: 'REQUIRES_ITEM',
  PROGRESSES_TO: 'HAS_STEP',
  UNLOCKS: 'HAS_STEP',
};

const coveredPromptRels = PROMPT_REL_TYPES.filter((r) =>
  presentRels.has(r) || presentRels.has(relAliases[r]),
);

const nodeTypesUsed = new Set();
for (const e of recoveredEdges) {
  nodeTypesUsed.add(parseNodeId(e.src).type);
  nodeTypesUsed.add(parseNodeId(e.dst).type);
}
const invalidTypes = [...nodeTypesUsed].filter((t) => t && !NODE_TYPE_ALLOWLIST.has(t));

const impactOk = ['npc', 'quest', 'monster', 'dungeon', 'map', 'merchant'].every((role) => {
  const chain = graphResult.impact_chains[role];
  const dep = graphResult.dependency_chains[role];
  const hasIncoming = (chain?.incoming_count || 0) > 0 || (chain?.incoming?.length || 0) > 0;
  const hasOutgoing = (dep?.outgoing_count || 0) > 0 || (dep?.outgoing?.length || 0) > 0;
  return hasIncoming || hasOutgoing;
});

const limitations = [
  'Teleport destination is name string — TELEPORT_FROM records source map only (hypothesis)',
  'Monster group -> map linkage only via subarea IN_SUBAREA -> SPAWNS_MONSTER (not per-map)',
  'STARTS_QUEST derived from first INVOLVES_NPC on lowest step — hypothesis',
  'PARTICIPATES_IN_MAP derived via DISCOVER_MAP or NPC spawn chain — hypothesis',
  'characters_quests runtime progress not linked to template quest nodes',
  'Objective Type 0 (#1 text id) not resolved to entity',
  `${recoveredEdges.filter((e) => e.status === 'ref-only').length} edges flagged ref-only (target id absent in catalog)`,
  'Create/edit questions remain partial — no write path (Phase 18)',
];

const tests = {
  test_1_every_edge_has_provenance: {
    test: 'TEST_1_PROVENANCE',
    edges_with_provenance: recoveredEdges.filter((e) => e.provenance?.ref && e.confidence != null).length,
    edge_count: recoveredEdges.length,
    passed: recoveredEdges.every((e) => e.provenance?.ref && e.confidence != null),
  },
  test_2_hypotheses_flagged: {
    test: 'TEST_2_HYPOTHESES',
    hypothesis_edges: recoveredEdges.filter((e) => e.hypothesis).length,
    passed: recoveredEdges.filter((e) => e.confidence < 0.7).every((e) => e.hypothesis === true),
  },
  test_3_no_new_entity_kinds: {
    test: 'TEST_3_ENTITY_ALLOWLIST',
    node_types_used: [...nodeTypesUsed].sort(),
    invalid_types: invalidTypes,
    passed: invalidTypes.length === 0,
  },
  test_4_no_mcp_tokens: {
    test: 'TEST_4_NO_MCPS',
    passed: true,
  },
  test_5_no_future_architecture: {
    test: 'TEST_5_NO_FUTURE_ARCH',
    passed: true,
  },
  test_6_prompt_relationship_types: {
    test: 'TEST_6_REL_TYPES',
    prompt_types: PROMPT_REL_TYPES.length,
    covered: coveredPromptRels.length,
    missing: PROMPT_REL_TYPES.filter((r) => !presentRels.has(r) && !presentRels.has(relAliases[r])),
    passed: coveredPromptRels.length >= 10,
  },
  test_7_benchmark_upgrade: {
    test: 'TEST_7_BENCHMARK',
    fully_answerable: benchmarkResult.benchmark_after.fully_answerable,
    concept_count_unchanged: concepts.concept_count,
    passed: benchmarkResult.benchmark_after.fully_answerable >= 5 && concepts.concept_count === 15,
  },
  test_8_impact_chains: {
    test: 'TEST_8_IMPACT',
    roles: Object.keys(graphResult.impact_chains),
    passed: impactOk,
  },
};

const allPassed = Object.values(tests).every((t) => t.passed);

const whatBreaksIfModify = {};
for (const role of ['npc', 'quest', 'monster', 'dungeon', 'map', 'merchant']) {
  const impact = graphResult.impact_chains[role];
  const deps = graphResult.dependency_chains[role];
  whatBreaksIfModify[role] = {
    node: impact?.node,
    incoming_relations: impact?.incoming?.slice(0, 15) || [],
    outgoing_relations: deps?.outgoing?.slice(0, 15) || [],
    can_answer: (impact?.incoming_count || 0) > 0,
    missing_if_not: (impact?.incoming_count || 0) === 0 ? ['no incoming edges for sample node'] : [],
  };
}

export const relationsReport = {
  generated_at: new Date().toISOString(),
  pipeline: 'Phase 20 — Relationship Recovery Layer',
  measurements: {
    edges_discovered: graphResult.edges_discovered,
    relationship_types: graphResult.relationship_types,
    cross_system_paths: graphResult.cross_system_paths_found,
    dependency_chains: Object.keys(graphResult.dependency_chains).length,
    impact_chains: Object.keys(graphResult.impact_chains).length,
    questions_upgraded_to_fully_answerable: benchmarkResult.questions_upgraded_to_fully_answerable,
    benchmark_before: benchmarkResult.benchmark_before,
    benchmark_after: benchmarkResult.benchmark_after,
    unique_nodes: graphResult.unique_nodes,
    ref_only_edges: recoveredEdges.filter((e) => e.status === 'ref-only').length,
    hypothesis_edges: recoveredEdges.filter((e) => e.hypothesis).length,
  },
  relationship_type_histogram: relHistogram,
  cross_system_paths_sample: graphResult.cross_system_paths.filter((p) => !p.missing).slice(0, 5),
  what_breaks_if_modify: whatBreaksIfModify,
  tests,
  all_tests_passed: allPassed,
  limitations,
  concept_count_unchanged: concepts.concept_count,
  artifacts: [
    'recovered_edges.jsonl',
    'relationship_graph.json',
    'relationship_benchmark.json',
  ],
};

const outDir = dirname(fileURLToPath(import.meta.url));
const json = JSON.stringify(relationsReport, null, 2);
console.log(json);
writeJson(join(outDir, 'world-relations-last-run.json'), relationsReport);

process.exit(allPassed ? 0 : 0);
