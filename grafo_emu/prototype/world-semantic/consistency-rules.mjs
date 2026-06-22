#!/usr/bin/env node
/** Phase E — World consistency rules with REAL SQL violation scans */
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  parseTableInserts,
  writeJson,
  splitCsv,
  splitCsvPairs,
  idSet,
} from './_semantic-lib.mjs';

const sql = loadSql();
const SAMPLE = 5;

function rule(id, ruleText, evidence, confidence, violations, totalChecked) {
  return {
    rule_id: id,
    rule: ruleText,
    evidence,
    confidence,
    violation_count: violations.length,
    total_checked: totalChecked,
    violations_sample: violations.slice(0, SAMPLE),
    hypothesis: confidence < 0.7,
  };
}

const { rows: npcs } = parseTableInserts(sql, 'npcs');
const { rows: worldNpcs } = parseTableInserts(sql, 'worlds_npcs');
const { rows: quests } = parseTableInserts(sql, 'quests');
const { rows: questSteps } = parseTableInserts(sql, 'quests_steps');
const { rows: questObjectives } = parseTableInserts(sql, 'quests_objectives');
const { rows: items } = parseTableInserts(sql, 'items');
const { rows: monsters } = parseTableInserts(sql, 'monsters');
const { rows: dungeons } = parseTableInserts(sql, 'dungeons');
const { rows: worldMonsters } = parseTableInserts(sql, 'worlds_monsters');
const { rows: npcItems } = parseTableInserts(sql, 'npcs_items');
const { rows: npcActions } = parseTableInserts(sql, 'npcs_actions');

const npcIds = idSet(npcs, 'Id');
const itemIds = idSet(items, 'Id');
const monsterIds = idSet(monsters, 'Id');
const questIds = idSet(quests, 'Id');
const stepIds = idSet(questSteps, 'Id');

const spawnNpcIds = new Set(worldNpcs.map((r) => Number(r.Npc)).filter((n) => !Number.isNaN(n)));
const objectiveStepIds = new Set(questObjectives.map((r) => Number(r.Step)).filter((n) => !Number.isNaN(n)));

const rules = [];

// NPC without world spawn
const npcsWithoutSpawn = npcs.filter((n) => !spawnNpcIds.has(Number(n.Id)));
rules.push(
  rule(
    'npc_without_spawn',
    'Every npc template should have at least one worlds_npcs spawn row',
    ['npcs.Id', 'worlds_npcs.Npc', 'NpcManager.GetNpcSpawns()'],
    0.85,
    npcsWithoutSpawn.map((n) => ({ npc_id: n.Id, name: n.Name })),
    npcs.length,
  ),
);

// Spawn referencing missing NPC
const orphanSpawns = worldNpcs.filter((s) => !npcIds.has(Number(s.Npc)));
rules.push(
  rule(
    'spawn_orphan_npc',
    'worlds_npcs.Npc must reference existing npcs.Id',
    ['worlds_npcs.Npc -> npcs.Id'],
    0.95,
    orphanSpawns.map((s) => ({ spawn_npc: s.Npc, map: s.Map, cell: s.Cell })),
    worldNpcs.length,
  ),
);

// Quest without steps in StepIdsCSV
const questsNoSteps = quests.filter((q) => {
  const steps = splitCsv(q.StepIdsCSV);
  return steps.length === 0;
});
rules.push(
  rule(
    'quest_empty_steps_csv',
    'quests.StepIdsCSV should list at least one quests_steps.Id',
    ['quests.StepIdsCSV', 'quests_steps.Id'],
    0.9,
    questsNoSteps.map((q) => ({ quest_id: q.Id, name: q.Name })),
    quests.length,
  ),
);

// StepIdsCSV references missing step
const questsBadStepRef = [];
for (const q of quests) {
  for (const sid of splitCsv(q.StepIdsCSV)) {
    const id = Number(sid);
    if (!Number.isNaN(id) && !stepIds.has(id)) {
      questsBadStepRef.push({ quest_id: q.Id, missing_step_id: id });
    }
  }
}
rules.push(
  rule(
    'quest_step_csv_orphan',
    'quests.StepIdsCSV ids must exist in quests_steps',
    ['quests.StepIdsCSV -> quests_steps.Id'],
    0.9,
    questsBadStepRef,
    quests.length,
  ),
);

