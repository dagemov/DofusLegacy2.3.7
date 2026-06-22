#!/usr/bin/env node
/** §5 — TEST 1–4 validation per proposed MCP */
import {
  loadGraph,
  loadSql,
  parseTableColumns,
  parseTableInserts,
} from './_lib.mjs';
import { result as proposed } from './propose-mcps.mjs';
import { result as mined } from './mine-actions.mjs';
import { result as economy } from './infer-economy.mjs';
import { result as completeness } from './relation-completeness.mjs';
import { result as actionSim } from './action-simulate.mjs';

const { nodes, edges } = loadGraph();
const sql = loadSql();
const actionByName = new Map(mined.discovered_actions.map((a) => [a.action_name, a]));

function test1Graph(action) {
  const failures = [];
  const warnings = [];
  for (const edgeId of action.evidence_edges || []) {
    const edge = edges.find((e) => e.id === edgeId);
    if (!edge) {
      failures.push(`edge ${edgeId} not in graph`);
      continue;
    }
    if (!nodes.has(edge.src)) failures.push(`src ${edge.src} missing`);
    if (!nodes.has(edge.dst)) failures.push(`dst ${edge.dst} missing`);
  }
  if (!action.graph_evidence) {
    warnings.push(`${action.action_name}: no graph evidence (SQL-only pattern)`);
  }
  if (action.graph_evidence && !(action.evidence_edges || []).length) {
    warnings.push(`${action.action_name}: graph_evidence=true but no evidence_edges`);
  }
  return {
    test: 'TEST_1_GRAPH',
    passed: failures.length === 0,
    failures,
    warnings,
    edges_checked: (action.evidence_edges || []).length,
  };
}

function test2Sql(action) {
  const tables = action.required_sources?.sql || [];
  const failures = [];
  const warnings = [];
  const validated = [];

  for (const table of tables) {
    const cols = parseTableColumns(sql, table);
    if (!cols.length) {
      failures.push(`table ${table} not found in sunshine.sql`);
      continue;
    }
    const { rows } = parseTableInserts(sql, table);
    validated.push({ table, columns: cols.length, rows: rows.length, ok: true });
  }

  if (!action.sql_evidence && tables.length) {
    warnings.push(`${action.action_name}: SQL tables referenced but sql_evidence=false`);
  }

  return {
    test: 'TEST_2_SQL',
    passed: failures.length === 0,
    failures,
    warnings,
    tables_validated: validated,
  };
}

function test3Simulation(action) {
  if (!['write/sim', 'read/write'].includes(action.type)) {
    return {
      test: 'TEST_3_SIMULATION',
      passed: true,
      skipped: true,
      reason: 'read-only action',
    };
  }

  const simMap = {
    modify_npc_shop: ['npcs_items', 'npcs'],
    create_quest_flow: ['quests', 'quests_steps', 'quests_objectives'],
    spawn_npc_in_world: ['worlds_npcs', 'npcs'],
    adjust_quest_rewards: ['quests_steps'],
  };

  const tables = simMap[action.action_name] || action.required_sources?.sql || [];
  const schemaOk = tables.every((t) => {
    const check = actionSim.schema_checks?.find((s) => s.table === t);
    return check ? check.ok : parseTableColumns(sql, t).length > 0;
  });

  const relevantPlan = actionSim.mutation_plan?.statements?.filter((s) =>
    tables.includes(s.table),
  );

  return {
    test: 'TEST_3_SIMULATION',
    passed: schemaOk && actionSim.integrity_valid,
    dry_run: true,
    schema_ok: schemaOk,
    integrity_valid: actionSim.integrity_valid,
    mutation_tables: tables,
    sample_statements: relevantPlan?.map((s) => s.table) || [],
    failures: schemaOk ? [] : [`schema gap for ${action.action_name}`],
  };
}

