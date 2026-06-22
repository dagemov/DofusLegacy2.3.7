import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

export const WC_DIR = dirname(fileURLToPath(import.meta.url));
export const PROTO_DIR = join(WC_DIR, '..');
export const REPO_ROOT = join(PROTO_DIR, '..', '..');
export const SQL_PATH = join(REPO_ROOT, 'database', 'sunshine.sql');

export const readJsonl = (f) =>
  readFileSync(f, 'utf8')
    .split('\n')
    .filter((l) => l.trim())
    .map((l) => JSON.parse(l));

export function loadGraph() {
  const nodes = readJsonl(join(PROTO_DIR, 'nodes.jsonl'));
  const edges = readJsonl(join(PROTO_DIR, 'edges.jsonl'));
  return {
    nodes: new Map(nodes.map((n) => [n.id, n])),
    edges,
    nodeList: nodes,
  };
}

/** Parse column names from CREATE TABLE `name` block (exclude KEY/CONSTRAINT lines) */
export function parseTableColumns(sql, tableName) {
  const re = new RegExp(
    `CREATE TABLE \`${tableName}\` \\(([\\s\\S]*?)\\) ENGINE=`,
    'i',
  );
  const m = sql.match(re);
  if (!m) return [];
  const body = m[1]
    .split('\n')
    .filter(
      (line) =>
        !/^\s*(PRIMARY KEY|UNIQUE KEY|KEY|CONSTRAINT|INDEX)\b/i.test(line.trim()),
    )
    .join('\n');
  return [...body.matchAll(/`(\w+)`/g)].map((x) => x[1]);
}

/** Split MySQL VALUES tuple respecting single-quoted strings */
function splitSqlValues(inner) {
  const out = [];
  let cur = '';
  let inStr = false;
  for (let i = 0; i < inner.length; i++) {
    const ch = inner[i];
    if (ch === "'" && inner[i - 1] !== '\\') {
      inStr = !inStr;
      cur += ch;
      continue;
    }
    if (ch === ',' && !inStr) {
      out.push(cur.trim());
      cur = '';
      continue;
    }
    cur += ch;
  }
  if (cur.trim()) out.push(cur.trim());
  return out;
}

function unquoteSql(v) {
  const t = v.trim();
  if (t === 'NULL' || t === 'null') return null;
  if (t.startsWith("'") && t.endsWith("'")) {
    return t.slice(1, -1).replace(/\\'/g, "'").replace(/\\\\/g, '\\');
  }
  const n = Number(t);
  return Number.isNaN(n) ? t : n;
}

/** Parse all INSERT rows for a table into objects keyed by column name */
export function parseTableInserts(sql, tableName) {
  const cols = parseTableColumns(sql, tableName);
  if (!cols.length) return { columns: [], rows: [] };

  const prefix = `INSERT INTO \`${tableName}\` VALUES (`;
  const rows = [];
  for (const line of sql.split('\n')) {
    if (!line.startsWith(prefix)) continue;
    const end = line.lastIndexOf(');');
    if (end < 0) continue;
    const inner = line.slice(prefix.length, end);
    const parts = splitSqlValues(inner);
    if (parts.length !== cols.length) continue;
    const row = {};
    cols.forEach((c, i) => {
      row[c] = unquoteSql(parts[i]);
    });
    rows.push(row);
  }
  return { columns: cols, rows };
}

export function relSignature(edges) {
  const sig = {};
  for (const e of edges) sig[e.rel] = (sig[e.rel] || 0) + 1;
  return sig;
}

export function inferClusterLabel(sig, nodeTypes) {
  const rels = Object.keys(sig);
  if (sig.USES_EFFECT && sig.OBSERVED_IN) return 'combat_runtime_spell';
  if (sig.CONTRADICTS && sig.PARSED_EFFECT) return 'static_vs_observed_conflict';
  if (sig.HAS_STEP && sig.INVOLVES_NPC) return 'quest_progression';
  if (sig.SELLS || sig.HAS_TYPE) return 'economy_catalog';
  if (sig.MATCHES && sig.EVIDENCES) return 'diagnostic_epistemic';
  if (nodeTypes.has('Quest')) return 'quest_content';
  if (nodeTypes.has('Npc')) return 'npc_content';
  if (nodeTypes.has('Item')) return 'item_catalog';
  return 'unclassified';
}

export function loadSql() {
  return readFileSync(SQL_PATH, 'utf8');
}
