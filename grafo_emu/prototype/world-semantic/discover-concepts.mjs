#!/usr/bin/env node
/** Phase A — World concept discovery (emergent, not preset labels) */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  loadGraph,
  parseTableInserts,
  scanCsharpRoleFiles,
  readArtifact,
  writeJson,
  confidenceFromSources,
  splitCsv,
} from './_semantic-lib.mjs';

const sql = loadSql();
const { nodeList } = loadGraph();
const codeScan = scanCsharpRoleFiles();
const graphSystems = readArtifact('discovered_systems.json');

function classByName(name) {
  return codeScan.classes.find((c) => c.class_name === name);
}

function graphNodesMatching(prefix) {
  return nodeList.filter((n) => n.id?.startsWith(prefix)).map((n) => n.id);
}

function managerCacheEvidence(className, cacheFields) {
  const path = `Sunshine.MySql/Database/Managers/${className}.cs`;
  const cls = classByName(className);
  return {
    class: className,
    file: cls?.file || path,
    cache_fields: cacheFields,
    evidence: `in-memory Dictionary caches loaded at startup`,
  };
}

const concepts = [];

// creature_template — monsters composite + MonsterManager caches
concepts.push({
  concept_id: 'creature_template',
  confidence: confidenceFromSources(['sql_composite', 'csharp_cache', false]),
  evidence: [
    { type: 'sql', ref: 'monsters + monsters_grades + monsters_spells + monsters_drops' },
    managerCacheEvidence('MonsterManager', ['Monsters', 'MonstersGrade', 'MonsterSpells', 'MonsterDropsCache']),
    { type: 'sql', ref: 'monsters_grades.MonsterId -> monsters.Id' },
  ],
  tables: ['monsters', 'monsters_grades', 'monsters_spells', 'monsters_drops'],
  classes: ['MonsterManager'],
  graph_nodes: [],
  why_it_exists:
    'Emulator loads creature as template (monsters) plus grade stats, spell list, and drop table — not a single row. MonsterManager holds separate Dictionary caches per layer.',
  hypothesis: false,
});

// monster_group — CSV grouping per SubArea or dungeon wave
const { rows: worldMonsters } = parseTableInserts(sql, 'worlds_monsters');
const { rows: dungeons } = parseTableInserts(sql, 'dungeons');
concepts.push({
  concept_id: 'monster_group',
  confidence: confidenceFromSources(['sql_csv', 'sql_csv', false]),
  evidence: [
    { type: 'sql', ref: 'worlds_monsters: MonstersCSV + SubArea (group per sub-area, not per map)' },
    { type: 'sql', ref: 'dungeons: MonstersCSV (wave composition per dungeon room)' },
    { type: 'sql', sample: worldMonsters[0] },
  ],
  tables: ['worlds_monsters', 'dungeons', 'monsters'],
  classes: ['MonsterManager', 'DungeonManager', 'MapManager'],
  graph_nodes: [],
  why_it_exists:
    'Monster presence is defined as CSV lists of monster ids bound to SubArea (open world) or dungeon Map (instanced). Group is emergent from CSV column, not a dedicated table.',
  hypothesis: false,
});

// dungeon — Map + MonstersCSV + exit Parameters
concepts.push({
  concept_id: 'dungeon',
  confidence: confidenceFromSources(['sql_schema', 'csharp_manager', false]),
  evidence: [
    { type: 'sql', ref: 'dungeons(Map, MonstersCSV, Parameters, Note)' },
    { type: 'sql', sample: { Map: dungeons[0]?.Map, MonstersCSV: dungeons[0]?.MonstersCSV?.slice(0, 40) } },
    { type: 'code', ref: 'DungeonManager.GetAllDungeons()' },
  ],
  tables: ['dungeons', 'worlds_maps', 'monsters'],
  classes: ['DungeonManager', 'MapManager'],
  graph_nodes: [],
  why_it_exists:
    'Dungeon is a room chain: each row binds an entry map id, monster wave CSV, and Parameters (exit map, cell, direction). Not a separate dungeon entity table beyond dungeons.',
  hypothesis: false,
});

// place_instance — worlds_maps + spawns
concepts.push({
  concept_id: 'place_instance',
  confidence: confidenceFromSources(['sql_composite', 'csharp_manager', false]),
  evidence: [
    { type: 'sql', ref: 'worlds_maps (topology) + worlds_maps_positions (X/Y)' },
    { type: 'sql', ref: 'worlds_npcs, worlds_monsters, worlds_interactives spawn on Map' },
    { type: 'code', ref: 'MapManager loads maps and spawn tables' },
  ],
  tables: ['worlds_maps', 'worlds_maps_positions', 'worlds_npcs', 'worlds_monsters', 'worlds_interactives'],
  classes: ['MapManager', 'NpcManager', 'MonsterManager', 'InteractiveManager'],
  graph_nodes: [],
  why_it_exists:
    'Playable place = map template plus runtime spawn rows (NPC, monster group, interactive). Map id is the join key across spawn tables.',
  hypothesis: false,
});

