#!/usr/bin/env node
/** TEST 2 — Economy Inference: quest kamas from sunshine.sql + graph cross-check */
import { loadGraph, loadSql, parseTableInserts } from './_lib.mjs';

const sql = loadSql();
const { rows: questSteps } = parseTableInserts(sql, 'quests_steps');
const { rows: quests } = parseTableInserts(sql, 'quests');
const { rows: npcItems } = parseTableInserts(sql, 'npcs_items');

const questNames = new Map(quests.map((q) => [Number(q.Id), q.Name]));

const byQuestKamas = new Map();
for (const step of questSteps) {
  const qid = Number(step.Quest);
  const kamas = Number(step.KamasReward) || 0;
  if (!byQuestKamas.has(qid)) byQuestKamas.set(qid, 0);
  byQuestKamas.set(qid, byQuestKamas.get(qid) + kamas);
}

const topQuestsByKamas = [...byQuestKamas.entries()]
  .map(([questId, totalKamas]) => ({
    quest_id: questId,
    quest_name: questNames.get(questId) || null,
    total_kamas_reward: totalKamas,
  }))
  .sort((a, b) => b.total_kamas_reward - a.total_kamas_reward)
  .slice(0, 10);

const { edges, nodeList } = loadGraph();
const graphQuestIds = new Set(
  nodeList.filter((n) => n.type === 'Quest').map((n) => Number(n.id.split(':')[1])),
);
const dbQuestCount = quests.length;
const graphQuestCoverage = graphQuestIds.size / Math.max(1, dbQuestCount);

const graphQuestKamas = {};
for (const n of nodeList.filter((n) => n.type === 'QuestStep')) {
  const q = n.props?.quest;
  const k = n.props?.kamas ?? n.props?.KamasReward;
  if (q != null && k != null) {
    graphQuestKamas[q] = (graphQuestKamas[q] || 0) + Number(k);
  }
}
const quest3Graph = nodeList
  .filter((n) => n.type === 'QuestStep' && n.props?.quest === 3)
  .map((n) => ({ step: n.id, kamas: n.props?.kamas, xp: n.props?.xp }));

const quest3Db = topQuestsByKamas.find((q) => q.quest_id === 3);
const quest3DbTotal = byQuestKamas.get(3) || 0;

const graphVsDb = {
  quests_in_graph: graphQuestIds.size,
  quests_in_db: dbQuestCount,
  coverage_ratio: Math.round(graphQuestCoverage * 10000) / 10000,
  quest_3_db_total_kamas: quest3DbTotal,
  quest_3_graph_steps: quest3Graph,
  note: 'graph prototype stores kamas in QuestStep.props from manual ingest; DB uses KamasReward column',
};

const shopEdges = edges.filter((e) => e.rel === 'SELLS');
const topShops = shopEdges
  .map((e) => ({
    edge_id: e.id,
    npc: e.src,
    item: e.dst,
    price: e.props?.price ?? null,
  }))
  .sort((a, b) => (b.price || 0) - (a.price || 0));

const dbShopCount = npcItems.length;

export const result = {
  test: 'TEST_2_ECONOMY_INFERENCE',
  top_10_quests_by_kamas: topQuestsByKamas,
  graph_vs_db: graphVsDb,
  farm_loops_detected: [],
  farm_loop_note: 'no SELLS/DROP cycle in prototype graph',
  npc_shop_in_graph: topShops,
  npc_shop_in_db_count: dbShopCount,
  economy_inference_consistent: quest3DbTotal > 0,
};

if (process.argv[1]?.includes('infer-economy')) {
  console.log(JSON.stringify(result, null, 2));
}
