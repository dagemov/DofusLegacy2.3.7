#!/usr/bin/env node
/** F25 Transaction Assembler — build IWorldTransaction from F22/F23/F24 artifacts */
import { join } from 'node:path';
import {
  loadF22Artifacts,
  findPlanAndValidation,
  loadF23Run,
  loadF24Artifacts,
  findConsistencyReportForRun,
  buildReingestProposalFromF24,
  createLifecycle,
  transitionLifecycle,
  COMMIT_RUN_ID,
  ROLLBACK_RUN_ID,
  SIM_DIR,
} from './_txn-lib.mjs';

function baseTransaction(id, intentId, targetNode, plan) {
  return {
    transaction_id: id,
    intent_id: intentId,
    target_node: targetNode,
    lifecycle: createLifecycle('PLANNED'),
    mutation_plan_ref: join(SIM_DIR, 'execution_plans.json'),
    validation: null,
    execution: null,
    consistency: null,
    reingest_proposal: null,
    commit_model: 'confirm_gated_f23',
    rollback_model: 'f23_backup_plus_rollback_sql',
    case_id: null,
    parent_transaction_id: null,
    mode: 'real',
  };
}

function attachValidation(txn, validation) {
  txn.validation = {
    verdict: validation.verdict,
    blast_radius_total: validation.blast_radius_total,
    max_modification_risk: validation.max_modification_risk,
    why: validation.why,
  };
  transitionLifecycle(txn.lifecycle, 'VALIDATED', `F22 verdict ${validation.verdict}`);
  return txn;
}

function attachExecution(txn, f23Run) {
  if (!f23Run) return txn;
  txn.execution = {
    executed: f23Run.trace.executed === true,
    success: f23Run.trace.success === true,
    run_id: f23Run.run_id,
    trace_ref: f23Run.trace_ref,
    event_ref: f23Run.event_ref,
    backup_id: f23Run.trace.backup_id || null,
    rollback_sql_ref: f23Run.rollback_sql_ref,
    dry_run: f23Run.trace.dry_run,
  };
  return txn;
}

function attachConsistency(txn, consistencyData) {
  if (!consistencyData?.report && !consistencyData?.diff) return txn;
  const report = consistencyData.report;
  const diff = consistencyData.diff;
  txn.consistency = {
    verdict: report?.consistency_verdict || 'CONSISTENT_TOPOLOGY',
    graph_requires_update: diff?.graph_requires_update ?? report?.graph_requires_update ?? false,
    causal_recompute_required: diff?.causal_recompute_required ?? report?.causal_recompute_required ?? false,
    recovery_required: report?.recovery_required || diff?.recovery_required || [],
    edges_checked: report?.edges_checked,
    runtime_before: diff?.runtime_before,
    runtime_after: diff?.runtime_after,
  };
  return txn;
}

export function assembleCaseA(f22, f24) {
  const { plan, validation } = findPlanAndValidation(f22, 'modify_item', 'item:519');
  const txn = baseTransaction('txn-item519-commit', 'modify_item', 'item:519', plan);
  txn.case_id = 'CASE_A';

  attachValidation(txn, validation);
  transitionLifecycle(txn.lifecycle, 'READY_TO_COMMIT', 'F22 APPROVE and F23 gates pass');

  const f23Run = loadF23Run(COMMIT_RUN_ID);
  attachExecution(txn, f23Run);

  if (f23Run?.trace?.success && f23Run.trace.executed) {
    transitionLifecycle(txn.lifecycle, 'COMMITTED', `F23 run ${COMMIT_RUN_ID} executed successfully`);
    if (f23Run.trace.rollback_available) {
      transitionLifecycle(txn.lifecycle, 'ROLLBACK_AVAILABLE', `backup ${f23Run.trace.backup_id}`);
    }
  } else if (f23Run && !f23Run.trace.success) {
    transitionLifecycle(txn.lifecycle, 'FAILED', 'F23 execution failed');
  }

  attachConsistency(txn, findConsistencyReportForRun(f24, COMMIT_RUN_ID, 'item:519'));
  return txn;
}

