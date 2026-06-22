#!/usr/bin/env node
/** TEST 4 — Action Simulation (dry-run): validate mutation plan against sunshine.sql schema */
import { loadSql, parseTableColumns, parseTableInserts } from './_lib.mjs';

const sql = loadSql();

const REQUIRED = {
  npcs: ['Id', 'Name', 'EntityLook', 'DialogMessagesIdCSV', 'ActionsIdCSV'],
  worlds_npcs: ['Npc', 'Map', 'Cell', 'Direction'],
  npcs_items: ['NpcId', 'Item', 'Price'],
  quests: ['Id', 'Name'],
  quests_steps: ['Id', 'Quest', 'Name', 'ObjectiveIdsCSV'],
  quests_objectives: ['Id', 'Step', 'Type', 'ParametersCSV'],
};

function validateTable(table) {
  const cols = parseTableColumns(sql, table);
  const missing = (REQUIRED[table] || []).filter((c) => !cols.includes(c));
  return { table, columns_found: cols.length, missing_required: missing, ok: !missing.length };
}

const schemaChecks = Object.keys(REQUIRED).map(validateTable);

const { rows: items } = parseTableInserts(sql, 'items');
const itemExists = (id) => items.some((r) => Number(r.Id) === id);

const fictionalNpc = {
  Id: 99999,
  Name: 'MCP Test Vendor',
  EntityLook: '{1}',
  Gender: 0,
  HasQuest: 0,
  DialogMessagesIdCSV: '1,1',
  DialogRepliesIdCSV: '',
  ActionsIdCSV: '1',
  Token: null,
};

const fictionalSpawn = {
  Npc: 99999,
  Map: 191105026,
  Cell: 300,
  Direction: 3,
  Note: 'mcp-world-control dry-run',
};

const fictionalShop = {
  NpcId: 99999,
  Item: 12116,
  Price: 1000,
  Token: null,
  ActionId: 1,
  Note: 'mcp dry-run',
};

const fictionalQuest = {
  Id: 99999,
  Name: 'MCP Test Quest',
};

const fictionalStep = {
  Id: 999998,
  Quest: 99999,
  Name: 'Test step',
  Description: 'dry-run',
  Dialog: '',
  OptimalLevel: 1,
  ExperienceReward: 100,
  KamasReward: 500,
  ItemsRewardCSV: '12116,1',
  ObjectiveIdsCSV: '',
};

const structuralErrors = [];
if (!itemExists(12116)) structuralErrors.push('item 12116 not in DB items table');
if (schemaChecks.some((s) => !s.ok)) {
  structuralErrors.push(...schemaChecks.filter((s) => !s.ok).map((s) => `schema gap: ${s.table}`));
}

const mutationPlan = {
  order: [
    'INSERT npcs',
    'INSERT worlds_npcs',
    'INSERT npcs_items',
    'INSERT quests',
    'INSERT quests_steps',
  ],
  statements: [
    { table: 'npcs', row: fictionalNpc },
    { table: 'worlds_npcs', row: fictionalSpawn },
    { table: 'npcs_items', row: fictionalShop },
    { table: 'quests', row: fictionalQuest },
    { table: 'quests_steps', row: fictionalStep },
  ],
  rollback: 'DELETE FROM worlds_npcs WHERE Npc=99999; DELETE FROM npcs_items WHERE NpcId=99999; DELETE FROM npcs WHERE Id=99999; DELETE FROM quests_steps WHERE Quest=99999; DELETE FROM quests WHERE Id=99999;',
};

const integrityOk = structuralErrors.length === 0;

export const result = {
  test: 'TEST_4_ACTION_SIMULATION',
  dry_run: true,
  db_writes_executed: false,
  schema_checks: schemaChecks,
  mutation_plan: mutationPlan,
  structural_errors: structuralErrors,
  integrity_valid: integrityOk,
  graph_update_required: 'prototype JSONL would not reflect mutation — F1 ingestion needed',
};

if (process.argv[1]?.includes('action-simulate')) {
  console.log(JSON.stringify(result, null, 2));
}
