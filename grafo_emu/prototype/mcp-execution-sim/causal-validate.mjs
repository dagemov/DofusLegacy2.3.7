#!/usr/bin/env node
/** Simulates ICausalValidator — blast radius + verdict per plan */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  readCausalEdges,
  buildAdjacency,
  CAUSAL_SUBGRAPH_EXCLUDE,
  GRAPH_TO_RUNTIME,
  parseNodeId,
  blastRadius,
  impactChain,
  verdict,
  assertNoWrite,
  writeJson,
} from './_sim-lib.mjs';
import { plansResult } from './intent-plan.mjs';

const edges = readCausalEdges();
const { out, inn } = buildAdjacency(edges, CAUSAL_SUBGRAPH_EXCLUDE);

const validations = [];

for (const plan of plansResult.plans) {
  const target = plan.target_node;
  const { type } = parseNodeId(target);
  const runtime = GRAPH_TO_RUNTIME[type] || GRAPH_TO_RUNTIME[plan.target_type];

  const downstream = target && !target.includes('PLACEHOLDER')
    ? blastRadius(target, out, 6)
    : { hops: [], blast_radius_total: 0, blast_by_depth: {}, max_depth: 0, max_causal_weight: 0, max_modification_risk: 'LOW', affected_roles: [] };

  const upstream = target && !target.includes('PLACEHOLDER')
    ? impactChain(target, inn, 6)
    : { incoming_hops: [], incoming_count: 0 };

  const v = verdict(downstream);
  const destructiveCascade = downstream.max_modification_risk === 'HIGH' && downstream.blast_radius_total > 20;

  validations.push({
    intent_id: plan.intent_id,
    target_node: target,
    detecting_system: runtime?.cs_manager || 'unknown',
    downstream_propagation: downstream,
    upstream_impact: upstream,
    max_modification_risk: downstream.max_modification_risk,
    blast_radius_total: downstream.blast_radius_total,
    affected_roles: downstream.affected_roles,
    destructive_cascade_detected: destructiveCascade,
    verdict: v.verdict,
    why: v.reason,
    what_breaks: upstream.incoming_hops.slice(0, 10).map((h) => `${h.from} --${h.rel}--> ${target}`),
    how_far: downstream.max_depth,
    ...assertNoWrite(),
  });
}

export const blastReport = {
  phase: 'BLAST_RADIUS_REPORT',
  validator: 'ICausalValidator (simulated)',
  validation_count: validations.length,
  verdict_distribution: validations.reduce((acc, v) => {
    acc[v.verdict] = (acc[v.verdict] || 0) + 1;
    return acc;
  }, {}),
  validations,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'blast_radius_report.json'), blastReport);

if (process.argv[1]?.includes('causal-validate')) {
  console.log(JSON.stringify(blastReport.verdict_distribution, null, 2));
}
