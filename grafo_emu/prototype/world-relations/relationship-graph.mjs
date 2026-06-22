#!/usr/bin/env node
/** Build adjacency, paths, dependency/impact chains from recovered edges */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson, parseNodeId } from './_relations-lib.mjs';
import { recoveredEdges } from './recover-edges.mjs';

const edges = recoveredEdges;

function buildAdjacency(edgeList) {
  const out = new Map();
  const inn = new Map();
  for (const e of edgeList) {
    if (!out.has(e.src)) out.set(e.src, []);
    out.get(e.src).push(e);
    if (!inn.has(e.dst)) inn.set(e.dst, []);
    inn.get(e.dst).push(e);
  }
  return { out, inn };
}

const { out, inn } = buildAdjacency(edges);

const relHistogram = {};
for (const e of edges) relHistogram[e.rel] = (relHistogram[e.rel] || 0) + 1;

function bfsReachable(start, adjacency, maxDepth = 6) {
  const visited = new Set();
  const queue = [{ node: start, depth: 0, path: [start] }];
  const paths = [];
  while (queue.length) {
    const { node, depth, path } = queue.shift();
    if (depth > maxDepth) continue;
    const nextEdges = adjacency.get(node) || [];
    for (const e of nextEdges) {
      if (visited.has(e.dst + e.rel)) continue;
      visited.add(e.dst + e.rel);
      const newPath = [...path, `${e.rel}->${e.dst}`];
      paths.push({ target: e.dst, rel: e.rel, depth: depth + 1, path: newPath });
      queue.push({ node: e.dst, depth: depth + 1, path: newPath });
    }
  }
  return paths;
}

function walkTemplateFromRel(firstRel, relChain) {
  for (const e of edges) {
    if (e.rel !== firstRel) continue;
    const steps = [{ rel: e.rel, node: e.src }, { node: e.dst }];
    let cur = e.dst;
    let ok = true;
    for (const rel of relChain) {
      const next = (out.get(cur) || []).find((x) => x.rel === rel);
      if (!next) {
        ok = false;
        break;
      }
      steps.push({ rel, node: next.dst });
      cur = next.dst;
    }
    if (ok) {
      return { template: [firstRel, ...relChain].join(' -> '), start: e.src, end: cur, steps };
    }
  }
  return null;
}

const pathTemplates = [
  ['HAS_STEP', 'HAS_OBJECTIVE', 'INVOLVES_NPC'],
  ['HAS_STEP', 'HAS_OBJECTIVE', 'INVOLVES_NPC', 'SPAWNED_IN'],
  ['CONTAINS_MONSTER', 'USES_SPELL'],
  ['CONTAINS_MONSTER', 'DROPS_ITEM'],
  ['IN_SUBAREA', 'SPAWNS_MONSTER'],
  ['SELLS'],
  ['LOCATED_AT'],
  ['HAS_STEP', 'HAS_OBJECTIVE', 'DISCOVER_MAP'],
];

const crossSystemPaths = [];
for (const tmpl of pathTemplates) {
  const [first, ...rest] = tmpl;
  const found = walkTemplateFromRel(first, rest);
  crossSystemPaths.push(found || { template: tmpl.join(' -> '), missing: true });
}

function sampleNode(type, relFilter) {
  for (const e of edges) {
    const { type: st } = parseNodeId(e.src);
    if (st !== type) continue;
    if (relFilter && e.rel !== relFilter) continue;
    return e.src;
  }
  for (const e of edges) {
    const { type: dt } = parseNodeId(e.dst);
    if (dt === type) return e.dst;
  }
  return null;
}

const entitySamples = {
  npc: sampleNode('npc', 'SELLS') || sampleNode('npc', 'SPAWNED_IN') || 'npc:449',
  quest: sampleNode('quest', 'HAS_STEP') || 'quest:3',
  monster: sampleNode('monster', 'DROPS_ITEM') || sampleNode('monster', 'USES_SPELL') || 'monster:31',
  dungeon: sampleNode('dungeon', 'LOCATED_AT') || 'dungeon:1',
  map: sampleNode('map', 'IN_SUBAREA') || 'map:13603',
  merchant: sampleNode('npc', 'SELLS') || 'npc:1053',
};

const dependencyChains = {};
const impactChains = {};

for (const [role, node] of Object.entries(entitySamples)) {
  dependencyChains[role] = {
    node,
    outgoing: bfsReachable(node, out, 5).slice(0, 40),
    outgoing_count: (out.get(node) || []).length,
  };
  impactChains[role] = {
    node,
    incoming: bfsReachable(node, inn, 5).slice(0, 40),
    incoming_count: (inn.get(node) || []).length,
  };
}

export const graphResult = {
  phase: 'RELATIONSHIP_GRAPH',
  edges_discovered: edges.length,
  relationship_types: Object.keys(relHistogram).length,
  relationship_type_histogram: relHistogram,
  unique_nodes: new Set(edges.flatMap((e) => [e.src, e.dst])).size,
  cross_system_paths: crossSystemPaths,
  cross_system_paths_found: crossSystemPaths.filter((p) => !p.missing).length,
  dependency_chains: dependencyChains,
  impact_chains: impactChains,
  entity_samples: entitySamples,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'relationship_graph.json'), graphResult);

if (process.argv[1]?.includes('relationship-graph')) {
  console.log(JSON.stringify({
    edges_discovered: graphResult.edges_discovered,
    relationship_types: graphResult.relationship_types,
    cross_system_paths: graphResult.cross_system_paths_found,
  }, null, 2));
}
