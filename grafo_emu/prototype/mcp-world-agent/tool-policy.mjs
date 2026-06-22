#!/usr/bin/env node
/** F28 — tool policy: maps intents to F26 tools and caller roles */
export const AGENT_DECISION_TOOLS = [
  'beginTransaction',
  'explainImpact',
  'commitTransaction',
  'rollbackTransaction',
];

export const TOOL_CALLER_ROLE = {
  beginTransaction: 'planner',
  explainImpact: 'reader',
  commitTransaction: 'operator',
  rollbackTransaction: 'rollback_operator',
};

export function f26IntentId(parsedIntent) {
  if (parsedIntent.intent_type === 'modify_item') return 'modify_item';
  if (parsedIntent.intent_type === 'modify_npc') return 'modify_npc';
  if (parsedIntent.intent_type === 'explain_impact') return 'modify_npc';
  return null;
}

export function allowsCommit(parsedIntent) {
  return parsedIntent.intent_type === 'modify_item'
    || parsedIntent.intent_type === 'modify_npc'
    || parsedIntent.intent_type === 'commit_transaction';
}

export function isRollbackOnly(parsedIntent) {
  return parsedIntent.intent_type === 'rollback';
}

export function isExplainOnly(parsedIntent) {
  return parsedIntent.intent_type === 'explain_impact';
}

export function resolveTransactionId(parsedIntent, lastBeginResult) {
  if (parsedIntent.transaction_id) return parsedIntent.transaction_id;
  if (lastBeginResult?.transaction_id) return lastBeginResult.transaction_id;
  return null;
}

export function buildBeginArgs(parsedIntent) {
  const intent_id = f26IntentId(parsedIntent);
  const args = { intent_id, target_node: parsedIntent.target_node };
  if (parsedIntent.fields && Object.keys(parsedIntent.fields).length > 0) {
    args.fields = parsedIntent.fields;
  }
  return args;
}

export function buildExplainArgs(parsedIntent, transactionId) {
  if (parsedIntent.transaction_id) return { transaction_id: parsedIntent.transaction_id };
  if (transactionId) return { transaction_id: transactionId };
  return {
    intent_id: f26IntentId(parsedIntent),
    target_node: parsedIntent.target_node,
  };
}

export function buildCommitArgs(transactionId) {
  return { transaction_id: transactionId, confirm: true };
}

export function buildRollbackArgs(transactionId) {
  return { transaction_id: transactionId };
}

export function assertDecisionTool(toolName) {
  if (!AGENT_DECISION_TOOLS.includes(toolName)) {
    throw new Error(`Agent decision engine may only use: ${AGENT_DECISION_TOOLS.join(', ')}`);
  }
}
