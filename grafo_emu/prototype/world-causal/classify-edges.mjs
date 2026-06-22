#!/usr/bin/env node
/** Phase 21 — Classify and enrich existing edges with causal metadata */
import { writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  readRelationsEdges,
  readRelationsArtifact,
  enrichEdge,
  weightBucket,
  writeJson,
  BASE_WEIGHT_BY_REL,
  PROPAGATION_DEPTH_BY_REL,
  SEMANTIC_ROLE_BY_REL,
} from './_causal-lib.mjs';

const rawEdges = readRelationsEdges();
const graphPhase20 = readRelationsArtifact('relationship_graph.json');

const dstFanIn = new Map();
for (const e of rawEdges) {
  dstFanIn.set(e.dst, (dstFanIn.get(e.dst) || 0) + 1);
}

export const enrichedEdges = rawEdges.map((e) => enrichEdge(e, dstFanIn.get(e.dst) || 0));

const roleDist = {};
const weightHist = { '0.0': 0, '0.3': 0, '0.6': 0, '0.9': 0, '1.0': 0 };
const impactDist = {};
const riskDist = {};
const relStats = {};

for (const e of enrichedEdges) {
  roleDist[e.semantic_role] = (roleDist[e.semantic_role] || 0) + 1;
  const b = weightBucket(e.causal_weight);
  weightHist[b] = (weightHist[b] || 0) + 1;
  if (e.causal_weight >= 0.95) weightHist['1.0'] = (weightHist['1.0'] || 0) + 1;
  impactDist[e.gameplay_impact] = (impactDist[e.gameplay_impact] || 0) + 1;
  riskDist[e.modification_risk] = (riskDist[e.modification_risk] || 0) + 1;

  if (!relStats[e.rel]) {
    relStats[e.rel] = { count: 0, total_weight: 0, propagation_depth: PROPAGATION_DEPTH_BY_REL[e.rel] ?? 0 };
  }
  relStats[e.rel].count += 1;
  relStats[e.rel].total_weight += e.causal_weight;
}

const totalEdges = enrichedEdges.length;
const dominance = {};
for (const [rel, s] of Object.entries(relStats)) {
  const share = s.count / totalEdges;
  const avgWeight = s.total_weight / s.count;
  const base = BASE_WEIGHT_BY_REL[rel] ?? 0.3;
  dominance[rel] = {
    count: s.count,
    share: Math.round(share * 10000) / 10000,
    avg_causal_weight: Math.round(avgWeight * 100) / 100,
    base_weight: base,
    propagation_depth: s.propagation_depth,
    semantic_role: SEMANTIC_ROLE_BY_REL[rel] || 'DERIVATIVE',
    dominant: share >= 0.1,
    low_semantic_value: share >= 0.1 && base <= 0.3 && s.propagation_depth === 0,
  };
}

const sortedByImpact = [...enrichedEdges].sort((a, b) => {
  if (b.causal_weight !== a.causal_weight) return b.causal_weight - a.causal_weight;
  return b.propagation_depth - a.propagation_depth;
});

const top50Impact = sortedByImpact.slice(0, 50).map((e) => ({
  id: e.id,
  src: e.src,
  rel: e.rel,
  dst: e.dst,
  semantic_role: e.semantic_role,
  causal_weight: e.causal_weight,
  gameplay_impact: e.gameplay_impact,
  propagation_depth: e.propagation_depth,
}));

const noiseCandidates = Object.entries(dominance)
  .filter(([, d]) => d.low_semantic_value)
  .sort((a, b) => b[1].count - a[1].count);

const top50Noise = [];
for (const [rel, d] of noiseCandidates) {
  const sample = enrichedEdges.filter((e) => e.rel === rel).slice(0, Math.min(50, d.count));
  for (const e of sample) {
    if (top50Noise.length >= 50) break;
    top50Noise.push({
      id: e.id,
      rel: e.rel,
      src: e.src,
      dst: e.dst,
      causal_weight: e.causal_weight,
      noise_score: Math.round(d.share * (1 - e.causal_weight) * 10000) / 10000,
      reason: `${rel} dominates graph (${(d.share * 100).toFixed(1)}%) with low causal weight`,
    });
  }
}

const uniqueNodes = graphPhase20.unique_nodes;

export const causalityReport = {
  phase: 'EDGE_CAUSALITY_REPORT',
  edge_count: totalEdges,
  node_count: uniqueNodes,
  role_distribution: roleDist,
  role_distribution_pct: Object.fromEntries(
    Object.entries(roleDist).map(([k, v]) => [k, Math.round((v / totalEdges) * 10000) / 100]),
  ),
  causal_weight_histogram: weightHist,
  gameplay_impact_distribution: impactDist,
  modification_risk_distribution: riskDist,
  top_50_highest_impact_edges: top50Impact,
  top_50_noise_edges: top50Noise,
  dominance,
  dominant_relationship_types: Object.entries(dominance)
    .filter(([, d]) => d.dominant)
    .map(([rel, d]) => ({ rel, ...d })),
  low_value_relationship_types: Object.entries(dominance)
    .filter(([, d]) => d.low_semantic_value)
    .map(([rel, d]) => ({ rel, ...d })),
};

export const causalManifest = {
  phase: 'CAUSAL_GRAPH_MANIFEST',
  generated_at: new Date().toISOString(),
  node_count: uniqueNodes,
  edge_count: totalEdges,
  relationship_types: Object.keys(relStats).length,
  unchanged_vs_phase_20: true,
  enrichment_schema: {
    relation: 'same as rel',
    semantic_role: 'STRUCTURAL|FUNCTIONAL|BEHAVIORAL|ECONOMIC|NARRATIVE|DERIVATIVE',
    causal_weight: '0.0-1.0',
    gameplay_impact: 'HIGH|MEDIUM|LOW|NEGLIGIBLE',
    modification_risk: 'HIGH|MEDIUM|LOW',
    propagation_depth: 'integer hops',
    derivative: 'boolean',
  },
  role_distribution: roleDist,
  causal_weight_histogram: weightHist,
  enriched_stream: 'causal_graph.jsonl',
};

const outDir = dirname(fileURLToPath(import.meta.url));
const jsonl = enrichedEdges.map((e) => JSON.stringify(e)).join('\n') + '\n';
writeFileSync(join(outDir, 'causal_graph.jsonl'), jsonl, 'utf8');
writeJson(join(outDir, 'causal_graph.json'), causalManifest);
writeJson(join(outDir, 'edge_causality_report.json'), causalityReport);

if (process.argv[1]?.includes('classify-edges')) {
  console.log(JSON.stringify({
    edge_count: totalEdges,
    roles: Object.keys(roleDist).length,
    dominant: causalityReport.dominant_relationship_types.map((d) => d.rel),
  }, null, 2));
}
