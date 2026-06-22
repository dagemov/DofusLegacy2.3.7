#!/usr/bin/env node
/** F25 preflight — verify F22, F23, F24 artifacts before transaction assembly */
import { join } from 'node:path';
import { existsSync, readFileSync } from 'node:fs';
import {
  TXN_DIR,
  SIM_DIR,
  SYNC_DIR,
  BRIDGE_DIR,
  writeJson,
  listExecutedF23Runs,
  COMMIT_RUN_ID,
  ROLLBACK_RUN_ID,
} from './_txn-lib.mjs';

export function runPreflight() {
  const checks = [];
  const pass = (id, detail, evidence = {}) => checks.push({ id, status: 'PASS', detail, evidence });
  const fail = (id, detail) => checks.push({ id, status: 'FAIL', detail });

  const plansPath = join(SIM_DIR, 'execution_plans.json');
  const blastPath = join(SIM_DIR, 'blast_radius_report.json');
  if (existsSync(plansPath) && existsSync(blastPath)) {
    const plans = JSON.parse(readFileSync(plansPath, 'utf8'));
    const blast = JSON.parse(readFileSync(blastPath, 'utf8'));
    if (plans.plans?.length === 8 && blast.validations?.length === 8) {
      pass('F22', '8 plans and 8 validations', { plan_count: 8 });
    } else {
      fail('F22', `plans=${plans.plans?.length} validations=${blast.validations?.length}`);
    }
  } else {
    fail('F22', 'execution_plans or blast_radius_report missing');
  }

  const executedRuns = listExecutedF23Runs();
  if (executedRuns.length >= 2 && executedRuns.includes(COMMIT_RUN_ID) && executedRuns.includes(ROLLBACK_RUN_ID)) {
    pass('F23', `${executedRuns.length} executed runs including commit and rollback`, { run_ids: executedRuns });
  } else {
    fail('F23', `expected commit ${COMMIT_RUN_ID} and rollback ${ROLLBACK_RUN_ID}, got ${executedRuns.join(',')}`);
  }

  if (existsSync(join(BRIDGE_DIR, 'mcp-execution-bridge-last-run.json'))) {
    pass('F23_bridge', 'mcp-execution-bridge-last-run.json present');
  } else {
    fail('F23_bridge', 'bridge last-run missing');
  }

  const f24LastRun = join(SYNC_DIR, 'runtime-sync-last-run.json');
  const f24Consistency = join(SYNC_DIR, 'graph-consistency-report.json');
  if (existsSync(f24LastRun) && existsSync(f24Consistency)) {
    const lastRun = JSON.parse(readFileSync(f24LastRun, 'utf8'));
    if (lastRun.all_tests_passed === true) {
      pass('F24', 'runtime-sync all_tests_passed true', { f23_events: lastRun.preflight?.f23_executed_events?.count });
    } else {
      fail('F24', 'runtime-sync all_tests_passed not true');
    }
  } else {
    fail('F24', 'runtime-sync artifacts missing');
  }

  const passed = checks.every((c) => c.status === 'PASS');
  const report = {
    phase: 'PREFLIGHT_F25',
    timestamp: new Date().toISOString(),
    passed,
    checks,
    evidence: {
      f22_plan_count: existsSync(plansPath) ? JSON.parse(readFileSync(plansPath, 'utf8')).plans?.length : 0,
      f23_executed_runs: executedRuns,
      f24_all_tests_passed: existsSync(f24LastRun) ? JSON.parse(readFileSync(f24LastRun, 'utf8')).all_tests_passed : false,
    },
  };
  writeJson(join(TXN_DIR, 'preflight-report.json'), report);
  return report;
}

if (process.argv[1]?.includes('artifact-preflight')) {
  const r = runPreflight();
  console.log(JSON.stringify({ passed: r.passed }, null, 2));
  if (!r.passed) process.exit(1);
}
