#!/usr/bin/env node
// Fase 13.5 — TRUTH Interpretation Layer (runtime only).
// Lee nodes.jsonl + edges.jsonl SIN modificarlos. No reemplaza traverse.mjs.
// Uso: node truth-interpret.mjs
//      node truth-interpret.mjs --json

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const HERE = dirname(fileURLToPath(import.meta.url));
const readJsonl = (f) =>
  readFileSync(join(HERE, f), 'utf8')
    .split('\n')
    .filter((l) => l.trim())
    .map((l) => JSON.parse(l));

const edges = readJsonl('edges.jsonl');
const nodes = new Map(readJsonl('nodes.jsonl').map((n) => [n.id, n]));

const PRIORITY = { TRUTH: 1.0, OBSERVED: 0.8, DERIVED: 0.5, UNCERTAIN: 0.2 };

const UNCERTAIN_STATUS = new Set(['disputed', 'candidate', 'ref-only']);

/** Build loser set from CONTRADICTS: PARSED_EFFECT dst is loser effect node */
function buildContradictionIndex(allEdges) {
  const bySrc = new Map();
  for (const e of allEdges) {
    if (!bySrc.has(e.src)) bySrc.set(e.src, []);
    bySrc.get(e.src).push(e);
  }
  const contradictedEdgeIds = new Set();
  const pairs = [];

  for (const c of allEdges.filter((e) => e.rel === 'CONTRADICTS')) {
    const derivedDst = c.src;
    const logDst = c.dst;
    const parsed = allEdges.find(
      (e) => e.rel === 'PARSED_EFFECT' && e.dst === derivedDst && e.provenance?.source === 'BD',
    );
    const observed = allEdges.find(
      (e) =>
        e.rel === 'USES_EFFECT' &&
        e.dst === logDst &&
        e.provenance?.source === 'LOG',
    );
    if (parsed && observed) {
      contradictedEdgeIds.add(parsed.id);
      pairs.push({
        log_edge: observed.id,
        derived_edge: parsed.id,
        resolution: 'LOG_WINS',
      });
    }
  }
  return { contradictedEdgeIds, pairs };
}

function domainOf(edge) {
  const id = edge.src;
  if (id.startsWith('spell:') || id.startsWith('spelllevel:')) return 'spell';
  if (id.startsWith('npc:') || id.startsWith('queststep:')) return 'npc';
  if (id.startsWith('item:') || id.startsWith('itemtype:')) return 'item';
  if (id.startsWith('quest:')) return 'quest';
  return 'other';
}

function deriveTruthState(edge, contradictedEdgeIds) {
  if (edge.status && UNCERTAIN_STATUS.has(edge.status)) return 'UNCERTAIN';
  const src = edge.provenance?.source;
  if (src === 'LOG') {
    return contradictedEdgeIds.has(edge.id) ? 'OBSERVED' : 'TRUTH';
  }
  if (src === 'BD') return 'DERIVED';
  if (contradictedEdgeIds.has(edge.id)) return 'UNCERTAIN';
  return 'DERIVED';
}

function confidenceMinimal(edge, truthState, contradictedEdgeIds) {
  let c = edge.confidence ?? 1.0;
  if (edge.provenance?.source === 'LOG') c += 0.3;
  if (contradictedEdgeIds.has(edge.id)) c -= 0.4;
  if (edge.status && UNCERTAIN_STATUS.has(edge.status)) c -= 0.3;
  return Math.max(0, Math.min(1, c));
}

const { contradictedEdgeIds, pairs: conflictPairs } = buildContradictionIndex(edges);

const interpreted = edges.map((e) => {
  const truth_state = deriveTruthState(e, contradictedEdgeIds);
  return {
    id: e.id,
    src: e.src,
    rel: e.rel,
    dst: e.dst,
    truth_state,
    confidence_minimal: confidenceMinimal(e, truth_state, contradictedEdgeIds),
    truth_priority_rank: PRIORITY[truth_state],
    domain: domainOf(e),
  };
});

const logEdges = edges.filter((e) => e.provenance?.source === 'LOG');
const truthCoverageMinimal = logEdges.length / edges.length;

const domainLog = { spell: 0, npc: 0, item: 0, quest: 0, other: 0 };
const domainTotal = { spell: 0, npc: 0, item: 0, quest: 0, other: 0 };
for (const e of edges) {
  const d = domainOf(e);
  domainTotal[d]++;
  if (e.provenance?.source === 'LOG') domainLog[d]++;
}

const topTruthDomains = Object.entries(domainLog)
  .filter(([d, n]) => d !== 'other' && n > 0)
  .sort((a, b) => b[1] - a[1])
  .map(([d]) => d);

const lowTrustDomains = ['quest', 'npc', 'item'].filter((d) => domainLog[d] === 0);

function readinessBand(ratio) {
  if (ratio > 0.6) return 'QSG_READY';
  if (ratio >= 0.3) return 'PARTIAL_READY';
  return 'NOT_READY';
}

const truthSnapshot = {
  truth_coverage_minimal: Math.round(truthCoverageMinimal * 1000) / 1000,
  truth_coverage_band: readinessBand(truthCoverageMinimal),
  top_truth_domains: topTruthDomains,
  low_trust_domains: lowTrustDomains,
  conflict_pairs: conflictPairs,
  edge_interpretations: interpreted,
};

const graphReadiness = {
  phase_13_5_status: 'ACTIVE',
  qsg_readiness: truthCoverageMinimal >= 0.3 ? 'PARTIAL' : 'PARTIAL',
  truth_coverage_band: readinessBand(truthCoverageMinimal),
  blockers: [
    'no persistent truth_state',
    'no decoder validation layer',
    'no feedback signal system',
  ],
  allowed_next_phase: 'Fase 14 (QSG constrained mode)',
};

const output = { truth_snapshot: truthSnapshot, graph_readiness: graphReadiness };

if (process.argv.includes('--json')) {
  console.log(JSON.stringify(output, null, 2));
} else {
  console.log('=== Fase 13.5 — TRUTH Interpretation Layer ===');
  console.log(`edges=${edges.length}  LOG_edges=${logEdges.length}`);
  console.log(`truth_coverage_minimal=${truthSnapshot.truth_coverage_minimal}`);
  console.log(`band=${truthSnapshot.truth_coverage_band}`);
  console.log('\n--- conflict_pairs ---');
  for (const p of conflictPairs) console.log(`  ${p.log_edge} wins over ${p.derived_edge}`);
  console.log('\n--- top_truth_domains ---', topTruthDomains.join(', '));
  console.log('--- low_trust_domains ---', lowTrustDomains.join(', '));
  console.log('\n--- graph_readiness ---');
  console.log(JSON.stringify(graphReadiness, null, 2));
  console.log('\n(Run with --json for full snapshot)');
}
