#!/usr/bin/env node
/** Simulates IGraphMutationPlanner — intent → dry-run mutation plan */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  readCausalEdges,
  readRelArtifact,
  buildAdjacency,
  CAUSAL_SUBGRAPH_EXCLUDE,
  GRAPH_TO_RUNTIME,
  INTENT_CATALOG,
  parseNodeId,
  assertNoWrite,
  rollbackSketch,
  writeJson,
  loadSql,
  tablesExistInSql,
} from './_sim-lib.mjs';

const edges = readCausalEdges();
const graphPhase20 = readRelArtifact('relationship_graph.json');
const sql = loadSql();
const samples = graphPhase20.entity_samples || {};
const { out } = buildAdjacency(edges, CAUSAL_SUBGRAPH_EXCLUDE);

const SAMPLE_INTENTS = [
  { intent_id: 'modify_npc', target: samples.npc || 'npc:462' },
  { intent_id: 'modify_quest', target: samples.quest || 'quest:3' },
  { intent_id: 'modify_monster', target: samples.monster || 'monster:31' },
  { intent_id: 'modify_dungeon', target: samples.dungeon || 'dungeon:1' },
  { intent_id: 'modify_map', target: samples.map || 'map:10020' },
  { intent_id: 'modify_item', target: 'item:519' },
  { intent_id: 'create_quest', target: null },
  { intent_id: 'create_merchant', target: null },
];

function traverseForIntent(targetNode) {
  const visited = new Set();
  const relsUsed = new Set();
  const chain = [];
  const queue = [targetNode];
  let depth = 0;
  while (queue.length && depth < 4) {
    const next = [];
    for (const node of queue) {
      const nodeEdges = (out.get(node) || []).slice(0, 5);
      for (const e of nodeEdges) {
        if (visited.has(e.id)) continue;
        visited.add(e.id);
        relsUsed.add(e.rel);
        chain.push({
          rel: e.rel,
          src: e.src,
          dst: e.dst,
          causal_weight: e.causal_weight,
          semantic_role: e.semantic_role,
        });
        next.push(e.dst);
      }
    }
    queue.length = 0;
    queue.push(...next);
    depth += 1;
  }
  return { chain, rels_used: [...relsUsed] };
}

function buildMutationPlan(intentId, target) {
  const catalog = INTENT_CATALOG[intentId];
  const targetType = catalog.target_type;
  const runtime = GRAPH_TO_RUNTIME[targetType];
  const isCreate = catalog.action === 'create';

  const targetNode = target || `${targetType}:DRY_RUN_PLACEHOLDER`;
  const { type, id } = parseNodeId(targetNode);
  const traversal = target ? traverseForIntent(target) : { chain: [], rels_used: [] };

  const statements = runtime.sql_tables.map((table, i) => ({
    order: i + 1,
    table,
    operation: isCreate ? 'INSERT' : 'UPDATE',
    note: `${isCreate ? 'Create' : 'Modify'} via ${runtime.write_path}`,
  }));

  const tableChecks = tablesExistInSql(sql, runtime.sql_tables);

  return {
    intent_id: intentId,
    intent_description: catalog.description,
    action: catalog.action,
    target_node: targetNode,
    target_type: type || targetType,
    target_id: id,
    dry_run: true,
  ...assertNoWrite(),
    graph_traversal: traversal,
    mutation_plan: {
      order: statements.map((s) => `${s.operation} ${s.table}`),
      statements,
      cs_manager: runtime.cs_manager,
      write_path: runtime.write_path,
      rollback_sketch: isCreate
        ? rollbackSketch(runtime.sql_tables, 'NEW_ID', targetType)
        : rollbackSketch(runtime.sql_tables, id, targetType),
    },
    schema_validation: tableChecks,
    integrity_valid: tableChecks.every((t) => t.exists),
  };
}

export const plansResult = {
  phase: 'EXECUTION_PLANS',
  planner: 'IGraphMutationPlanner (simulated)',
  plan_count: SAMPLE_INTENTS.length,
  plans: SAMPLE_INTENTS.map(({ intent_id, target }) => buildMutationPlan(intent_id, target)),
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'execution_plans.json'), plansResult);

if (process.argv[1]?.includes('intent-plan')) {
  console.log(JSON.stringify({ plan_count: plansResult.plan_count }, null, 2));
}
