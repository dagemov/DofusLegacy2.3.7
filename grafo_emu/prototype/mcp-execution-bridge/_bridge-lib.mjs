import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

const BRIDGE_DIR = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = join(BRIDGE_DIR, '..', '..', '..');
const MCP_ROOT = join(REPO_ROOT, 'mcp');
const SIM_DIR = join(BRIDGE_DIR, '..', 'mcp-execution-sim');
const OUT_DIR = join(BRIDGE_DIR, 'out');

function loadMcpEnv() {
  const envPath = join(MCP_ROOT, '.env');
  if (!existsSync(envPath)) return;
  for (const line of readFileSync(envPath, 'utf8').split('\n')) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const eq = trimmed.indexOf('=');
    if (eq < 1) continue;
    const key = trimmed.slice(0, eq).trim();
    const val = trimmed.slice(eq + 1).trim();
    if (!process.env[key]) process.env[key] = val;
  }
}

loadMcpEnv();

export const F23_EXECUTION_GATES = {
  ALLOWED_INTENTS: new Set(['modify_item', 'modify_npc']),
  NPC_COSMETIC_COLUMNS: new Set(['Name', 'EntityLook']),
  ITEM_COLUMNS: new Set(['Name', 'Price', 'Level', 'Weight', 'IconId']),
  MAX_BLAST_RADIUS: 10,
  BLOCK_RISKS: new Set(['HIGH']),
  REQUIRE_F22_VERDICTS: new Set(['APPROVE']),
};

export const INTENT_TABLE_MAP = {
  modify_item: { table: 'items', columns: F23_EXECUTION_GATES.ITEM_COLUMNS },
  modify_npc: { table: 'npcs', columns: F23_EXECUTION_GATES.NPC_COSMETIC_COLUMNS },
};

function resolveSshKey(raw) {
  const defaultKey = join(REPO_ROOT, 'SSH', 'private_key_sebas.pem');
  const candidates = [raw, join(REPO_ROOT, raw), join(MCP_ROOT, raw), defaultKey].filter(Boolean);
  for (const c of candidates) {
    if (existsSync(c)) return c;
  }
  return raw || '';
}

export function bridgeConfig() {
  const key = resolveSshKey(process.env.BRIDGE_SSH_KEY || process.env.SSH_KEY || '');
  return {
    repoRoot: REPO_ROOT,
    bridgeDir: BRIDGE_DIR,
    simDir: SIM_DIR,
    outDir: OUT_DIR,
    ssh: {
      host: process.env.BRIDGE_SSH_HOST || process.env.SSH_HOST || '174.138.35.107',
      user: process.env.BRIDGE_SSH_USER || process.env.SSH_USER || 'root',
      key,
    },
    db: {
      container: process.env.BRIDGE_DB_CONTAINER || 'sunshine-db',
      database: process.env.BRIDGE_MYSQL_DATABASE || 'sunshine',
      backupDir: process.env.BRIDGE_BACKUP_DIR || '/root/backups/mek',
    },
  };
}

export function writeJson(path, data) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

export function loadF22Artifacts() {
  const plans = JSON.parse(readFileSync(join(SIM_DIR, 'execution_plans.json'), 'utf8'));
  const blast = JSON.parse(readFileSync(join(SIM_DIR, 'blast_radius_report.json'), 'utf8'));
  return { plans, blast };
}

export function findPlanAndValidation(artifacts, intentId, targetNode) {
  const planIdx = artifacts.plans.plans.findIndex(
    (p) => p.intent_id === intentId && (!targetNode || p.target_node === targetNode),
  );
  if (planIdx < 0) return { plan: null, validation: null };
  return {
    plan: artifacts.plans.plans[planIdx],
    validation: artifacts.blast.validations[planIdx],
  };
}

export function makeRunId() {
  return new Date().toISOString().replace(/[-:]/g, '').replace(/\.\d{3}Z$/, 'Z');
}

export function sanitizeRunId(runId) {
  return String(runId).replace(/[^a-zA-Z0-9_-]/g, '');
}

export function sqlQuote(value) {
  if (value === null || value === undefined) return 'NULL';
  if (typeof value === 'number' && Number.isFinite(value)) return String(value);
  return `'${String(value).replace(/\\/g, '\\\\').replace(/'/g, "''")}'`;
}

export function hashString(s) {
  return createHash('sha256').update(s).digest('hex').slice(0, 16);
}

export function evaluateF23Gates(validation, intentId, confirm, translatorError, sshConfigured) {
  const logs = [];
  const push = (msg) => logs.push(msg);

  if (!F23_EXECUTION_GATES.ALLOWED_INTENTS.has(intentId)) {
    push(`gate: block intent ${intentId} not in v1 allowlist`);
    return { passed: false, blocked: true, reason: `intent_not_allowed:${intentId}`, logs, dry_run_only: !confirm };
  }

  if (!validation) {
    push('gate: block missing F22 validation');
    return { passed: false, blocked: true, reason: 'missing_f22_validation', logs, dry_run_only: !confirm };
  }

  if (!F23_EXECUTION_GATES.REQUIRE_F22_VERDICTS.has(validation.verdict)) {
    push(`gate: block F22 verdict ${validation.verdict} (requires APPROVE)`);
    return { passed: false, blocked: true, reason: `f22_verdict_${validation.verdict}`, logs, dry_run_only: !confirm };
  }

  if (F23_EXECUTION_GATES.BLOCK_RISKS.has(validation.max_modification_risk)) {
    push(`gate: block modification risk ${validation.max_modification_risk}`);
    return { passed: false, blocked: true, reason: 'high_modification_risk', logs, dry_run_only: !confirm };
  }

  if (validation.blast_radius_total > F23_EXECUTION_GATES.MAX_BLAST_RADIUS) {
    push(`gate: block blast radius ${validation.blast_radius_total} > ${F23_EXECUTION_GATES.MAX_BLAST_RADIUS}`);
    return { passed: false, blocked: true, reason: 'blast_radius_exceeded', logs, dry_run_only: !confirm };
  }

  if (translatorError) {
    push(`gate: block translator — ${translatorError}`);
    return { passed: false, blocked: true, reason: `translator:${translatorError}`, logs, dry_run_only: !confirm };
  }

  if (confirm && !sshConfigured) {
    push('gate: block SSH not configured for live execution');
    return { passed: false, blocked: true, reason: 'ssh_unconfigured', logs, dry_run_only: false };
  }

  if (!confirm) {
    push('gate: dry-run preview (confirm not received)');
    return { passed: true, blocked: false, reason: null, logs, dry_run_only: true };
  }

  push('gate: all F23 execution gates passed');
  return { passed: true, blocked: false, reason: null, logs, dry_run_only: false };
}

export { BRIDGE_DIR, OUT_DIR, SIM_DIR, REPO_ROOT };
