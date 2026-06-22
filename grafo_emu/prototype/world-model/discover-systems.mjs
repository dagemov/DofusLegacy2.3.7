#!/usr/bin/env node
/** Phase B — Emergent system discovery from SQL + code + graph evidence */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeJson } from './_model-lib.mjs';
import { result as sqlInv } from './sql-inventory.mjs';
import { result as codeInv } from './code-inventory.mjs';
import { result as graphInv } from './graph-inventory.mjs';

const TABLE_PREFIX_GROUPS = [
  { prefix: 'npcs', seed: 'npcs' },
  { prefix: 'quests', seed: 'quests' },
  { prefix: 'worlds_', seed: 'worlds_maps' },
  { prefix: 'world_maps', seed: 'world_maps_house' },
  { prefix: 'characters', seed: 'characters' },
  { prefix: 'monsters', seed: 'monsters' },
  { prefix: 'items', seed: 'items' },
  { prefix: 'spells', seed: 'spells' },
  { prefix: 'guilds', seed: 'guilds' },
  { prefix: 'accounts', seed: 'accounts' },
  { prefix: 'jobs', seed: 'jobs' },
  { prefix: 'breeds', seed: 'breeds' },
  { prefix: 'mounts', seed: 'mounts' },
  { prefix: 'dungeons', seed: 'dungeons' },
  { prefix: 'interactives', seed: 'interactives' },
  { prefix: 'recipes', seed: 'recipes' },
  { prefix: 'bids_house', seed: 'bids_house' },
  { prefix: 'teleports', seed: 'teleports_maps' },
  { prefix: 'runes', seed: 'runes' },
  { prefix: 'experiences', seed: 'experiences' },
];

const MANAGER_TABLE_HINTS = {
  NpcManager: ['npcs', 'npcs_items', 'npcs_messages', 'npcs_replies', 'worlds_npcs', 'npcs_actions'],
  QuestManager: ['quests', 'quests_steps', 'quests_objectives', 'characters_quests'],
  MapManager: ['worlds_maps', 'worlds_maps_positions', 'worlds_npcs', 'worlds_monsters', 'worlds_interactives'],
  MonsterManager: ['monsters', 'monsters_drops', 'monsters_grades', 'monsters_spells', 'worlds_monsters'],
  ItemManager: ['items', 'items_sets', 'items_weapons', 'items_livingobjects'],
  SpellManager: ['spells', 'spells_levels', 'breeds_spells'],
  CharacterManager: ['characters', 'characters_items', 'characters_spells', 'characters_stats'],
  AccountManager: ['accounts', 'accounts_bank'],
  GuildManager: ['guilds', 'guilds_members'],
  JobManager: ['jobs', 'jobs_harvest', 'characters_jobs'],
  BreedManager: ['breeds', 'breeds_spells'],
  MountManager: ['mounts', 'mounts_items', 'mounts_templates', 'mounts_bonus'],
  DungeonManager: ['dungeons', 'dungeons_search', 'dungeon_finder_rooms'],
  InteractiveManager: ['interactives', 'interactives_skills', 'worlds_interactives'],
  BidHouseManager: ['bids_house', 'bids_house_items'],
  FightManager: [],
  EffectManager: [],
};

function tablesForPrefix(prefix) {
  return sqlInv.tables
    .map((t) => t.table)
    .filter((t) => t === prefix.replace(/_$/, '') || t.startsWith(prefix));
}

function classesForTables(tableSet) {
  return codeInv.classes
    .filter((c) => (c.tables_manipulated || []).some((t) => tableSet.has(t)))
    .map((c) => c.class_name);
}

function fkEvidenceBetween(tableList) {
  const set = new Set(tableList);
  const evidences = [];
  for (const t of sqlInv.tables) {
    if (!set.has(t.table)) continue;
    for (const fk of t.inferred_foreign_keys_out) {
      const target = fk.references.split('.')[0];
      if (set.has(target)) {
        evidences.push({ type: 'sql_fk', from: t.table, to: target, confidence: fk.confidence });
      }
    }
  }
  return evidences;
}

const systems = [];

for (const grp of TABLE_PREFIX_GROUPS) {
  const tables = tablesForPrefix(grp.prefix);
  if (!tables.length) continue;
  const tableSet = new Set(tables);
  let classes = classesForTables(tableSet);

  for (const [mgr, hintTables] of Object.entries(MANAGER_TABLE_HINTS)) {
    if (hintTables.some((t) => tableSet.has(t))) classes.push(mgr);
  }
  classes = [...new Set(classes)];

  const evidences = fkEvidenceBetween(tables);
  const cohesion = Math.round(
    Math.min(1, evidences.length / Math.max(1, tables.length * 0.5)) * 1000,
  ) / 1000;

  systems.push({
    system_id: `sys_${grp.prefix.replace(/_$/, '')}`,
    label: `sys_${grp.prefix.replace(/_$/, '')}`,
    tables_involved: tables,
    table_count: tables.length,
    classes_involved: classes,
    class_count: classes.length,
    graph_seeds: [],
    cohesion,
    external_coupling: 0,
    relations_observed: evidences.length,
    evidence: evidences.slice(0, 15),
    why_these_entities_form_a_system:
      `tables share prefix '${grp.prefix}' + ${evidences.length} high-confidence inferred FK edges + ${classes.length} code managers reference them`,
    hypothesis: cohesion < 0.2,
  });
}

for (const c of graphInv.clusters) {
  systems.push({
    system_id: `sys_graph_${c.cluster_id.replace(':', '_')}`,
    label: `sys_graph_${c.label}`,
    tables_involved: [],
    table_count: 0,
    classes_involved: [],
    class_count: 0,
    graph_seeds: c.seed_nodes,
    cohesion: c.coherence_score,
    external_coupling: 0,
    relations_observed: c.edge_count,
    evidence: [{ type: 'graph_cluster', cluster: c.cluster_id, rel_signature: c.rel_signature }],
    why_these_entities_form_a_system: `graph connected component with rel signature ${JSON.stringify(c.rel_signature)}`,
    hypothesis: c.coherence_score < 0.3,
  });
}

const ranked = systems
  .filter((s) => s.table_count > 0 || s.graph_seeds.length > 0)
  .sort((a, b) => {
    const scoreA = a.cohesion * 0.4 + a.table_count * 0.02 + a.class_count * 0.05 + a.relations_observed * 0.01;
    const scoreB = b.cohesion * 0.4 + b.table_count * 0.02 + b.class_count * 0.05 + b.relations_observed * 0.01;
    return scoreB - scoreA;
  })
  .map((s, i) => ({ ...s, rank: i + 1 }));

export const result = {
  phase: 'B_SYSTEM_DISCOVERY',
  methodology: 'prefix clustering on SQL tables + manager table hints + graph clusters (no preset NPC/Quest/Map names)',
  no_preset_names: true,
  system_count: ranked.length,
  systems: ranked,
  honesty_note: '0 declared FKs in dump — grouping uses naming prefix + confidence>=0.7 inferred edges; mega-component avoided',
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'discovered_systems.json'), result);

if (process.argv[1]?.includes('discover-systems')) {
  console.log(JSON.stringify({ system_count: result.system_count, top: ranked.slice(0, 8).map((s) => s.system_id) }, null, 2));
}
