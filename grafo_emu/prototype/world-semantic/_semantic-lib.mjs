import { readFileSync, writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  loadGraph,
  parseTableColumns,
  parseTableInserts,
  parseAllTables,
  scanCsharpRoleFiles,
  REPO_ROOT,
} from '../world-model/_model-lib.mjs';

export {
  loadSql,
  loadGraph,
  parseTableColumns,
  parseTableInserts,
  parseAllTables,
  scanCsharpRoleFiles,
  REPO_ROOT,
};

export const WS_DIR = dirname(fileURLToPath(import.meta.url));
export const WM_ARTIFACTS = join(WS_DIR, '..', 'world-model');

export function readArtifact(name) {
  const path = join(WM_ARTIFACTS, name);
  return JSON.parse(readFileSync(path, 'utf8'));
}

export function writeJson(path, data) {
  writeFileSync(path, JSON.stringify(data, null, 2), 'utf8');
}

/** Split CSV tolerating spaces: "288,5" or "970, 974, 975" */
export function splitCsv(value) {
  if (value == null || value === '') return [];
  return String(value)
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);
}

/** Build Set of numeric ids from a column */
export function idSet(rows, col) {
  const s = new Set();
  for (const r of rows) {
    const v = r[col];
    if (v == null || v === '') continue;
    const n = Number(v);
    if (!Number.isNaN(n)) s.add(n);
  }
  return s;
}

/** Parse paired CSV like ItemsRewardCSV "288,5" -> [{id, qty}] */
export function splitCsvPairs(value) {
  const parts = splitCsv(value);
  const pairs = [];
  for (let i = 0; i < parts.length; i += 2) {
    if (parts[i + 1] != null) {
      pairs.push({ id: Number(parts[i]), qty: Number(parts[i + 1]) });
    }
  }
  return pairs;
}

export function confidenceFromSources(sources) {
  const count = sources.filter(Boolean).length;
  if (count >= 3) return 0.95;
  if (count === 2) return 0.8;
  if (count === 1) return 0.6;
  return 0.3;
}
