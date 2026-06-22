#!/usr/bin/env node
/** §3 — Action mining from observable patterns A–G (graph + SQL) */
import { loadGraph, loadSql, parseTableInserts } from './_lib.mjs';
import { result as explore } from './explore-entities.mjs';
import { result as clusters } from './discover-clusters.mjs';

const { edges, nodeList } = loadGraph();
const sql = loadSql();

function action(id, fields) {
  return { action_id: id, ...fields };
}

function confidence(graphEvidence, sqlEvidence) {
  if (graphEvidence && sqlEvidence) return 0.95;
  if (graphEvidence) return 0.85;
  if (sqlEvidence) return 0.7;
  return 0.3;
}

const discovered = [];

// Pattern A — NPC→SELLS→ITEM
const sellsEdges = edges.filter((e) => e.rel === 'SELLS');
if (sellsEdges.length) {
  discovered.push(
    action('modify_npc_shop', {
      action_name: 'modify_npc_shop',
      pattern: 'A',
      type: 'write/sim',
      description: 'Modify NPC shop inventory or prices via npcs_items',
      graph_evidence: true,
      sql_evidence: true,
      confidence: confidence(true, true),
      evidence_edges: sellsEdges.map((e) => e.id),
      evidence_sample: sellsEdges.slice(0, 3).map((e) => ({
        edge: e.id,
        npc: e.src,
        item: e.dst,
        price: e.props?.price,
      })),
      required_sources: {
        graph: ['SELLS'],
        sql: ['npcs_items', 'npcs'],
      },
      cluster_affinity: 'economy_catalog',
    }),
  );
}

// Pattern B — QUEST→HAS_STEP→REWARDS / INVOLVES_NPC
const hasStep = edges.filter((e) => e.rel === 'HAS_STEP');
const rewards = edges.filter((e) => e.rel === 'REWARDS');
const involvesNpc = edges.filter((e) => e.rel === 'INVOLVES_NPC');
if (hasStep.length || rewards.length || involvesNpc.length) {
  discovered.push(
    action('create_quest_flow', {
      action_name: 'create_quest_flow',
      pattern: 'B',
      type: 'write/sim',
      description: 'Author quest with steps, objectives, rewards and NPC involvement',
      graph_evidence: true,
      sql_evidence: true,
      confidence: confidence(true, true),
      evidence_edges: [
        ...hasStep.map((e) => e.id),
        ...rewards.map((e) => e.id),
        ...involvesNpc.map((e) => e.id),
      ].slice(0, 10),
      required_sources: {
        graph: ['HAS_STEP', 'REWARDS', 'INVOLVES_NPC'],
        sql: ['quests', 'quests_steps', 'quests_objectives'],
      },
      cluster_affinity: 'quest_progression',
    }),
  );
}

// Pattern C — ITEM→HAS_TYPE
const hasType = edges.filter((e) => e.rel === 'HAS_TYPE');
if (hasType.length) {
  discovered.push(
    action('link_item_catalog', {
      action_name: 'link_item_catalog',
      pattern: 'C',
      type: 'read',
      description: 'Link items to catalog types (items.TypeId; client D2O for names)',
      graph_evidence: true,
      sql_evidence: true,
      confidence: confidence(true, true),
      evidence_edges: hasType.map((e) => e.id),
      required_sources: {
        graph: ['HAS_TYPE'],
        sql: ['items'],
      },
      cluster_affinity: 'economy_catalog',
    }),
  );
}

// Pattern D — SQL only worlds_npcs
const { rows: worldSpawns } = parseTableInserts(sql, 'worlds_npcs');
if (worldSpawns.length) {
  discovered.push(
    action('spawn_npc_in_world', {
      action_name: 'spawn_npc_in_world',
      pattern: 'D',
      type: 'write/sim',
      description: 'Place NPC on map via worlds_npcs (Npc, Map, Cell, Direction)',
      graph_evidence: false,
      sql_evidence: true,
      confidence: confidence(false, true),
      evidence_edges: [],
      evidence_sql: {
        table: 'worlds_npcs',
        row_count: worldSpawns.length,
        sample: worldSpawns.slice(0, 2).map((r) => ({
          Npc: r.Npc,
          Map: r.Map,
          Cell: r.Cell,
        })),
      },
      required_sources: {
        graph: [],
        sql: ['worlds_npcs', 'npcs'],
      },
      cluster_affinity: null,
      warning: 'no graph Map nodes — SQL-only action',
    }),
  );
}

