#!/usr/bin/env node
/** F27 MCP Runtime Gateway — read-only validation harness */
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import {
  GATEWAY_DIR,
  GATEWAY_PHASE,
  GATEWAY_VERSION,
  RESTRICTION_FLAGS,
  FIXED_REPLAY_TIMESTAMP,
  TOOL_SURFACE,
  ERROR_CODES,
  CONTRACT_DIR,
  TXN_DIR,
  writeJson,
  hashString,
  loadWorldTransactions,
  buildRequest,
  makeRequestId,
  gatewayContractPath,
  isValidErrorCode,
} from './_gateway-lib.mjs';
import { buildToolRegistry, assertExactlyEight } from './tool-registry.mjs';
import { invokeGateway } from './tool-dispatcher.mjs';

function runPreflight() {
  const checks = [];
  const pass = (id, detail, evidence = {}) => checks.push({ id, status: 'PASS', detail, evidence });
  const fail = (id, detail) => checks.push({ id, status: 'FAIL', detail });

  const contractLib = join(CONTRACT_DIR, '_contract-lib.mjs');
  const toolContract = gatewayContractPath();
  const worldTxns = join(TXN_DIR, 'world-transactions.json');
  const f26LastRun = join(CONTRACT_DIR, 'mcp-tool-contract-last-run.json');

  if (existsSync(contractLib) && existsSync(toolContract)) {
    pass('F26_lib', 'F26 contract lib and tool-contract.json present');
  } else {
    fail('F26_lib', 'F26 artifacts missing');
  }

  if (existsSync(f26LastRun)) {
    const lr = JSON.parse(readFileSync(f26LastRun, 'utf8'));
    if (lr.all_tests_passed === true) pass('F26_tests', 'F26 all_tests_passed true');
    else fail('F26_tests', 'F26 tests not passed');
  } else {
    fail('F26_tests', 'F26 last-run missing');
  }

  const bundle = loadWorldTransactions();
  if (bundle?.transactions?.length === 4) {
    const cases = bundle.transactions.map((t) => t.case_id);
    pass('F25_bundle', 'world-transactions.json has 4 cases', { cases });
  } else {
    fail('F25_bundle', 'world-transactions.json missing or incomplete');
  }

  const passed = checks.every((c) => c.status === 'PASS');
  const report = {
    phase: 'PREFLIGHT_F27',
    timestamp: new Date().toISOString(),
    passed,
    checks,
  };
  writeJson(join(GATEWAY_DIR, 'gateway-preflight-report.json'), report);
  return { report, bundle };
}

function invoke(bundle, registry, prefix, index, tool_name, args, caller_role) {
  const request = buildRequest({
    request_id: makeRequestId(prefix, index),
    tool_name,
    arguments: args,
    caller_role,
    timestamp: FIXED_REPLAY_TIMESTAMP,
  });
  const response = invokeGateway(request, bundle, registry);
  return { request, response };
}

function replayCaseA(bundle, registry) {
  const steps = [];
  steps.push(invoke(bundle, registry, 'A', 1, 'beginTransaction', {
    intent_id: 'modify_item',
    target_node: 'item:519',
    fields: { Name: 'MEK-F23-bridge-test' },
  }, 'planner'));
  steps.push(invoke(bundle, registry, 'A', 2, 'explainImpact', {
    transaction_id: 'txn-item519-commit',
  }, 'reader'));
  steps.push(invoke(bundle, registry, 'A', 3, 'commitTransaction', {
    transaction_id: 'txn-item519-commit',
    confirm: true,
  }, 'operator'));
  steps.push(invoke(bundle, registry, 'A', 4, 'getTransactionConsistency', {
    transaction_id: 'txn-item519-commit',
  }, 'reader'));
  steps.push(invoke(bundle, registry, 'A', 5, 'getTransaction', {
    transaction_id: 'txn-item519-commit',
  }, 'reader'));

  const last = steps[steps.length - 1].response;
  const consistency = steps[3].response;
  return {
    case_id: 'CASE_A',
    transaction_id: 'txn-item519-commit',
    steps,
    actual: {
      state: last.result?.state,
      states_reached: last.result?.states_reached,
      rollback_available: last.result?.rollback_available,
      consistency_verdict: consistency.result?.verdict,
    },
    expected: {
      states_include: ['COMMITTED', 'ROLLBACK_AVAILABLE'],
      rollback_available: true,
      consistency_verdict: 'CONSISTENT_TOPOLOGY',
    },
  };
}

