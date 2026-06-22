import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadSql, parseTableColumns } from '../world-control/_lib.mjs';
import { writeJson } from '../world-semantic/_semantic-lib.mjs';

export { writeJson, loadSql, parseTableColumns };

export const SIM_DIR = dirname(fileURLToPath(import.meta.url));
export const WC_DIR = join(SIM_DIR, '..', 'world-causal');
export const WR_DIR = join(SIM_DIR, '..', 'world-relations');

export const NODE_TYPE_ALLOWLIST = new Set([
  'npc', 'map', 'monster', 'item', 'spell', 'quest', 'queststep',
  'objective', 'dungeon', 'subarea', 'interactive',
]);

export const CAUSAL_SUBGRAPH_EXCLUDE = new Set(['NEIGHBOR_OF']);

export const GRAPH_TO_RUNTIME = {
  npc: {
    sql_tables: ['npcs', 'worlds_npcs', 'npcs_items', 'npcs_actions', 'npcs_messages', 'npcs_replies'],
    cs_manager: 'NpcManager',
    write_path: 'sql_patch_or_gm_spawn',
  },
  quest: {
    sql_tables: ['quests', 'quests_steps', 'quests_objectives'],
    cs_manager: 'QuestManager',
    write_path: 'sql_patch',
  },
  queststep: {
    sql_tables: ['quests_steps'],
    cs_manager: 'QuestManager',
    write_path: 'sql_patch',
  },
  objective: {
    sql_tables: ['quests_objectives'],
    cs_manager: 'QuestManager',
    write_path: 'sql_patch',
  },
  monster: {
    sql_tables: ['monsters', 'monsters_grades', 'monsters_spells', 'monsters_drops'],
    cs_manager: 'MonsterManager',
    write_path: 'sql_patch',
  },
  map: {
    sql_tables: ['worlds_maps', 'worlds_maps_positions', 'worlds_npcs', 'worlds_monsters', 'worlds_interactives'],
    cs_manager: 'MapManager',
    write_path: 'sql_patch',
  },
  dungeon: {
    sql_tables: ['dungeons'],
    cs_manager: 'DungeonManager',
    write_path: 'sql_patch',
  },
  item: {
    sql_tables: ['items', 'npcs_items', 'monsters_drops'],
    cs_manager: 'InventoryHandler',
    write_path: 'admin_api_items_only',
  },
  spell: {
    sql_tables: ['spells', 'spells_levels', 'monsters_spells'],
    cs_manager: 'SpellManager',
    write_path: 'sql_patch',
  },
  interactive: {
    sql_tables: ['interactives', 'worlds_interactives', 'interactives_skills'],
    cs_manager: 'InteractiveManager',
    write_path: 'sql_patch',
  },
  subarea: {
    sql_tables: ['worlds_monsters', 'worlds_maps'],
    cs_manager: 'MapManager',
    write_path: 'derived_no_direct_table',
  },
};

export const INTENT_CATALOG = {
  modify_npc: { action: 'modify', target_type: 'npc', description: 'Modify existing NPC template or spawn' },
  modify_quest: { action: 'modify', target_type: 'quest', description: 'Modify quest chain' },
  modify_monster: { action: 'modify', target_type: 'monster', description: 'Modify monster template' },
  modify_dungeon: { action: 'modify', target_type: 'dungeon', description: 'Balance or reconfigure dungeon' },
  modify_map: { action: 'modify', target_type: 'map', description: 'Modify map spawns or topology' },
  modify_item: { action: 'modify', target_type: 'item', description: 'Modify item referenced in economy' },
  create_quest: { action: 'create', target_type: 'quest', description: 'Create new quest from template pattern' },
  create_merchant: { action: 'create', target_type: 'npc', description: 'Create merchant NPC with shop' },
};

export function readCausalEdges() {
  const path = join(WC_DIR, 'causal_graph.jsonl');
  return readFileSync(path, 'utf8')
    .split('\n')
    .filter(Boolean)
    .map((line) => JSON.parse(line));
}

export function readRelArtifact(name) {
  return JSON.parse(readFileSync(join(WR_DIR, name), 'utf8'));
}

export function parseNodeId(nodeIdStr) {
  const i = nodeIdStr.indexOf(':');
  if (i < 0) return { type: null, id: nodeIdStr };
  return { type: nodeIdStr.slice(0, i), id: nodeIdStr.slice(i + 1) };
}

