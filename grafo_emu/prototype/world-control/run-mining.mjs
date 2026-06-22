#!/usr/bin/env node
/** Action Discovery Layer — MCP World Mining v1 orchestrator */
import { writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { result as explore } from './explore-entities.mjs';
import { result as clusters } from './discover-clusters.mjs';
import { result as mined } from './mine-actions.mjs';
import { result as proposed } from './propose-mcps.mjs';
import { result as validated } from './validate-mcp-proposals.mjs';
import { result as economy } from './infer-economy.mjs';
import { result as completeness } from './relation-completeness.mjs';

function computeFinalVerdict() {
  const graphCoverage = completeness.coverage_score;
  const truthCoverage = 8 / 44;
  const clusterScore = clusters.global_coherence_score;
  const testCoverage = validated.test_results.coverage;
  const actionCount = mined.action_count;
  const mcpCount = proposed.mcp_count;

  const systemUnderstanding =
    clusterScore * 0.25 +
    graphCoverage * 0.2 +
    truthCoverage * 0.15 +
    testCoverage * 0.2 +
    Math.min(1, actionCount / 8) * 0.2;

  const adminPotential =
    validated.mcps_all_passed ? 0.35 : 0.25 +
    (mined.dual_source_actions / Math.max(1, actionCount)) * 0.1;

  const adjustedAdmin = Math.min(
    0.5,
    adminPotential * (validated.test_results.passed ? 1 : 0.7),
  );

  return {
    mcp_discovery_status:
      mcpCount >= 4 && validated.test_results.passed ? 'partial' : 'early',
    system_understanding: Math.round(systemUnderstanding * 1000) / 1000,
    admin_potential: Math.round(adjustedAdmin * 1000) / 1000,
    next_gap_to_close: [
      'F1 ingest maps/spawns into graph',
      'Materialize GTL for quest/npc domains',
      'MCP write path to MariaDB (v2)',
      `Graph coverage ${graphCoverage} → target >0.05`,
    ],
    autonomous_admin_agent: false,
    note:
      'Emergent actions validatable in dry-run for economy/quest/NPC spawn via SQL; full admin autonomy blocked by graph coverage and write path',
  };
}

const finalVerdict = computeFinalVerdict();

export const miningReport = {
  generated_at: new Date().toISOString(),
  pipeline: 'Action Discovery Layer — MCP World Mining v1',
  section_0_sources: {
    graph: 'grafo_emu/prototype/nodes.jsonl + edges.jsonl',
    sql: 'database/sunshine.sql',
    not_present_in_graph: explore.not_present_in_graph,
  },
  section_1_exploration: explore,
  section_2_clusters: {
    clusters: clusters.clusters_detected,
    cluster_count: clusters.cluster_count,
    global_coherence: clusters.global_coherence_score,
    map_spawn_clusters: clusters.map_spawn_clusters,
  },
  section_3_discovered_actions: mined.discovered_actions,
  section_4_proposed_mcps: proposed.proposed_mcps,
  section_5_validation: validated,
  section_6_summary: {
    cluster_count: clusters.cluster_count,
    discovered_action_count: mined.action_count,
    proposed_mcp_count: proposed.mcp_count,
    dual_source_actions: mined.dual_source_actions,
    sql_only_actions: mined.sql_only_actions,
    test_coverage: validated.test_results.coverage,
    test_passed: validated.test_results.passed,
    test_warnings: validated.test_results.warnings,
    economy_top_quest: economy.top_10_quests_by_kamas?.[0],
    graph_db_coverage: completeness.coverage_score,
  },
  section_7_final: finalVerdict,
};

const json = JSON.stringify(miningReport, null, 2);
console.log(json);

const outPath = join(dirname(fileURLToPath(import.meta.url)), 'mining-last-run.json');
writeFileSync(outPath, json, 'utf8');

process.exit(0);
