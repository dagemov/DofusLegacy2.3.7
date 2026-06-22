#!/usr/bin/env node
/** Phase D — Workflow reconstruction per operation */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from './_model-lib.mjs';
import { result as operations } from './discover-operations.mjs';
import { result as codeInv } from './code-inventory.mjs';

const INVERSE = {
  create_entity: 'delete_entity',
  modify_entity: 'restore_previous_values',
  delete_entity: 'recreate_from_backup',
  link_entity: 'unlink_entity',
  place_entity: 'remove_spawn',
  read_entity: null,
  validate_entity: null,
  coordinate_flow: null,
};

const workflows = operations.operations.map((op) => {
  const cls = codeInv.classes.find((c) => c.class_name === op.classes_affected[0]);
  const validations = (cls?.methods || [])
    .filter((m) => m.verb === 'validate' || /Can|IsValid|Check/i.test(m.name))
    .map((m) => m.name);

  const sideEffects = [];
  if (op.tables_affected.some((t) => t.startsWith('worlds_'))) {
    sideEffects.push('visible change on map for connected clients');
  }
  if (op.tables_affected.includes('npcs_items')) {
    sideEffects.push('shop UI refresh for affected NPC');
  }
  if (op.tables_affected.some((t) => t.startsWith('characters_'))) {
    sideEffects.push('player state persistence on next save');
  }
  if (op.operation_type === 'link_entity' && op.tables_affected.includes('quests_steps')) {
    sideEffects.push('quest progression chain may break if step order invalid');
  }

  const gaps = [];
  if (!validations.length) gaps.push('no explicit Validate/Can* method detected in scanned class');
  if (op.risks.includes('CSV column round-trip required')) {
    gaps.push('CSV serialization logic not extracted — workflow incomplete');
  }

  const adminSteps = [
    { step: 1, action: 'identify_target_entities', data: op.tables_affected },
    { step: 2, action: 'load_current_state', tables: op.tables_affected, classes: op.classes_affected },
  ];
  if (validations.length) {
    adminSteps.push({ step: 3, action: 'run_validations', methods: validations });
  }
  adminSteps.push({
    step: validations.length ? 4 : 3,
    action: op.operation_type,
    execution_order: op.execution_order,
  });
  adminSteps.push({
    step: adminSteps.length + 1,
    action: 'verify_downstream_refs',
    tables: op.tables_affected,
  });

  return {
    workflow_id: `wf_${op.operation_id}`,
    operation_id: op.operation_id,
    system_id: op.system_id,
    operation_type: op.operation_type,
    operation_name: op.operation_name,
    if_admin_performs: {
      data_involved: op.tables_affected,
      tables_participating: op.tables_affected,
      classes_participating: op.classes_affected,
      validations_detected: validations,
      validations_gaps: gaps,
      side_effects: sideEffects,
      revert_strategy: INVERSE[op.operation_type]
        ? { inverse_operation: INVERSE[op.operation_type], note: 'manual rollback or SQL DELETE/UPDATE — no transaction layer in MCP' }
        : { inverse_operation: null, note: 'read-only — no revert needed' },
    },
    steps: adminSteps,
    confidence: gaps.length ? 0.65 : 0.8,
    hypothesis: gaps.length > 1,
  };
});

export const result = {
  phase: 'D_WORKFLOWS',
  workflow_count: workflows.length,
  workflows,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'workflow_catalog.json'), result);

if (process.argv[1]?.includes('discover-workflows')) {
  console.log(JSON.stringify({ workflow_count: result.workflow_count }, null, 2));
}
