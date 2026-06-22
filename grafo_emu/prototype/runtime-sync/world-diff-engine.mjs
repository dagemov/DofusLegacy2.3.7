#!/usr/bin/env node
/** F24 World Diff Engine — classify runtime changes vs graph impact */
import {
  classifyTableChange,
  invalidatedArtifactsForPhases,
  edgesForEntity,
  readCausalEdges,
  relTypesForEntity,
} from './_sync-lib.mjs';

export function buildWorldDiff(events, causalEdges = readCausalEdges()) {
  const realEvents = [];
  const netByEntity = new Map();

  for (const event of events) {
    const cols = event.columns_changed || [];
    const classification = classifyTableChange(event.table, cols, 'UPDATE');
    const entityEdges = edgesForEntity(causalEdges, event.entity);
    const edgeRelTypes = relTypesForEntity(causalEdges, event.entity);

    const entry = {
      mode: 'real',
      source: event.source,
      run_id: event.run_id,
      changed: cols.length > 0,
      entity: event.entity,
      intent_id: event.intent_id,
      table: event.table,
      columns_changed: cols,
      runtime_before: event.runtime_before,
      runtime_after: event.runtime_after,
      impact_class: classification.impact_class,
      graph_requires_update: classification.graph_requires_update,
      causal_recompute_required: classification.causal_recompute_required,
      affected_edges: classification.affected_edges,
      edges_in_graph: entityEdges.length,
      edge_rel_types_in_graph: edgeRelTypes,
      recovery_required: classification.recovery_required,
      invalidated_artifacts: invalidatedArtifactsForPhases(classification.recovery_required),
      write_executed: event.write_executed,
      timestamp: event.timestamp,
      notes: [],
    };

    if (classification.impact_class === 'metadata') {
      entry.notes.push(`${cols.join(', ')} is metadata-only on ${event.table}`);
      if (edgeRelTypes.length) {
        entry.notes.push(`${edgeRelTypes.length} rel types in graph remain valid by entity Id`);
      }
    }

    if (event.intent_id === 'modify_item' && event.entity === 'item:519') {
      entry.notes.push('Name is metadata-only; DROPS_ITEM edges remain valid by item Id');
    }

    realEvents.push(entry);

    const key = event.entity;
    if (!netByEntity.has(key)) {
      netByEntity.set(key, { entity: key, columns: new Set(), before: {}, after: {} });
    }
    const net = netByEntity.get(key);
    for (const c of cols) net.columns.add(c);
    Object.assign(net.before, event.runtime_before);
    Object.assign(net.after, event.runtime_after);
  }

  const netChanges = [...netByEntity.values()].map((n) => {
    const entityEvents = events.filter((e) => e.entity === n.entity);
    const cols = [...new Set(entityEvents.flatMap((e) => e.columns_changed || []))];
    const first = entityEvents[0];
    const last = entityEvents[entityEvents.length - 1];
    const beforeVals = {};
    const afterVals = {};
    for (const c of cols) {
      beforeVals[c] = first?.runtime_before?.[c];
      afterVals[c] = last?.runtime_after?.[c];
    }
    const reverted = cols.every((c) => beforeVals[c] === afterVals[c]);
    return { entity: n.entity, columns: cols, net_changed: !reverted, runtime_before: beforeVals, runtime_after: afterVals };
  });

  const topologyChanges = realEvents.filter((e) => e.graph_requires_update && e.impact_class === 'structural').length;
  const metadataOnly = realEvents.filter((e) => e.impact_class === 'metadata').length;

  return {
    phase: 'WORLD_DIFF',
    timestamp: new Date().toISOString(),
    events_processed: events.length,
    real_events: realEvents,
    net_changes: netChanges,
    summary: {
      real_changes: events.length,
      topology_changes: topologyChanges,
      metadata_only_changes: metadataOnly,
      entities_touched: [...new Set(events.map((e) => e.entity))],
    },
  };
}

if (process.argv[1]?.includes('world-diff-engine')) {
  const { collectRuntimeEvents } = await import('./collect-runtime-events.mjs');
  const report = buildWorldDiff(collectRuntimeEvents());
  console.log(JSON.stringify(report.summary, null, 2));
}
