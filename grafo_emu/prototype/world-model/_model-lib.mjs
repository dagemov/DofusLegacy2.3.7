import { readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  loadGraph,
  parseTableColumns,
  parseTableInserts,
  relSignature,
  REPO_ROOT,
} from '../world-control/_lib.mjs';

export { loadSql, loadGraph, parseTableColumns, parseTableInserts, relSignature, REPO_ROOT };

export const WM_DIR = dirname(fileURLToPath(import.meta.url));
export const SUNSHINE_ROOT = join(REPO_ROOT, 'Sunshine net11.0', 'Sunshine net11.0');

const CRUD_VERBS = [
  'Get', 'Load', 'Fetch', 'Create', 'Insert', 'Add', 'Update', 'Set', 'Modify',
  'Delete', 'Remove', 'Link', 'Move', 'Validate', 'Spawn', 'Place', 'Save',
];

/** Parse all CREATE TABLE blocks from sunshine.sql */
export function parseAllTables(sql) {
  const tables = [];
  const re = /CREATE TABLE `(\w+)` \(([\s\S]*?)\) ENGINE=/gi;
  let m;
  while ((m = re.exec(sql)) !== null) {
    const name = m[1];
    const body = m[2];
    const columns = [];
    const pk = [];
    const keyCols = [];

    for (const line of body.split('\n')) {
      const trimmed = line.trim();
      if (/^PRIMARY KEY/i.test(trimmed)) {
        const pkMatch = trimmed.match(/`(\w+)`/g);
        if (pkMatch) pk.push(...pkMatch.map((x) => x.replace(/`/g, '')));
        continue;
      }
      if (/^(UNIQUE KEY|KEY|CONSTRAINT|INDEX)\b/i.test(trimmed)) {
        const kMatch = trimmed.match(/`(\w+)`/g);
        if (kMatch) keyCols.push(...kMatch.map((x) => x.replace(/`/g, '')));
        continue;
      }
      const colMatch = trimmed.match(/^`(\w+)`\s+(\w+)/);
      if (colMatch) {
        columns.push({ name: colMatch[1], type: colMatch[2] });
      }
    }

    if (!pk.length && columns.length) {
      const idCol = columns.find((c) => /^Id$/i.test(c.name));
      if (idCol) pk.push(idCol.name);
    }

    tables.push({ name, columns, pk: [...new Set(pk)], keyCols: [...new Set(keyCols)] });
  }
  return tables;
}

function normalizeEntityName(name) {
  let n = name.replace(/CSV$/i, '').replace(/Ids?$/i, '').replace(/Id$/i, '');
  if (n.endsWith('ies')) return n.slice(0, -3) + 'y';
  if (n.endsWith('ses')) return n.slice(0, -2);
  if (n.endsWith('s') && n.length > 3) return n.slice(0, -1);
  return n;
}

function tableCandidatesForColumn(colName, allTableNames) {
  const explicit = {
    Npc: 'npcs', NpcId: 'npcs', Item: 'items', Quest: 'quests', Step: 'quests_steps',
    Map: 'worlds_maps', Monster: 'monsters', Spell: 'spells', SpellId: 'spells',
    Breed: 'breeds', Job: 'jobs', Guild: 'guilds', Character: 'characters', Account: 'accounts',
    Dungeon: 'dungeons', Mount: 'mounts', Interactive: 'interactives', World: 'worlds',
  };
  if (explicit[colName]) return [explicit[colName]];

  const candidates = new Set();
  const base = normalizeEntityName(colName);
  if (!base || base.length < 2) return [];

  for (const t of allTableNames) {
    const singular = t.replace(/_/g, '').toLowerCase();
    const baseLower = base.toLowerCase();
    if (singular === baseLower || singular === `${baseLower}s`) candidates.add(t);
    if (t === `${baseLower}s` || t === `${baseLower}es`) candidates.add(t);
    if (colName.endsWith('Id') && t.replace(/s$/, '') === baseLower.replace(/s$/, '')) candidates.add(t);
  }
  return [...candidates].slice(0, 2);
}