export function buildAdjacency(edges, excludeRels = new Set()) {
  const out = new Map();
  const inn = new Map();
  for (const e of edges) {
    if (excludeRels.has(e.rel)) continue;
    if (!out.has(e.src)) out.set(e.src, []);
    out.get(e.src).push(e);
    if (!inn.has(e.dst)) inn.set(e.dst, []);
    inn.get(e.dst).push(e);
  }
  return { out, inn };
}

export function blastRadius(startNode, adjOut, maxDepth = 6) {
  const visited = new Set();
  const queue = [{ node: startNode, depth: 0, path: [] }];
  const hops = [];
  const roles = new Set();
  let maxWeight = 0;
  let maxRisk = 'LOW';

  while (queue.length) {
    const { node, depth, path } = queue.shift();
    if (depth > maxDepth) continue;
    const edges = adjOut.get(node) || [];
    for (const e of edges) {
      const key = `${e.dst}:${e.rel}`;
      if (visited.has(key)) continue;
      visited.add(key);
      roles.add(e.semantic_role);
      maxWeight = Math.max(maxWeight, e.causal_weight || 0);
      if (e.modification_risk === 'HIGH') maxRisk = 'HIGH';
      else if (e.modification_risk === 'MEDIUM' && maxRisk !== 'HIGH') maxRisk = 'MEDIUM';

      const hop = {
        from: e.src,
        rel: e.rel,
        to: e.dst,
        causal_weight: e.causal_weight,
        semantic_role: e.semantic_role,
        propagation_depth: e.propagation_depth,
      };
      hops.push({ ...hop, depth: depth + 1 });
      queue.push({ node: e.dst, depth: depth + 1, path: [...path, hop] });
    }
  }

  const byDepth = {};
  for (const h of hops) {
    byDepth[h.depth] = (byDepth[h.depth] || 0) + 1;
  }

  return {
    hops: hops.slice(0, 50),
    blast_radius_total: hops.length,
    blast_by_depth: byDepth,
    max_depth: Math.max(0, ...Object.keys(byDepth).map(Number)),
    max_causal_weight: maxWeight,
    max_modification_risk: maxRisk,
    affected_roles: [...roles],
  };
}

export function impactChain(startNode, adjIn, maxDepth = 6) {
  const visited = new Set();
  const queue = [{ node: startNode, depth: 0 }];
  const hops = [];

  while (queue.length) {
    const { node, depth } = queue.shift();
    if (depth > maxDepth) continue;
    const edges = adjIn.get(node) || [];
    for (const e of edges) {
      const key = `${e.src}:${e.rel}`;
      if (visited.has(key)) continue;
      visited.add(key);
      hops.push({
        from: e.src,
        rel: e.rel,
        to: e.dst,
        causal_weight: e.causal_weight,
        semantic_role: e.semantic_role,
        depth: depth + 1,
      });
      queue.push({ node: e.src, depth: depth + 1 });
    }
  }

  return {
    incoming_hops: hops.slice(0, 30),
    incoming_count: hops.length,
  };
}

export function verdict(blast) {
  const total = blast.blast_radius_total;
  const maxRisk = blast.max_modification_risk;
  const maxWeight = blast.max_causal_weight;

  if (maxRisk === 'HIGH' && total > 20) {
    return { verdict: 'BLOCK', reason: 'HIGH modification risk with blast radius > 20' };
  }
  if (total > 50 || maxWeight >= 0.9 && total > 15) {
    return { verdict: 'REVIEW', reason: `Blast radius ${total} or high-weight cascade requires human review` };
  }
  if (total > 10) {
    return { verdict: 'REVIEW', reason: `Moderate blast radius (${total})` };
  }
  return { verdict: 'APPROVE', reason: 'Blast radius within safe threshold' };
}

export function assertNoWrite() {
  return {
    db_writes_executed: false,
    graph_mutated: false,
    persisted: false,
    dry_run: true,
  };
}

export function tablesExistInSql(sql, tables) {
  return tables.map((t) => ({
    table: t,
    exists: parseTableColumns(sql, t).length > 0,
  }));
}

export function rollbackSketch(tables, targetId, targetType) {
  const deletes = [...tables].reverse().map((t) => {
    const col = targetType === 'npc' && t === 'worlds_npcs' ? 'Npc'
      : targetType === 'quest' && t === 'quests_steps' ? 'Quest'
      : 'Id';
    return `DELETE FROM ${t} WHERE ${col}=${targetId};`;
  });
  return deletes.join(' ');
}
