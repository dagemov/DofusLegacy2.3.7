#!/usr/bin/env node
/** Phase 21 — Propagation models for 6 entity modification templates */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  readRelationsArtifact,
  writeJson,
  CAUSAL_SUBGRAPH_EXCLUDE,
} from './_causal-lib.mjs';
import { enrichedEdges } from './classify-edges.mjs';

const graphPhase20 = readRelationsArtifact('relationship_graph.json');
const samples = graphPhase20.entity_samples || {};

function buildAdjacency(edges, excludeRels = CAUSAL_SUBGRAPH_EXCLUDE) {
  const out = new Map();
  for (const e of edges) {
    if (excludeRels.has(e.rel)) continue;
    if (!out.has(e.src)) out.set(e.src, []);
    out.get(e.src).push(e);
  }
  return out;
}

const causalOut = buildAdjacency(enrichedEdges);

function bfsPropagation(startNode, maxDepth = 6) {
  const visited = new Set();
  const queue = [{ node: startNode, depth: 0, path: [] }];
  const chains = [];
  const blastByDepth = {};
  const roles = new Set();

  while (queue.length) {
    const { node, depth, path } = queue.shift();
    if (depth > maxDepth) continue;

    const edges = causalOut.get(node) || [];
    for (const e of edges) {
      const key = `${e.dst}:${e.rel}`;
      if (visited.has(key)) continue;
      visited.add(key);

      const hop = {
        from: e.src,
        rel: e.rel,
        to: e.dst,
        semantic_role: e.semantic_role,
        causal_weight: e.causal_weight,
        propagation_depth: e.propagation_depth,
      };
      const newPath = [...path, hop];
      const d = depth + 1;
      blastByDepth[d] = (blastByDepth[d] || 0) + 1;
      roles.add(e.semantic_role);

      chains.push({ target: e.dst, depth: d, path: newPath });
      queue.push({ node: e.dst, depth: d, path: newPath });
    }
  }

  const maxD = Math.max(0, ...Object.keys(blastByDepth).map(Number));
  return {
    chains: chains.slice(0, 30),
    max_depth: maxD,
    blast_radius: blastByDepth,
    blast_radius_total: chains.length,
    affected_roles: [...roles],
  };
}

function findItemSample() {
  const e = enrichedEdges.find((x) => x.rel === 'DROPS_ITEM' && x.causal_weight >= 0.5);
  return e ? e.dst : 'item:519';
}

const templates = {
  npc_modification: {
    trigger: samples.npc || 'npc:462',
    entity_type: 'npc',
  },
  quest_modification: {
    trigger: samples.quest || 'quest:3',
    entity_type: 'quest',
  },
  monster_modification: {
    trigger: samples.monster || 'monster:31',
    entity_type: 'monster',
  },
  dungeon_modification: {
    trigger: samples.dungeon || 'dungeon:1',
    entity_type: 'dungeon',
  },
  map_modification: {
    trigger: samples.map || 'map:10020',
    entity_type: 'map',
  },
  item_modification: {
    trigger: findItemSample(),
    entity_type: 'item',
  },
};

const models = {};
for (const [key, tmpl] of Object.entries(templates)) {
  const prop = bfsPropagation(tmpl.trigger);
  const bestChain = prop.chains.sort((a, b) => b.depth - a.depth)[0];

  models[key] = {
    entity_type: tmpl.entity_type,
    trigger: tmpl.trigger,
    propagation_chain: bestChain?.path || [],
    max_depth: prop.max_depth,
    blast_radius: prop.blast_radius,
    blast_radius_total: prop.blast_radius_total,
    affected_roles: prop.affected_roles,
    sample_chains: prop.chains.slice(0, 5).map((c) => ({
      target: c.target,
      depth: c.depth,
      path_summary: c.path.map((h) => `${h.rel}(${h.semantic_role},w=${h.causal_weight})`).join(' -> '),
    })),
    why: prop.affected_roles.length
      ? `Change propagates via ${prop.affected_roles.join(', ')} edges — excluded NEIGHBOR_OF topology noise`
      : 'No downstream causal edges from trigger (isolated node in causal subgraph)',
  };
}

export const propagationResult = {
  phase: 'PROPAGATION_MODELS',
  model_count: Object.keys(models).length,
  subgraph_excludes: [...CAUSAL_SUBGRAPH_EXCLUDE],
  models,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'propagation_models.json'), propagationResult);

if (process.argv[1]?.includes('propagation-models')) {
  console.log(JSON.stringify({ model_count: propagationResult.model_count }, null, 2));
}
