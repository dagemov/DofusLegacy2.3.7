import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from '../world-semantic/_semantic-lib.mjs';

export { writeJson };

export const WC_DIR = dirname(fileURLToPath(import.meta.url));
export const WR_DIR = join(WC_DIR, '..', 'world-relations');

const DERIVATIVE_METHODS = new Set(['derived-from-objectives', 'path-derive']);

export const SEMANTIC_ROLE_BY_REL = {
  NEIGHBOR_OF: 'STRUCTURAL',
  IN_SUBAREA: 'STRUCTURAL',
  LOCATED_AT: 'STRUCTURAL',
  EXITS_TO: 'STRUCTURAL',
  TELEPORT_FROM: 'STRUCTURAL',
  SPAWNED_IN: 'STRUCTURAL',
  CONTAINS_MONSTER: 'FUNCTIONAL',
  SPAWNS_MONSTER: 'FUNCTIONAL',
  DEFEAT_MONSTER: 'FUNCTIONAL',
  REQUIRES_ITEM: 'FUNCTIONAL',
  OFFERS_ACTION: 'FUNCTIONAL',
  USES_SPELL: 'BEHAVIORAL',
  SELLS: 'ECONOMIC',
  DROPS_ITEM: 'ECONOMIC',
  REWARDS_ITEM: 'ECONOMIC',
  REWARDS_SPELL: 'ECONOMIC',
  HAS_STEP: 'NARRATIVE',
  HAS_OBJECTIVE: 'NARRATIVE',
  INVOLVES_NPC: 'NARRATIVE',
  STARTS_QUEST: 'NARRATIVE',
  PARTICIPATES_IN_MAP: 'NARRATIVE',
  DISCOVER_MAP: 'NARRATIVE',
  DISCOVER_AREA: 'NARRATIVE',
};

export const BASE_WEIGHT_BY_REL = {
  SPAWNED_IN: 0.9,
  LOCATED_AT: 0.9,
  HAS_STEP: 0.9,
  HAS_OBJECTIVE: 0.9,
  INVOLVES_NPC: 0.9,
  CONTAINS_MONSTER: 0.9,
  SPAWNS_MONSTER: 0.9,
  SELLS: 0.6,
  DROPS_ITEM: 0.6,
  USES_SPELL: 0.6,
  EXITS_TO: 0.6,
  DEFEAT_MONSTER: 0.6,
  REQUIRES_ITEM: 0.6,
  REWARDS_ITEM: 0.6,
  REWARDS_SPELL: 0.6,
  DISCOVER_MAP: 0.6,
  DISCOVER_AREA: 0.6,
  OFFERS_ACTION: 0.6,
  PARTICIPATES_IN_MAP: 0.6,
  STARTS_QUEST: 0.6,
  IN_SUBAREA: 0.3,
  NEIGHBOR_OF: 0.3,
  TELEPORT_FROM: 0.3,
};

export const PROPAGATION_DEPTH_BY_REL = {
  STARTS_QUEST: 4,
  HAS_STEP: 3,
  INVOLVES_NPC: 3,
  HAS_OBJECTIVE: 2,
  SPAWNED_IN: 2,
  LOCATED_AT: 2,
  EXITS_TO: 2,
  DISCOVER_MAP: 2,
  PARTICIPATES_IN_MAP: 2,
  CONTAINS_MONSTER: 1,
  SPAWNS_MONSTER: 1,
  DEFEAT_MONSTER: 1,
  IN_SUBAREA: 1,
  DISCOVER_AREA: 1,
  USES_SPELL: 0,
  DROPS_ITEM: 0,
  SELLS: 0,
  REWARDS_ITEM: 0,
  REWARDS_SPELL: 0,
  REQUIRES_ITEM: 0,
  NEIGHBOR_OF: 0,
  TELEPORT_FROM: 0,
  OFFERS_ACTION: 0,
};

export const CAUSAL_SUBGRAPH_EXCLUDE = new Set(['NEIGHBOR_OF']);

export function readRelationsArtifact(name) {
  const path = join(WR_DIR, name);
  return JSON.parse(readFileSync(path, 'utf8'));
}

export function readRelationsEdges() {
  const path = join(WR_DIR, 'recovered_edges.jsonl');
  const text = readFileSync(path, 'utf8');
  return text
    .split('\n')
    .filter(Boolean)
    .map((line) => JSON.parse(line));
}

export function semanticRoleForEdge(edge) {
  const method = edge.provenance?.method;
  if (method && DERIVATIVE_METHODS.has(method)) return 'DERIVATIVE';
  if (edge.hypothesis && (edge.rel === 'STARTS_QUEST' || edge.rel === 'PARTICIPATES_IN_MAP')) {
    return 'DERIVATIVE';
  }
  return SEMANTIC_ROLE_BY_REL[edge.rel] || 'STRUCTURAL';
}

export function causalWeight(edge) {
  const base = BASE_WEIGHT_BY_REL[edge.rel] ?? 0.3;
  const conf = edge.confidence ?? 1.0;
  let w = base * (0.6 + 0.4 * conf);
  if (edge.status === 'ref-only') w *= 0.5;
  return Math.round(w * 100) / 100;
}

export function gameplayImpact(weight) {
  if (weight >= 0.75) return 'HIGH';
  if (weight >= 0.5) return 'MEDIUM';
  if (weight >= 0.3) return 'LOW';
  return 'NEGLIGIBLE';
}

export function modificationRisk(weight, dstFanIn) {
  if (weight >= 0.75 && dstFanIn >= 10) return 'HIGH';
  if (weight >= 0.5 || dstFanIn >= 5) return 'MEDIUM';
  return 'LOW';
}

export function propagationDepth(edge) {
  return PROPAGATION_DEPTH_BY_REL[edge.rel] ?? 0;
}

export function enrichEdge(edge, dstFanIn) {
  const role = semanticRoleForEdge(edge);
  const weight = causalWeight(edge);
  const derivative = role === 'DERIVATIVE';
  return {
    ...edge,
    relation: edge.rel,
    semantic_role: role,
    causal_weight: weight,
    gameplay_impact: gameplayImpact(weight),
    modification_risk: modificationRisk(weight, dstFanIn),
    propagation_depth: propagationDepth(edge),
    derivative,
  };
}

export function weightBucket(w) {
  if (w >= 0.9) return '0.9';
  if (w >= 0.6) return '0.6';
  if (w >= 0.3) return '0.3';
  return '0.0';
}
