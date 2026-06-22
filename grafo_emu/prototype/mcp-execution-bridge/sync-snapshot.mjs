import { join } from 'node:path';
import { writeJson } from './_bridge-lib.mjs';

/**
 * Post-execution: fetch row snapshot and emit re-ingestion proposal (no graph mutation).
 */
export function buildRuntimeChangeEvent({
  intentId,
  targetNode,
  table,
  rowId,
  before,
  after,
}) {
  return {
    event_type: 'runtime_change_proposal',
    intent_id: intentId,
    target_node: targetNode,
    table,
    row_id: String(rowId),
    before: before || {},
    after: after || {},
    graph_reingest: 'manual — run Phase 20 ingest (NOT auto)',
    timestamp: new Date().toISOString(),
  };
}

export function writeRuntimeChangeEvent(outDir, runId, event) {
  const path = join(outDir, runId, 'runtime_change_event.json');
  writeJson(path, event);
  return path;
}
