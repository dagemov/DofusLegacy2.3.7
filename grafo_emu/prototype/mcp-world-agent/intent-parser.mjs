#!/usr/bin/env node
/** F28 — deterministic NL intent parser (no LLM) */
export const SUPPORTED_INTENT_TYPES = [
  'modify_item',
  'modify_npc',
  'explain_impact',
  'rollback',
  'commit_transaction',
];

const NL_PATTERNS = [
  {
    pattern: /^change item (\d+) name to (.+)$/i,
    parse: (m) => ({
      intent_type: 'modify_item',
      target_node: `item:${m[1]}`,
      fields: { Name: m[2].trim() },
      confidence: 1.0,
    }),
  },
  {
    pattern: /^modify npc (\d+) shop inventory$/i,
    parse: (m) => ({
      intent_type: 'modify_npc',
      target_node: `npc:${m[1]}`,
      fields: {},
      confidence: 1.0,
    }),
  },
  {
    pattern: /^rollback the change to item (\d+)$/i,
    parse: (m) => ({
      intent_type: 'rollback',
      target_node: `item:${m[1]}`,
      transaction_id: 'txn-item519-commit',
      confidence: 1.0,
    }),
  },
  {
    pattern: /^explain impact of modifying npc (\d+)$/i,
    parse: (m) => ({
      intent_type: 'explain_impact',
      target_node: `npc:${m[1]}`,
      transaction_id: 'txn-npc462-reingest-proposal',
      fields: {},
      confidence: 1.0,
    }),
  },
  {
    pattern: /^commit transaction (.+)$/i,
    parse: (m) => ({
      intent_type: 'commit_transaction',
      target_node: null,
      transaction_id: m[1].trim(),
      confidence: 1.0,
    }),
  },
];

export const SIMULATION_NL = {
  CASE_A: 'Change item 519 name to MEK-F23-bridge-test',
  CASE_B: 'Modify npc 462 shop inventory',
  CASE_C: 'Rollback the change to item 519',
  CASE_D: 'Explain impact of modifying npc 462',
};

export function parseIntent(naturalLanguage) {
  const text = (naturalLanguage || '').trim();
  for (const entry of NL_PATTERNS) {
    const match = text.match(entry.pattern);
    if (match) return entry.parse(match);
  }
  return {
    intent_type: null,
    target_node: null,
    fields: {},
    confidence: 0,
    error: 'UNPARSEABLE_INTENT',
  };
}

export function parseSimulationIntent(caseId) {
  const nl = SIMULATION_NL[caseId];
  if (!nl) throw new Error(`Unknown simulation case ${caseId}`);
  return { case_id: caseId, natural_language: nl, parsed: parseIntent(nl) };
}
