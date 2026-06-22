#!/usr/bin/env node
/** F28 — decision engine: rules from F26 error codes + F25 state + F24 consistency only */
import { allowsCommit, isExplainOnly } from './tool-policy.mjs';

export function decideAfterBegin(parsedIntent, response) {
  if (!response.success) {
    return { action: 'STOP', reason: response.error?.error_code || 'BEGIN_FAILED' };
  }
  return { action: 'EXPLAIN', reason: 'proceed_to_explain' };
}

export function decideAfterExplain(parsedIntent, response) {
  if (!response.success) {
    return { action: 'STOP', reason: response.error?.error_code || 'EXPLAIN_FAILED' };
  }
  const impact = response.result;

  if (isExplainOnly(parsedIntent)) {
    return {
      action: 'STOP',
      reason: 'explain_only',
      consistency_verdict: impact.consistency_verdict,
      reingest_required: impact.reingest_required,
      recovery_required: impact.recovery_required || [],
    };
  }

  if (impact.validation_verdict === 'REVIEW') {
    return { action: 'STOP', reason: 'REVIEW' };
  }

  if (impact.validation_verdict === 'BLOCK' && allowsCommit(parsedIntent)) {
    return { action: 'COMMIT', reason: 'BLOCK_ATTEMPT_COMMIT_FOR_F26_ERROR' };
  }

  if (impact.validation_verdict === 'BLOCK') {
    return { action: 'STOP', reason: 'BLOCK', blast_radius: impact.blast_radius };
  }

  if (impact.validation_verdict === 'APPROVE' && allowsCommit(parsedIntent)) {
    return { action: 'COMMIT', reason: 'APPROVE' };
  }

  return { action: 'STOP', reason: 'NO_COMMIT_PATH' };
}

export function decideAfterCommit(parsedIntent, response, priorRollbackAvailable) {
  if (response.success) {
    return {
      action: 'DONE',
      reason: 'COMMITTED',
      state: response.result?.state,
      rollback_available: response.result?.rollback_available,
    };
  }

  const code = response.error?.error_code;
  if (code === 'BLOCKED_BY_BLAST_RADIUS' || code === 'BLOCKED_BY_MODIFICATION_RISK') {
    return { action: 'STOP', reason: code };
  }
  if (code === 'VALIDATION_REVIEW_REQUIRED') {
    return { action: 'STOP', reason: code };
  }
  if (priorRollbackAvailable && parsedIntent.intent_type === 'rollback') {
    return { action: 'ROLLBACK', reason: 'commit_failed_rollback' };
  }
  return { action: 'STOP', reason: code || 'COMMIT_FAILED' };
}

export function decideAfterRollback(response) {
  if (!response.success) {
    return { action: 'STOP', reason: response.error?.error_code || 'ROLLBACK_FAILED' };
  }
  return {
    action: 'DONE',
    reason: 'ROLLED_BACK',
    state: response.result?.state,
    parent_transaction_id: response.result?.parent_transaction_id,
  };
}
