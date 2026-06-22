#!/usr/bin/env node
/** F28 MCP World Agent — simulation harness + TEST 1-8 */
import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import {
  AGENT_DIR,
  AGENT_PHASE,
  AGENT_VERSION,
  RESTRICTION_FLAGS,
  writeJson,
  hashString,
  assertNoForbiddenExposure,
  TOOL_SURFACE,
  gatewayLastRunPath,
  createDefaultContext,
} from './_agent-core.mjs';
import { parseSimulationIntent, SIMULATION_NL } from './intent-parser.mjs';
import { AGENT_DECISION_TOOLS } from './tool-policy.mjs';
import { runAgentLoop, assertOnlyDecisionTools } from './execution-loop.mjs';

const CASE_IDS = ['CASE_A', 'CASE_B', 'CASE_C', 'CASE_D'];

const FORBIDDEN_IMPORT_PATTERNS = [
  'mcp-execution-sim',
  'mcp-execution-bridge',
  'runtime-sync',
  'world-causal',
  'mcp-tool-contract/_contract-lib',
];

function runPreflight() {
  const checks = [];
  const pass = (id, detail, evidence = {}) => checks.push({ id, status: 'PASS', detail, evidence });
  const fail = (id, detail) => checks.push({ id, status: 'FAIL', detail });

  const f27Last = gatewayLastRunPath();
  if (existsSync(f27Last)) {
    const lr = JSON.parse(readFileSync(f27Last, 'utf8'));
    if (lr.all_tests_passed === true) pass('F27_gateway', 'F27 all_tests_passed true');
    else fail('F27_gateway', 'F27 tests not passed');
  } else {
    fail('F27_gateway', 'mcp-runtime-gateway-last-run.json missing');
  }

  const passed = checks.every((c) => c.status === 'PASS');
  const report = {
    phase: 'PREFLIGHT_F28',
    timestamp: new Date().toISOString(),
    passed,
    checks,
  };
  writeJson(join(AGENT_DIR, 'agent-preflight-report.json'), report);
  return report;
}

function simulateCase(caseId) {
  const { natural_language, parsed } = parseSimulationIntent(caseId);
  const ctx = createDefaultContext(caseId.replace('CASE_', '').toLowerCase());
  const run = runAgentLoop(ctx, parsed);
  return {
    case_id: caseId,
    natural_language,
    parsed_intent: parsed,
    terminal_decision: run.terminal_decision,
    tools_called: run.tools_called,
    steps: run.steps,
    call_log: ctx.callLog,
  };
}

function hashSimulations(cases) {
  const payload = Object.fromEntries(
    Object.entries(cases).map(([k, v]) => [k, v.steps.map((s) => ({
      tool: s.tool,
      arguments: s.arguments,
      response: s.response,
      decision: s.decision,
    }))]),
  );
  return hashString(JSON.stringify(payload));
}

function scanAgentImports() {
  const files = readdirSync(AGENT_DIR).filter((f) =>
    f.endsWith('.mjs') && f !== 'run-agent-simulation.mjs',
  );
  const violations = [];
  for (const file of files) {
    const content = readFileSync(join(AGENT_DIR, file), 'utf8');
    for (const pattern of FORBIDDEN_IMPORT_PATTERNS) {
      if (content.includes(pattern)) violations.push({ file, pattern });
    }
  }
  return violations;
}

