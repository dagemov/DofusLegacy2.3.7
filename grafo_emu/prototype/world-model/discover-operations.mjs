#!/usr/bin/env node
/** Phase C — Operations catalog from observable code/SQL patterns */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from './_model-lib.mjs';
import { result as systems } from './discover-systems.mjs';
import { result as codeInv } from './code-inventory.mjs';

const VERB_TO_OP = {
  get: 'read_entity',
  load: 'read_entity',
  fetch: 'read_entity',
  create: 'create_entity',
  insert: 'create_entity',
  add: 'link_entity',
  update: 'modify_entity',
  set: 'modify_entity',
  modify: 'modify_entity',
  delete: 'delete_entity',
  remove: 'delete_entity',
  link: 'link_entity',
  move: 'move_entity',
  validate: 'validate_entity',
  spawn: 'place_entity',
  place: 'place_entity',
  save: 'modify_entity',
  other: 'coordinate_flow',
};

const operations = [];
let opId = 0;

for (const sys of systems.systems) {
  const sysClasses = codeInv.classes.filter((c) =>
    sys.classes_involved.includes(c.class_name) ||
    (c.tables_manipulated || []).some((t) => sys.tables_involved.includes(t)),
  );

  for (const cls of sysClasses) {
    if (!cls || typeof cls !== 'object') continue;
    const methods = Array.isArray(cls.methods) ? cls.methods : [];
    const tablesManip = Array.isArray(cls.tables_manipulated) ? cls.tables_manipulated : [];
    const seenVerbs = new Set();
    for (const m of methods) {
      const opType = VERB_TO_OP[m.verb] || 'coordinate_flow';

      const tables = tablesManip.filter((t) =>
        sys.tables_involved.includes(t) || tablesManip.includes(t),
      );

      const risks = [];
      if (tables.some((t) => t.startsWith('characters_'))) risks.push('mutates player runtime state');
      if (tables.includes('worlds_npcs') || tables.includes('worlds_monsters')) risks.push('affects live world spawns');
      if (cls.tables_manipulated.length && !tables.length) risks.push('class references tables outside system boundary');
      if (m.verb === 'delete') risks.push('irreversible without backup');
      if (tables.some((t) => sqlHasCsvColumns(t))) risks.push('CSV column round-trip required');

      operations.push({
        operation_id: `op_${++opId}`,
        system_id: sys.system_id,
        system_label: sys.label,
        operation_type: opType,
        operation_name: `${opType} via ${cls.class_name}.${m.name}`,
        source: 'csharp_method',
        evidence: { class: cls.class_name, method: m.name, verb: m.verb, file: cls.file },
        tables_affected: tables.length ? tables : tablesManip,
        classes_affected: [cls.class_name],
        execution_order: inferOrder(opType, tables),
        dependencies: cls.dependencies,
        risks,
        confidence: tables.length ? 0.85 : 0.6,
      });
    }
  }

  for (const tbl of sys.tables_involved) {
    if (sys.tables_involved.length >= 2) {
      operations.push({
        operation_id: `op_${++opId}`,
        system_id: sys.system_id,
        system_label: sys.label,
        operation_type: 'link_entity',
        operation_name: `link_tables_in_${sys.system_id}`,
        source: 'sql_fk_inference',
        evidence: { tables: sys.tables_involved.slice(0, 5) },
        tables_affected: sys.tables_involved,
        classes_affected: sys.classes_involved.slice(0, 5),
        execution_order: sys.tables_involved,
        dependencies: [],
        risks: ['no declared FK — integrity enforced by application only'],
        confidence: 0.7,
      });
    }
  }
}

function sqlHasCsvColumns(tableName) {
  const csvTables = ['quests', 'quests_steps', 'npcs', 'spells', 'monsters_spells', 'breeds'];
  return csvTables.some((t) => tableName.includes(t));
}

function inferOrder(opType, tables) {
  if (opType === 'create_entity') return ['parent tables first', ...tables];
  if (opType === 'delete_entity') return [...tables].reverse();
  if (opType === 'place_entity') return tables.filter((t) => t.includes('worlds_')).concat(tables.filter((t) => !t.includes('worlds_')));
  return tables;
}

const deduped = [];
const seen = new Set();
for (const op of operations) {
  const k = `${op.system_id}:${op.operation_type}:${op.operation_name}`;
  if (seen.has(k)) continue;
  seen.add(k);
  deduped.push(op);
}

export const result = {
  phase: 'C_OPERATIONS',
  operation_count: deduped.length,
  by_type: deduped.reduce((acc, o) => {
    acc[o.operation_type] = (acc[o.operation_type] || 0) + 1;
    return acc;
  }, {}),
  operations: deduped,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'operations_catalog.json'), result);

if (process.argv[1]?.includes('discover-operations')) {
  console.log(JSON.stringify({ operation_count: result.operation_count, by_type: result.by_type }, null, 2));
}
