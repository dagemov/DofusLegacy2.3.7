#!/usr/bin/env node
/** Phase G — MCP emergence from discovered systems (not doc 16/17 reuse) */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from './_model-lib.mjs';
import { result as systems } from './discover-systems.mjs';
import { result as operations } from './discover-operations.mjs';
import { result as queries } from './discover-queries.mjs';
import { result as automation } from './automation-eval.mjs';

function dominantAutomationForSystem(systemId) {
  const evals = automation.evaluations.filter((e) => e.system_id === systemId);
  if (!evals.length) return 'READ_ONLY';
  const levels = evals.map((e) => e.automation_level);
  if (levels.every((l) => l === 'READ_ONLY')) return 'READ_ONLY';
  if (levels.some((l) => l === 'PARTIALLY_AUTOMATABLE')) return 'PARTIALLY_AUTOMATABLE';
  if (levels.some((l) => l === 'SIMULATABLE')) return 'SIMULATABLE';
  return 'READ_ONLY';
}

const topSystems = systems.systems
  .filter((s) => s.table_count >= 1 && !s.hypothesis)
  .slice(0, 12);

const emergentMcps = topSystems.map((sys) => {
  const sysOps = operations.operations.filter((o) => o.system_id === sys.system_id);
  const sysQueries = queries.queries.filter((q) =>
    q.evidence_source?.system_id === sys.system_id ||
    (q.evidence_source?.table && sys.tables_involved.includes(q.evidence_source.table)),
  );
  const opTypes = [...new Set(sysOps.map((o) => o.operation_type))];
  const readOps = sysOps.filter((o) => o.operation_type === 'read_entity').length;
  const writeOps = sysOps.filter((o) => ['create_entity', 'modify_entity', 'link_entity', 'place_entity'].includes(o.operation_type)).length;

  let powerLevel = 'read';
  if (writeOps > 0 && readOps > 0) powerLevel = 'read + simulate';
  else if (writeOps > 0) powerLevel = 'simulate';

  return {
    mcp_name: `mcp_emergent_${sys.system_id}`,
    emerged_from: sys.system_id,
    system_label: sys.label,
    systems_covered: [sys.system_id],
    tables_covered: sys.tables_involved,
    operations_covered: sysOps.slice(0, 10).map((o) => o.operation_name),
    operation_count: sysOps.length,
    operation_types: opTypes,
    questions_supported: sysQueries.slice(0, 8).map((q) => q.question_template),
    question_count: sysQueries.length,
    automation_level: dominantAutomationForSystem(sys.system_id),
    admin_power_level: powerLevel,
    cohesion: sys.cohesion,
    evidence_strength: sys.relations_observed,
    emergent: true,
    note: 'Derived from Phase B system — not copied from docs 16/17 MCP names',
  };
})
  .filter((m) => m.operation_count > 0 || m.question_count > 0)
  .sort((a, b) => b.evidence_strength - a.evidence_strength)
  .slice(0, 8);

export const result = {
  phase: 'G_MCP_EMERGENCE',
  mcp_count: emergentMcps.length,
  emergent_mcps: emergentMcps,
  methodology: 'one MCP candidate per high-cohesion discovered system with ops+queries',
  not_reused_from_doc_16_17: true,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'mcp_emergence.json'), result);

if (process.argv[1]?.includes('mcp-emergence')) {
  console.log(JSON.stringify({ mcp_count: result.mcp_count, names: emergentMcps.map((m) => m.mcp_name) }, null, 2));
}