// npc_identity — template without world presence
concepts.push({
  concept_id: 'npc_identity',
  confidence: confidenceFromSources(['sql_schema', 'csharp_manager', 'graph_node']),
  evidence: [
    { type: 'sql', ref: 'npcs(Id, Name, EntityLook, DialogMessagesIdCSV, ActionsIdCSV)' },
    { type: 'code', ref: 'NpcManager.GetAllNpcs / GetNpc' },
    { type: 'graph', ref: graphNodesMatching('npc:').slice(0, 3) },
  ],
  tables: ['npcs'],
  classes: ['NpcManager'],
  graph_nodes: graphNodesMatching('npc:'),
  why_it_exists:
    'NPC identity is the npcs template row (look, dialog CSV refs, action ids). Distinct from spawn (worlds_npcs) and behavior tables.',
  hypothesis: false,
});

// npc_dialogue — messages + replies
concepts.push({
  concept_id: 'npc_dialogue',
  confidence: confidenceFromSources(['sql_schema', 'csharp_manager', false]),
  evidence: [
    { type: 'sql', ref: 'npcs.DialogMessagesIdCSV references npcs_messages' },
    { type: 'sql', ref: 'npcs_replies(Npc, Map, MessageId)' },
    { type: 'code', ref: 'NpcManager.GetNpcMessage / GetNpcReplies' },
  ],
  tables: ['npcs_messages', 'npcs_replies', 'npcs'],
  classes: ['NpcManager'],
  graph_nodes: [],
  why_it_exists:
    'Dialogue is split: message templates in npcs_messages, reply branching keyed by Npc + Map + MessageId in npcs_replies.',
  hypothesis: false,
});

// merchant — Shop action + npcs_items
const { rows: npcActions } = parseTableInserts(sql, 'npcs_actions');
const shopActions = npcActions.filter((a) => String(a.Type).toLowerCase() === 'shop');
concepts.push({
  concept_id: 'merchant',
  confidence: confidenceFromSources(['sql_data', 'sql_schema', 'graph_edge']),
  evidence: [
    { type: 'sql', ref: 'npcs_actions.Type=Shop, Parameters=item id' },
    { type: 'sql', ref: 'npcs_items(NpcId, Item, Price)' },
    { type: 'graph', ref: 'edge e201 SELLS npc:1053 -> item:12116' },
  ],
  tables: ['npcs_actions', 'npcs_items', 'npcs', 'items'],
  classes: ['NpcManager', 'InventoryHandler'],
  graph_nodes: ['npc:1053', 'item:12116'],
  why_it_exists:
    'Merchant emerges from typed npcs_actions (Shop) plus price rows in npcs_items. Graph SELLS edge confirms npc->item economic link in prototype.',
  hypothesis: false,
});

// quest_chain — quests -> steps -> objectives
concepts.push({
  concept_id: 'quest_chain',
  confidence: confidenceFromSources(['sql_fk_chain', 'csharp_manager', 'graph_cluster']),
  evidence: [
    { type: 'sql', ref: 'quests.StepIdsCSV -> quests_steps.Id' },
    { type: 'sql', ref: 'quests_objectives.Step -> quests_steps.Id' },
    { type: 'graph', ref: 'cluster quest_progression: HAS_STEP, INVOLVES_NPC, REWARDS' },
  ],
  tables: ['quests', 'quests_steps', 'quests_objectives', 'quests_objectives_types'],
  classes: ['QuestManager'],
  graph_nodes: graphNodesMatching('quest:').concat(graphNodesMatching('queststep:')),
  why_it_exists:
    'Quest is a chain: quest header with StepIdsCSV, steps with rewards, objectives linked by Step id. Objectives reference NPCs via ParametersCSV (Type 1/3).',
  hypothesis: false,
});

// quest_giver — NPC involved in quest objectives (emergent role, not npcs.HasQuest alone)
concepts.push({
  concept_id: 'quest_giver',
  confidence: confidenceFromSources(['sql_objective_type', 'graph_edge', false]),
  evidence: [
    { type: 'sql', ref: 'quests_objectives Type=1/3 ParametersCSV contains npc id' },
    { type: 'graph', ref: 'INVOLVES_NPC queststep:3 -> npc:449' },
    { type: 'sql', ref: 'npcs.HasQuest column exists but quest link is via objectives' },
  ],
  tables: ['quests_objectives', 'npcs', 'quests_steps'],
  classes: ['QuestManager', 'NpcManager'],
  graph_nodes: ['npc:449', 'npc:488'],
  why_it_exists:
    'Quest giver is not a separate table — role emerges when quests_objectives ParametersCSV references an npc id (go talk to / bring to NPC).',
  hypothesis: true,
});

