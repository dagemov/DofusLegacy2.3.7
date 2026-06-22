#!/usr/bin/env node
/** F24 Classification Benchmark — npc:462 simulated predictions only (no fake runtime) */
import {
  loadF22Artifacts,
  findPlanAndValidation,
  readCausalEdges,
  edgesForEntity,
  relTypesForEntity,
  invalidatedArtifactsForPhases,
  classifyTableChange,
} from './_sync-lib.mjs';

const BENCHMARK_ENTITY = 'npc:462';
const HYPOTHETICAL_TABLE = 'npcs_items';
const HYPOTHETICAL_COLUMNS = ['Item', 'Price'];

export function buildClassificationBenchmark(causalEdges = readCausalEdges()) {
  const f22 = loadF22Artifacts();
  const { plan, validation } = findPlanAndValidation(f22, 'modify_npc', BENCHMARK_ENTITY);

  const entityEdges = edgesForEntity(causalEdges, BENCHMARK_ENTITY);
  const relTypes = relTypesForEntity(causalEdges, BENCHMARK_ENTITY);
  const classification = classifyTableChange(HYPOTHETICAL_TABLE, HYPOTHETICAL_COLUMNS, 'UPDATE');

  const predictedEdges = [...new Set([
    ...classification.affected_edges,
    ...relTypes.filter((r) => ['SPAWNED_IN', 'STARTS_QUEST', 'INVOLVES_NPC', 'SELLS'].includes(r)),
  ])].sort();

  const recoveryPhases = ['Phase20', 'Phase21'];

  return {
    mode: 'simulated',
    write_executed: false,
    runtime_snapshot: null,
    entity: BENCHMARK_ENTITY,
    source: 'f22_classification_benchmark',
    f22_plan: plan ? {
      intent_id: plan.intent_id,
      target_node: plan.target_node,
      cs_manager: plan.mutation_plan?.cs_manager,
    } : null,
    f22_blast_radius: validation?.blast_radius_total ?? null,
    f22_verdict: validation?.verdict ?? null,
    hypothetical_change: {
      table: HYPOTHETICAL_TABLE,
      operation: 'UPDATE',
      columns: HYPOTHETICAL_COLUMNS,
      note: 'classification benchmark only — not executed',
    },
    graph_requires_update: true,
    causal_recompute_required: true,
    predicted_affected_edges: predictedEdges,
    predicted_recovery_phases: recoveryPhases,
    edges_in_current_graph: entityEdges.length,
    current_rel_types: relTypes,
    sample_edges: entityEdges.slice(0, 8).map((e) => ({
      rel: e.rel,
      src: e.src,
      dst: e.dst,
      ref: e.provenance?.ref,
    })),
    invalidated_artifacts: invalidatedArtifactsForPhases(recoveryPhases),
    constraints: [
      'NO writes',
      'NO fake runtime_change_event',
      'NO fake execution_trace',
      'predictions only',
    ],
  };
}

if (process.argv[1]?.includes('classification-benchmark')) {
  const bench = buildClassificationBenchmark();
  console.log(JSON.stringify({
    entity: bench.entity,
    predicted_affected_edges: bench.predicted_affected_edges,
    predicted_recovery_phases: bench.predicted_recovery_phases,
  }, null, 2));
}
