#!/usr/bin/env node
/** TEST 5 — MCP Readiness: aggregate tests 1-4 */
import { result as t1 } from './discover-clusters.mjs';
import { result as t2 } from './infer-economy.mjs';
import { result as t3 } from './relation-completeness.mjs';
import { result as t4 } from './action-simulate.mjs';

const truthCoverage = 8 / 44;

const scores = {
  discovery: t1.global_coherence_score,
  economy: t2.economy_inference_consistent ? 0.7 : 0.3,
  completeness: Math.min(1, t3.coverage_score * 10),
  action: t4.integrity_valid ? 0.6 : 0.2,
  truth_layer: truthCoverage,
};

const readinessScore =
  scores.discovery * 0.2 +
  scores.economy * 0.25 +
  scores.completeness * 0.2 +
  scores.action * 0.15 +
  scores.truth_layer * 0.2;

const blockingGaps = [
  'F1 ingestion not built — world graph absent beyond vertical slice',
  `prototype truth_coverage_minimal ${Math.round(truthCoverage * 1000) / 1000}`,
  'no MCP write path to MariaDB',
  'Admin API items-only (no NPC/quest CRUD HTTP)',
  'quest/npc/item domains LOW_TRUTH_COVERAGE in graph',
  'map/spawn/monster nodes NOT PRESENT in prototype',
];

if (t3.coverage_score < 0.05) {
  blockingGaps.push(`graph catalog coverage ${t3.coverage_score} vs DB`);
}

export const result = {
  test: 'TEST_5_MCP_READINESS',
  readiness_score: Math.round(readinessScore * 1000) / 1000,
  score_breakdown: scores,
  can_discover_systems: t1.cluster_count >= 3,
  can_generate_admin_actions: t4.integrity_valid,
  can_mutate_world_safely: false,
  blocking_gaps: blockingGaps,
  admin_without_manual_domain_knowledge: false,
  admin_without_manual_domain_note:
    'DB schema inferable and dry-run plans valid, but graph coverage and write path insufficient',
};

export const fullReport = {
  generated_at: new Date().toISOString(),
  test_1: t1,
  test_2: t2,
  test_3: t3,
  test_4: t4,
  test_5: result,
};

if (process.argv[1]?.includes('mcp-readiness')) {
  console.log(JSON.stringify(fullReport, null, 2));
}