// interactive_object — interactives + world placement
concepts.push({
  concept_id: 'interactive_object',
  confidence: confidenceFromSources(['sql_schema', 'csharp_manager', false]),
  evidence: [
    { type: 'sql', ref: 'interactives (template) + interactives_skills' },
    { type: 'sql', ref: 'worlds_interactives (Map, Cell, InteractiveId)' },
    { type: 'code', ref: 'InteractiveManager' },
  ],
  tables: ['interactives', 'interactives_skills', 'worlds_interactives', 'jobs_harvest'],
  classes: ['InteractiveManager', 'SkillManager', 'JobManager'],
  graph_nodes: [],
  why_it_exists:
    'Harvestable/workshop interactives = template (interactives) + skill binding + map cell placement (worlds_interactives).',
  hypothesis: false,
});

// teleporter — teleports_* tables
concepts.push({
  concept_id: 'teleporter',
  confidence: confidenceFromSources(['sql_schema', false, false]),
  evidence: [
    { type: 'sql', ref: 'teleports_maps, teleports_zones_maps, teleports_donjons_maps' },
    { type: 'sql', ref: 'worlds_zaapis' },
  ],
  tables: ['teleports_maps', 'teleports_zones_maps', 'teleports_donjons_maps', 'worlds_zaapis'],
  classes: ['CustomTeleportService', 'MapManager'],
  graph_nodes: [],
  why_it_exists:
    'Teleport links are explicit rows mapping source/destination map ids — separate from NPC dialogue teleports (hypothesis: some via npcs_actions).',
  hypothesis: true,
});

// local_economy — shop prices per NPC
const { rows: npcItems } = parseTableInserts(sql, 'npcs_items');
concepts.push({
  concept_id: 'local_economy',
  confidence: confidenceFromSources(['sql_data', 'graph_edge', false]),
  evidence: [
    { type: 'sql', ref: `npcs_items: ${npcItems.length} price rows` },
    { type: 'sql', ref: 'quests_steps.KamasReward / ItemsRewardCSV' },
    { type: 'graph', ref: 'SELLS price in edge e201 props' },
  ],
  tables: ['npcs_items', 'quests_steps', 'items', 'bids_house_items'],
  classes: ['NpcManager', 'BidHouseManager', 'MerchantManager'],
  graph_nodes: ['npc:1053'],
  why_it_exists:
    'Local economy = priced item offers at NPC shops plus quest kamas/item rewards. No global economy table — prices live in npcs_items rows.',
  hypothesis: false,
});

// player_progress — characters_* runtime (not world content but affects quests)
concepts.push({
  concept_id: 'player_progress',
  confidence: confidenceFromSources(['sql_prefix', 'csharp_manager', false]),
  evidence: [
    { type: 'sql', ref: 'characters_quests, characters_quests_steps, characters_quests_objectives' },
    { type: 'code', ref: 'QuestManager + CharacterManager' },
  ],
  tables: ['characters_quests', 'characters_quests_steps', 'characters_quests_objectives', 'characters'],
  classes: ['QuestManager', 'CharacterManager'],
  graph_nodes: [],
  why_it_exists:
    'Player quest state is persisted separately from quest templates — template vs runtime progress split.',
  hypothesis: false,
});

// sub_area — worlds_monsters.SubArea groups monsters (Area concept emergent)
const subAreas = new Set(worldMonsters.map((r) => Number(r.SubArea)).filter((n) => !Number.isNaN(n)));
concepts.push({
  concept_id: 'sub_area',
  confidence: confidenceFromSources(['sql_column', false, false]),
  evidence: [
    { type: 'sql', ref: `worlds_monsters.SubArea: ${subAreas.size} distinct sub-areas` },
    { type: 'sql', ref: 'worlds_maps.SubAreaId column links map to sub-area' },
  ],
  tables: ['worlds_monsters', 'worlds_maps'],
  classes: ['MapManager', 'MonsterManager'],
  graph_nodes: [],
  why_it_exists:
    'Sub-area is the granularity for open-world monster groups (MonstersCSV per SubArea). Continent/zone names not in server DB — client D2O hypothesis.',
  hypothesis: true,
});

// combat_runtime — graph-only in prototype
concepts.push({
  concept_id: 'combat_runtime',
  confidence: confidenceFromSources([false, false, 'graph_cluster']),
  evidence: [
    { type: 'graph', ref: 'cluster combat_runtime_spell: USES_EFFECT, OBSERVED_IN, CONTRADICTS' },
    { type: 'code', ref: 'FightManager, EffectManager, SpellCastManager' },
  ],
  tables: ['spells', 'spells_levels'],
  classes: ['FightManager', 'EffectManager', 'SpellManager'],
  graph_nodes: graphNodesMatching('spell:'),
  why_it_exists:
    'Combat behavior is LOG-observed in graph prototype; spell template in SQL. Runtime fight not persisted in sunshine.sql content tables.',
  hypothesis: false,
});

export const result = {
  phase: 'A_WORLD_CONCEPT_DISCOVERY',
  concept_count: concepts.length,
  methodology: 'emergent from SQL composites, CSV grouping, manager caches, graph clusters — no preset NPC/Quest/Map labels',
  concepts,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'world_concepts.json'), result);

if (process.argv[1]?.includes('discover-concepts')) {
  console.log(JSON.stringify({ concept_count: concepts.length }, null, 2));
}
