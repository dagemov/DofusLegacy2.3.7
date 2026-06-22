#!/usr/bin/env node
/** Phase A — SQL system inventory */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  parseAllTables,
  inferForeignKeys,
  countTableRows,
  inferTablePurpose,
  writeJson,
} from './_model-lib.mjs';

const sql = loadSql();
const tables = parseAllTables(sql);
const fkEdges = inferForeignKeys(tables);

const inbound = new Map();
const outbound = new Map();
for (const e of fkEdges) {
  if (!inbound.has(e.to_table)) inbound.set(e.to_table, []);
  inbound.get(e.to_table).push(e);
  if (!outbound.has(e.from_table)) outbound.set(e.from_table, []);
  outbound.get(e.from_table).push(e);
}

const inventory = tables.map((t) => {
  const rowCount = countTableRows(sql, t.name);
  const inFks = inbound.get(t.name) || [];
  const outFks = outbound.get(t.name) || [];
  const purpose = inferTablePurpose(t.name, t.columns, inFks, outFks);
  return {
    table: t.name,
    purpose: purpose.purpose,
    purpose_confidence: purpose.confidence,
    purpose_inferred: purpose.inferred,
    primary_key: t.pk,
    columns: t.columns.map((c) => c.name),
    column_count: t.columns.length,
    inferred_foreign_keys_out: outFks.map((e) => ({
      column: e.from_column,
      references: `${e.to_table}.${e.to_column}`,
      confidence: e.confidence,
      status: e.status,
      multi_valued: e.multi_valued,
    })),
    inferred_foreign_keys_in: inFks.map((e) => ({
      from: `${e.from_table}.${e.from_column}`,
      confidence: e.confidence,
      status: e.status,
    })),
    row_count: rowCount,
    usage_frequency: rowCount + inFks.length * 10,
  };
});

inventory.sort((a, b) => b.usage_frequency - a.usage_frequency);

export const result = {
  phase: 'A_SQL_INVENTORY',
  source: 'database/sunshine.sql',
  table_count: tables.length,
  declared_foreign_keys: 0,
  inferred_fk_edge_count: fkEdges.length,
  tables: inventory,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'system_inventory.json'), result);

if (process.argv[1]?.includes('sql-inventory')) {
  console.log(JSON.stringify({ table_count: result.table_count, inferred_fk_edge_count: result.inferred_fk_edge_count }, null, 2));
}