function replayCaseB(bundle, registry) {
  const steps = [];
  steps.push(invoke(bundle, registry, 'B', 1, 'beginTransaction', {
    intent_id: 'modify_npc',
    target_node: 'npc:462',
  }, 'planner'));
  steps.push(invoke(bundle, registry, 'B', 2, 'commitTransaction', {
    transaction_id: 'txn-npc462-blocked',
    confirm: true,
  }, 'operator'));

  const begin = steps[0].response;
  const commit = steps[1].response;
  return {
    case_id: 'CASE_B',
    transaction_id: 'txn-npc462-blocked',
    steps,
    actual: {
      begin_state: begin.result?.state,
      blast_radius: begin.result?.validation?.blast_radius,
      commit_success: commit.success,
      commit_error_code: commit.error?.error_code,
      execution_executed: begin.result?.execution_executed,
    },
    expected: {
      state: 'BLOCKED',
      blast_radius: 48,
      commit_error_code: 'BLOCKED_BY_BLAST_RADIUS',
      execution_exposed: false,
    },
  };
}

function replayCaseC(bundle, registry) {
  const steps = [];
  steps.push(invoke(bundle, registry, 'C', 1, 'rollbackTransaction', {
    transaction_id: 'txn-item519-commit',
  }, 'rollback_operator'));

  const rollback = steps[0].response;
  return {
    case_id: 'CASE_C',
    transaction_id: 'txn-item519-rollback',
    steps,
    actual: {
      state: rollback.result?.state,
      parent_transaction_id: rollback.result?.parent_transaction_id,
      success: rollback.success,
    },
    expected: {
      state: 'ROLLED_BACK',
      parent_transaction_id: 'txn-item519-commit',
    },
  };
}

function replayCaseD(bundle, registry) {
  const steps = [];
  steps.push(invoke(bundle, registry, 'D', 1, 'explainImpact', {
    transaction_id: 'txn-npc462-reingest-proposal',
  }, 'reader'));
  steps.push(invoke(bundle, registry, 'D', 2, 'getReingestProposal', {
    transaction_id: 'txn-npc462-reingest-proposal',
  }, 'reader'));

  const impact = steps[0].response;
  const reingest = steps[1].response;
  return {
    case_id: 'CASE_D',
    transaction_id: 'txn-npc462-reingest-proposal',
    steps,
    actual: {
      consistency_verdict: impact.result?.consistency_verdict,
      recovery_required: reingest.result?.recovery_required,
      execution_executed: impact.result?.execution_executed ?? null,
    },
    expected: {
      consistency_verdict: 'TOPOLOGY_STALE',
      reingest_phases: ['Phase20', 'Phase21'],
      no_f23_execution: true,
    },
  };
}

function buildToolSmokeRequests(bundle) {
  return [
    { tool: 'beginTransaction', args: { intent_id: 'modify_item', target_node: 'item:519' }, role: 'planner' },
    { tool: 'explainImpact', args: { transaction_id: 'txn-item519-commit' }, role: 'reader' },
    { tool: 'getTransaction', args: { transaction_id: 'txn-item519-commit' }, role: 'reader' },
    { tool: 'listTransactions', args: { state: 'ROLLBACK_AVAILABLE' }, role: 'reader' },
    { tool: 'commitTransaction', args: { transaction_id: 'txn-item519-commit', confirm: true }, role: 'operator' },
    { tool: 'rollbackTransaction', args: { transaction_id: 'txn-item519-commit' }, role: 'rollback_operator' },
    { tool: 'getReingestProposal', args: { transaction_id: 'txn-npc462-reingest-proposal' }, role: 'reader' },
    { tool: 'getTransactionConsistency', args: { transaction_id: 'txn-item519-commit' }, role: 'reader' },
  ];
}

