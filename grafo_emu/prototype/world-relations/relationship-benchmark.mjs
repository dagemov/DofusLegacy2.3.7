#!/usr/bin/env node
/** Re-run Phase 19 benchmark against recovered relationship graph */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { readArtifact, readSemanticArtifact, writeJson } from './_relations-lib.mjs';
import { recoveredEdges } from './recover-edges.mjs';

const phase19 = readSemanticArtifact('semantic_benchmark.json');
const automation = readArtifact('automation_eval.json');

const relSet = new Set(recoveredEdges.map((e) => e.rel));
const relCount = (rel) => recoveredEdges.filter((e) => e.rel === rel).length;

function hasRel(rel, minInstances = 1) {
  return relSet.has(rel) && relCount(rel) >= minInstances;
}

function evaluateRead(question, requiredRels, extraBlockers = []) {
  const missing = requiredRels.filter((r) => !hasRel(r));
  if (missing.length) {
    return {
      question,
      answerability: 'not_answerable',
      required_rels: requiredRels,
      evidence_edges: requiredRels.filter((r) => hasRel(r)),
      blockers: [`missing relationship types: ${missing.join(', ')}`, ...extraBlockers],
    };
  }
  return {
    question,
    answerability: 'fully_answerable',
    required_rels: requiredRels,
    evidence_edges: requiredRels,
    rel_instances: Object.fromEntries(requiredRels.map((r) => [r, relCount(r)])),
    blockers: extraBlockers,
  };
}

function evaluateCreate(question, requiredRels, tables) {
  const missing = requiredRels.filter((r) => !hasRel(r));
  const blockers = [
    automation.real_blockers_global?.[0] || 'no MCP write path to MariaDB',
    'create/edit requires write path — relationships recovered for read-only traversal only',
  ];
  if (missing.length) blockers.unshift(`missing relationship types: ${missing.join(', ')}`);
  return {
    question,
    answerability: 'partially_answerable',
    required_rels: requiredRels,
    evidence_tables: tables,
    evidence_edges: requiredRels.filter((r) => hasRel(r)),
    blockers,
  };
}

const questions = [
  evaluateRead('What defines a dungeon?', ['LOCATED_AT', 'CONTAINS_MONSTER', 'EXITS_TO']),
  evaluateRead('What NPC starts this quest?', ['STARTS_QUEST', 'INVOLVES_NPC', 'HAS_STEP']),
  evaluateRead('What maps participate in this quest?', ['PARTICIPATES_IN_MAP', 'HAS_STEP', 'HAS_OBJECTIVE'], []),
  evaluateRead('What monsters appear in this zone?', ['IN_SUBAREA', 'SPAWNS_MONSTER'], ['zone = subarea granularity via IN_SUBAREA -> SPAWNS_MONSTER']),
  evaluateRead('What content depends on this NPC?', ['SELLS', 'SPAWNED_IN', 'INVOLVES_NPC', 'STARTS_QUEST']),
  evaluateCreate('What must I modify to create a new quest?', ['HAS_STEP', 'HAS_OBJECTIVE', 'INVOLVES_NPC'], ['quests', 'quests_steps', 'quests_objectives']),
  evaluateCreate('What must I modify to create a new dungeon?', ['LOCATED_AT', 'CONTAINS_MONSTER', 'EXITS_TO'], ['dungeons', 'monsters', 'worlds_maps']),
  evaluateCreate('What must I modify to create a new merchant?', ['SELLS', 'SPAWNED_IN', 'OFFERS_ACTION'], ['npcs', 'npcs_items', 'npcs_actions', 'worlds_npcs']),
  evaluateCreate('What must I modify to create a new boss?', ['CONTAINS_MONSTER', 'USES_SPELL', 'DROPS_ITEM', 'SPAWNS_MONSTER'], ['monsters', 'monsters_grades', 'monsters_spells', 'dungeons', 'worlds_monsters']),
  evaluateCreate('What must I modify to create a new zone?', ['IN_SUBAREA', 'SPAWNS_MONSTER', 'NEIGHBOR_OF', 'SPAWNED_IN'], ['worlds_maps', 'worlds_maps_positions', 'worlds_monsters', 'worlds_npcs']),
];

const distribution = questions.reduce((acc, q) => {
  acc[q.answerability] = (acc[q.answerability] || 0) + 1;
  return acc;
}, {});

const before = {
  fully_answerable: phase19.fully_answerable || 0,
  partially_answerable: phase19.partially_answerable || 10,
  not_answerable: phase19.not_answerable || 0,
};

const after = {
  fully_answerable: distribution.fully_answerable || 0,
  partially_answerable: distribution.partially_answerable || 0,
  not_answerable: distribution.not_answerable || 0,
};

export const benchmarkResult = {
  phase: 'RELATIONSHIP_BENCHMARK_RERUN',
  question_count: questions.length,
  benchmark_before: before,
  benchmark_after: after,
  questions_upgraded_to_fully_answerable: Math.max(0, after.fully_answerable - before.fully_answerable),
  distribution,
  questions,
  edges_used: recoveredEdges.length,
  relationship_types_available: relSet.size,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'relationship_benchmark.json'), benchmarkResult);

if (process.argv[1]?.includes('relationship-benchmark')) {
  console.log(JSON.stringify({ before, after, upgraded: benchmarkResult.questions_upgraded_to_fully_answerable }, null, 2));
}
