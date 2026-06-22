#!/usr/bin/env node
// Reconstructor del Vertical Slice — sin dependencias externas.
// Lee nodes.jsonl + edges.jsonl y reconstruye las cadenas diseñadas en grafo_emu/04-modelo-grafo.md.
// Uso: node traverse.mjs            -> recorre los 5 seeds
//      node traverse.mjs spell:196  -> recorre un seed concreto

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const HERE = dirname(fileURLToPath(import.meta.url));
const readJsonl = (f) =>
  readFileSync(join(HERE, f), 'utf8')
    .split('\n')
    .filter((l) => l.trim())
    .map((l) => JSON.parse(l));

const nodes = new Map(readJsonl('nodes.jsonl').map((n) => [n.id, n]));
const edges = readJsonl('edges.jsonl');

const out = new Map();
for (const e of edges) {
  if (!out.has(e.src)) out.set(e.src, []);
  out.get(e.src).push(e);
}

const SEEDS = process.argv.slice(2).length
  ? process.argv.slice(2)
  : ['spell:189', 'spell:196', 'item:12116', 'npc:1053', 'quest:3'];

const label = (id) => {
  const n = nodes.get(id);
  if (!n) return `${id}  ⚠️ NODO AUSENTE`;
  const name = n.props?.name || n.props?.enum || n.props?.titulo || n.props?.nombre || '';
  const flag = n.status === 'disputed' ? ' ⚠️disputed' : n.status === 'candidate' ? ' ⌛candidate' : '';
  return `${id} [${n.type}/${n.layer}]${name ? ' «' + name + '»' : ''} conf=${n.confidence}${flag}`;
};

const conf = (e) => {
  const c = e.confidence ?? 1;
  const bar = c >= 0.9 ? '●●●' : c >= 0.6 ? '●●○' : c >= 0.3 ? '●○○' : '○○○';
  return `${bar} ${c}`;
};

function walk(id, depth, seen, lineHints) {
  const pad = '  '.repeat(depth);
  const kids = out.get(id) || [];
  for (const e of kids) {
    const arrow = e.rel === 'CONTRADICTS' || e.rel === 'VIOLATES' ? '✗' : '↓';
    const src = e.provenance?.source || '?';
    console.log(`${pad}${arrow} ${e.rel}  (${conf(e)} | ${src}) → ${label(e.dst)}`);
    const key = `${e.src}->${e.dst}`;
    if (seen.has(key)) continue;
    seen.add(key);
    if (depth < 9) walk(e.dst, depth + 1, seen, lineHints);
  }
}

console.log('======================================================================');
console.log(' VERTICAL SLICE — reconstrucción de cadenas desde datos reales');
console.log(` nodos=${nodes.size}  aristas=${edges.length}`);
console.log('======================================================================');

for (const seed of SEEDS) {
  console.log(`\n### SEED: ${label(seed)}`);
  walk(seed, 1, new Set(), []);
}

// Auditoría de integridad: aristas que apuntan a nodos inexistentes
const missing = edges.filter((e) => !nodes.has(e.src) || !nodes.has(e.dst));
console.log('\n----------------------------------------------------------------------');
console.log(` Integridad: ${missing.length} arista(s) con extremos ausentes`);
for (const e of missing) console.log(`   ✗ ${e.id}: ${e.src} -${e.rel}-> ${e.dst}`);

// Resumen de contradicciones (eje epistémico clave)
const contra = edges.filter((e) => e.rel === 'CONTRADICTS' || e.rel === 'VIOLATES');
console.log(`\n Contradicciones estático↔observado: ${contra.length}`);
for (const e of contra) console.log(`   ✗ ${e.src} -${e.rel}-> ${e.dst}  «${e.note || e.provenance?.ref}»`);
console.log('======================================================================');
