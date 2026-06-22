#!/usr/bin/env node
/** Phase F — Automation classification per workflow */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from './_model-lib.mjs';
import { result as workflows } from './discover-workflows.mjs';
import { result as graphInv } from './graph-inventory.mjs';

const REAL_BLOCKERS = [
  'no MCP write path to MariaDB',
  'no transaction/rollback layer exposed to admin tools',
  'MyISAM schema — 0 declared FKs; integrity app-enforced only',
  'CSV columns (StepIdsCSV, ItemsRewardCSV) require parse+reserialize round-trip',
  `graph world coverage severely low (${graphInv.node_count} nodes vs thousands of DB entities)`,
  'mutations do not propagate to prototype JSONL graph automatically',
];

function classifyWorkflow(wf) {
  const opType = wf.operation_type;
  const tables = wf.if_admin_performs.tables_participating || [];
  const hasPlayerState = tables.some((t) => t.startsWith('characters_'));
  const hasWorldSpawn = tables.some((t) => t.startsWith('worlds_'));
  const hasCsvRisk = wf.if_admin_performs.validations_gaps?.some((g) => g.includes('CSV'));
  const isRead = opType === 'read_entity' || opType === 'validate_entity';

  if (isRead) {
    return {
      level: 'READ_ONLY',
      reason: 'Operation only reads state via Get/Load methods or SQL SELECT',
      blockers: [],
    };
  }

  if (opType === 'coordinate_flow') {
    return {
      level: 'READ_ONLY',
      reason: 'Coordination flow without direct mutation detected',
      blockers: ['full flow semantics not extracted from method bodies'],
    };
  }

  if (hasPlayerState) {
    return {
      level: 'PARTIALLY_AUTOMATABLE',
      reason: 'Touches live player runtime tables — dry-run possible, live write risky',
      blockers: [...REAL_BLOCKERS, 'live player state mutation'],
    };
  }

  if (hasWorldSpawn || tables.includes('npcs_items') || tables.includes('quests_steps')) {
    const level = hasCsvRisk ? 'PARTIALLY_AUTOMATABLE' : 'SIMULATABLE';
    return {
      level,
      reason: level === 'SIMULATABLE'
        ? 'Schema inferable; dry-run mutation plan validatable (see doc 16 action-simulate)'
        : 'Schema inferable but CSV round-trip and spawn side-effects block full automation',
      blockers: hasCsvRisk
        ? REAL_BLOCKERS
        : REAL_BLOCKERS.filter((b) => !b.includes('CSV')),
    };
  }

  if (opType === 'delete_entity') {
    return {
      level: 'PARTIALLY_AUTOMATABLE',
      reason: 'Delete operations need orphan checks and cascade analysis',
      blockers: [...REAL_BLOCKERS, 'no cascade delete semantics in DB'],
    };
  }

  return {
    level: 'SIMULATABLE',
    reason: 'Static catalog mutation — structure known, write path absent',
    blockers: REAL_BLOCKERS,
  };
}

const evaluations = workflows.workflows.map((wf) => {
  const classification = classifyWorkflow(wf);
  return {
    workflow_id: wf.workflow_id,
    operation_id: wf.operation_id,
    system_id: wf.system_id,
    operation_type: wf.operation_type,
    automation_level: classification.level,
    reason: classification.reason,
    blockers: classification.blockers,
    fully_automatable: classification.level === 'FULLY_AUTOMATABLE',
  };
});

const distribution = evaluations.reduce((acc, e) => {
  acc[e.automation_level] = (acc[e.automation_level] || 0) + 1;
  return acc;
}, {});

const achievableLevel =
  distribution.FULLY_AUTOMATABLE > 0 ? 'PARTIAL' :
  distribution.SIMULATABLE > 0 || distribution.PARTIALLY_AUTOMATABLE > 0 ? 'SIMULATE_ONLY' :
  'READ_ONLY';

export const result = {
  phase: 'F_AUTOMATION_EVAL',
  workflow_count: evaluations.length,
  automation_distribution: distribution,
  achievable_automation_level: achievableLevel,
  real_blockers_global: REAL_BLOCKERS,
  evaluations,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'automation_eval.json'), result);

if (process.argv[1]?.includes('automation-eval')) {
  console.log(JSON.stringify({ distribution, achievableLevel }, null, 2));
}
