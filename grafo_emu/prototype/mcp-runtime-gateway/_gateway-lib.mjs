import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  TOOL_SURFACE,
  ERROR_CODES,
  PERMISSION_MODEL,
  WORLD_TRANSACTION_STATES,
  STATE_TOOL_MATRIX,
  CONTRACT_DIR,
  TXN_DIR,
  writeJson,
  hashString,
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
} from '../mcp-tool-contract/_contract-lib.mjs';

export {
  TOOL_SURFACE,
  ERROR_CODES,
  PERMISSION_MODEL,
  WORLD_TRANSACTION_STATES,
  STATE_TOOL_MATRIX,
  CONTRACT_DIR,
  TXN_DIR,
  writeJson,
  hashString,
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
};

export const GATEWAY_DIR = dirname(fileURLToPath(import.meta.url));
export const GATEWAY_VERSION = 'v1';
export const GATEWAY_PHASE = 'MCP_RUNTIME_GATEWAY_F27';

export const FIXED_REPLAY_TIMESTAMP = '2026-06-22T08:00:00.000Z';

export const RESTRICTION_FLAGS = {
  read_only: true,
  no_mcp_server: true,
  no_json_rpc: true,
  no_stdio: true,
  no_http: true,
  no_rest: true,
  no_websocket: true,
  no_sql: true,
  no_ssh: true,
  no_docker: true,
  no_writes: true,
  no_graph_mutation: true,
  no_runtime_writes: true,
  no_agent_layer: true,
};

export const TOOL_OUTPUT_TYPES = {
  beginTransaction: 'TransactionView',
  explainImpact: 'ImpactView',
  getTransaction: 'TransactionView',
  listTransactions: 'TransactionView[]',
  commitTransaction: 'TransactionView',
  rollbackTransaction: 'TransactionView',
  getReingestProposal: 'ReingestProposalView',
  getTransactionConsistency: 'ConsistencyView',
};

export const TOOL_REQUIRED_ARGS = {
  beginTransaction: ['intent_id', 'target_node'],
  explainImpact: [],
  getTransaction: ['transaction_id'],
  listTransactions: [],
  commitTransaction: ['transaction_id', 'confirm'],
  rollbackTransaction: ['transaction_id'],
  getReingestProposal: ['transaction_id'],
  getTransactionConsistency: ['transaction_id'],
};

export function makeRequestId(prefix, index) {
  return `req-${prefix}-${index}`;
}

export function buildRequest({ request_id, tool_name, arguments: args, caller_role, timestamp }) {
  return {
    request_id,
    tool_name,
    arguments: args ?? {},
    caller_role,
    timestamp: timestamp ?? FIXED_REPLAY_TIMESTAMP,
  };
}

export function buildSuccessResponse(request_id, result) {
  return {
    request_id,
    success: true,
    result,
    error: null,
  };
}

export function buildErrorResponse(request_id, contractError) {
  return {
    request_id,
    success: false,
    result: null,
    error: {
      error_code: contractError.error_code,
      message: contractError.message,
      transaction_id: contractError.transaction_id,
    },
  };
}

export function isValidErrorCode(code) {
  return Object.prototype.hasOwnProperty.call(ERROR_CODES, code);
}

export function resolveRolesForTool(toolName, permissionModel) {
  return Object.entries(permissionModel)
    .filter(([, tools]) => tools.includes(toolName))
    .map(([role]) => role);
}

export function findTransactionByIntent(bundle, intentId, targetNode) {
  return bundle?.transactions?.find(
    (t) => t.intent_id === intentId && t.target_node === targetNode,
  ) || null;
}

export function findRollbackChild(bundle, parentTransactionId) {
  return bundle?.transactions?.find(
    (t) => t.parent_transaction_id === parentTransactionId,
  ) || null;
}

export function gatewayContractPath() {
  return join(GATEWAY_DIR, '..', 'mcp-tool-contract', 'tool-contract.json');
}