export function assembleCaseB(f22) {
  const { plan, validation } = findPlanAndValidation(f22, 'modify_npc', 'npc:462');
  const txn = baseTransaction('txn-npc462-blocked', 'modify_npc', 'npc:462', plan);
  txn.case_id = 'CASE_B';

  attachValidation(txn, validation);
  transitionLifecycle(txn.lifecycle, 'BLOCKED', `F22 BLOCK blast_radius=${validation.blast_radius_total}`);
  txn.execution = null;
  txn.consistency = null;
  txn.reingest_proposal = null;
  return txn;
}

export function assembleCaseC(f22, f24) {
  const parent = assembleCaseA(f22, f24);
  const f23Run = loadF23Run(ROLLBACK_RUN_ID);

  const txn = baseTransaction('txn-item519-rollback', 'modify_item', 'item:519', null);
  txn.case_id = 'CASE_C';
  txn.parent_transaction_id = parent.transaction_id;
  txn.validation = { ...parent.validation };
  transitionLifecycle(txn.lifecycle, 'PLANNED', 'Rollback child of txn-item519-commit');
  transitionLifecycle(txn.lifecycle, 'VALIDATED', 'Inherits APPROVE from parent commit context');
  transitionLifecycle(txn.lifecycle, 'READY_TO_COMMIT', 'Rollback uses F23 confirm path');

  attachExecution(txn, f23Run);
  if (f23Run?.trace?.success && f23Run.trace.executed) {
    transitionLifecycle(txn.lifecycle, 'COMMITTED', `F23 restore run ${ROLLBACK_RUN_ID}`);
    transitionLifecycle(txn.lifecycle, 'ROLLED_BACK', 'Runtime restored to pre-commit state');
  }

  attachConsistency(txn, findConsistencyReportForRun(f24, ROLLBACK_RUN_ID, 'item:519'));
  txn.net_runtime_unchanged = f24.lastRun?.highlights?.item_519_net?.net_changed === false;
  return txn;
}

export function assembleCaseD(f22, f24) {
  const { plan, validation } = findPlanAndValidation(f22, 'modify_npc', 'npc:462');
  const benchmark = f24.consistency?.simulated_benchmark;

  const txn = baseTransaction('txn-npc462-reingest-proposal', 'modify_npc', 'npc:462', plan);
  txn.case_id = 'CASE_D';
  txn.mode = 'simulated';

  attachValidation(txn, validation);
  transitionLifecycle(txn.lifecycle, 'BLOCKED', `F22 BLOCK — no F23 execution; reingest proposal from F24 benchmark`);

  txn.execution = null;

  if (benchmark) {
    txn.consistency = {
      verdict: 'TOPOLOGY_STALE',
      graph_requires_update: benchmark.graph_requires_update,
      causal_recompute_required: benchmark.causal_recompute_required,
      recovery_required: benchmark.predicted_recovery_phases || [],
      predicted_affected_edges: benchmark.predicted_affected_edges,
      mode: 'simulated',
      runtime_snapshot: null,
    };
    txn.reingest_proposal = buildReingestProposalFromF24(f24, 'npc:462');
    txn.hypothetical_change = benchmark.hypothetical_change;
  }

  return txn;
}

export function assembleAllTransactions() {
  const f22 = loadF22Artifacts();
  const f24 = loadF24Artifacts();

  return {
    phase: 'WORLD_TRANSACTIONS',
    timestamp: new Date().toISOString(),
    read_only: true,
    no_graph_mutation: true,
    no_runtime_writes: true,
    transactions: [
      assembleCaseA(f22, f24),
      assembleCaseB(f22),
      assembleCaseC(f22, f24),
      assembleCaseD(f22, f24),
    ],
  };
}

if (process.argv[1]?.includes('transaction-assembler')) {
  const result = assembleAllTransactions();
  console.log(JSON.stringify({
    count: result.transactions.length,
    cases: result.transactions.map((t) => ({ id: t.transaction_id, state: t.lifecycle.current_state, case_id: t.case_id })),
  }, null, 2));
}
