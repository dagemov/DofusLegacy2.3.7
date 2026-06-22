import { readFileSync, writeFileSync, mkdirSync, existsSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

export const TXN_DIR = dirname(fileURLToPath(import.meta.url));
export const PROTO_DIR = join(TXN_DIR, '..');
export const SIM_DIR = join(PROTO_DIR, 'mcp-execution-sim');
export const BRIDGE_DIR = join(PROTO_DIR, 'mcp-execution-bridge');
export const BRIDGE_OUT = join(BRIDGE_DIR, 'out');
export const SYNC_DIR = join(PROTO_DIR, 'runtime-sync');
export const WC_DIR = join(PROTO_DIR, 'world-causal');
export const WR_DIR = join(PROTO_DIR, 'world-relations');

export const WORLD_TRANSACTION_STATES = [
  'PLANNED',
  'VALIDATED',
  'BLOCKED',
  'READY_TO_COMMIT',
  'COMMITTED',
  'ROLLBACK_AVAILABLE',
  'ROLLED_BACK',
  'FAILED',
];

export const COMMIT_RUN_ID = '20260622T061552Z';
export const ROLLBACK_RUN_ID = '20260622T061633Z';

export function writeJson(path, data) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

export function hashString(s) {
  return createHash('sha256').update(s).digest('hex').slice(0, 16);
}

export function createLifecycle(initialState = 'PLANNED') {
  return {
    current_state: initialState,
    history: [],
  };
}

export function transitionLifecycle(lifecycle, state, reason) {
  lifecycle.history.push({
    state,
    at: new Date().toISOString(),
    reason,
  });
  lifecycle.current_state = state;
  return lifecycle;
}

export function loadF22Artifacts() {
  const plans = JSON.parse(readFileSync(join(SIM_DIR, 'execution_plans.json'), 'utf8'));
  const blast = JSON.parse(readFileSync(join(SIM_DIR, 'blast_radius_report.json'), 'utf8'));
  return { plans, blast };
}

export function findPlanAndValidation(artifacts, intentId, targetNode) {
  const planIdx = artifacts.plans.plans.findIndex(
    (p) => p.intent_id === intentId && (!targetNode || p.target_node === targetNode),
  );
  if (planIdx < 0) return { plan: null, validation: null, planIdx: -1 };
  return {
    plan: artifacts.plans.plans[planIdx],
    validation: artifacts.blast.validations[planIdx],
    planIdx,
  };
}

export function loadF23Run(runId) {
  const runDir = join(BRIDGE_OUT, runId);
  const tracePath = join(runDir, 'execution_trace.json');
  const eventPath = join(runDir, 'runtime_change_event.json');
  const rollbackPath = join(runDir, 'rollback.sql');
  if (!existsSync(tracePath)) return null;
  const trace = JSON.parse(readFileSync(tracePath, 'utf8'));
  const event = existsSync(eventPath) ? JSON.parse(readFileSync(eventPath, 'utf8')) : null;
  return {
    run_id: runId,
    trace,
    event,
    trace_ref: tracePath,
    event_ref: existsSync(eventPath) ? eventPath : null,
    rollback_sql_ref: existsSync(rollbackPath) ? rollbackPath : null,
  };
}

export function loadF24Artifacts() {
  const consistencyPath = join(SYNC_DIR, 'graph-consistency-report.json');
  const diffPath = join(SYNC_DIR, 'world-diff-report.json');
  const lastRunPath = join(SYNC_DIR, 'runtime-sync-last-run.json');
  return {
    consistency: existsSync(consistencyPath) ? JSON.parse(readFileSync(consistencyPath, 'utf8')) : null,
    worldDiff: existsSync(diffPath) ? JSON.parse(readFileSync(diffPath, 'utf8')) : null,
    lastRun: existsSync(lastRunPath) ? JSON.parse(readFileSync(lastRunPath, 'utf8')) : null,
  };
}

export function listExecutedF23Runs() {
  if (!existsSync(BRIDGE_OUT)) return [];
  const runs = [];
  for (const name of readdirSync(BRIDGE_OUT, { withFileTypes: true })) {
    if (!name.isDirectory()) continue;
    const run = loadF23Run(name.name);
    if (run?.trace?.executed === true && run.trace.success === true) {
      runs.push(name.name);
    }
  }
  return runs.sort();
}

export function findConsistencyReportForRun(f24, runId, entity) {
  const diffEvent = f24.worldDiff?.real_events?.find((e) => e.run_id === runId && e.entity === entity);
  if (!diffEvent) return null;
  const report = f24.consistency?.reports?.find(
    (r) => r.entity === entity && r.mode === 'real',
  );
  return {
    diff: diffEvent,
    report: report || null,
  };
}

export function buildReingestProposalFromF24(f24, entity) {
  const plan = f24.consistency?.reingest_plan;
  const entityPlan = plan?.entity_plans?.find((p) => p.entity === entity);
  if (!entityPlan && entity !== 'npc:462') return null;
  const benchmark = f24.consistency?.simulated_benchmark;
  if (entity === 'npc:462' && benchmark) {
    return {
      invalidated_artifacts: (benchmark.invalidated_artifacts || []).map((a) => ({
        path: a.path,
        rerun: a.rerun,
        reason: a.reason,
      })),
      rerun_commands: (plan?.rerun_commands || []).map((r) => ({
        phase: r.phase,
        command: r.command,
        cwd: r.cwd,
      })),
      recovery_required: benchmark.predicted_recovery_phases || [],
    };
  }
  return null;
}

export function terminalStatesInclude(lifecycle, ...states) {
  const historyStates = new Set(lifecycle.history.map((h) => h.state));
  historyStates.add(lifecycle.current_state);
  return states.every((s) => historyStates.has(s));
}
