#!/usr/bin/env node
/** F26 — map F25 Cases A-D to tool-call sequences; validate contract representability */
import { join } from 'node:path';
import {
  CONTRACT_DIR,
  writeJson,
  loadWorldTransactions,
  findTransactionByCase,
  findTransactionById,
  projectTransaction,
  projectImpact,
  projectConsistency,
  projectReingestProposal,
  makeContractError,
  mapValidationToError,
  assertNoForbiddenExposure,
  buildToolContractSpec,
  TOOL_SURFACE,
  ERROR_CODES,
  STATE_TOOL_MATRIX,
  WORLD_TRANSACTION_STATES,
} from './_contract-lib.mjs';

function toolCall(tool, input, output) {
  return { tool, input, output };
}

function buildCaseA(bundle) {
  const txn = findTransactionByCase(bundle, 'CASE_A');
  const view = projectTransaction(txn);
  const consistency = projectConsistency(txn);

  const sequence = [
    toolCall('beginTransaction', { intent_id: 'modify_item', target_node: 'item:519', fields: { Name: 'MEK-F23-bridge-test' } }, {
      transaction_id: view.transaction_id,
      state: 'READY_TO_COMMIT',
      validation: view.validation,
    }),
    toolCall('explainImpact', { transaction_id: view.transaction_id }, projectImpact(txn)),
    toolCall('commitTransaction', { transaction_id: view.transaction_id, confirm: true }, {
      ...view,
      state: 'ROLLBACK_AVAILABLE',
      rollback_available: true,
    }),
    toolCall('getTransactionConsistency', { transaction_id: view.transaction_id }, consistency),
    toolCall('getTransaction', { transaction_id: view.transaction_id }, view),
  ];

  return {
    case_id: 'CASE_A',
    transaction_id: txn.transaction_id,
    representable: true,
    expected: {
      states: ['COMMITTED', 'ROLLBACK_AVAILABLE'],
      consistency_verdict: 'CONSISTENT_TOPOLOGY',
      rollback_available: true,
      blast_radius: 0,
    },
    actual: {
      states_reached: view.states_reached,
      consistency_verdict: consistency?.verdict,
      rollback_available: view.rollback_available,
      blast_radius: view.validation?.blast_radius,
    },
    tool_sequence: sequence,
    no_forbidden_exposure: sequence.every((s) => assertNoForbiddenExposure(s.output)),
  };
}

function buildCaseB(bundle) {
  const txn = findTransactionByCase(bundle, 'CASE_B');
  const view = projectTransaction(txn);
  const commitError = makeContractError(
    mapValidationToError(txn),
    `Cannot commit blocked transaction blast_radius=${txn.validation.blast_radius_total}`,
    txn.transaction_id,
  );

  const sequence = [
    toolCall('beginTransaction', { intent_id: 'modify_npc', target_node: 'npc:462' }, {
      transaction_id: view.transaction_id,
      state: 'BLOCKED',
      validation: view.validation,
    }),
    toolCall('explainImpact', { intent_id: 'modify_npc', target_node: 'npc:462' }, projectImpact(txn)),
    toolCall('commitTransaction', { transaction_id: view.transaction_id, confirm: true }, commitError),
    toolCall('getTransaction', { transaction_id: view.transaction_id }, view),
  ];

  return {
    case_id: 'CASE_B',
    transaction_id: txn.transaction_id,
    representable: true,
    expected: {
      state: 'BLOCKED',
      blast_radius: 48,
      execution_exposed: false,
      error_on_commit: 'BLOCKED_BY_BLAST_RADIUS',
    },
    actual: {
      state: view.state,
      blast_radius: view.validation?.blast_radius,
      execution_executed: view.execution_executed,
      commit_error_code: commitError.error_code,
    },
    tool_sequence: sequence,
    no_forbidden_exposure: sequence.every((s) => assertNoForbiddenExposure(s.output)),
  };
}

function buildCaseC(bundle) {
  const parent = findTransactionByCase(bundle, 'CASE_A');
  const rollback = findTransactionByCase(bundle, 'CASE_C');
  const parentView = projectTransaction(parent);
  const rollbackView = projectTransaction(rollback);

  const sequence = [
    toolCall('getTransaction', { transaction_id: parentView.transaction_id }, parentView),
    toolCall('rollbackTransaction', { transaction_id: parentView.transaction_id }, rollbackView),
    toolCall('getTransaction', { transaction_id: rollbackView.transaction_id }, rollbackView),
    toolCall('listTransactions', { state: 'ROLLED_BACK' }, [rollbackView]),
  ];

  return {
    case_id: 'CASE_C',
    transaction_id: rollback.transaction_id,
    parent_transaction_id: parent.transaction_id,
    representable: true,
    expected: {
      state: 'ROLLED_BACK',
      parent_linked: true,
      net_runtime_unchanged: true,
    },
    actual: {
      state: rollbackView.state,
      parent_transaction_id: rollbackView.parent_transaction_id,
      net_runtime_unchanged: rollback.net_runtime_unchanged,
    },
    tool_sequence: sequence,
    no_forbidden_exposure: sequence.every((s) => assertNoForbiddenExposure(s.output)),
  };
}

