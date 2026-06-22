import { F23_EXECUTION_GATES, INTENT_TABLE_MAP, sqlQuote } from './_bridge-lib.mjs';

/**
 * Convert MutationPlan + field payload into ordered forward SQL + rollback SQL.
 * v1: single-table UPDATE only (items or npcs cosmetic).
 */
export function translateMutationPlan(plan, fieldsAfter, fieldsBefore = {}) {
  const intentId = plan.intent_id;
  const mapping = INTENT_TABLE_MAP[intentId];
  if (!mapping) {
    return { error: `unsupported_intent:${intentId}` };
  }

  const table = mapping.table;
  const allowedCols = mapping.columns;
  const targetId = plan.target_id;
  if (!targetId || targetId.includes('PLACEHOLDER')) {
    return { error: 'invalid_target_id' };
  }

  const entries = Object.entries(fieldsAfter || {});
  if (!entries.length) {
    return { error: 'no_field_changes' };
  }

  for (const [col] of entries) {
    if (!allowedCols.has(col)) {
      return { error: `column_not_allowed:${col}` };
    }
  }

  // F22 plans may list related tables; v1 executes primary table only.
  const f22ExtraTables = (plan.mutation_plan?.statements || [])
    .map((s) => s.table)
    .filter((t) => t !== table);

  const setClauses = entries.map(([col, val]) => `${col}=${sqlQuote(val)}`).join(', ');
  const forwardUpdate = `UPDATE ${table} SET ${setClauses} WHERE Id=${sqlQuote(Number(targetId) || targetId)};`;

  const orderedSteps = ['START TRANSACTION;', forwardUpdate, 'COMMIT;'];
  const forwardSql = `${orderedSteps.join('\n')}\n`;

  const rollbackCols = Object.keys(fieldsAfter);
  const rollbackParts = rollbackCols
    .filter((col) => fieldsBefore[col] !== undefined && fieldsBefore[col] !== null)
    .map((col) => `${col}=${sqlQuote(fieldsBefore[col])}`);

  let rollbackSql = '';
  let rollbackAvailable = false;
  if (rollbackParts.length === rollbackCols.length && rollbackParts.length > 0) {
    rollbackSql = `START TRANSACTION;\nUPDATE ${table} SET ${rollbackParts.join(', ')} WHERE Id=${sqlQuote(Number(targetId) || targetId)};\nCOMMIT;\n`;
    rollbackAvailable = true;
  } else if (rollbackParts.length > 0) {
    rollbackSql = `-- partial rollback (missing before values for: ${rollbackCols.filter((c) => fieldsBefore[c] == null).join(', ')})\n`;
  }

  return {
    patch: {
      forward_sql: forwardSql,
      rollback_sql: rollbackSql,
      affected_tables: [table],
      ordered_steps: orderedSteps,
      table,
      row_id: targetId,
      columns: rollbackCols,
      f22_extra_tables_ignored: f22ExtraTables,
    },
    rollback_available: rollbackAvailable,
  };
}

export function buildSelectSql(table, rowId, columns) {
  const cols = columns.length ? columns.join(', ') : '*';
  const id = Number(rowId);
  const whereVal = Number.isFinite(id) ? id : rowId;
  return `SELECT ${cols} FROM ${table} WHERE Id=${whereVal}`;
}

export function parseSelectRow(stdout, columns) {
  if (!stdout || !stdout.trim()) return null;
  const line = stdout.trim().split('\n')[0];
  const parts = line.split('\t');
  const row = {};
  columns.forEach((col, i) => {
    let val = parts[i];
    if (val === 'NULL') val = null;
    else if (val !== undefined && val !== '' && !Number.isNaN(Number(val)) && col !== 'Name' && col !== 'EntityLook') {
      val = Number(val);
    }
    row[col] = val ?? null;
  });
  return row;
}

export function fieldsToChanges(fieldsAfter, fieldsBefore) {
  return Object.keys(fieldsAfter).map((column) => ({
    column,
    before: fieldsBefore[column] ?? null,
    after: fieldsAfter[column],
  }));
}
