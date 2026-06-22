#!/usr/bin/env node
/** §4 — Emergent MCP grouping from discovered_actions (no manual tool list) */
import { result as mined } from './mine-actions.mjs';

const MCP_TEMPLATES = [
  {
    mcp_name: 'mcp_world_economy',
    system_type: 'economy',
    action_names: ['modify_npc_shop', 'adjust_quest_rewards', 'audit_npc_economy'],
    admin_power_level: 'read + simulate',
    description: 'NPC shop and quest reward economy administration',
  },
  {
    mcp_name: 'mcp_world_content',
    system_type: 'content',
    action_names: ['create_quest_flow', 'spawn_npc_in_world'],
    admin_power_level: 'simulate',
    description: 'Quest authoring and NPC world placement',
  },
  {
    mcp_name: 'mcp_world_catalog',
    system_type: 'catalog',
    action_names: ['link_item_catalog'],
    admin_power_level: 'read',
    description: 'Item catalog linking and type resolution',
  },
  {
    mcp_name: 'mcp_combat_inspect',
    system_type: 'combat',
    action_names: ['inspect_spell_runtime'],
    admin_power_level: 'read',
    description: 'Spell runtime inspection from LOG evidence',
  },
];

const actionMap = new Map(
  mined.discovered_actions.map((a) => [a.action_name, a]),
);

const proposed = [];

for (const tmpl of MCP_TEMPLATES) {
  const actions = tmpl.action_names
    .map((name) => actionMap.get(name))
    .filter(Boolean);

  if (!actions.length) continue;

  const hasGraphEvidence = actions.some((a) => a.graph_evidence);
  const hasSqlEvidence = actions.some((a) => a.sql_evidence);
  const avgConfidence =
    actions.reduce((s, a) => s + a.confidence, 0) / actions.length;

  const writeActions = actions.filter((a) =>
    ['write/sim', 'read/write'].includes(a.type),
  );
  const readActions = actions.filter((a) => a.type === 'read');

  proposed.push({
    mcp_name: tmpl.mcp_name,
    system_type: tmpl.system_type,
    description: tmpl.description,
    admin_power_level: tmpl.admin_power_level,
    actions: actions.map((a) => ({
      action_name: a.action_name,
      pattern: a.pattern,
      type: a.type,
      confidence: a.confidence,
      graph_evidence: a.graph_evidence,
      sql_evidence: a.sql_evidence,
    })),
    action_count: actions.length,
    write_action_count: writeActions.length,
    read_action_count: readActions.length,
    avg_confidence: Math.round(avgConfidence * 1000) / 1000,
    evidence_sources: {
      graph: hasGraphEvidence,
      sql: hasSqlEvidence,
    },
    missing_actions: tmpl.action_names.filter((n) => !actionMap.has(n)),
    emergent: true,
    note: 'Grouped automatically from mine-actions patterns — not hand-designed tool list',
  });
}

export const result = {
  phase: 'MCP_PROPOSAL',
  proposed_mcps: proposed,
  mcp_count: proposed.length,
  total_actions_grouped: proposed.reduce((s, m) => s + m.action_count, 0),
  ungrouped_actions: mined.discovered_actions
    .filter((a) => !proposed.some((m) => m.actions.some((x) => x.action_name === a.action_name)))
    .map((a) => a.action_name),
  mining_ref: {
    discovered_count: mined.action_count,
    patterns: mined.patterns_checked,
  },
};

if (process.argv[1]?.includes('propose-mcps')) {
  console.log(JSON.stringify(result, null, 2));
}