function hashReplay(cases) {
  const payload = Object.fromEntries(
    Object.entries(cases).map(([k, v]) => [k, v.steps.map((s) => ({
      tool: s.request.tool_name,
      arguments: s.request.arguments,
      response: s.response,
    }))]),
  );
  return hashString(JSON.stringify(payload));
}

function runTests(preflight, registry, cases, bundle) {
  const results = [];
  const pass = (id, detail) => results.push({ id, status: 'PASS', detail });
  const fail = (id, detail) => results.push({ id, status: 'FAIL', detail });

  if (preflight.report.passed) {
    pass('T1', 'F26 present and F25 bundle OK');
  } else {
    fail('T1', 'Preflight failed');
  }

  try {
    assertExactlyEight(registry);
    const namesMatch = registry.tool_names.every((n, i) => n === TOOL_SURFACE[i]);
    if (registry.tool_count === 8 && namesMatch) {
      pass('T2', 'Registry contains exactly 8 tools from F26 TOOL_SURFACE');
    } else {
      fail('T2', 'Registry tool mismatch');
    }
  } catch (e) {
    fail('T2', e.message);
  }

  const smoke = buildToolSmokeRequests(bundle);
  let dispatchOk = true;
  for (let i = 0; i < smoke.length; i += 1) {
    const { tool, args, role } = smoke[i];
    const { response } = invoke(bundle, registry, 'smoke', i + 1, tool, args, role);
    const code = response.error?.error_code;
    if (code && !isValidErrorCode(code)) dispatchOk = false;
    if (!response.request_id) dispatchOk = false;
  }
  if (dispatchOk) pass('T3', 'All 8 tools dispatch to ResponseModel');
  else fail('T3', 'Tool dispatch failure');

  const caseA = cases.CASE_A;
  if (
    caseA.actual.states_reached?.includes('COMMITTED')
    && caseA.actual.states_reached?.includes('ROLLBACK_AVAILABLE')
    && caseA.actual.rollback_available === true
    && caseA.actual.consistency_verdict === 'CONSISTENT_TOPOLOGY'
  ) {
    pass('T4', 'CASE A executable via gateway');
  } else {
    fail('T4', `CASE A mismatch ${JSON.stringify(caseA.actual)}`);
  }

  const caseB = cases.CASE_B;
  if (
    caseB.actual.begin_state === 'BLOCKED'
    && caseB.actual.blast_radius === 48
    && caseB.actual.commit_success === false
    && caseB.actual.commit_error_code === 'BLOCKED_BY_BLAST_RADIUS'
    && caseB.actual.execution_executed === null
  ) {
    pass('T5', 'CASE B returns BLOCKED_BY_BLAST_RADIUS');
  } else {
    fail('T5', `CASE B mismatch ${JSON.stringify(caseB.actual)}`);
  }

  const caseC = cases.CASE_C;
  if (
    caseC.actual.success === true
    && caseC.actual.state === 'ROLLED_BACK'
    && caseC.actual.parent_transaction_id === 'txn-item519-commit'
  ) {
    pass('T6', 'CASE C rollback valid');
  } else {
    fail('T6', `CASE C mismatch ${JSON.stringify(caseC.actual)}`);
  }

  const caseD = cases.CASE_D;
  if (
    caseD.actual.consistency_verdict === 'TOPOLOGY_STALE'
    && caseD.actual.recovery_required?.includes('Phase20')
    && caseD.actual.recovery_required?.includes('Phase21')
    && caseD.actual.execution_executed === null
  ) {
    pass('T7', 'CASE D reingest proposal Phase20+Phase21');
  } else {
    fail('T7', `CASE D mismatch ${JSON.stringify(caseD.actual)}`);
  }

  const hash1 = hashReplay(cases);
  const cases2 = {
    CASE_A: replayCaseA(bundle, registry),
    CASE_B: replayCaseB(bundle, registry),
    CASE_C: replayCaseC(bundle, registry),
    CASE_D: replayCaseD(bundle, registry),
  };
  const hash2 = hashReplay(cases2);

  const corePassed = results.every((r) => r.status === 'PASS');
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

export function runGatewayValidation() {
  const { report: preflightReport, bundle } = runPreflight();
  if (!preflightReport.passed || !bundle) {
    const abort = {
      phase: GATEWAY_PHASE,
      version: GATEWAY_VERSION,
      aborted: true,
      reason: 'preflight_failed',
      preflight: preflightReport,
      all_tests_passed: false,
      ...RESTRICTION_FLAGS,
    };
    writeJson(join(GATEWAY_DIR, 'mcp-runtime-gateway-last-run.json'), abort);
    return abort;
  }

  const registry = buildToolRegistry();
  writeJson(join(GATEWAY_DIR, 'tool-registry.json'), registry);

  const cases = {
    CASE_A: replayCaseA(bundle, registry),
    CASE_B: replayCaseB(bundle, registry),
    CASE_C: replayCaseC(bundle, registry),
    CASE_D: replayCaseD(bundle, registry),
  };

  const caseReplayReport = {
    phase: 'GATEWAY_CASE_REPLAY',
    timestamp: new Date().toISOString(),
    all_cases_replayed: true,
    cases,
    error_codes_used: Object.keys(ERROR_CODES),
  };
  writeJson(join(GATEWAY_DIR, 'case-replay-report.json'), caseReplayReport);

  const { results, all_tests_passed, determinism_hash } = runTests(
    { report: preflightReport },
    registry,
    cases,
    bundle,
  );

  const lastRun = {
    phase: GATEWAY_PHASE,
    version: GATEWAY_VERSION,
    timestamp: new Date().toISOString(),
    ...RESTRICTION_FLAGS,
    preflight: { passed: preflightReport.passed },
    registry: {
      tool_count: registry.tool_count,
      tool_names: registry.tool_names,
      source: 'F26_TOOL_SURFACE',
    },
    case_summary: Object.fromEntries(
      Object.entries(cases).map(([k, v]) => [k, {
        transaction_id: v.transaction_id,
        tools_used: v.steps.map((s) => s.request.tool_name),
      }]),
    ),
    outputs: {
      gateway_preflight: join(GATEWAY_DIR, 'gateway-preflight-report.json'),
      tool_registry: join(GATEWAY_DIR, 'tool-registry.json'),
      case_replay: join(GATEWAY_DIR, 'case-replay-report.json'),
    },
    tests: results,
    all_tests_passed,
    determinism_hash,
    f28_readiness: {
      gateway_stable: all_tests_passed,
      any_client_can_consume: true,
      public_surface: 'Gateway -> Tool Contract -> World Transaction',
    },
  };

  writeJson(join(GATEWAY_DIR, 'mcp-runtime-gateway-last-run.json'), lastRun);
  return lastRun;
}

if (process.argv[1]?.includes('run-gateway-validation')) {
  const result = runGatewayValidation();
  console.log(JSON.stringify({
    all_tests_passed: result.all_tests_passed,
    aborted: result.aborted || false,
    tools: result.registry?.tool_count,
    cases: result.case_summary ? Object.keys(result.case_summary) : [],
    tests_failed: (result.tests || []).filter((t) => t.status === 'FAIL'),
  }, null, 2));
  if (!result.all_tests_passed) process.exit(1);
}
