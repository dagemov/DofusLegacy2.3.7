import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadWorldTransactions,
  writeJson,
  hashString,
  assertNoForbiddenExposure,
  TOOL_SURFACE,
  buildRequest,
  makeRequestId,
  FIXED_REPLAY_TIMESTAMP,
} from '../mcp-runtime-gateway/_gateway-lib.mjs';
import { buildToolRegistry } from '../mcp-runtime-gateway/tool-registry.mjs';
import { invokeGateway } from '../mcp-runtime-gateway/tool-dispatcher.mjs';

export {
  loadWorldTransactions,
  writeJson,
  hashString,
  assertNoForbiddenExposure,
  TOOL_SURFACE,
  FIXED_REPLAY_TIMESTAMP,
};

export const AGENT_DIR = dirname(fileURLToPath(import.meta.url));
export const AGENT_PHASE = 'MCP_WORLD_AGENT_F28';
export const AGENT_VERSION = 'v1';

export const RESTRICTION_FLAGS = {
  read_only: true,
  no_mcp_server: true,
  no_json_rpc: true,
  no_http: true,
  no_llm: true,
  no_sql: true,
  no_ssh: true,
  no_docker: true,
  no_writes: true,
  no_graph_mutation: true,
  no_runtime_writes: true,
  gateway_only: true,
  no_f22_f23_f24_direct: true,
};

export function createAgentContext(casePrefix, bundle, registry) {
  return {
    prefix: `agent-${casePrefix}`,
    stepIndex: 0,
    bundle,
    registry,
    callLog: [],
  };
}

export function agentCall(ctx, tool_name, args, caller_role) {
  const request = buildRequest({
    request_id: makeRequestId(ctx.prefix, ++ctx.stepIndex),
    tool_name,
    arguments: args,
    caller_role,
    timestamp: FIXED_REPLAY_TIMESTAMP,
  });
  const response = invokeGateway(request, ctx.bundle, ctx.registry);
  ctx.callLog.push({
    via: 'invokeGateway',
    tool_name,
    request,
    response,
  });
  return response;
}

export function createDefaultContext(casePrefix) {
  const bundle = loadWorldTransactions();
  const registry = buildToolRegistry();
  return createAgentContext(casePrefix, bundle, registry);
}

export function gatewayLastRunPath() {
  return join(AGENT_DIR, '..', 'mcp-runtime-gateway', 'mcp-runtime-gateway-last-run.json');
}
