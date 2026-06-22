#!/usr/bin/env node
/** Phase E — Evidence-backed query capabilities */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from './_model-lib.mjs';
import { result as systems } from './discover-systems.mjs';
import { result as sqlInv } from './sql-inventory.mjs';
import { result as graphInv } from './graph-inventory.mjs';

const queries = [];
let qid = 0;

for (const sys of systems.systems) {
  if (sys.tables_involved.length >= 2) {
    queries.push({
      query_id: `q_${++qid}`,
      question_template: `What entities participate in ${sys.label}?`,
      question_example: `What entities participate in ${sys.tables_involved[0] || sys.label}?`,
      evidence_source: { type: 'system', system_id: sys.system_id, tables: sys.tables_involved },
      answerable: true,
      confidence: sys.cohesion,
    });
  }

  for (const tbl of sys.tables_involved.slice(0, 3)) {
    const inv = sqlInv.tables.find((t) => t.table === tbl);
    if (inv?.inferred_foreign_keys_out?.length) {
      const refs = inv.inferred_foreign_keys_out.map((fk) => fk.references);
      queries.push({
        query_id: `q_${++qid}`,
        question_template: `What is ${tbl} related to?`,
        question_example: `What NPCs are related to ${tbl} rows?`,
        evidence_source: { type: 'sql_fk', table: tbl, references: refs },
        answerable: true,
        confidence: 0.8,
      });
    }
  }
}

const orphanCandidates = sqlInv.tables.filter((t) =>
  t.table.startsWith('worlds_') && t.inferred_foreign_keys_in.length === 0 && t.row_count > 0,
);
if (orphanCandidates.length) {
  queries.push({
    query_id: `q_${++qid}`,
    question_template: 'Which maps contain orphan or unreferenced world content?',
    question_example: 'Which maps contain NPC spawns without matching npcs template?',
    evidence_source: { type: 'sql_orphan_analysis', tables: ['worlds_npcs', 'worlds_monsters', 'npcs', 'monsters'] },
    answerable: true,
    confidence: 0.75,
    note: 'requires cross-table join; no graph Map nodes in prototype',
  });
}

const rewardTables = sqlInv.tables.filter((t) =>
  t.columns.some((c) => /kamasreward|itemsreward|experiencereward/i.test(c)),
);
for (const rt of rewardTables) {
  queries.push({
    query_id: `q_${++qid}`,
    question_template: `What rewards are defined in ${rt.table}?`,
    question_example: 'What quest rewards are potentially unbalanced by kamas total?',
    evidence_source: { type: 'sql_column', table: rt.table, columns: rt.columns.filter((c) => /reward/i.test(c)) },
    answerable: true,
    confidence: 0.9,
  });
}

for (const c of graphInv.clusters) {
  queries.push({
    query_id: `q_${++qid}`,
    question_template: `What graph relationships exist in cluster ${c.label}?`,
    question_example: `What spells show static vs observed conflicts in ${c.label}?`,
    evidence_source: { type: 'graph_cluster', cluster_id: c.cluster_id, rel_signature: c.rel_signature },
    answerable: c.node_count > 0,
    confidence: c.coherence_score,
  });
}

for (const sys of systems.systems.filter((s) => s.tables_involved.length >= 2)) {
  queries.push({
    query_id: `q_${++qid}`,
    question_template: `What changes would modifying ${sys.tables_involved[0] || 'a core table'} produce?`,
    question_example: `What changes would modifying npcs_items prices produce?`,
    evidence_source: { type: 'system_cascade', system_id: sys.system_id, outbound_fks: sys.evidence.filter((e) => e.type === 'sql_fk') },
    answerable: sys.relations_observed > 0,
    confidence: 0.7,
    hypothesis: sys.external_coupling > 5,
  });
}

const deduped = [];
const seen = new Set();
for (const q of queries) {
  const k = q.question_template;
  if (seen.has(k)) continue;
  seen.add(k);
  deduped.push(q);
}

export const result = {
  phase: 'E_QUERY_CAPABILITIES',
  query_count: deduped.length,
  answerable_count: deduped.filter((q) => q.answerable).length,
  queries: deduped,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'query_capabilities.json'), result);

if (process.argv[1]?.includes('discover-queries')) {
  console.log(JSON.stringify({ query_count: result.query_count }, null, 2));
}
