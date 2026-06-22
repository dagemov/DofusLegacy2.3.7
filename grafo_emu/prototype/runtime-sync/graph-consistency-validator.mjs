#!/usr/bin/env node
/** F24 Graph Consistency Validator — cross-check F21 edges vs runtime change classification */
import {
  edgesForEntity,
  readCausalEdges,
  relTypesForEntity,
} from './_sync-lib.mjs';

function consistencyVerdict(classification, entityEdges) {
  if (classification.impact_class === 'metadata') return 'CONSISTENT_TOPOLOGY';
  if (classification.impact_class === 'edge_props') return 'PROPS_STALE';
  if (classification.impact_class === 'structural') return 'TOPOLOGY_STALE';
  if (classification.graph_requires_update) return 'PROPS_STALE';
  return 'CONSISTENT_TOPOLOGY';
}

export function validateGraphConsistency(diffEntries, causalEdges = readCausalEdges()) {
  const entityReports = [];

  for (const entry of diffEntries) {
    const entity = entry.entity;
    const entityEdges = edgesForEntity(causalEdges, entity);
    const relTypes = relTypesForEntity(causalEdges, entity);
    const verdict = consistencyVerdict(entry, entityEdges);

    const edgesPotentiallyInvalid = [];
    if (entry.graph_requires_update && entry.affected_edges?.length) {
      for (const edge of entityEdges) {
        if (entry.affected_edges.includes(edge.rel)) {
          edgesPotentiallyInvalid.push({
            rel: edge.rel,
            src: edge.src,
            dst: edge.dst,
            provenance_ref: edge.provenance?.ref || edge.provenance?.source,
            reason: `${entry.impact_class} change on ${entry.table}`,
          });
        }
      }
    }

    const graphUpdateProposal = [];
    if (entry.graph_requires_update) {
      graphUpdateProposal.push({
        action: 'propose_reingest',
        entity,
        reason: entry.impact_class,
        affected_rel_types: entry.affected_edges,
        recovery_required: entry.recovery_required,
        note: 'NOT APPLIED — proposal only',
      });
    } else {
      graphUpdateProposal.push({
        action: 'no_graph_update',
        entity,
        reason: 'metadata-only or topology unchanged',
        edges_checked: entityEdges.length,
        note: 'Graph topology remains valid',
      });
    }

    entityReports.push({
      entity,
      mode: entry.mode,
      consistency_verdict: verdict,
      graph_requires_update: entry.graph_requires_update,
      causal_recompute_required: entry.causal_recompute_required,
      edges_checked: entityEdges.length,
      rel_types_present: relTypes,
      edges_potentially_invalid: edgesPotentiallyInvalid,
      graph_update_proposal: graphUpdateProposal,
      recovery_required: entry.recovery_required || [],
      invalidated_artifacts: entry.invalidated_artifacts || [],
    });
  }

  return {
    phase: 'GRAPH_CONSISTENCY',
    timestamp: new Date().toISOString(),
    entity_count: entityReports.length,
    reports: entityReports,
    summary: {
      consistent_topology: entityReports.filter((r) => r.consistency_verdict === 'CONSISTENT_TOPOLOGY').length,
      props_stale: entityReports.filter((r) => r.consistency_verdict === 'PROPS_STALE').length,
      topology_stale: entityReports.filter((r) => r.consistency_verdict === 'TOPOLOGY_STALE').length,
    },
  };
}

if (process.argv[1]?.includes('graph-consistency-validator')) {
  const { collectRuntimeEvents } = await import('./collect-runtime-events.mjs');
  const { buildWorldDiff } = await import('./world-diff-engine.mjs');
  const diff = buildWorldDiff(collectRuntimeEvents());
  const report = validateGraphConsistency(diff.real_events);
  console.log(JSON.stringify(report.summary, null, 2));
}