/** Infer FK edges from column naming (0 declared FKs in dump) */
export function inferForeignKeys(tables) {
  const tableMap = new Map(tables.map((t) => [t.name, t]));
  const names = tables.map((t) => t.name);
  const edges = [];

  for (const table of tables) {
    for (const col of table.columns) {
      if (table.pk.includes(col.name) && !/CSV$/i.test(col.name)) continue;

      const isCsv = /CSV$/i.test(col.name);
      const candidates = tableCandidatesForColumn(col.name, names);

      for (const targetName of candidates) {
        if (targetName === table.name) continue;
        const target = tableMap.get(targetName);
        if (!target) continue;

        const targetPk = target.pk[0] || 'Id';
        let confidence = 0.5;
        if (col.name === targetPk.replace(/^./, (c) => c)) confidence = 0.6;
        if (col.name === `${normalizeEntityName(targetName)}Id`) confidence = 0.85;
        if (col.name === 'NpcId' && targetName === 'npcs') confidence = 0.95;
        if (col.name === 'Npc' && targetName === 'npcs') confidence = 0.9;
        if (col.name === 'Quest' && targetName === 'quests') confidence = 0.95;
        if (col.name === 'Step' && targetName === 'quests_steps') confidence = 0.9;
        if (col.name === 'Item' && targetName === 'items') confidence = 0.9;
        if (col.name === 'Map' && targetName === 'worlds_maps') confidence = 0.85;
        if (isCsv) confidence = Math.min(confidence, 0.75);

        edges.push({
          from_table: table.name,
          from_column: col.name,
          to_table: targetName,
          to_column: targetPk,
          confidence: Math.round(confidence * 1000) / 1000,
          evidence: `column ${table.name}.${col.name}`,
          multi_valued: isCsv,
          status: confidence >= 0.7 ? 'inferred' : 'hypothesis',
        });
      }
    }
  }

  const seen = new Set();
  return edges.filter((e) => {
    const k = `${e.from_table}.${e.from_column}->${e.to_table}`;
    if (seen.has(k)) return false;
    seen.add(k);
    return e.confidence >= 0.7;
  });
}

export function countTableRows(sql, tableName) {
  const prefix = `INSERT INTO \`${tableName}\` VALUES (`;
  let count = 0;
  for (const line of sql.split('\n')) {
    if (line.startsWith(prefix)) count++;
  }
  return count;
}

export function inferTablePurpose(tableName, columns, inboundFks, outboundFks) {
  const colNames = columns.map((c) => c.name).join(' ').toLowerCase();
  const hints = [];

  if (tableName.startsWith('characters_')) hints.push('per-character runtime state');
  if (tableName.startsWith('worlds_')) hints.push('world placement / map instance');
  if (tableName.startsWith('npcs_')) hints.push('npc extension');
  if (tableName.startsWith('quests_')) hints.push('quest structure');
  if (tableName.startsWith('monsters_')) hints.push('monster extension');
  if (tableName.startsWith('items_')) hints.push('item extension');
  if (tableName === 'accounts' || tableName === 'characters') hints.push('player identity');
  if (colNames.includes('price') || colNames.includes('token')) hints.push('economy');
  if (colNames.includes('kamasreward') || colNames.includes('itemsreward')) hints.push('rewards');
  if (colNames.includes('droprate')) hints.push('loot drops');
  if (colNames.includes('effects') && tableName.includes('spell')) hints.push('spell effects blob');
  if (inboundFks.length > 3) hints.push('hub table (many references)');
  if (outboundFks.length > 3) hints.push('aggregates multiple entities');

  const purpose = hints.length
    ? hints.join('; ')
    : `data store (${tableName.replace(/_/g, ' ')})`;

  return {
    purpose,
    inferred: true,
    confidence: hints.length >= 2 ? 0.8 : hints.length === 1 ? 0.65 : 0.4,
  };
}

function walkDir(dir, acc = []) {
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return acc;
  }
  for (const name of entries) {
    const full = join(dir, name);
    let st;
    try {
      st = statSync(full);
    } catch {
      continue;
    }
    if (st.isDirectory()) {
      if (name === 'bin' || name === 'obj' || name === 'node_modules') continue;
      walkDir(full, acc);
    } else if (name.endsWith('.cs')) {
      acc.push(full);
    }
  }
  return acc;
}

