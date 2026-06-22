#!/usr/bin/env node
/** F24 Re-Ingest Planner — aggregate recovery proposals (read-only) */
import { invalidatedArtifactsForPhases } from './_sync-lib.mjs';

export function buildReIngestPlan(worldDiff, consistencyReport, benchmark) {
  const phaseSet = new Set();
  const entityPlans = [];

  for (const report of consistencyReport.reports || []) {
    for (const phase of report.recovery_required || []) phaseSet.add(phase);
    if (report.recovery_required?.length) {
      entityPlans.push({
        entity: report.entity,
        mode: report.mode,
        recovery_required: report.recovery_required,
        invalidated_artifacts: report.invalidated_artifacts,
        graph_update_proposal: report.graph_update_proposal,
      });
    }
  }

  if (benchmark?.predicted_recovery_phases) {
    for (const phase of benchmark.predicted_recovery_phases) phaseSet.add(phase);
    entityPlans.push({
      entity: benchmark.entity,
      mode: 'simulated',
      recovery_required: benchmark.predicted_recovery_phases,
      invalidated_artifacts: benchmark.invalidated_artifacts,
      graph_update_proposal: [{
        action: 'propose_reingest_if_executed',
        entity: benchmark.entity,
        note: 'Benchmark prediction — no runtime write occurred',
      }],
    });
  }

  const phases = [...phaseSet].sort();
  const allInvalidated = invalidatedArtifactsForPhases(phases);

  const realNeedsReingest = (consistencyReport.reports || []).some(
    (r) => r.mode === 'real' && (r.recovery_required?.length || 0) > 0,
  );

  return {
    phase: 'REINGEST_PLAN',
    timestamp: new Date().toISOString(),
    auto_sync: false,
    graph_mutation: false,
    real_writes_require_reingest: realNeedsReingest,
    recovery_phases_required: phases,
    invalidated_artifacts: allInvalidated,
    entity_plans: entityPlans,
    rerun_commands: [
      { phase: 'Phase20', cwd: 'grafo_emu/prototype/world-relations', command: 'node run-relations.mjs' },
      { phase: 'Phase21', cwd: 'grafo_emu/prototype/world-causal', command: 'node run-causal.mjs' },
    ].filter((r) => phases.includes(r.phase)),
    world_diff_summary: worldDiff.summary,
    consistency_summary: consistencyReport.summary,
    benchmark_entity: benchmark?.entity || null,
  };
}

if (process.argv[1]?.includes('reingest-planner')) {
  const { collectRuntimeEvents } = await import('./collect-runtime-events.mjs');
  const { buildWorldDiff } = await import('./world-diff-engine.mjs');
  const { validateGraphConsistency } = await import('./graph-consistency-validator.mjs');
  const { buildClassificationBenchmark } = await import('./classification-benchmark.mjs');
  const events = collectRuntimeEvents();
  const diff = buildWorldDiff(events);
  const consistency = validateGraphConsistency(diff.real_events);
  const benchmark = buildClassificationBenchmark();
  const plan = buildReIngestPlan(diff, consistency, benchmark);
  console.log(JSON.stringify({ phases: plan.recovery_phases_required, invalidated: plan.invalidated_artifacts.length }, null, 2));
}
