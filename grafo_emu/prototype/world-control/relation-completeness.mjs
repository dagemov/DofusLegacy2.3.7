#!/usr/bin/env node
/** TEST 3 — Relation Completeness: graph anomalies + DB orphan counts */
import { loadGraph, loadSql, parseTableInserts } from './_lib.mjs';

const { nodes, edges, nodeList } = loadGraph();
const sql = loadSql();

const out = new Map();
for (const e of edges) {
  if (!out.has(e.src)) out.set(e.src, []);
  out.get(e.src).push(e);
}

const anomalies = [];

for (const n of nodeList) {
  if (n.status === 'ref-only' || n.props?.resolved === false) {
    anomalies.push({
      type: 'unresolved_node',
      id: n.id,
      reason: n.status || 'resolved=false',
    });
  }
  if (n.status === 'client-side') {
    anomalies.push({ type: 'client_side_gap', id: n.id, reason: 'data not in server BD' });
  }
}

const npcs = nodeList.filter((n) => n.type === 'Npc');
for (const n of npcs) {
  const hasOut = (out.get(n.id) || []).length > 0;
  const hasIn = edges.some((e) => e.dst === n.id);
  if (!hasOut && !hasIn) {
    anomalies.push({ type: 'npc_isolated', id: n.id });
  }
}

const quests = nodeList.filter((n) => n.type === 'Quest');
for (const q of quests) {
  const steps = edges.filter((e) => e.src === q.id && e.rel === 'HAS_STEP');
  if (!steps.length) {
    anomalies.push({ type: 'quest_without_steps', id: q.id });
  }
  for (const st of steps) {
    const rewards = edges.filter((e) => e.src === st.dst && e.rel === 'REWARDS');
    const involves = edges.filter((e) => e.src === st.dst && e.rel === 'INVOLVES_NPC');
    if (!rewards.length && !involves.length) {
      anomalies.push({
        type: 'quest_step_without_reward_or_npc',
        id: st.dst,
      });
    }
  }
}

const items = nodeList.filter((n) => n.type === 'Item');
for (const it of items) {
  const hasOrigin =
    edges.some((e) => e.dst === it.id && ['SELLS', 'REWARDS', 'DROPS'].includes(e.rel));
  if (!hasOrigin) {
    anomalies.push({ type: 'item_without_origin_in_graph', id: it.id });
  }
}

const { rows: dbNpcs } = parseTableInserts(sql, 'npcs');
const { rows: dbSpawns } = parseTableInserts(sql, 'worlds_npcs');
const { rows: dbQuests } = parseTableInserts(sql, 'quests');
const { rows: dbObjectives } = parseTableInserts(sql, 'quests_objectives');
const { rows: dbItems } = parseTableInserts(sql, 'items');

const spawnNpcIds = new Set(dbSpawns.map((r) => Number(r.Npc)));
const npcsWithoutSpawn = dbNpcs.filter((n) => !spawnNpcIds.has(Number(n.Id))).length;

const questIds = new Set(dbQuests.map((q) => Number(q.Id)));
const objectivesByQuest = new Map();
for (const o of dbObjectives) {
  const qs = Number(o.Step);
  if (!objectivesByQuest.has(qs)) objectivesByQuest.set(qs, 0);
  objectivesByQuest.set(qs, objectivesByQuest.get(qs) + 1);
}

const { rows: dbSteps } = parseTableInserts(sql, 'quests_steps');
const stepsWithoutObjectives = dbSteps.filter(
  (s) => !objectivesByQuest.has(Number(s.Id)),
).length;

const graphEntityCount =
  nodeList.filter((n) =>
    ['Quest', 'Npc', 'Item', 'Map'].includes(n.type),
  ).length;
const dbEntityCount = dbQuests.length + dbNpcs.length + dbItems.length;
const coverageScore =
  graphEntityCount / Math.max(1, dbEntityCount);

export const result = {
  test: 'TEST_3_RELATION_COMPLETENESS',
  graph_anomalies: anomalies,
  graph_anomaly_count: anomalies.length,
  db_orphans: {
    npcs_without_world_spawn: npcsWithoutSpawn,
    quest_steps_without_objectives: stepsWithoutObjectives,
    total_npcs_db: dbNpcs.length,
    total_quests_db: dbQuests.length,
    total_items_db: dbItems.length,
  },
  coverage_score: Math.round(coverageScore * 10000) / 10000,
  coverage_note: 'graph prototype entities / DB catalog entities (quest+npc+item)',
};

if (process.argv[1]?.includes('relation-completeness')) {
  console.log(JSON.stringify(result, null, 2));
}
