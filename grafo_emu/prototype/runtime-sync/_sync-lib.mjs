import { readFileSync, writeFileSync, mkdirSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

export const SYNC_DIR = dirname(fileURLToPath(import.meta.url));
export const PROTO_DIR = join(SYNC_DIR, '..');
export const WC_DIR = join(PROTO_DIR, 'world-causal');
export const WR_DIR = join(PROTO_DIR, 'world-relations');
export const SIM_DIR = join(PROTO_DIR, 'mcp-execution-sim');
export const BRIDGE_DIR = join(PROTO_DIR, 'mcp-execution-bridge');
export const BRIDGE_OUT = join(BRIDGE_DIR, 'out');

export const PHASE20_ARTIFACTS = [
  { path: join(WR_DIR, 'recovered_edges.jsonl'), label: 'F20 recovered_edges' },
  { path: join(WR_DIR, 'relationship_graph.json'), label: 'F20 relationship_graph' },
];

export const PHASE21_ARTIFACTS = [
  { path: join(WC_DIR, 'causal_graph.jsonl'), label: 'F21 causal_graph' },
  { path: join(WC_DIR, 'propagation_models.json'), label: 'F21 propagation_models' },
];

export const PHASE22_ARTIFACTS = [
  { path: join(SIM_DIR, 'execution_plans.json'), label: 'F22 execution_plans' },
  { path: join(SIM_DIR, 'blast_radius_report.json'), label: 'F22 blast_radius_report' },
];

export const PHASE23_ARTIFACTS = [
  { path: join(BRIDGE_DIR, 'mcp-execution-bridge-last-run.json'), label: 'F23 bridge last-run' },
];

/** Table → edge rel types sourced from recover-edges.mjs */
export const TABLE_IMPACT_MAP = {
  items: {
    edge_types: [],
    structural_columns: ['Id'],
    recovery_phases: [],
  },
  npcs: {
    edge_types: [],
    structural_columns: ['Id', 'ActionsIdCSV', 'DialogMessagesIdCSV', 'DialogRepliesIdCSV'],
    recovery_phases: [],
  },
  npcs_items: {
    edge_types: ['SELLS'],
    structural_columns: ['NpcId', 'Item', 'Id'],
    prop_columns: ['Price', 'Token', 'ActionId'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  worlds_npcs: {
    edge_types: ['SPAWNED_IN'],
    structural_columns: ['Npc', 'Map', 'Id'],
    prop_columns: ['Cell', 'Direction'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  npcs_actions: {
    edge_types: ['OFFERS_ACTION', 'SELLS'],
    structural_columns: ['NpcId', 'Type', 'Parameters', 'Id'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  monsters_drops: {
    edge_types: ['DROPS_ITEM'],
    structural_columns: ['MonsterId', 'ItemId', 'Id'],
    prop_columns: ['DropRateForGrade1', 'ProspectingLock'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  quests_objectives: {
    edge_types: ['INVOLVES_NPC', 'STARTS_QUEST'],
    structural_columns: ['Id', 'Type', 'ParametersCSV', 'StepId'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  quests_steps: {
    edge_types: ['HAS_STEP', 'REWARDS_ITEM', 'REWARDS_SPELL', 'STARTS_QUEST'],
    structural_columns: ['Id', 'QuestId', 'ItemsRewardCSV', 'SpellsRewardCSV'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  worlds_maps: {
    edge_types: ['IN_SUBAREA', 'NEIGHBOR_OF'],
    structural_columns: ['Id', 'SubAreaId', 'TopNeighbourId', 'BottomNeighbourId', 'LeftNeighbourId', 'RightNeighbourId'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
  dungeons: {
    edge_types: ['LOCATED_AT', 'CONTAINS_MONSTER', 'EXITS_TO'],
    structural_columns: ['Id', 'Map', 'MonstersCSV', 'Parameters'],
    recovery_phases: ['Phase20', 'Phase21'],
  },
};

/** Column → impact class for metadata-only detection */
export const COLUMN_IMPACT_MAP = {
  items: {
    Name: 'metadata',
    Price: 'metadata',
    Level: 'metadata',
    Weight: 'metadata',
    IconId: 'metadata',
    Id: 'structural',
  },
  npcs: {
    Name: 'metadata',
    EntityLook: 'metadata',
    Id: 'structural',
    ActionsIdCSV: 'structural',
    DialogMessagesIdCSV: 'structural',
    DialogRepliesIdCSV: 'structural',
  },
  npcs_items: {
    Price: 'edge_props',
    NpcId: 'structural',
    Item: 'structural',
    Id: 'structural',
  },
};

export const INVALIDATED_BY_PHASE = {
  Phase20: [
    { path: 'world-relations/recovered_edges.jsonl', rerun: 'node run-relations.mjs', dir: WR_DIR },
    { path: 'world-relations/relationship_graph.json', rerun: 'node run-relations.mjs', dir: WR_DIR },
    { path: 'world-relations/relationship_benchmark.json', rerun: 'node run-relations.mjs', dir: WR_DIR },
  ],
  Phase21: [
    { path: 'world-causal/causal_graph.jsonl', rerun: 'node run-causal.mjs', dir: WC_DIR },
    { path: 'world-causal/propagation_models.json', rerun: 'node run-causal.mjs', dir: WC_DIR },
    { path: 'world-causal/causal_benchmark.json', rerun: 'node run-causal.mjs', dir: WC_DIR },
  ],
};

export function writeJson(path, data) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

export function hashString(s) {
  return createHash('sha256').update(s).digest('hex').slice(0, 16);
}

export function parseNodeId(nodeIdStr) {
  const i = nodeIdStr.indexOf(':');
  if (i < 0) return { type: null, id: nodeIdStr };
  return { type: nodeIdStr.slice(0, i), id: nodeIdStr.slice(i + 1) };
}

export function countJsonlLines(path) {
  const content = readFileSync(path, 'utf8');
  return content.split('\n').filter(Boolean).length;
}

export function artifactStat(path) {
  if (!existsSync(path)) return { exists: false, bytes: 0 };
  const st = statSync(path);
  return { exists: true, bytes: st.size, mtime_ms: st.mtimeMs };
}

export function readCausalEdges() {
  const path = join(WC_DIR, 'causal_graph.jsonl');
  return readFileSync(path, 'utf8')
    .split('\n')
    .filter(Boolean)
    .map((line) => JSON.parse(line));
}

export function loadF22Artifacts() {
  const plans = JSON.parse(readFileSync(join(SIM_DIR, 'execution_plans.json'), 'utf8'));
  const blast = JSON.parse(readFileSync(join(SIM_DIR, 'blast_radius_report.json'), 'utf8'));
  return { plans, blast };
}

export function findPlanAndValidation(artifacts, intentId, targetNode) {
  const planIdx = artifacts.plans.plans.findIndex(
    (p) => p.intent_id === intentId && (!targetNode || p.target_node === targetNode),
  );
  if (planIdx < 0) return { plan: null, validation: null };
  return {
    plan: artifacts.plans.plans[planIdx],
    validation: artifacts.blast.validations[planIdx],
  };
}

export function columnsChanged(before, after) {
  const cols = new Set([...Object.keys(before || {}), ...Object.keys(after || {})]);
  const changed = [];
  for (const col of cols) {
    if ((before || {})[col] !== (after || {})[col]) changed.push(col);
  }
  return changed;
}

export function classifyColumnImpact(table, columns) {
  const tableMap = COLUMN_IMPACT_MAP[table] || {};
  const tableRules = TABLE_IMPACT_MAP[table] || {};
  const impacts = columns.map((col) => {
    if (tableMap[col]) return tableMap[col];
    if (tableRules.structural_columns?.includes(col)) return 'structural';
    if (tableRules.prop_columns?.includes(col)) return 'edge_props';
    return 'unknown';
  });
  const hasStructural = impacts.includes('structural') || impacts.includes('unknown');
  const hasEdgeProps = impacts.includes('edge_props');
  const allMetadata = impacts.every((i) => i === 'metadata');
  return { impacts, hasStructural, hasEdgeProps, allMetadata };
}

export function classifyTableChange(table, columns, operation = 'UPDATE') {
  const rules = TABLE_IMPACT_MAP[table];
  if (!rules) {
    return {
      graph_requires_update: true,
      causal_recompute_required: true,
      affected_edges: [],
      recovery_required: ['Phase20', 'Phase21'],
      impact_class: 'unknown_table',
    };
  }

  const colImpact = classifyColumnImpact(table, columns);
  if (operation === 'DELETE' || operation === 'INSERT') {
    return {
      graph_requires_update: true,
      causal_recompute_required: true,
      affected_edges: rules.edge_types || [],
      recovery_required: rules.recovery_phases?.length ? [...rules.recovery_phases] : ['Phase20', 'Phase21'],
      impact_class: 'structural',
    };
  }

  if (colImpact.allMetadata && (rules.edge_types?.length || 0) === 0) {
    return {
      graph_requires_update: false,
      causal_recompute_required: false,
      affected_edges: [],
      recovery_required: [],
      impact_class: 'metadata',
    };
  }

  if (colImpact.hasStructural) {
    return {
      graph_requires_update: true,
      causal_recompute_required: true,
      affected_edges: rules.edge_types || [],
      recovery_required: rules.recovery_phases?.length ? [...rules.recovery_phases] : ['Phase20', 'Phase21'],
      impact_class: 'structural',
    };
  }

  if (colImpact.hasEdgeProps) {
    return {
      graph_requires_update: true,
      causal_recompute_required: false,
      affected_edges: rules.edge_types || [],
      recovery_required: rules.recovery_phases?.length ? [...rules.recovery_phases] : [],
      impact_class: 'edge_props',
    };
  }

  return {
    graph_requires_update: false,
    causal_recompute_required: false,
    affected_edges: [],
    recovery_required: [],
    impact_class: 'metadata',
  };
}

export function invalidatedArtifactsForPhases(phases) {
  const seen = new Set();
  const out = [];
  for (const phase of phases) {
    for (const art of INVALIDATED_BY_PHASE[phase] || []) {
      if (seen.has(art.path)) continue;
      seen.add(art.path);
      out.push({ path: art.path, rerun: art.rerun, reason: `${phase} re-ingest required` });
    }
  }
  return out;
}

export function edgesForEntity(edges, entity) {
  return edges.filter((e) => e.src === entity || e.dst === entity);
}

export function relTypesForEntity(edges, entity) {
  return [...new Set(edgesForEntity(edges, entity).map((e) => e.rel))];
}