function runTests(preflight, cases) {
  const results = [];
  const pass = (id, detail) => results.push({ id, status: 'PASS', detail });
  const fail = (id, detail) => results.push({ id, status: 'FAIL', detail });

  if (preflight.passed) pass('T1_preflight', 'F27 gateway present');
  else fail('T1_preflight', 'Preflight failed');

  const allViaGateway = Object.values(cases).every((c) =>
    c.call_log.every((entry) => entry.via === 'invokeGateway'),
  );
  if (allViaGateway) pass('T1', 'Agent only uses F27 invokeGateway');
  else fail('T1', 'Call log contains non-gateway path');

  const allToolsValid = Object.values(cases).every((c) =>
    c.tools_called.every((t) => TOOL_SURFACE.includes(t))
    && assertOnlyDecisionTools(c.tools_called),
  );
  if (allToolsValid) {
    pass('T2', `All tool calls valid F26 tools; decision tools subset of ${AGENT_DECISION_TOOLS.length}`);
  } else {
    fail('T2', 'Invalid tool in agent trace');
  }

  const caseA = cases.CASE_A;
  const commitStep = caseA.steps.find((s) => s.tool === 'commitTransaction');
  if (
    commitStep?.response.success === true
    && caseA.terminal_decision.action === 'DONE'
    && (caseA.terminal_decision.state === 'ROLLBACK_AVAILABLE' || commitStep.response.result?.rollback_available)
  ) {
    pass('T3', 'CASE A commit success');
  } else {
    fail('T3', `CASE A mismatch ${JSON.stringify(caseA.terminal_decision)}`);
  }

  const caseB = cases.CASE_B;
  const bCommit = caseB.steps.find((s) => s.tool === 'commitTransaction');
  if (
    caseB.terminal_decision.reason === 'BLOCKED_BY_BLAST_RADIUS'
    || bCommit?.response.error?.error_code === 'BLOCKED_BY_BLAST_RADIUS'
  ) {
    pass('T4', 'CASE B stops at BLOCKED_BY_BLAST_RADIUS');
  } else {
    fail('T4', `CASE B mismatch ${JSON.stringify(caseB.terminal_decision)}`);
  }

  const caseC = cases.CASE_C;
  if (
    caseC.terminal_decision.action === 'DONE'
    && caseC.terminal_decision.state === 'ROLLED_BACK'
    && caseC.terminal_decision.parent_transaction_id === 'txn-item519-commit'
  ) {
    pass('T5', 'CASE C rollback correct');
  } else {
    fail('T5', `CASE C mismatch ${JSON.stringify(caseC.terminal_decision)}`);
  }

  const caseD = cases.CASE_D;
  const dCommit = caseD.tools_called.includes('commitTransaction');
  const traceJson = JSON.stringify(caseD.steps);
  const noF23F24 = !traceJson.includes('execution-bridge') && !traceJson.includes('runtime-sync');
  if (
    !dCommit
    && caseD.terminal_decision.reason === 'explain_only'
    && caseD.terminal_decision.consistency_verdict === 'TOPOLOGY_STALE'
    && caseD.terminal_decision.recovery_required?.includes('Phase20')
    && caseD.terminal_decision.recovery_required?.includes('Phase21')
    && noF23F24
  ) {
    pass('T6', 'CASE D explain only no F23/F24 direct');
  } else {
    fail('T6', `CASE D mismatch commit=${dCommit} terminal=${JSON.stringify(caseD.terminal_decision)}`);
  }

  const importViolations = scanAgentImports();
  const exposureOk = Object.values(cases).every((c) =>
    c.steps.every((s) => assertNoForbiddenExposure(s.response)),
  );
  if (importViolations.length === 0 && exposureOk) {
    pass('T7', 'No forbidden imports or exposure in agent trace');
  } else {
    fail('T7', `violations=${JSON.stringify(importViolations)} exposureOk=${exposureOk}`);
  }

  const hash1 = hashSimulations(cases);
  const cases2 = Object.fromEntries(CASE_IDS.map((id) => [id, simulateCase(id)]));
  const hash2 = hashSimulations(cases2);

  const corePassed = results.filter((r) => r.id !== 'T1_preflight').every((r) => r.status === 'PASS');
  if (hash1 === hash2 && corePassed) {
    pass('T8', `all_tests_passed=true determinism_hash=${hash1}`);
  } else if (hash1 !== hash2) {
    fail('T8', `hash mismatch ${hash1} vs ${hash2}`);
  } else {
    fail('T8', 'prior tests failed');
  }

  return {
    results,
    all_tests_passed: results.every((r) => r.status === 'PASS'),
    determinism_hash: hash1,
  };
}

export function runAgentSimulation() {
  const preflight = runPreflight();
  if (!preflight.passed) {
    const abort = {
      phase: AGENT_PHASE,
      version: AGENT_VERSION,
      aborted: true,
      reason: 'preflight_failed',
      preflight,
      all_tests_passed: false,
      ...RESTRICTION_FLAGS,
    };
    writeJson(join(AGENT_DIR, 'mcp-world-agent-last-run.json'), abort);
    return abort;
  }

  const cases = Object.fromEntries(CASE_IDS.map((id) => [id, simulateCase(id)]));

  const simulationReport = {
    phase: 'AGENT_SIMULATION',
    timestamp: new Date().toISOString(),
    simulation_nl: SIMULATION_NL,
    cases,
    decision_tools: AGENT_DECISION_TOOLS,
  };
  writeJson(join(AGENT_DIR, 'agent-simulation-report.json'), simulationReport);

  const { results, all_tests_passed, determinism_hash } = runTests(preflight, cases);

  const lastRun = {
    phase: AGENT_PHASE,
    version: AGENT_VERSION,
    timestamp: new Date().toISOString(),
    ...RESTRICTION_FLAGS,
    preflight: { passed: preflight.passed },
    simulation_nl: SIMULATION_NL,
    case_summary: Object.fromEntries(
      Object.entries(cases).map(([k, v]) => [k, {
        natural_language: v.natural_language,
        tools_called: v.tools_called,
        terminal: v.terminal_decision,
      }]),
    ),
    outputs: {
      agent_preflight: join(AGENT_DIR, 'agent-preflight-report.json'),
      agent_simulation: join(AGENT_DIR, 'agent-simulation-report.json'),
    },
    tests: results,
    all_tests_passed,
    determinism_hash,
    public_surface: 'User Intent -> Agent -> Gateway -> Tool Contract -> World Transaction',
  };

  writeJson(join(AGENT_DIR, 'mcp-world-agent-last-run.json'), lastRun);
  return lastRun;
}

if (process.argv[1]?.includes('run-agent-simulation')) {
  const result = runAgentSimulation();
  console.log(JSON.stringify({
    all_tests_passed: result.all_tests_passed,
    aborted: result.aborted || false,
    cases: result.case_summary ? Object.keys(result.case_summary) : [],
    tests_failed: (result.tests || []).filter((t) => t.status === 'FAIL'),
  }, null, 2));
  if (!result.all_tests_passed) process.exit(1);
}
