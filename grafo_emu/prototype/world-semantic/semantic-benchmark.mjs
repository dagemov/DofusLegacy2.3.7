#!/usr/bin/env node
/** Phase F — Semantic question benchmark (10 questions) */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  loadGraph,
  parseTableInserts,
  parseAllTables,
  readArtifact,
  writeJson,
} from './_semantic-lib.mjs';
import { result as concepts } from './discover-concepts.mjs';

const sql = loadSql();
const { nodeList, edges } = loadGraph();
const tables = parseAllTables(sql);
const tableNames = new Set(tables.map((t) => t.name));
const automation = readArtifact('automation_eval.json');

const hasTable = (...names) => names.every((n) => tableNames.has(n));
const graphHas = (prefix) => nodeList.some((n) => n.id?.startsWith(prefix));
const graphRel = (rel) => edges.some((e) => e.rel === rel);

function evaluate(question, requiredTables, requiredGraph, blockers = []) {
  const tablesOk = requiredTables.every((t) => tableNames.has(t));
  const graphOk = requiredGraph.every((g) => {
    if (g.type === 'node') return graphHas(g.prefix);
    if (g.type === 'rel') return graphRel(g.rel);
    return true;
  });
  const graphCoverage = nodeList.length < 50;

  let answerability = 'fully_answerable';
  const realBlockers = [...blockers];

  if (!tablesOk) {
    answerability = 'not_answerable';
    realBlockers.push(`missing tables: ${requiredTables.filter((t) => !tableNames.has(t)).join(', ')}`);
  } else if (!graphOk || graphCoverage) {
    answerability = 'partially_answerable';
    if (graphCoverage) realBlockers.push('graph prototype covers <50 nodes — world semantics mostly SQL-only');
    if (!graphOk) realBlockers.push('required graph evidence absent in prototype');
  }

  return {
    question,
    answerability,
    evidence_tables: requiredTables.filter((t) => tableNames.has(t)),
    evidence_graph: requiredGraph,
    blockers: realBlockers,
    sql_priority: true,
  };
}

const questions = [
  evaluate(
    'What defines a dungeon?',
    ['dungeons', 'worlds_maps', 'monsters'],
    [],
    [],
  ),
  evaluate(
    'What NPC starts this quest?',
    ['quests_objectives', 'npcs', 'quests_steps'],
    [{ type: 'rel', rel: 'INVOLVES_NPC' }],
    ['quest giver role inferred from objective ParametersCSV — not a dedicated column'],
  ),
  evaluate(
    'What maps participate in this quest?',
    ['quests_objectives', 'worlds_maps', 'worlds_npcs'],
    [],
    ['map participation only explicit when objective type references map or NPC spawn map'],
  ),
  evaluate(
    'What monsters appear in this zone?',
    ['worlds_monsters', 'monsters', 'worlds_maps'],
    [],
    ['zone = SubArea via worlds_monsters — not per-map in open world'],
  ),
  evaluate(
    'What content depends on this NPC?',
    ['npcs', 'npcs_items', 'quests_objectives', 'worlds_npcs'],
    [{ type: 'rel', rel: 'SELLS' }],
    [],
  ),
  evaluate(
    'What must I modify to create a new quest?',
    ['quests', 'quests_steps', 'quests_objectives'],
    [{ type: 'rel', rel: 'HAS_STEP' }],
    automation.real_blockers_global?.slice(0, 2) || ['no write path'],
  ),
  evaluate(
    'What must I modify to create a new dungeon?',
    ['dungeons', 'monsters', 'worlds_maps'],
    [],
    ['dungeon row + monster CSV + exit Parameters'],
  ),
  evaluate(
    'What must I modify to create a new merchant?',
    ['npcs', 'npcs_items', 'npcs_actions', 'worlds_npcs'],
    [{ type: 'rel', rel: 'SELLS' }],
    [],
  ),
  evaluate(
    'What must I modify to create a new boss?',
    ['monsters', 'monsters_grades', 'monsters_spells', 'dungeons', 'worlds_monsters'],
    [],
    ['boss = monster template + grade + placement in dungeon or sub-area group'],
  ),
  evaluate(
    'What must I modify to create a new zone?',
    ['worlds_maps', 'worlds_maps_positions', 'worlds_monsters', 'worlds_npcs'],
    [],
    ['sub-area and map topology — continent names not in server DB (hypothesis: client D2O)'],
  ),
];

const distribution = questions.reduce((acc, q) => {
  acc[q.answerability] = (acc[q.answerability] || 0) + 1;
  return acc;
}, {});

export const result = {
  phase: 'F_SEMANTIC_BENCHMARK',
  question_count: questions.length,
  distribution,
  fully_answerable: distribution.fully_answerable || 0,
  partially_answerable: distribution.partially_answerable || 0,
  not_answerable: distribution.not_answerable || 0,
  concept_count: concepts.concept_count,
  questions,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'semantic_benchmark.json'), result);

if (process.argv[1]?.includes('semantic-benchmark')) {
  console.log(JSON.stringify({ distribution }, null, 2));
}
