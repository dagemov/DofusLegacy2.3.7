#!/usr/bin/env node
/** Collect real F23 runtime change events (executed traces only) */
import { join } from 'node:path';
import { readdirSync, existsSync, readFileSync } from 'node:fs';
import { BRIDGE_OUT, columnsChanged } from './_sync-lib.mjs';

export function collectRuntimeEvents() {
  const events = [];
  if (!existsSync(BRIDGE_OUT)) return events;

  for (const dirent of readdirSync(BRIDGE_OUT, { withFileTypes: true })) {
    if (!dirent.isDirectory()) continue;
    const runId = dirent.name;
    const tracePath = join(BRIDGE_OUT, runId, 'execution_trace.json');
    const eventPath = join(BRIDGE_OUT, runId, 'runtime_change_event.json');
    if (!existsSync(tracePath) || !existsSync(eventPath)) continue;

    const trace = JSON.parse(readFileSync(tracePath, 'utf8'));
    if (trace.executed !== true) continue;

    const raw = JSON.parse(readFileSync(eventPath, 'utf8'));
    const cols = columnsChanged(raw.before, raw.after);

    events.push({
      mode: 'real',
      source: 'f23_runtime_change_event',
      run_id: runId,
      event_type: raw.event_type,
      intent_id: raw.intent_id || trace.intent_id,
      target_node: raw.target_node || trace.target_node,
      entity: raw.target_node || trace.target_node,
      table: raw.table,
      row_id: raw.row_id,
      columns_changed: cols,
      runtime_before: raw.before || {},
      runtime_after: raw.after || {},
      write_executed: trace.success === true,
      execution_trace_path: tracePath,
      runtime_change_event_path: eventPath,
      timestamp: raw.timestamp,
      trace_summary: {
        success: trace.success,
        executed: trace.executed,
        backup_id: trace.backup_id,
        f22_verdict: trace.f22_verdict,
      },
    });
  }

  return events.sort((a, b) => (a.timestamp || '').localeCompare(b.timestamp || ''));
}

if (process.argv[1]?.includes('collect-runtime-events')) {
  const events = collectRuntimeEvents();
  console.log(JSON.stringify({ count: events.length, entities: events.map((e) => e.entity) }, null, 2));
}
