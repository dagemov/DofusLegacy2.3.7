#!/usr/bin/env node
/** F27 — tool registry sourced exclusively from F26 TOOL_SURFACE */
import { join } from 'node:path';
import {
  GATEWAY_DIR,
  GATEWAY_PHASE,
  GATEWAY_VERSION,
  TOOL_SURFACE,
  PERMISSION_MODEL,
  TOOL_OUTPUT_TYPES,
  TOOL_REQUIRED_ARGS,
  resolveRolesForTool,
  writeJson,
} from './_gateway-lib.mjs';

export function assertExactlyEight(registry) {
  const names = Object.keys(registry.tools);
  if (names.length !== 8) {
    throw new Error(`Registry must contain exactly 8 tools, got ${names.length}`);
  }
  for (let i = 0; i < TOOL_SURFACE.length; i += 1) {
    if (names[i] !== TOOL_SURFACE[i]) {
      throw new Error(`Tool order mismatch at index ${i}: expected ${TOOL_SURFACE[i]}, got ${names[i]}`);
    }
  }
  return true;
}

export function buildToolRegistry() {
  const tools = {};
  for (const toolName of TOOL_SURFACE) {
    tools[toolName] = {
      name: toolName,
      source: 'F26_TOOL_SURFACE',
      output_type: TOOL_OUTPUT_TYPES[toolName],
      required_args: TOOL_REQUIRED_ARGS[toolName],
      allowed_roles: resolveRolesForTool(toolName, PERMISSION_MODEL),
    };
  }

  const registry = {
    phase: GATEWAY_PHASE,
    version: GATEWAY_VERSION,
    tool_count: TOOL_SURFACE.length,
    tool_names: [...TOOL_SURFACE],
    tools,
    permission_model_source: 'F26_PERMISSION_MODEL',
  };

  assertExactlyEight(registry);
  return registry;
}

export function emitToolRegistry() {
  const registry = buildToolRegistry();
  writeJson(join(GATEWAY_DIR, 'tool-registry.json'), registry);
  return registry;
}

if (process.argv[1]?.includes('tool-registry')) {
  const registry = emitToolRegistry();
  console.log(JSON.stringify({
    tool_count: registry.tool_count,
    tool_names: registry.tool_names,
  }, null, 2));
}