// Pattern E — SQL quests_steps.KamasReward aggregation
const { rows: questSteps } = parseTableInserts(sql, 'quests_steps');
const kamasSteps = questSteps.filter((s) => Number(s.KamasReward) > 0);
if (kamasSteps.length) {
  const totalKamas = kamasSteps.reduce(
    (s, r) => s + (Number(r.KamasReward) || 0),
    0,
  );
  discovered.push(
    action('adjust_quest_rewards', {
      action_name: 'adjust_quest_rewards',
      pattern: 'E',
      type: 'read/write',
      description: 'Adjust quest step kamas/XP/item rewards in quests_steps',
      graph_evidence: nodeList.some(
        (n) => n.type === 'QuestStep' && (n.props?.kamas || n.props?.KamasReward),
      ),
      sql_evidence: true,
      confidence: confidence(
        nodeList.some((n) => n.type === 'QuestStep' && n.props?.kamas),
        true,
      ),
      evidence_edges: edges
        .filter((e) => e.rel === 'REWARDS')
        .map((e) => e.id),
      evidence_sql: {
        table: 'quests_steps',
        steps_with_kamas: kamasSteps.length,
        total_kamas_sampled: totalKamas,
      },
      required_sources: {
        graph: ['HAS_STEP', 'REWARDS'],
        sql: ['quests_steps', 'quests'],
      },
      cluster_affinity: 'quest_progression',
    }),
  );
}

// Pattern F — USES_EFFECT + OBSERVED_IN
const usesEffect = edges.filter((e) => e.rel === 'USES_EFFECT');
const observedIn = edges.filter((e) => e.rel === 'OBSERVED_IN');
if (usesEffect.length && observedIn.length) {
  const spellIds = new Set([
    ...usesEffect.map((e) => e.src),
    ...observedIn.map((e) => e.src),
  ]);
  discovered.push(
    action('inspect_spell_runtime', {
      action_name: 'inspect_spell_runtime',
      pattern: 'F',
      type: 'read',
      description: 'Inspect spell runtime behavior from LOG edges (USES_EFFECT, OBSERVED_IN)',
      graph_evidence: true,
      sql_evidence: false,
      confidence: confidence(true, false),
      evidence_edges: [
        ...usesEffect.map((e) => e.id),
        ...observedIn.map((e) => e.id),
      ].slice(0, 8),
      spell_nodes: [...spellIds].slice(0, 5),
      required_sources: {
        graph: ['USES_EFFECT', 'OBSERVED_IN', 'CONTRADICTS'],
        sql: [],
      },
      cluster_affinity: 'combat_runtime_spell',
    }),
  );
}

// Pattern G — SQL npcs_items bulk
const { rows: npcItems } = parseTableInserts(sql, 'npcs_items');
if (npcItems.length) {
  discovered.push(
    action('audit_npc_economy', {
      action_name: 'audit_npc_economy',
      pattern: 'G',
      type: 'read',
      description: 'Audit NPC shop economy across all npcs_items rows',
      graph_evidence: sellsEdges.length > 0,
      sql_evidence: true,
      confidence: confidence(sellsEdges.length > 0, true),
      evidence_edges: sellsEdges.map((e) => e.id),
      evidence_sql: {
        table: 'npcs_items',
        row_count: npcItems.length,
        unique_npcs: new Set(npcItems.map((r) => r.NpcId)).size,
        unique_items: new Set(npcItems.map((r) => r.Item)).size,
      },
      required_sources: {
        graph: ['SELLS'],
        sql: ['npcs_items', 'npcs', 'items'],
      },
      cluster_affinity: 'economy_catalog',
    }),
  );
}

export const result = {
  phase: 'ACTION_MINING',
  patterns_checked: ['A', 'B', 'C', 'D', 'E', 'F', 'G'],
  discovered_actions: discovered,
  action_count: discovered.length,
  graph_only_actions: discovered.filter((a) => a.graph_evidence && !a.sql_evidence).length,
  sql_only_actions: discovered.filter((a) => !a.graph_evidence && a.sql_evidence).length,
  dual_source_actions: discovered.filter((a) => a.graph_evidence && a.sql_evidence).length,
  cluster_context: clusters.clusters_detected.map((c) => ({
    id: c.cluster_id,
    label: c.label,
    coherence: c.coherence_score,
  })),
  exploration_ref: {
    graph_node_count: explore.graph.node_count,
    not_present: explore.not_present_in_graph,
  },
};

if (process.argv[1]?.includes('mine-actions')) {
  console.log(JSON.stringify(result, null, 2));
}
