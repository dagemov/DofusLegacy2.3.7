#!/usr/bin/env node
/** F26 preflight — verify F21-F25 artifacts before contract validation */
import { join } from 'node:path';
import { existsSync, readFileSync } from 'node:fs';
import {
  CONTRACT_DIR,
  WC_DIR,
  SIM_DIR,
  BRIDGE_DIR,
  SYNC_DIR,
  TXN_DIR,
  writeJson,
  countJsonlLines,
  listExecutedF23Runs,
  loadWorldTransactions,
} from './_contract-lib.mjs';

const COMMIT_RUN = '20260622T061552Z';
const ROLLBACK_RUN = '20260622T061633Z';
const REQUIRED_CASES = ['CASE_A', 'CASE_B', 'CASE_C', 'CASE_D'];

export function runPreflight() {
  const checks = [];
  const pass = (id, detail, evidence = {}) => checks.push({ id, status: 'PASS', detail, evidence });
  const fail = (id, detail) => checks.push({ id, status: 'FAIL', detail });

  const causalPath = join(WC_DIR, 'causal_graph.jsonl');
  if (existsSync(causalPath)) {
    const lines = countJsonlLines(causalPath);
    if (lines === 112635) pass('F21', 'causal_graph.jsonl 112635 edges', { lines });
    else fail('F21', `expected 112635 edges, got ${lines}`);
  } else {
    fail('F21', 'causal_graph.jsonl missing');
  }

  const plansPath = join(SIM_DIR, 'execution_plans.json');
  const blastPath = join(SIM_DIR, 'blast_radius_report.json');
  if (existsSync(plansPath) && existsSync(blastPath)) {
    const plans = JSON.parse(readFileSync(plansPath, 'utf8'));
    const blast = JSON.parse(readFileSync(blastPath, 'utf8'));
    if (plans.plans?.length === 8 && blast.validations?.length === 8) {
      pass('F22', '8 plans and 8 validations');
    } else {
      fail('F22', `plans=${plans.plans?.length} validations=${blast.validations?.length}`);
    }
  } else {
    fail('F22', 'F22 artifacts missing');
  }

  const runs = listExecutedF23Runs();
  if (runs.includes(COMMIT_RUN) && runs.includes(ROLLBACK_RUN)) {
    pass('F23', `${runs.length} executed runs`, { run_ids: runs });
  } else {
    fail('F23', `missing commit/rollback runs: ${runs.join(',')}`);
  }

  if (existsSync(join(BRIDGE_DIR, 'mcp-execution-bridge-last-run.json'))) {
    pass('F23_bridge', 'bridge last-run present');
  } else {
    fail('F23_bridge', 'bridge last-run missing');
  }

  const f24Last = join(SYNC_DIR, 'runtime-sync-last-run.json');
  const f24Diff = join(SYNC_DIR, 'world-diff-report.json');
  const f24Cons = join(SYNC_DIR, 'graph-consistency-report.json');
  if (existsSync(f24Last) && existsSync(f24Diff) && existsSync(f24Cons)) {
    const lr = JSON.parse(readFileSync(f24Last, 'utf8'));
    if (lr.all_tests_passed === true) pass('F24', 'all_tests_passed true');
    else fail('F24', 'runtime-sync tests not passed');
  } else {
    fail('F24', 'F24 artifacts missing');
  }

  const bundle = loadWorldTransactions();
  const f25Last = join(TXN_DIR, 'world-transaction-last-run.json');
  if (bundle?.transactions?.length === 4 && existsSync(f25Last)) {
    const cases = bundle.transactions.map((t) => t.case_id);
    const missing = REQUIRED_CASES.filter((c) => !cases.includes(c));
    const lr = JSON.parse(readFileSync(f25Last, 'utf8'));
    if (!missing.length && lr.all_tests_passed === true) {
      pass('F25', '4 cases A-D, all_tests_passed true', { cases });
    } else {
      fail('F25', `missing cases ${missing.join(',')} or tests failed`);
    }
  } else {
    fail('F25', 'world-transactions.json or last-run missing');
  }

  const passed = checks.every((c) => c.status === 'PASS');
  const report = {
    phase: 'PREFLIGHT_F26',
    timestamp: new Date().toISOString(),
    passed,
    checks,
    evidence: {
      f21_edges: existsSync(causalPath) ? countJsonlLines(causalPath) : 0,
      f22_plans: existsSync(plansPath) ? JSON.parse(readFileSync(plansPath, 'utf8')).plans?.length : 0,
      f23_runs: runs,
      f25_cases: bundle?.transactions?.map((t) => t.case_id) || [],
    },
  };
  writeJson(join(CONTRACT_DIR, 'preflight-report.json'), report);
  return report;
}

if (process.argv[1]?.includes('artifact-preflight')) {
  const r = runPreflight();
  console.log(JSON.stringify({ passed: r.passed }, null, 2));
  if (!r.passed) process.exit(1);
}