function test4Consistency(action) {
  const warnings = [];
  const failures = [];

  if (action.action_name === 'audit_npc_economy' || action.action_name === 'modify_npc_shop') {
    const graphShops = edges.filter((e) => e.rel === 'SELLS').length;
    const dbShops = economy.npc_shop_in_db_count || 0;
    const ratio = graphShops / Math.max(1, dbShops);
    if (ratio < 0.001) {
      warnings.push(
        `graph SELLS=${graphShops} vs DB npcs_items=${dbShops} (coverage ${Math.round(ratio * 10000) / 10000})`,
      );
    }
  }

  if (action.action_name === 'create_quest_flow' || action.action_name === 'adjust_quest_rewards') {
    const cov = economy.graph_vs_db?.coverage_ratio ?? completeness.coverage_score;
    if (cov < 0.01) {
      warnings.push(`quest graph coverage ${cov} vs DB`);
    }
  }

  if (action.action_name === 'spawn_npc_in_world') {
    warnings.push('spawn_npc_in_world: no graph Map nodes');
    const orphans = completeness.db_orphans?.npcs_without_world_spawn;
    if (orphans > 0) {
      warnings.push(`${orphans} DB npcs without world spawn`);
    }
  }

  if (action.action_name === 'inspect_spell_runtime') {
    const logEdges = edges.filter((e) =>
      ['USES_EFFECT', 'OBSERVED_IN'].includes(e.rel),
    ).length;
    if (logEdges < 2) failures.push('insufficient LOG edges for spell runtime');
  }

  return {
    test: 'TEST_4_CONSISTENCY',
    passed: failures.length === 0,
    failures,
    warnings,
    coverage_score: completeness.coverage_score,
    economy_coverage: economy.graph_vs_db?.coverage_ratio,
  };
}

function validateMcp(mcp) {
  const actionResults = mcp.actions.map((a) => {
    const full = actionByName.get(a.action_name);
    if (!full) {
      return {
        action_name: a.action_name,
        error: 'action not found in mine-actions',
      };
    }
    const t1 = test1Graph(full);
    const t2 = test2Sql(full);
    const t3 = test3Simulation(full);
    const t4 = test4Consistency(full);
    const passed = [t1, t2, t3, t4].every((t) => t.passed || t.skipped);
    return {
      action_name: a.action_name,
      passed,
      tests: { test_1_graph: t1, test_2_sql: t2, test_3_simulation: t3, test_4_consistency: t4 },
    };
  });

  const allWarnings = actionResults.flatMap((ar) =>
    Object.values(ar.tests || {}).flatMap((t) => t.warnings || []),
  );
  const allFailures = actionResults.flatMap((ar) =>
    Object.values(ar.tests || {}).flatMap((t) => t.failures || []),
  );

  const testsRun = actionResults.reduce((s, ar) => s + Object.keys(ar.tests || {}).length, 0);
  const testsPassed = actionResults.reduce(
    (s, ar) =>
      s + Object.values(ar.tests || {}).filter((t) => t.passed || t.skipped).length,
    0,
  );

  return {
    mcp_name: mcp.mcp_name,
    system_type: mcp.system_type,
    test_results: {
      passed: allFailures.length === 0,
      failures: allFailures,
      warnings: allWarnings,
      coverage: testsRun ? Math.round((testsPassed / testsRun) * 1000) / 1000 : 0,
    },
    action_validations: actionResults,
  };
}

const validations = proposed.proposed_mcps.map(validateMcp);

const globalFailures = validations.flatMap((v) => v.test_results.failures);
const globalWarnings = validations.flatMap((v) => v.test_results.warnings);
const globalCoverage =
  validations.reduce((s, v) => s + v.test_results.coverage, 0) /
  Math.max(1, validations.length);

export const result = {
  phase: 'MCP_VALIDATION',
  mcp_validations: validations,
  test_results: {
    passed: globalFailures.length === 0,
    failures: globalFailures,
    warnings: globalWarnings,
    coverage: Math.round(globalCoverage * 1000) / 1000,
  },
  mcp_count: validations.length,
  mcps_all_passed: validations.every((v) => v.test_results.passed),
};

if (process.argv[1]?.includes('validate-mcp-proposals')) {
  console.log(JSON.stringify(result, null, 2));
}
