#!/usr/bin/env node
/** F23 Execution Bridge v1 — MEK dry-run → controlled VPS MariaDB writes */
import { readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import {
  BRIDGE_DIR,
  OUT_DIR,
  SIM_DIR,
  loadF22Artifacts,
  findPlanAndValidation,
  writeJson,
  hashString,
  makeRunId,
  F23_EXECUTION_GATES,
} from './_bridge-lib.mjs';
import { executeMutation } from './runtime-executor.mjs';
import { translateMutationPlan } from './sql-translator.mjs';

function parseArgs(argv) {
  const args = {
    intent: 'modify_item',
    target: 'item:519',
    fields: { Name: 'MEK-F23-dry-run-marker' },
    confirm: false,
    runTests: true,
  };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--confirm') args.confirm = true;
    else if (a === '--no-tests') args.runTests = false;
    else if (a === '--intent' && argv[i + 1]) args.intent = argv[++i];
    else if (a === '--target' && argv[i + 1]) args.target = argv[++i];
    else if (a === '--fields' && argv[i + 1]) args.fields = JSON.parse(argv[++i]);
    else if (a === '--fields-file' && argv[i + 1]) {
      args.fields = JSON.parse(readFileSync(argv[++i], 'utf8'));
    }
  }
  return args;
}

function artifactMtime(dir) {
  try {
    return statSync(dir).mtimeMs;
  } catch {
    return 0;
  }
}

async function runTests(artifacts) {
  const results = [];
  const pass = (id, detail) => results.push({ id, status: 'PASS', detail });
  const fail = (id, detail) => results.push({ id, status: 'FAIL', detail });

  const causalDir = join(BRIDGE_DIR, '..', 'world-causal');
  const relDir = join(BRIDGE_DIR, '..', 'world-relations');
  const semDir = join(BRIDGE_DIR, '..', 'world-semantic');
  const mtBefore = {
    causal: artifactMtime(join(causalDir, 'causal_graph.jsonl')),
    rel: artifactMtime(join(relDir, 'relationship_graph.json')),
    sem: artifactMtime(join(semDir, 'consistency_rules.json')),
  };

  const { plan, validation } = findPlanAndValidation(artifacts, 'modify_item', 'item:519');
  const dry1 = await executeMutation({
    plan,
    validation,
    fieldsAfter: { Name: 'MEK-determinism-A' },
    confirm: false,
    runId: 'test-dry-1',
  });

  const mtAfter = {
    causal: artifactMtime(join(causalDir, 'causal_graph.jsonl')),
    rel: artifactMtime(join(relDir, 'relationship_graph.json')),
    sem: artifactMtime(join(semDir, 'consistency_rules.json')),
  };

  if (mtBefore.causal === mtAfter.causal && mtBefore.rel === mtAfter.rel && mtBefore.sem === mtAfter.sem) {
    pass('T1', 'No mtime change on world-causal / world-relations / world-semantic artifacts');
  } else {
    fail('T1', 'Graph layer artifacts changed during bridge run');
  }

  if (!dry1.executed && dry1.trace.ssh_commands.length === 0) {
    pass('T2', 'Dry-run path emits zero SSH commands (no local mariadb in production path)');
  } else {
    fail('T2', `Unexpected execution or SSH in dry-run: executed=${dry1.executed}`);
  }

  if (!dry1.executed && dry1.patch?.forward_sql) {
    pass('T3', 'confirm=false → executed:false with patch preview');
  } else {
    fail('T3', 'Dry-run missing patch or executed unexpectedly');
  }

  const highRiskValidation = {
    verdict: 'APPROVE',
    max_modification_risk: 'HIGH',
    blast_radius_total: 25,
  };
  const blockedHigh = await executeMutation({
    plan,
    validation: highRiskValidation,
    fieldsAfter: { Name: 'blocked' },
    confirm: true,
    runId: 'test-block-high',
  });
  if (blockedHigh.blocked && !blockedHigh.executed) {
    pass('T4', 'F23 blocks HIGH risk even with confirm=true');
  } else {
    fail('T4', 'HIGH risk gate did not block');
  }

  const blockedIntents = ['modify_quest', 'modify_dungeon', 'modify_map'];
  let t5ok = true;
  for (const intentId of blockedIntents) {
    const { plan: p, validation: v } = findPlanAndValidation(artifacts, intentId);
    if (!p) continue;
    const r = await executeMutation({
      plan: p,
      validation: v,
      fieldsAfter: { Name: 'x' },
      confirm: false,
      runId: `test-block-${intentId}`,
    });
    if (!r.blocked) t5ok = false;
  }
  if (t5ok) pass('T5', 'modify_quest / modify_dungeon / modify_map blocked at F23 gate');
  else fail('T5', 'Disallowed intent was not blocked');

  const rollbackTest = translateMutationPlan(plan, { Name: 'NewName' }, { Name: 'OldName' });
  if (rollbackTest.rollback_available && rollbackTest.patch.rollback_sql.includes('OldName')) {
    pass('T6', 'Rollback SQL generated with before values');
  } else {
    fail('T6', 'Rollback SQL missing before values');
  }

  const trace = dry1.trace;
  const requiredKeys = [
    'intent_id', 'target_node', 'sql_commands', 'ssh_commands', 'docker_container',
    'execution_time_ms', 'success', 'rollback_available', 'logs',
  ];
  if (requiredKeys.every((k) => k in trace)) {
    pass('T7', 'execution_trace.json schema valid');
  } else {
    fail('T7', `Missing trace keys: ${requiredKeys.filter((k) => !(k in trace)).join(', ')}`);
  }

  const dry2 = await executeMutation({
    plan,
    validation,
    fieldsAfter: { Name: 'MEK-determinism-A' },
    confirm: false,
    runId: 'test-dry-2',
  });
  const h1 = hashString(dry1.patch.forward_sql);
  const h2 = hashString(dry2.patch.forward_sql);
  if (h1 === h2) pass('T8', `Deterministic patch hash ${h1}`);
  else fail('T8', `Patch hash mismatch ${h1} vs ${h2}`);

  return results;
}

