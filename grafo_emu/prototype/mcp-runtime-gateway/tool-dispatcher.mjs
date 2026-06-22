#!/usr/bin/env node
/** F27 — dispatch gateway requests to F26 projections over F25 bundle (read-only) */
import {
  findTransactionById,
  findTransactionByIntent,
  findRollbackChild,
  projectTransaction,
  projectImpact,
  projectConsistency,
  projectReingestProposal,
  makeContractError,
  mapValidationToError,
} from './_gateway-lib.mjs';
import { validateRequest } from './request-validator.mjs';
import { projectGatewayResponse } from './response-projector.mjs';

function resolveByTxnOrIntent(bundle, args) {
  if (args.transaction_id) {
    return findTransactionById(bundle, args.transaction_id);
  }
  if (args.intent_id && args.target_node) {
    return findTransactionByIntent(bundle, args.intent_id, args.target_node);
  }
  return null;
}

function handleBeginTransaction(bundle, args) {
  const txn = findTransactionByIntent(bundle, args.intent_id, args.target_node);
  if (!txn) {
    return makeContractError(
      'TRANSACTION_NOT_FOUND',
      `No transaction for ${args.intent_id} on ${args.target_node}`,
    );
  }
  return projectTransaction(txn);
}

function handleExplainImpact(bundle, args) {
  const txn = resolveByTxnOrIntent(bundle, args);
  if (!txn) {
    return makeContractError(
      'TRANSACTION_NOT_FOUND',
      'Transaction not found for explainImpact',
    );
  }
  return projectImpact(txn);
}

function handleGetTransaction(bundle, args) {
  const txn = findTransactionById(bundle, args.transaction_id);
  if (!txn) {
    return makeContractError('TRANSACTION_NOT_FOUND', `Unknown transaction ${args.transaction_id}`, args.transaction_id);
  }
  return projectTransaction(txn);
}

function handleListTransactions(bundle, args) {
  let txns = bundle.transactions || [];
  if (args.state) {
    txns = txns.filter((t) => t.lifecycle?.current_state === args.state);
  }
  return txns.map(projectTransaction);
}

function handleCommitTransaction(bundle, args) {
  const txn = findTransactionById(bundle, args.transaction_id);
  if (!txn) {
    return makeContractError('TRANSACTION_NOT_FOUND', `Unknown transaction ${args.transaction_id}`, args.transaction_id);
  }

  if (args.confirm !== true) {
    return makeContractError(
      'REQUIRES_HUMAN_CONFIRMATION',
      'commitTransaction requires confirm:true',
      args.transaction_id,
    );
  }

  const state = txn.lifecycle?.current_state;
  if (state === 'BLOCKED' || txn.validation?.verdict === 'BLOCK' || txn.validation?.verdict === 'REVIEW') {
    return makeContractError(
      mapValidationToError(txn),
      `Cannot commit transaction in state ${state}`,
      args.transaction_id,
    );
  }

  if (state === 'ROLLBACK_AVAILABLE' || state === 'COMMITTED') {
    return projectTransaction(txn);
  }

  if (state === 'READY_TO_COMMIT' || txn.validation?.verdict === 'APPROVE') {
    const view = projectTransaction(txn);
    return {
      ...view,
      state: 'ROLLBACK_AVAILABLE',
      states_reached: [...new Set([...(view.states_reached || []), 'COMMITTED', 'ROLLBACK_AVAILABLE'])],
      rollback_available: true,
    };
  }

  return makeContractError(
    'INVALID_STATE_TRANSITION',
    `Cannot commit transaction in state ${state}`,
    args.transaction_id,
  );
}

function handleRollbackTransaction(bundle, args) {
  const parent = findTransactionById(bundle, args.transaction_id);
  if (!parent) {
    return makeContractError('TRANSACTION_NOT_FOUND', `Unknown transaction ${args.transaction_id}`, args.transaction_id);
  }

  const child = findRollbackChild(bundle, args.transaction_id);
  if (!child) {
    return makeContractError(
      'ROLLBACK_NOT_AVAILABLE',
      `No rollback child for ${args.transaction_id}`,
      args.transaction_id,
    );
  }

  return projectTransaction(child);
}

function handleGetReingestProposal(bundle, args) {
  const txn = findTransactionById(bundle, args.transaction_id);
  if (!txn) {
    return makeContractError('TRANSACTION_NOT_FOUND', `Unknown transaction ${args.transaction_id}`, args.transaction_id);
  }

  const proposal = projectReingestProposal(txn);
  if (!proposal) {
    return makeContractError(
      'REINGEST_REQUIRED',
      'No reingest proposal for this transaction',
      args.transaction_id,
    );
  }

  return proposal;
}

function handleGetTransactionConsistency(bundle, args) {
  const txn = findTransactionById(bundle, args.transaction_id);
  if (!txn) {
    return makeContractError('TRANSACTION_NOT_FOUND', `Unknown transaction ${args.transaction_id}`, args.transaction_id);
  }

  const consistency = projectConsistency(txn);
  if (!consistency) {
    return makeContractError(
      'INVALID_STATE_TRANSITION',
      'Consistency not available for this transaction',
      args.transaction_id,
    );
  }

  return consistency;
}

const HANDLERS = {
  beginTransaction: handleBeginTransaction,
  explainImpact: handleExplainImpact,
  getTransaction: handleGetTransaction,
  listTransactions: handleListTransactions,
  commitTransaction: handleCommitTransaction,
  rollbackTransaction: handleRollbackTransaction,
  getReingestProposal: handleGetReingestProposal,
  getTransactionConsistency: handleGetTransactionConsistency,
};

export function dispatchTool(request, bundle, registry) {
  const validationError = validateRequest(request, registry);
  if (validationError) {
    return projectGatewayResponse(request, validationError);
  }

  const handler = HANDLERS[request.tool_name];
  const result = handler(bundle, request.arguments);
  return projectGatewayResponse(request, result);
}

export function invokeGateway(request, bundle, registry) {
  return dispatchTool(request, bundle, registry);
}
