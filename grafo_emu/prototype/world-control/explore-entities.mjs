#!/usr/bin/env node
/** §1 — Entity/relationship inventory from graph + SQL table counts */
import {
  loadGraph,
  loadSql,
  parseTableColumns,
  parseTableInserts,
  relSignature,
} from './_lib.mjs';

const { nodes, edges, nodeList } = loadGraph();
const sql = loadSql();

const graphEntitiesByType = {};
for (const n of nodeList) {
  graphEntitiesByType[n.type] = (graphEntitiesByType[n.type] || 0) + 1;
}

const graphRelations = relSignature(edges);
const graphLayers = {};
for (const n of nodeList) {
  graphLayers[n.layer] = (graphLayers[n.layer] || 0) + 1;
}

const SQL_TABLES = [
  'npcs',
  'worlds_npcs',
  'npcs_items',
  'quests',
  'quests_steps',
  'quests_objectives',
  'items',
  'spells',
  'spells_levels',
];

const sqlInventory = {};
for (const table of SQL_TABLES) {
  const cols = parseTableColumns(sql, table);
  const { rows } = parseTableInserts(sql, table);
  sqlInventory[table] = {
    columns: cols.length,
    row_count: rows.length,
    key_columns: cols.slice(0, 8),
  };
}

const NOT_PRESENT_IN_GRAPH = ['Map', 'Spawn', 'Monster', 'WorldMap'];
const presentInGraph = new Set(nodeList.map((n) => n.type));
const notPresent = NOT_PRESENT_IN_GRAPH.filter((t) => !presentInGraph.has(t));

const implicitOperations = [];
if (graphRelations.SELLS) {
  implicitOperations.push({
    pattern: 'NPC→SELLS→ITEM',
    rel: 'SELLS',
    count: graphRelations.SELLS,
    implies: 'shop price mutation via npcs_items',
  });
}
if (graphRelations.HAS_STEP) {
  implicitOperations.push({
    pattern: 'QUEST→HAS_STEP',
    rel: 'HAS_STEP',
    count: graphRelations.HAS_STEP,
    implies: 'quest flow authoring',
  });
}
if (graphRelations.USES_EFFECT && graphRelations.OBSERVED_IN) {
  implicitOperations.push({
    pattern: 'SPELL→USES_EFFECT + OBSERVED_IN',
    rels: ['USES_EFFECT', 'OBSERVED_IN'],
    implies: 'runtime spell inspection',
  });
}
if (sqlInventory.worlds_npcs?.row_count > 0) {
  implicitOperations.push({
    pattern: 'SQL worlds_npcs(Npc,Map,Cell)',
    sql_only: true,
    row_count: sqlInventory.worlds_npcs.row_count,
    implies: 'NPC world spawn placement',
  });
}

export const result = {
  phase: 'EXPLORATION',
  graph: {
    node_count: nodeList.length,
    edge_count: edges.length,
    entities_by_type: graphEntitiesByType,
    relations: graphRelations,
    layers: graphLayers,
  },
  sql: {
    source: 'database/sunshine.sql',
    tables: sqlInventory,
  },
  not_present_in_graph: notPresent,
  not_present_note:
    'Map/Spawn/Monster nodes absent from prototype JSONL; worlds_npcs exists in SQL only',
  implicit_operations_observed: implicitOperations,
  cross_source_entities: {
    npc: { graph: graphEntitiesByType.Npc || 0, sql: sqlInventory.npcs?.row_count || 0 },
    quest: { graph: graphEntitiesByType.Quest || 0, sql: sqlInventory.quests?.row_count || 0 },
    item: { graph: graphEntitiesByType.Item || 0, sql: sqlInventory.items?.row_count || 0 },
    spell: { graph: graphEntitiesByType.Spell || 0, sql: sqlInventory.spells?.row_count || 0 },
  },
};

if (process.argv[1]?.includes('explore-entities')) {
  console.log(JSON.stringify(result, null, 2));
}
