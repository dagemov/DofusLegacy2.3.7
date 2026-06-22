#!/usr/bin/env node
/** F28 — execution loop over F27 gateway only */
import { agentCall } from './_agent-core.mjs';
import {
  AGENT_DECISION_TOOLS,
  TOOL_CALLER_ROLE,
  isRollbackOnly,
  isExplainOnly,
  buildBeginArgs,
  buildExplainArgs,
  buildCommitArgs,
  buildRollbackArgs,
  resolveTransactionId,
  assertDecisionTool,
} from './tool-policy.mjs';
import {
  decideAfterBegin,
  decideAfterExplain,
  decideAfterCommit,
  decideAfterRollback,
} from './decision-engine.mjs';

function recordStep(steps, tool, args, response, decision) {
  steps.push({ tool, arguments: args, response, decision });
}

export function runAgentLoop(ctx, parsedIntent) {
  const steps = [];
  const tools_called = [];

  if (isRollbackOnly(parsedIntent)) {
    const txnId = parsedIntent.transaction_id || 'txn-item519-commit';
    assertDecisionTool('rollbackTransaction');
    const args = buildRollbackArgs(txnId);
    const response = agentCall(ctx, 'rollbackTransaction', args, TOOL_CALLER_ROLE.rollbackTransaction);
    tools_called.push('rollbackTransaction');
    const decision = decideAfterRollback(response);
    recordStep(steps, 'rollbackTransaction', args, response, decision);
    return { terminal_decision: decision, steps, tools_called };
  }

  let txnId = parsedIntent.transaction_id || null;
  let beginResponse = null;

  if (!(isExplainOnly(parsedIntent) && txnId)) {
    assertDecisionTool('beginTransaction');
    const beginArgs = buildBeginArgs(parsedIntent);
    beginResponse = agentCall(ctx, 'beginTransaction', beginArgs, TOOL_CALLER_ROLE.beginTransaction);
    tools_called.push('beginTransaction');
    let decision = decideAfterBegin(parsedIntent, beginResponse);
    recordStep(steps, 'beginTransaction', beginArgs, beginResponse, decision);

    if (decision.action === 'STOP') {
      return { terminal_decision: decision, steps, tools_called };
    }

    txnId = resolveTransactionId(parsedIntent, beginResponse.result);
  }

  assertDecisionTool('explainImpact');
  const explainArgs = buildExplainArgs(parsedIntent, txnId);
  const explainResponse = agentCall(ctx, 'explainImpact', explainArgs, TOOL_CALLER_ROLE.explainImpact);
  tools_called.push('explainImpact');
  let decision = decideAfterExplain(parsedIntent, explainResponse);
  recordStep(steps, 'explainImpact', explainArgs, explainResponse, decision);

  if (decision.action === 'STOP') {
    return { terminal_decision: decision, steps, tools_called };
  }

  if (decision.action === 'COMMIT') {
    assertDecisionTool('commitTransaction');
    const commitArgs = buildCommitArgs(txnId);
    const commitResponse = agentCall(ctx, 'commitTransaction', commitArgs, TOOL_CALLER_ROLE.commitTransaction);
    tools_called.push('commitTransaction');
    const priorRollback = beginResponse.result?.rollback_available ?? false;
    decision = decideAfterCommit(parsedIntent, commitResponse, priorRollback);
    recordStep(steps, 'commitTransaction', commitArgs, commitResponse, decision);

    if (decision.action === 'ROLLBACK') {
      assertDecisionTool('rollbackTransaction');
      const rollbackArgs = buildRollbackArgs(txnId);
      const rollbackResponse = agentCall(ctx, 'rollbackTransaction', rollbackArgs, TOOL_CALLER_ROLE.rollbackTransaction);
      tools_called.push('rollbackTransaction');
      decision = decideAfterRollback(rollbackResponse);
      recordStep(steps, 'rollbackTransaction', rollbackArgs, rollbackResponse, decision);
    }
  }

  return { terminal_decision: decision, steps, tools_called };
}

export function assertOnlyDecisionTools(tools_called) {
  return tools_called.every((t) => AGENT_DECISION_TOOLS.includes(t));
}