// Quest step without objectives
const stepsNoObjectives = questSteps.filter((s) => !objectiveStepIds.has(Number(s.Id)));
rules.push(
  rule(
    'quest_step_without_objectives',
    'quests_steps should have at least one quests_objectives row (Step = step Id)',
    ['quests_objectives.Step -> quests_steps.Id'],
    0.75,
    stepsNoObjectives.map((s) => ({ step_id: s.Id, quest: s.Quest, name: s.Name })),
    questSteps.length,
  ),
);

// Reward items not in catalog
const badRewardItems = [];
for (const s of questSteps) {
  for (const pair of splitCsvPairs(s.ItemsRewardCSV)) {
    if (!Number.isNaN(pair.id) && !itemIds.has(pair.id)) {
      badRewardItems.push({ step_id: s.Id, item_id: pair.id, qty: pair.qty });
    }
  }
}
rules.push(
  rule(
    'quest_reward_item_orphan',
    'quests_steps.ItemsRewardCSV item ids must exist in items',
    ['quests_steps.ItemsRewardCSV', 'items.Id'],
    0.9,
    badRewardItems,
    questSteps.length,
  ),
);

// Dungeon monsters not in catalog
const badDungeonMonsters = [];
for (const d of dungeons) {
  for (const mid of splitCsv(d.MonstersCSV)) {
    const id = Number(mid);
    if (!Number.isNaN(id) && !monsterIds.has(id)) {
      badDungeonMonsters.push({ dungeon_id: d.Id, map: d.Map, monster_id: id });
    }
  }
}
rules.push(
  rule(
    'dungeon_monster_orphan',
    'dungeons.MonstersCSV ids must exist in monsters',
    ['dungeons.MonstersCSV', 'monsters.Id'],
    0.9,
    badDungeonMonsters,
    dungeons.length,
  ),
);

// World monster group orphans
const badWorldMonsters = [];
for (const wm of worldMonsters) {
  for (const mid of splitCsv(wm.MonstersCSV)) {
    const id = Number(mid);
    if (!Number.isNaN(id) && !monsterIds.has(id)) {
      badWorldMonsters.push({ sub_area: wm.SubArea, monster_id: id });
    }
  }
}
rules.push(
  rule(
    'world_monster_group_orphan',
    'worlds_monsters.MonstersCSV ids must exist in monsters',
    ['worlds_monsters.MonstersCSV', 'monsters.Id'],
    0.9,
    badWorldMonsters,
    worldMonsters.length,
  ),
);

// Shop items orphan
const badShopItems = npcItems.filter((r) => !itemIds.has(Number(r.Item)));
rules.push(
  rule(
    'npc_shop_item_orphan',
    'npcs_items.Item must reference items.Id',
    ['npcs_items.Item -> items.Id'],
    0.95,
    badShopItems.map((r) => ({ npc_id: r.NpcId, item: r.Item, price: r.Price })),
    npcItems.length,
  ),
);

// Shop action param orphan
const badShopActions = npcActions.filter((a) => {
  if (String(a.Type).toLowerCase() !== 'shop') return false;
  const itemId = Number(a.Parameters);
  return !Number.isNaN(itemId) && !itemIds.has(itemId);
});
rules.push(
  rule(
    'npc_action_shop_item_orphan',
    "npcs_actions Type=Shop Parameters must reference valid item id",
    ['npcs_actions.Type=Shop', 'npcs_actions.Parameters -> items.Id'],
    0.85,
    badShopActions.map((a) => ({ npc_id: a.NpcId, param: a.Parameters })),
    npcActions.length,
  ),
);

// Quest step references quest not in quests
const stepsOrphanQuest = questSteps.filter((s) => !questIds.has(Number(s.Quest)));
rules.push(
  rule(
    'quest_step_orphan_quest',
    'quests_steps.Quest must reference quests.Id',
    ['quests_steps.Quest -> quests.Id'],
    0.95,
    stepsOrphanQuest.map((s) => ({ step_id: s.Id, quest: s.Quest })),
    questSteps.length,
  ),
);

export const result = {
  phase: 'E_WORLD_CONSISTENCY_RULES',
  rule_count: rules.length,
  total_violations: rules.reduce((s, r) => s + r.violation_count, 0),
  rules,
};

const outDir = dirname(fileURLToPath(import.meta.url));
writeJson(join(outDir, 'consistency_rules.json'), result);

if (process.argv[1]?.includes('consistency-rules')) {
  console.log(JSON.stringify({
    rule_count: rules.length,
    total_violations: result.total_violations,
  }, null, 2));
}
