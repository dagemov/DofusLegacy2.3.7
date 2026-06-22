import { readFileSync, writeFileSync, mkdirSync, existsSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

export const CONTRACT_DIR = dirname(fileURLToPath(import.meta.url));
export const PROTO_DIR = join(CONTRACT_DIR, '..');
export const WC_DIR = join(PROTO_DIR, 'world-causal');
export const SIM_DIR = join(PROTO_DIR, 'mcp-execution-sim');
export const BRIDGE_DIR = join(PROTO_DIR, 'mcp-execution-bridge');
export const BRIDGE_OUT = join(BRIDGE_DIR, 'out');
export const SYNC_DIR = join(PROTO_DIR, 'runtime-sync');
export const TXN_DIR = join(PROTO_DIR, 'world-transaction');

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

export const TOOL_SURFACE = [
  'beginTransaction',
  'explainImpact',
  'getTransaction',
  'listTransactions',
  'commitTransaction',
  'rollbackTransaction',
  'getReingestProposal',
  'getTransactionConsistency',
];

export const ERROR_CODES = {
  BLOCKED_BY_BLAST_RADIUS: {
    code: 'BLOCKED_BY_BLAST_RADIUS',
    derived_from: 'F22 verdict BLOCK or blast_radius_total > threshold',
    f23_gate: 'blast_radius_exceeded',
  },
  BLOCKED_BY_MODIFICATION_RISK: {
    code: 'BLOCKED_BY_MODIFICATION_RISK',
    derived_from: 'F22 max_modification_risk HIGH',
    f23_gate: 'high_modification_risk',
  },
  REQUIRES_HUMAN_CONFIRMATION: {
    code: 'REQUIRES_HUMAN_CONFIRMATION',
    derived_from: 'F23 confirm gate — commit without confirm:true',
    f23_gate: 'confirm_not_received',
  },
  VALIDATION_REVIEW_REQUIRED: {
    code: 'VALIDATION_REVIEW_REQUIRED',
    derived_from: 'F22 verdict REVIEW (F23 requires APPROVE)',
    f23_gate: 'f22_verdict_REVIEW',
  },
  TRANSACTION_NOT_FOUND: {
    code: 'TRANSACTION_NOT_FOUND',
    derived_from: 'unknown transaction_id',
  },
  INVALID_STATE_TRANSITION: {
    code: 'INVALID_STATE_TRANSITION',
    derived_from: 'tool called in incompatible F25 state',
  },
  ROLLBACK_NOT_AVAILABLE: {
    code: 'ROLLBACK_NOT_AVAILABLE',
    derived_from: 'no ROLLBACK_AVAILABLE state or backup',
  },
  REINGEST_REQUIRED: {
    code: 'REINGEST_REQUIRED',
    derived_from: 'F24 TOPOLOGY_STALE + recovery_required non-empty',
  },
};

export const PERMISSION_MODEL = {
  reader: ['getTransaction', 'listTransactions', 'explainImpact', 'getReingestProposal', 'getTransactionConsistency'],
  planner: ['getTransaction', 'listTransactions', 'explainImpact', 'getReingestProposal', 'getTransactionConsistency', 'beginTransaction'],
  operator: ['getTransaction', 'listTransactions', 'explainImpact', 'getReingestProposal', 'getTransactionConsistency', 'beginTransaction', 'commitTransaction'],
  rollback_operator: ['getTransaction', 'listTransactions', 'explainImpact', 'getReingestProposal', 'getTransactionConsistency', 'rollbackTransaction'],
};

export const STATE_TOOL_MATRIX = {
  PLANNED: { observable_via: ['beginTransaction', 'getTransaction', 'listTransactions'], transition_via: ['beginTransaction'] },
  VALIDATED: { observable_via: ['getTransaction', 'listTransactions', 'explainImpact'], transition_via: ['commitTransaction', 'explainImpact'] },
  BLOCKED: { observable_via: ['getTransaction', 'listTransactions', 'explainImpact', 'getReingestProposal'], transition_via: [] },
  READY_TO_COMMIT: { observable_via: ['getTransaction', 'listTransactions', 'explainImpact'], transition_via: ['commitTransaction'] },
  COMMITTED: { observable_via: ['getTransaction', 'getTransactionConsistency', 'listTransactions'], transition_via: [] },
  ROLLBACK_AVAILABLE: { observable_via: ['getTransaction', 'getTransactionConsistency', 'listTransactions'], transition_via: ['rollbackTransaction'] },
  ROLLED_BACK: { observable_via: ['getTransaction', 'listTransactions'], transition_via: [] },
  FAILED: { observable_via: ['getTransaction', 'listTransactions'], transition_via: [] },
};

const FORBIDDEN_EXPOSURE_KEYS = [
  'mutation_plan_ref',
  'trace_ref',
  'event_ref',
  'backup_id',
  'rollback_sql_ref',
  'run_id',
  'commit_model',
  'rollback_model',
  'execution',
];

export function writeJson(path, data) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

export function hashString(s) {
  return createHash('sha256').update(s).digest('hex').slice(0, 16);
}

export function countJsonlLines(path) {
  return readFileSync(path, 'utf8').split('\n').filter(Boolean).length;
}

export function loadWorldTransactions() {
  const path = join(TXN_DIR, 'world-transactions.json');
  if (!existsSync(path)) return null;
  return JSON.parse(readFileSync(path, 'utf8'));
}

export function findTransactionByCase(bundle, caseId) {
  return bundle?.transactions?.find((t) => t.case_id === caseId) || null;
}

export function findTransactionById(bundle, transactionId) {
  return bundle?.transactions?.find((t) => t.transaction_id === transactionId) || null;
}

export function statesReached(txn) {
  const hist = txn.lifecycle?.history?.map((h) => h.state) || [];
  return [...new Set([...hist, txn.lifecycle?.current_state].filter(Boolean))];
}

export function projectTransaction(txn) {
  const reached = statesReached(txn);
  const recovery = txn.consistency?.recovery_required
    || txn.reingest_proposal?.recovery_required
    || [];

  return {
    transaction_id: txn.transaction_id,
    intent_id: txn.intent_id,
    target_node: txn.target_node,
    state: txn.lifecycle?.current_state,
    states_reached: reached,
    validation: txn.validation ? {
      verdict: txn.validation.verdict,
      blast_radius: txn.validation.blast_radius_total,
      modification_risk: txn.validation.max_modification_risk,
    } : null,
    consistency: txn.consistency ? {
      verdict: txn.consistency.verdict,
      graph_requires_update: txn.consistency.graph_requires_update ?? false,
      recovery_required: recovery,
    } : null,
    rollback_available: reached.includes('ROLLBACK_AVAILABLE'),
    reingest_required: recovery.length > 0,
    parent_transaction_id: txn.parent_transaction_id || null,
    mode: txn.mode || 'real',
    execution_executed: txn.execution?.executed ?? null,
  };
}

export function projectImpact(txn) {
  const view = projectTransaction(txn);
  return {
    transaction_id: view.transaction_id,
    intent_id: view.intent_id,
    target_node: view.target_node,
    blast_radius: view.validation?.blast_radius ?? null,
    modification_risk: view.validation?.modification_risk ?? null,
    validation_verdict: view.validation?.verdict ?? null,
    consistency_verdict: view.consistency?.verdict ?? null,
    predicted_affected_edges: txn.consistency?.predicted_affected_edges || [],
    recovery_required: view.consistency?.recovery_required || [],
    reingest_required: view.reingest_required,
  };
}

export function projectReingestProposal(txn) {
  if (!txn.reingest_proposal) return null;
  return {
    transaction_id: txn.transaction_id,
    recovery_required: txn.reingest_proposal.recovery_required || [],
    invalidated_artifacts: (txn.reingest_proposal.invalidated_artifacts || []).map((a) => ({
      path: a.path,
      rerun: a.rerun,
      reason: a.reason,
    })),
    rerun_commands: (txn.reingest_proposal.rerun_commands || []).map((r) => ({
      phase: r.phase,
      command: r.command,
      cwd: r.cwd,
    })),
  };
}

export function projectConsistency(txn) {
  if (!txn.consistency) return null;
  return {
    transaction_id: txn.transaction_id,
    verdict: txn.consistency.verdict,
    graph_requires_update: txn.consistency.graph_requires_update ?? false,
    causal_recompute_required: txn.consistency.causal_recompute_required ?? false,
    recovery_required: txn.consistency.recovery_required || [],
    edges_checked: txn.consistency.edges_checked,
  };
}

export function makeContractError(code, message, transactionId) {
  return {
    error: true,
    error_code: code,
    message,
    transaction_id: transactionId,
  };
}

export function mapValidationToError(txn) {
  if (!txn.validation) return 'INVALID_STATE_TRANSITION';
  if (txn.validation.verdict === 'BLOCK' && txn.validation.blast_radius_total > 10) {
    return 'BLOCKED_BY_BLAST_RADIUS';
  }
  if (txn.validation.verdict === 'BLOCK' && txn.validation.max_modification_risk === 'HIGH') {
    return 'BLOCKED_BY_MODIFICATION_RISK';
  }
  if (txn.validation.verdict === 'BLOCK') return 'BLOCKED_BY_BLAST_RADIUS';
  if (txn.validation.verdict === 'REVIEW') return 'VALIDATION_REVIEW_REQUIRED';
  return 'INVALID_STATE_TRANSITION';
}

export function assertNoForbiddenExposure(obj) {
  const json = JSON.stringify(obj);
  for (const key of FORBIDDEN_EXPOSURE_KEYS) {
    if (json.includes(`"${key}"`)) return false;
  }
  if (json.includes('ssh') || json.includes('docker exec') || json.includes('mariadb')) return false;
  return true;
}

export function listExecutedF23Runs() {
  if (!existsSync(BRIDGE_OUT)) return [];
  const runs = [];
  for (const name of readdirSync(BRIDGE_OUT, { withFileTypes: true })) {
    if (!name.isDirectory()) continue;
    const tracePath = join(BRIDGE_OUT, name.name, 'execution_trace.json');
    if (!existsSync(tracePath)) continue;
    const trace = JSON.parse(readFileSync(tracePath, 'utf8'));
    if (trace.executed === true && trace.success === true) runs.push(name.name);
  }
  return runs.sort();
}

export function buildToolContractSpec() {
  return {
    phase: 'MCP_TOOL_CONTRACT_F26',
    version: 'v1',
    read_only: true,
    tool_surface: TOOL_SURFACE,
    error_codes: Object.keys(ERROR_CODES),
    permission_model: PERMISSION_MODEL,
    state_tool_matrix: STATE_TOOL_MATRIX,
    world_transaction_states: WORLD_TRANSACTION_STATES,
    inputs: {
      beginTransaction: { intent_id: 'string', target_node: 'string', fields: 'optional Record' },
      explainImpact: { transaction_id: 'string OR intent_id+target_node' },
      getTransaction: { transaction_id: 'string' },
      listTransactions: { state: 'optional WorldTransactionState' },
      commitTransaction: { transaction_id: 'string', confirm: 'true required' },
      rollbackTransaction: { transaction_id: 'string' },
      getReingestProposal: { transaction_id: 'string' },
      getTransactionConsistency: { transaction_id: 'string' },
    },
    output_types: ['TransactionView', 'ImpactView', 'ConsistencyView', 'ReingestProposalView', 'ContractError'],
    forbidden_exposure: FORBIDDEN_EXPOSURE_KEYS.concat(['ssh', 'sql', 'docker', 'causal_graph.jsonl']),
  };
}