function classifyRole(filePath, content) {
  const rel = relative(SUNSHINE_ROOT, filePath).replace(/\\/g, '/');
  const base = rel.split('/').pop() || '';

  if (base.endsWith('Manager.cs')) {
    if (rel.includes('MySql/Database/Managers')) return 'data_manager';
    if (rel.includes('WorldServer/Handlers')) return 'handler_manager';
    return 'game_manager';
  }
  if (base.endsWith('Handler.cs')) {
    if (rel.includes('WorldServer/Handlers')) return 'protocol_handler';
    if (rel.includes('Game/Effects') || rel.includes('Game/Spells')) return 'effect_handler';
    return 'handler';
  }
  if (base.endsWith('Service.cs')) return 'service';
  if (base.endsWith('Repository.cs')) return 'repository';
  return 'other';
}

function extractCrudVerb(methodName) {
  for (const v of CRUD_VERBS) {
    if (methodName.startsWith(v)) return v.toLowerCase();
  }
  return 'other';
}

const SQL_TABLE_BLOCKLIST = new Set([
  'this', 'x', 'database', 'entry', 'information_schema', 'INFORMATION_SCHEMA',
  'AccountId', 'effect_metadata', 'characters_dopeul_cooldown', 'SELECT', 'WHERE',
]);

function extractSqlTables(content) {
  const tables = new Set();
  const patterns = [
    /(?:FROM|INTO|UPDATE|JOIN)\s+`?(\w+)`?/gi,
    /\[Table\("(\w+)"\)\]/gi,
  ];
  for (const re of patterns) {
    let m;
    while ((m = re.exec(content)) !== null) {
      const name = m[1];
      if (!name || SQL_TABLE_BLOCKLIST.has(name)) continue;
      if (/^[A-Z]/.test(name) && name !== name.toLowerCase() && !name.includes('_')) continue;
      tables.add(name);
    }
  }
  return [...tables];
}

function extractMethods(content) {
  const methods = [];
  const re = /(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?[\w<>,\[\]\s]+\s+(\w+)\s*\(/g;
  let m;
  while ((m = re.exec(content)) !== null) {
    const name = m[1];
    if (name === 'if' || name === 'for' || name === 'foreach') continue;
    methods.push({
      name,
      verb: extractCrudVerb(name),
    });
  }
  return methods;
}

function extractDependencies(content) {
  const deps = new Set();
  const mgrRe = /(\w+Manager)\./g;
  let m;
  while ((m = mgrRe.exec(content)) !== null) deps.add(m[1]);
  const usingRe = /using\s+([\w.]+);/g;
  while ((m = usingRe.exec(content)) !== null) {
    if (m[1].includes('Database.World') || m[1].includes('Game.')) {
      deps.add(m[1].split('.').pop());
    }
  }
  return [...deps];
}

/** Scan C# Managers, Handlers, Services, Repositories */
export function scanCsharpRoleFiles() {
  const allCs = walkDir(SUNSHINE_ROOT);
  const rolePattern = /(Manager|Handler|Service|Repository)\.cs$/;
  const roleFiles = allCs.filter((f) => rolePattern.test(f));

  const scanned = roleFiles.map((filePath) => {
    const content = readFileSync(filePath, 'utf8');
    const rel = relative(SUNSHINE_ROOT, filePath).replace(/\\/g, '/');
    const classMatch = content.match(/(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)/);
    return {
      file: rel,
      class_name: classMatch?.[1] || rel.split('/').pop().replace('.cs', ''),
      role: classifyRole(filePath, content),
      tables_referenced: extractSqlTables(content),
      methods: extractMethods(content),
      dependencies: extractDependencies(content),
      namespace: content.match(/namespace\s+([\w.]+)/)?.[1] || null,
    };
  });

  return {
    sunshine_root: SUNSHINE_ROOT,
    total_cs_files: allCs.length,
    role_files_scanned: scanned.length,
    classes: scanned,
  };
}

export function writeJson(path, data) {
  writeFileSync(path, JSON.stringify(data, null, 2), 'utf8');
}
