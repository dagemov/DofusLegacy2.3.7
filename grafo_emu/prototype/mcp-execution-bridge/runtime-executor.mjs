import { writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';
import {
  bridgeConfig,
  evaluateF23Gates,
  makeRunId,
  sanitizeRunId,
  writeJson,
} from './_bridge-lib.mjs';
import { createSSHDockerAdapter } from './ssh-docker-adapter.mjs';
import {
  translateMutationPlan,
  buildSelectSql,
  parseSelectRow,
  fieldsToChanges,
} from './sql-translator.mjs';
import { buildRuntimeChangeEvent, writeRuntimeChangeEvent } from './sync-snapshot.mjs';

/**
 * IRuntimeExecutor — F23 controlled remote execution bridge.
 */
export async function executeMutation({
  plan,
  validation,
  fieldsAfter,
  confirm = false,
  runId = makeRunId(),
  adapter = null,
}) {
  const cfg = bridgeConfig();
  const safeRunId = sanitizeRunId(runId);
  const runOutDir = join(cfg.outDir, safeRunId);
  mkdirSync(runOutDir, { recursive: true });

  const sshConfigured = Boolean(cfg.ssh.host && cfg.ssh.key);
  const startMs = Date.now();
  const logs = [];
  const sshCommands = [];
  const sqlCommands = [];

  const translation = translateMutationPlan(plan, fieldsAfter, {});
  const translatorError = translation.error || null;
  const gate = evaluateF23Gates(validation, plan.intent_id, confirm, translatorError, sshConfigured);
  logs.push(...gate.logs);

  let patch = translation.patch || {
    forward_sql: '',
    rollback_sql: '',
    affected_tables: [],
    ordered_steps: [],
  };
  let fieldsBefore = {};
  let rollbackAvailable = false;
  let backupId = null;
  let executed = false;
  let success = false;
  let blocked = gate.blocked;
  let blockReason = gate.reason;

  if (translation.patch) {
    sqlCommands.push(...translation.patch.ordered_steps);
  }

  const dryRun = gate.dry_run_only || !confirm;

  if (translation.patch && !blocked) {
  // Re-translate with before values when we have them (after pre-exec fetch on live path)
    const retrans = translateMutationPlan(plan, fieldsAfter, fieldsBefore);
    if (retrans.patch) {
      patch = retrans.patch;
      rollbackAvailable = retrans.rollback_available;
    }
  }

  if (!dryRun && !blocked && translation.patch) {
    const ssh = adapter || createSSHDockerAdapter(cfg);
    try {
      const { table, row_id: rowId, columns } = translation.patch;
      const selectSql = buildSelectSql(table, rowId, columns);
      logs.push(`pre-exec: ${selectSql}`);

      const rawBefore = await ssh.queryRows(cfg.db.container, selectSql);
      sshCommands.push(...ssh.getCommandLog().map((c) => c.command));
      fieldsBefore = parseSelectRow(rawBefore, columns) || {};
      logs.push(`pre-exec snapshot: ${JSON.stringify(fieldsBefore)}`);

      const retrans = translateMutationPlan(plan, fieldsAfter, fieldsBefore);
      patch = retrans.patch;
      rollbackAvailable = retrans.rollback_available;
      sqlCommands.length = 0;
      sqlCommands.push(...patch.ordered_steps);

      const patchPath = join(runOutDir, 'patch.sql');
      const rollbackPath = join(runOutDir, 'rollback.sql');
      writeFileSync(patchPath, patch.forward_sql, 'utf8');
      writeFileSync(rollbackPath, patch.rollback_sql || '-- no rollback\n', 'utf8');

      const backup = await ssh.backupDatabase(cfg.db.container, patch.affected_tables, safeRunId);
      backupId = backup.backup_id;
      sshCommands.push(backup.ssh_command);
      logs.push(`backup: ${backup.remote_path}`);

      const applyRes = await ssh.uploadAndApplyPatch(cfg.db.container, patchPath, safeRunId);
      sshCommands.push(applyRes.ssh_command);
      logs.push('apply: ok');

      const rawAfter = await ssh.queryRows(cfg.db.container, selectSql);
      sshCommands.push(...ssh.getCommandLog().slice(-1).map((c) => c.command));
      const afterRow = parseSelectRow(rawAfter, columns) || {};

      const changeEvent = buildRuntimeChangeEvent({
        intentId: plan.intent_id,
        targetNode: plan.target_node,
        table,
        rowId,
        before: fieldsBefore,
        after: afterRow,
      });
      writeRuntimeChangeEvent(cfg.outDir, safeRunId, changeEvent);
      logs.push('sync: runtime_change_event.json emitted');

      executed = true;
      success = true;
    } catch (err) {
      logs.push(`error: ${err.message}`);
      success = false;
      executed = false;
      blockReason = err.message;
    }
  } else if (translation.patch) {
    const patchPath = join(runOutDir, 'patch.sql');
    const rollbackPath = join(runOutDir, 'rollback.sql');
    writeFileSync(patchPath, patch.forward_sql, 'utf8');
    writeFileSync(rollbackPath, patch.rollback_sql || '-- awaiting pre-exec before values\n', 'utf8');
    if (dryRun) {
      logs.push('dry-run: patch.sql written locally, no SSH execution');
    }
    success = gate.passed && !gate.blocked;
  }

  const fieldChanges = translation.patch
    ? fieldsToChanges(fieldsAfter, fieldsBefore)
    : [];

  const trace = {
    intent_id: plan.intent_id,
    target_node: plan.target_node,
    run_id: safeRunId,
    sql_commands: sqlCommands,
    ssh_commands: sshCommands,
    docker_container: cfg.db.container,
    execution_time_ms: Date.now() - startMs,
    success,
    executed,
    dry_run: dryRun,
    confirm_received: Boolean(confirm),
    blocked,
    block_reason: blockReason,
    rollback_available: rollbackAvailable,
    backup_id: backupId,
    f22_verdict: validation?.verdict,
    f23_gates_passed: gate.passed && !blocked,
    field_changes: fieldChanges,
    logs,
  };

  const tracePath = join(runOutDir, 'execution_trace.json');
  writeJson(tracePath, trace);

  return {
    success,
    dry_run: dryRun,
    executed,
    blocked,
    block_reason: blockReason,
    backup_id: backupId,
    rollback_available: rollbackAvailable,
    patch,
    trace_path: tracePath,
    trace,
    field_changes: fieldChanges,
  };
}
