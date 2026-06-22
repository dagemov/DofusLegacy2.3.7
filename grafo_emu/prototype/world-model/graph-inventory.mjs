#!/usr/bin/env node
/** Phase A — Graph system inventory */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadGraph, relSignature, writeJson } from './_model-lib.mjs';
import { result as clusters } from '../world-control/discover-clusters.mjs';

const { nodes, edges, nodeList } = loadGraph();

const byType = {};
const byLayer = {};
const bySource = {};

for (const n of nodeList) {
  byType[n.type] = (byType[n.type] || 0) + 1;
  byLayer[n.layer] = (byLayer[n.layer] || 0) + 1;
  const src = n.provenance?.source || 'unknown';
  bySource[src] = (bySource[src] || 0) + 1;
}

const rels = relSignature(edges);

export const result = {
  phase: 'A_GRAPH_INVENTORY',
  source: 'grafo_emu/prototype/nodes.jsonl + edges.jsonl',
  node_count: nodeList.length,
  edge_count: edges.length,
  entities_by_type: byType,
  layers: byLayer,
  provenance_sources: bySource,
  relations: rels,
  clusters: clusters.clusters_detected.map((c) => ({
    cluster_id: c.cluster_id,
    label: c.label,
    node_count: c.node_count,
    edge_count: c.edge_count,
    rel_signature: c.rel_signature,
    coherence_score: c.coherence_score,
    seed_nodes: c.seed_nodes,
  })),
  cluster_count: clusters.cluster_count,
  global_coherence: clusters.global_coherence_score,
  not_present_in_graph: ['Map', 'Spawn', 'Monster', 'WorldMap', 'Character', 'Guild'],
  graph_coverage_note: 'vertical slice L1-L5; world catalog severely under-represented vs SQL',
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'graph_system_inventory.json'), result);

if (process.argv[1]?.includes('graph-inventory')) {
  console.log(JSON.stringify({
    node_count: result.node_count,
    cluster_count: result.cluster_count,
  }, null, 2));
}