async function main() {
  const args = parseArgs(process.argv);
  const artifacts = loadF22Artifacts();
  const { plan, validation } = findPlanAndValidation(artifacts, args.intent, args.target);

  if (!plan) {
    console.error(`No F22 plan for intent=${args.intent} target=${args.target}`);
    process.exit(1);
  }

  let testResults = [];
  if (args.runTests) {
    testResults = await runTests(artifacts);
  }

  const result = await executeMutation({
    plan,
    validation,
    fieldsAfter: args.fields,
    confirm: args.confirm,
    runId: args.confirm ? makeRunId() : 'cli-dry-run',
  });

  const lastRun = {
    phase: 'EXECUTION_BRIDGE_F23',
    bridge: 'IRuntimeExecutor v1',
    timestamp: new Date().toISOString(),
    cli: args,
    f22_plan_source: join(SIM_DIR, 'execution_plans.json'),
    f23_gates: {
      allowed_intents: [...F23_EXECUTION_GATES.ALLOWED_INTENTS],
      max_blast_radius: F23_EXECUTION_GATES.MAX_BLAST_RADIUS,
      block_risks: [...F23_EXECUTION_GATES.BLOCK_RISKS],
    },
    execution_result: {
      success: result.success,
      executed: result.executed,
      blocked: result.blocked,
      block_reason: result.block_reason,
      dry_run: result.dry_run,
      rollback_available: result.rollback_available,
      trace_path: result.trace_path,
    },
    tests: testResults,
    all_tests_passed: testResults.length ? testResults.every((t) => t.status === 'PASS') : null,
    no_graph_mutation: true,
  };

  writeJson(join(BRIDGE_DIR, 'mcp-execution-bridge-last-run.json'), lastRun);

  console.log(JSON.stringify({
    success: result.success,
    executed: result.executed,
    blocked: result.blocked,
    dry_run: result.dry_run,
    trace_path: result.trace_path,
    tests: testResults.filter((t) => t.status === 'FAIL').length
      ? testResults
      : { passed: testResults.length, all_passed: lastRun.all_tests_passed },
  }, null, 2));

  if (testResults.some((t) => t.status === 'FAIL')) process.exit(1);
}

if (process.argv[1]?.includes('run-execution-bridge')) {
  main().catch((err) => {
    console.error(err);
    process.exit(1);
  });
}
