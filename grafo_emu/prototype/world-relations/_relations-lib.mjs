import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  loadGraph,
  parseTableInserts,
} from '../world-control/_lib.mjs';
import {
  splitCsv,
  splitCsvPairs,
  idSet,
  readArtifact,
  writeJson,
} from '../world-semantic/_semantic-lib.mjs';

export {
  loadSql,
  loadGraph,
  parseTableInserts,
  splitCsv,
  splitCsvPairs,
  idSet,
  readArtifact,
  writeJson,
};

export const WR_DIR = dirname(fileURLToPath(import.meta.url));

export function readSemanticArtifact(name) {
  const path = join(WR_DIR, '..', 'world-semantic', name);
  return JSON.parse(readFileSync(path, 'utf8'));
}
export const NODE_TYPE_ALLOWLIST = new Set([
  'npc',
  'map',
  'monster',
  'item',
  'spell',
  'quest',
  'queststep',
  'objective',
  'dungeon',
  'subarea',
  'interactive',
]);

export function nodeId(type, id) {
  if (!NODE_TYPE_ALLOWLIST.has(type)) {
    throw new Error(`node type not in allowlist: ${type}`);
  }
  return `${type}:${id}`;
}

export function parseNodeId(nodeIdStr) {
  const i = nodeIdStr.indexOf(':');
  if (i < 0) return { type: null, id: nodeIdStr };
  return { type: nodeIdStr.slice(0, i), id: nodeIdStr.slice(i + 1) };
}

let edgeSeq = 0;

export function resetEdgeSeq() {
  edgeSeq = 0;
}

export function makeEdge({
  src,
  rel,
  dst,
  source = 'BD',
  ref,
  method,
  confidence = 1.0,
  props = undefined,
  hypothesis = false,
  status = undefined,
}) {
  edgeSeq += 1;
  const edge = {
    id: `r${String(edgeSeq).padStart(4, '0')}`,
    src,
    rel,
    dst,
    layer: 'L1',
    provenance: { source, ref, method },
    confidence,
  };
  if (props && Object.keys(props).length) edge.props = props;
  if (hypothesis) edge.hypothesis = true;
  else if (confidence < 0.7) edge.hypothesis = true;
  if (status) edge.status = status;
  return edge;
}

/**
 * Placeholder roles per objective Type from quests_objectives_types template text.
 * #N -> ParametersCSV[N-1] (positional).
 */
export function objectiveTypeRoles() {
  return {
    0: { 1: 'text_id', hypothesis: true },
    1: { 1: 'npc' },
    2: { 1: 'npc', 2: 'qty', 3: 'item' },
    3: { 1: 'npc', 2: 'item', 3: 'qty' },
    4: { 1: 'map', hypothesis: true },
    5: { 1: 'subarea', hypothesis: true },
    6: { 1: 'monster', 2: 'qty' },
    7: { 1: 'monster' },
    8: { 1: 'interactive', hypothesis: true },
    9: { 1: 'npc' },
    10: { 1: 'npc', 2: 'map', hypothesis: true },
    11: { 1: 'challenge', hypothesis: true },
    12: { 1: 'npc', 2: 'monster', 3: 'qty', hypothesis: true },
    13: { 1: 'monster' },
  };
}

export const OBJECTIVE_TYPE_RELS = {
  npc: 'INVOLVES_NPC',
  monster: 'DEFEAT_MONSTER',
  map: 'DISCOVER_MAP',
  subarea: 'DISCOVER_AREA',
  item: 'REQUIRES_ITEM',
  interactive: 'USES_INTERACTIVE',
};

export function buildCatalogs(sql) {
  const { rows: npcs } = parseTableInserts(sql, 'npcs');
  const { rows: monsters } = parseTableInserts(sql, 'monsters');
  const { rows: items } = parseTableInserts(sql, 'items');
  const { rows: spells } = parseTableInserts(sql, 'spells');
  const { rows: maps } = parseTableInserts(sql, 'worlds_maps');
  const { rows: quests } = parseTableInserts(sql, 'quests');
  const { rows: steps } = parseTableInserts(sql, 'quests_steps');
  const { rows: objectives } = parseTableInserts(sql, 'quests_objectives');
  const { rows: dungeons } = parseTableInserts(sql, 'dungeons');

  return {
    npcIds: idSet(npcs, 'Id'),
    monsterIds: idSet(monsters, 'Id'),
    itemIds: idSet(items, 'Id'),
    spellIds: idSet(spells, 'Id'),
    mapIds: idSet(maps, 'Id'),
    questIds: idSet(quests, 'Id'),
    stepIds: idSet(steps, 'Id'),
    objectiveIds: idSet(objectives, 'Id'),
    dungeonIds: idSet(dungeons, 'Id'),
    rows: { npcs, monsters, items, spells, maps, quests, steps, objectives, dungeons },
  };
}

export function catalogForType(type, catalogs) {
  switch (type) {
    case 'npc': return catalogs.npcIds;
    case 'monster': return catalogs.monsterIds;
    case 'item': return catalogs.itemIds;
    case 'spell': return catalogs.spellIds;
    case 'map': return catalogs.mapIds;
    case 'quest': return catalogs.questIds;
    case 'queststep': return catalogs.stepIds;
    case 'objective': return catalogs.objectiveIds;
    case 'dungeon': return catalogs.dungeonIds;
    case 'subarea': return null;
    case 'interactive': return null;
    default: return null;
  }
}

export function edgeStatusForDst(dstType, dstId, catalogs) {
  const cat = catalogForType(dstType, catalogs);
  if (cat == null) return undefined;
  const n = Number(dstId);
  if (Number.isNaN(n)) return 'ref-only';
  return cat.has(n) ? undefined : 'ref-only';
}