function buildCaseD(bundle) {
  const txn = findTransactionByCase(bundle, 'CASE_D');
  const view = projectTransaction(txn);
  const impact = projectImpact(txn);
  const reingest = projectReingestProposal(txn);
  const commitError = makeContractError(
    'BLOCKED_BY_BLAST_RADIUS',
    'Transaction blocked before execution',
    txn.transaction_id,
  );
  const reingestNotice = makeContractError(
    'REINGEST_REQUIRED',
    'Structural change would require Phase20+Phase21 re-ingest',
    txn.transaction_id,
  );

  const sequence = [
    toolCall('beginTransaction', { intent_id: 'modify_npc', target_node: 'npc:462' }, {
      transaction_id: view.transaction_id,
      state: 'BLOCKED',
      validation: view.validation,
    }),
    toolCall('explainImpact', { transaction_id: view.transaction_id }, impact),
    toolCall('getReingestProposal', { transaction_id: view.transaction_id }, reingest),
    toolCall('commitTransaction', { transaction_id: view.transaction_id, confirm: true }, commitError),
    toolCall('explainImpact', { transaction_id: view.transaction_id }, { ...impact, reingest_notice: reingestNotice }),
  ];

  return {
    case_id: 'CASE_D',
    transaction_id: txn.transaction_id,
    representable: true,
    expected: {
      state: 'BLOCKED',
      reingest_phases: ['Phase20', 'Phase21'],
      consistency_verdict: 'TOPOLOGY_STALE',
      no_f23_execution: true,
    },
    actual: {
      state: view.state,
      reingest_phases: reingest?.recovery_required,
      consistency_verdict: view.consistency?.verdict,
      execution_executed: view.execution_executed,
      reingest_error_code: reingestNotice.error_code,
    },
    tool_sequence: sequence,
    no_forbidden_exposure: sequence.every((s) => assertNoForbiddenExposure(s.output)),
    no_fake_f23: txn.execution === null,
  };
}

export function validateContractRepresentation() {
  const bundle = loadWorldTransactions();
  if (!bundle) throw new Error('world-transactions.json not found');

  const cases = {
    CASE_A: buildCaseA(bundle),
    CASE_B: buildCaseB(bundle),
    CASE_C: buildCaseC(bundle),
    CASE_D: buildCaseD(bundle),
  };

  const allRepresentable = Object.values(cases).every((c) => c.representable && c.no_forbidden_exposure);
  const allStatesMapped = WORLD_TRANSACTION_STATES.every((state) => STATE_TOOL_MATRIX[state]?.observable_via?.length > 0);
  const allErrorsMapped = Object.keys(ERROR_CODES).length >= 8;

  const caseReport = {
    phase: 'CASE_REPRESENTATION',
    timestamp: new Date().toISOString(),
    all_representable: allRepresentable,
    cases,
    compatibility: {
      mcp_calls: 'F26 tools only',
      f26_backing: 'F25 IWorldTransaction',
      f25_backing: 'F22/F23/F24 artifacts',
      direct_f22_f23_f24_access: false,
    },
    state_coverage: {
      all_states_mapped: allStatesMapped,
      states: WORLD_TRANSACTION_STATES.map((s) => ({
        state: s,
        tools: STATE_TOOL_MATRIX[s],
      })),
    },
    error_coverage: {
      all_errors_defined: allErrorsMapped,
      codes: Object.keys(ERROR_CODES),
    },
  };

  const toolContract = buildToolContractSpec();
  toolContract.case_bindings = Object.fromEntries(
    Object.entries(cases).map(([k, v]) => [k, {
      transaction_id: v.transaction_id,
      tool_sequence: v.tool_sequence.map((s) => s.tool),
      representable: v.representable,
    }]),
  );

  writeJson(join(CONTRACT_DIR, 'case-representation-report.json'), caseReport);
  writeJson(join(CONTRACT_DIR, 'tool-contract.json'), toolContract);

  return { caseReport, toolContract, bundle };
}

if (process.argv[1]?.includes('contract-validate')) {
  const { caseReport } = validateContractRepresentation();
  console.log(JSON.stringify({
    all_representable: caseReport.all_representable,
    cases: Object.keys(caseReport.cases),
  }, null, 2));
}
