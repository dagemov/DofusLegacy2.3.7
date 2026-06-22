#!/usr/bin/env node
/** F27 — validate gateway requests; rejections use F26 error codes only */
import {
  makeContractError,
  TOOL_REQUIRED_ARGS,
  WORLD_TRANSACTION_STATES,
} from './_gateway-lib.mjs';

function missingArgs(toolName, args) {
  const required = TOOL_REQUIRED_ARGS[toolName] || [];
  return required.filter((key) => {
    if (key === 'confirm') return args.confirm !== true;
    return args[key] === undefined || args[key] === null || args[key] === '';
  });
}

function validateExplainImpactArgs(args) {
  const hasTxn = typeof args.transaction_id === 'string' && args.transaction_id.length > 0;
  const hasIntent = typeof args.intent_id === 'string' && args.intent_id.length > 0;
  const hasTarget = typeof args.target_node === 'string' && args.target_node.length > 0;
  if (hasTxn) return [];
  if (hasIntent && hasTarget) return [];
  return ['transaction_id OR intent_id+target_node'];
}

export function validateRequest(request, registry) {
  if (!request || typeof request !== 'object') {
    return makeContractError('INVALID_STATE_TRANSITION', 'Request must be an object');
  }

  const { tool_name: toolName, arguments: args = {}, caller_role: callerRole } = request;

  if (!toolName || !registry.tools[toolName]) {
    return makeContractError('INVALID_STATE_TRANSITION', `Unknown tool: ${toolName}`);
  }

  if (!callerRole || !registry.tools[toolName].allowed_roles.includes(callerRole)) {
    return makeContractError(
      'INVALID_STATE_TRANSITION',
      `Role ${callerRole} not permitted for ${toolName}`,
    );
  }

  if (toolName === 'explainImpact') {
    const missing = validateExplainImpactArgs(args);
    if (missing.length) {
      return makeContractError(
        'INVALID_STATE_TRANSITION',
        `Missing required arguments for ${toolName}: ${missing.join(', ')}`,
      );
    }
    return null;
  }

  const missing = missingArgs(toolName, args);
  if (missing.length) {
    return makeContractError(
      'INVALID_STATE_TRANSITION',
      `Missing required arguments for ${toolName}: ${missing.join(', ')}`,
    );
  }

  if (toolName === 'listTransactions' && args.state !== undefined) {
    if (!WORLD_TRANSACTION_STATES.includes(args.state)) {
      return makeContractError(
        'INVALID_STATE_TRANSITION',
        `Invalid state filter: ${args.state}`,
      );
    }
  }

  return null;
}
