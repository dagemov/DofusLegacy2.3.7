#!/usr/bin/env node
/** TEST 1 — Graph Discovery: connected components + rel signature clustering */
import {
  loadGraph,
  relSignature,
  inferClusterLabel,
} from './_lib.mjs';

function buildAdjacency(edges) {
  const adj = new Map();
  const add = (a, b) => {
    if (!adj.has(a)) adj.set(a, new Set());
    adj.get(a).add(b);
  };
  for (const e of edges) {
    add(e.src, e.dst);
    add(e.dst, e.src);
  }
  return adj;
}

function connectedComponents(nodes, adj) {
  const seen = new Set();
  const comps = [];
  for (const id of nodes) {
    if (seen.has(id)) continue;
    const stack = [id];
    const comp = [];
    seen.add(id);
    while (stack.length) {
      const cur = stack.pop();
      comp.push(cur);
      for (const nb of adj.get(cur) || []) {
        if (!seen.has(nb)) {
          seen.add(nb);
          stack.push(nb);
        }
      }
    }
    comps.push(comp);
  }
  return comps;
}

function coherenceScore(clusterEdges, clusterNodes) {
  if (!clusterEdges.length) return 0;
  const sig = relSignature(clusterEdges);
  const relCount = Object.keys(sig).length;
  const dominant = Math.max(...Object.values(sig));
  const purity = dominant / clusterEdges.length;
  const typeSet = new Set(clusterNodes.map((n) => n.type));
  const typePenalty = typeSet.size > 6 ? 0.1 : 0;
  return Math.round(Math.max(0, Math.min(1, purity * 0.85 + (relCount > 0 ? 0.15 : 0) - typePenalty)) * 1000) / 1000;
}

const { nodes, edges, nodeList } = loadGraph();
const allNodeIds = nodeList.map((n) => n.id);
const adj = buildAdjacency(edges);
const components = connectedComponents(allNodeIds, adj);

const clusters = components.map((memberIds, idx) => {
  const memberSet = new Set(memberIds);
  const clusterEdges = edges.filter(
    (e) => memberSet.has(e.src) && memberSet.has(e.dst),
  );
  const clusterNodes = memberIds.map((id) => nodes.get(id)).filter(Boolean);
  const sig = relSignature(clusterEdges);
  const label = inferClusterLabel(
    sig,
    new Set(clusterNodes.map((n) => n.type)),
  );
  return {
    cluster_id: `cluster:${idx + 1}`,
    label,
    node_count: memberIds.length,
    edge_count: clusterEdges.length,
    rel_signature: sig,
    seed_nodes: memberIds.filter((id) =>
      ['spell:', 'quest:', 'npc:', 'item:'].some((p) => id.startsWith(p)),
    ).slice(0, 5),
    coherence_score: coherenceScore(clusterEdges, clusterNodes),
    game_system: label.replace(/_/g, ' '),
  };
});

const globalCoherence =
  clusters.reduce((s, c) => s + c.coherence_score * c.edge_count, 0) /
  Math.max(1, edges.length);

export const result = {
  test: 'TEST_1_GRAPH_DISCOVERY',
  clusters_detected: clusters,
  cluster_count: clusters.length,
  global_coherence_score: Math.round(globalCoherence * 1000) / 1000,
  map_spawn_clusters: clusters.filter((c) =>
    Object.keys(c.rel_signature).some((r) =>
      ['SPAWNS_ON', 'ELEMENT_AT', 'NEIGHBOUR'].includes(r),
    ),
  ).length,
};

if (import.meta.url === `file://${process.argv[1]?.replace(/\\/g, '/')}` ||
    process.argv[1]?.endsWith('discover-clusters.mjs')) {
  console.log(JSON.stringify(result, null, 2));
}
