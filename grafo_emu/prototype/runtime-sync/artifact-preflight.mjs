#!/usr/bin/env node
/** F24 preflight — verify F21–F23 artifacts exist with real evidence */
import { join } from 'node:path';
import { readdirSync, existsSync, readFileSync } from 'node:fs';
import {
  SYNC_DIR,
  WC_DIR,
  SIM_DIR,
  BRIDGE_DIR,
  BRIDGE_OUT,
  writeJson,
  countJsonlLines,
  artifactStat,
} from './_sync-lib.mjs';

function countExecutedRuntimeEvents() {
  if (!existsSync(BRIDGE_OUT)) return { count: 0, run_ids: [] };
  const runIds = [];
  for (const name of readdirSync(BRIDGE_OUT, { withFileTypes: true })) {
    if (!name.isDirectory()) continue;
    const tracePath = join(BRIDGE_OUT, name.name, 'execution_trace.json');
    const eventPath = join(BRIDGE_OUT, name.name, 'runtime_change_event.json');
    if (!existsSync(tracePath) || !existsSync(eventPath)) continue;
    const trace = JSON.parse(readFileSync(tracePath, 'utf8'));
    if (trace.executed === true && trace.success === true) {
      runIds.push(name.name);
    }
  }
  return { count: runIds.length, run_ids: runIds.sort() };
}

export function runPreflight() {
  const checks = [];
  const fail = (id, detail) => checks.push({ id, status: 'FAIL', detail });
  const pass = (id, detail, evidence = {}) => checks.push({ id, status: 'PASS', detail, evidence });

  const causalPath = join(WC_DIR, 'causal_graph.jsonl');
  const causalStat = artifactStat(causalPath);
  let causalLines = 0;
  if (causalStat.exists) {
    causalLines = countJsonlLines(causalPath);
    if (causalLines === 112635) {
      pass('F21_causal_graph', 'causal_graph.jsonl line count matches expected', { lines: causalLines, bytes: causalStat.bytes });
    } else {
      fail('F21_causal_graph', `expected 112635 lines, got ${causalLines}`);
    }
  } else {
    fail('F21_causal_graph', 'causal_graph.jsonl missing');
  }

  const plansPath = join(SIM_DIR, 'execution_plans.json');
  const blastPath = join(SIM_DIR, 'blast_radius_report.json');
  if (existsSync(plansPath) && existsSync(blastPath)) {
    const plans = JSON.parse(readFileSync(plansPath, 'utf8'));
    const blast = JSON.parse(readFileSync(blastPath, 'utf8'));
    if (plans.plans?.length === 8 && blast.validations?.length === 8) {
      pass('F22_plans', '8 execution plans present', { plan_count: 8 });
      pass('F22_validations', '8 blast validations present', { validation_count: 8 });
    } else {
      fail('F22_plans', `plans=${plans.plans?.length} validations=${blast.validations?.length}`);
    }
  } else {
    fail('F22_artifacts', 'execution_plans.json or blast_radius_report.json missing');
  }

  const bridgeLastRun = join(BRIDGE_DIR, 'mcp-execution-bridge-last-run.json');
  if (existsSync(bridgeLastRun)) {
    pass('F23_bridge', 'mcp-execution-bridge-last-run.json present', artifactStat(bridgeLastRun));
  } else {
    fail('F23_bridge', 'mcp-execution-bridge-last-run.json missing');
  }

  const events = countExecutedRuntimeEvents();
  if (events.count >= 1) {
    pass('F23_runtime_events', `${events.count} executed runtime_change_event(s) found`, events);
  } else {
    fail('F23_runtime_events', 'no executed runtime_change_event pairs in mcp-execution-bridge/out/');
  }

  const passed = checks.every((c) => c.status === 'PASS');
  const report = {
    phase: 'PREFLIGHT',
    timestamp: new Date().toISOString(),
    passed,
    checks,
    evidence: {
      f21_causal_edges: causalLines,
      f22_plan_count: existsSync(plansPath) ? JSON.parse(readFileSync(plansPath, 'utf8')).plans?.length : 0,
      f23_executed_events: events,
    },
  };

  writeJson(join(SYNC_DIR, 'preflight-report.json'), report);
  return report;
}

if (process.argv[1]?.includes('artifact-preflight')) {
  const report = runPreflight();
  console.log(JSON.stringify({ passed: report.passed, checks: report.checks.map((c) => ({ id: c.id, status: c.status })) }, null, 2));
  if (!report.passed) process.exit(1);
}
