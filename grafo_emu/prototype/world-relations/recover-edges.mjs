#!/usr/bin/env node
/** Phase 20 — Recover implicit SQL references as explicit graph edges */
import { writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  loadSql,
  parseTableInserts,
  splitCsv,
  splitCsvPairs,
  nodeId,
  makeEdge,
  resetEdgeSeq,
  objectiveTypeRoles,
  OBJECTIVE_TYPE_RELS,
  buildCatalogs,
  edgeStatusForDst,
  parseNodeId,
} from './_relations-lib.mjs';

const sql = loadSql();
const catalogs = buildCatalogs(sql);
resetEdgeSeq();
const edges = [];

function push(edge) {
  edges.push(edge);
}

// --- worlds_npcs: npc SPAWNED_IN map ---
const { rows: worldNpcs } = parseTableInserts(sql, 'worlds_npcs');
for (const row of worldNpcs) {
  const npc = row.Npc;
  const map = row.Map;
  if (!npc || !map) continue;
  push(makeEdge({
    src: nodeId('npc', npc),
    rel: 'SPAWNED_IN',
    dst: nodeId('map', map),
    ref: `worlds_npcs Id=${row.Id} Npc=${npc} Map=${map}`,
    method: 'column-ref',
    confidence: 1.0,
    props: { cell: row.Cell, direction: row.Direction },
    status: edgeStatusForDst('map', map, catalogs) || edgeStatusForDst('npc', npc, catalogs),
  }));
}

// --- dungeons ---
const { rows: dungeons } = parseTableInserts(sql, 'dungeons');
for (const d of dungeons) {
  const did = d.Id;
  if (d.Map) {
    push(makeEdge({
      src: nodeId('dungeon', did),
      rel: 'LOCATED_AT',
      dst: nodeId('map', d.Map),
      ref: `dungeons.Id=${did} Map=${d.Map}`,
      method: 'column-ref',
      confidence: 1.0,
      status: edgeStatusForDst('map', d.Map, catalogs),
    }));
  }
  for (const mid of splitCsv(d.MonstersCSV)) {
    push(makeEdge({
      src: nodeId('dungeon', did),
      rel: 'CONTAINS_MONSTER',
      dst: nodeId('monster', mid),
      ref: `dungeons.Id=${did} MonstersCSV`,
      method: 'csv-split',
      confidence: 0.9,
      status: edgeStatusForDst('monster', mid, catalogs),
    }));
  }
  const exitParts = splitCsv(d.Parameters);
  if (exitParts[0]) {
    push(makeEdge({
      src: nodeId('dungeon', did),
      rel: 'EXITS_TO',
      dst: nodeId('map', exitParts[0]),
      ref: `dungeons.Id=${did} Parameters='${d.Parameters}'`,
      method: 'csv-split[0]',
      confidence: 0.7,
      hypothesis: true,
      props: exitParts[1] ? { cell: exitParts[1], direction: exitParts[2] } : undefined,
      status: edgeStatusForDst('map', exitParts[0], catalogs),
    }));
  }
}

// --- worlds_monsters: subarea SPAWNS_MONSTER ---
const { rows: worldMonsters } = parseTableInserts(sql, 'worlds_monsters');
for (const wm of worldMonsters) {
  const sa = wm.SubArea;
  for (const mid of splitCsv(wm.MonstersCSV)) {
    push(makeEdge({
      src: nodeId('subarea', sa),
      rel: 'SPAWNS_MONSTER',
      dst: nodeId('monster', mid),
      ref: `worlds_monsters SubArea=${sa} MonstersCSV`,
      method: 'csv-split',
      confidence: 0.9,
      status: edgeStatusForDst('monster', mid, catalogs),
    }));
  }
}

// --- worlds_maps: IN_SUBAREA + NEIGHBOR_OF ---
const { rows: maps } = parseTableInserts(sql, 'worlds_maps');
for (const m of maps) {
  const mid = m.Id;
  if (m.SubAreaId != null && m.SubAreaId !== '') {
    push(makeEdge({
      src: nodeId('map', mid),
      rel: 'IN_SUBAREA',
      dst: nodeId('subarea', m.SubAreaId),
      ref: `worlds_maps.Id=${mid} SubAreaId=${m.SubAreaId}`,
      method: 'column-ref',
      confidence: 1.0,
    }));
  }
  for (const [col, rel] of [
    ['TopNeighbourId', 'NEIGHBOR_OF'],
    ['BottomNeighbourId', 'NEIGHBOR_OF'],
    ['LeftNeighbourId', 'NEIGHBOR_OF'],
    ['RightNeighbourId', 'NEIGHBOR_OF'],
  ]) {
    const nbr = Number(m[col]);
    if (!nbr || Number.isNaN(nbr) || nbr === 0) continue;
    push(makeEdge({
      src: nodeId('map', mid),
      rel,
      dst: nodeId('map', nbr),
      ref: `worlds_maps.Id=${mid} ${col}=${nbr}`,
      method: 'column-ref',
      confidence: 1.0,
      props: { direction: col.replace('NeighbourId', '').toLowerCase() },
      status: edgeStatusForDst('map', nbr, catalogs),
    }));
  }
}

// --- npcs_items: SELLS ---
const { rows: npcItems } = parseTableInserts(sql, 'npcs_items');
for (const ni of npcItems) {
  if (!ni.NpcId || !ni.Item) continue;
  push(makeEdge({
    src: nodeId('npc', ni.NpcId),
    rel: 'SELLS',
    dst: nodeId('item', ni.Item),
    ref: `npcs_items Id=${ni.Id} NpcId=${ni.NpcId} Item=${ni.Item}`,
    method: 'sql-insert',
    confidence: 1.0,
    props: ni.Price != null ? { price: Number(ni.Price) } : undefined,
    status: edgeStatusForDst('item', ni.Item, catalogs) || edgeStatusForDst('npc', ni.NpcId, catalogs),
  }));
}

// --- npcs_actions ---
const { rows: npcActions } = parseTableInserts(sql, 'npcs_actions');
for (const a of npcActions) {
  if (!a.NpcId) continue;
  push(makeEdge({
    src: nodeId('npc', a.NpcId),
    rel: 'OFFERS_ACTION',
    dst: nodeId('npc', a.NpcId),
    ref: `npcs_actions Id=${a.Id} Type=${a.Type}`,
    method: 'typed-action',
    confidence: 1.0,
    props: { type: a.Type, parameters: a.Parameters },
  }));
  if (String(a.Type).toLowerCase() === 'shop' && a.Parameters) {
    push(makeEdge({
      src: nodeId('npc', a.NpcId),
      rel: 'SELLS',
      dst: nodeId('item', a.Parameters),
      ref: `npcs_actions Id=${a.Id} Type=Shop Parameters=${a.Parameters}`,
      method: 'action-param',
      confidence: 0.9,
      status: edgeStatusForDst('item', a.Parameters, catalogs),
    }));
  }
}

// --- monsters_drops ---
const { rows: monsterDrops } = parseTableInserts(sql, 'monsters_drops');
for (const md of monsterDrops) {
  push(makeEdge({
    src: nodeId('monster', md.MonsterId),
    rel: 'DROPS_ITEM',
    dst: nodeId('item', md.ItemId),
    ref: `monsters_drops MonsterId=${md.MonsterId} ItemId=${md.ItemId}`,
    method: 'column-ref',
    confidence: 1.0,
    props: {
      dropRateG1: md.DropRateForGrade1,
      prospectingLock: md.ProspectingLock,
    },
    status: edgeStatusForDst('item', md.ItemId, catalogs) || edgeStatusForDst('monster', md.MonsterId, catalogs),
  }));
}

// --- monsters_spells ---
const { rows: monsterSpells } = parseTableInserts(sql, 'monsters_spells');
for (const ms of monsterSpells) {
  for (const sid of splitCsv(ms.SpellsCSV)) {
    push(makeEdge({
      src: nodeId('monster', ms.Monster),
      rel: 'USES_SPELL',
      dst: nodeId('spell', sid),
      ref: `monsters_spells Monster=${ms.Monster} SpellsCSV`,
      method: 'csv-split',
      confidence: 0.9,
      status: edgeStatusForDst('spell', sid, catalogs) || edgeStatusForDst('monster', ms.Monster, catalogs),
    }));
  }
}

// --- quests HAS_STEP ---
const { rows: quests } = parseTableInserts(sql, 'quests');
const { rows: questSteps } = parseTableInserts(sql, 'quests_steps');
const stepByQuest = new Map();
for (const s of questSteps) {
  const q = Number(s.Quest);
  if (!stepByQuest.has(q)) stepByQuest.set(q, []);
  stepByQuest.get(q).push(s);
}

for (const q of quests) {
  const qid = q.Id;
  const csvSteps = splitCsv(q.StepIdsCSV);
  const backSteps = (stepByQuest.get(Number(qid)) || []).map((s) => String(s.Id));
  const stepSet = new Set([...csvSteps, ...backSteps]);
  for (const sid of stepSet) {
    push(makeEdge({
      src: nodeId('quest', qid),
      rel: 'HAS_STEP',
      dst: nodeId('queststep', sid),
      ref: `quests.Id=${qid} StepIdsCSV + quests_steps.Quest`,
      method: 'csv+backref',
      confidence: 1.0,
      status: edgeStatusForDst('queststep', sid, catalogs),
    }));
  }
}

// --- queststep HAS_OBJECTIVE ---
const { rows: questObjectives } = parseTableInserts(sql, 'quests_objectives');
const objectivesByStep = new Map();
for (const o of questObjectives) {
  const st = Number(o.Step);
  if (!objectivesByStep.has(st)) objectivesByStep.set(st, []);
  objectivesByStep.get(st).push(o);
}

for (const s of questSteps) {
  const sid = s.Id;
  const csvObjs = splitCsv(s.ObjectiveIdsCSV);
  const backObjs = (objectivesByStep.get(Number(sid)) || []).map((o) => String(o.Id));
  const objSet = new Set([...csvObjs, ...backObjs]);
  for (const oid of objSet) {
    push(makeEdge({
      src: nodeId('queststep', sid),
      rel: 'HAS_OBJECTIVE',
      dst: nodeId('objective', oid),
      ref: `quests_steps.Id=${sid} ObjectiveIdsCSV + quests_objectives.Step`,
      method: 'csv+backref',
      confidence: 1.0,
      status: edgeStatusForDst('objective', oid, catalogs),
    }));
  }
}

// --- quests_objectives typed params ---
const typeRoles = objectiveTypeRoles();
for (const o of questObjectives) {
  const typeNum = Number(o.Type);
  const roles = typeRoles[typeNum];
  if (!roles) continue;
  const params = splitCsv(o.ParametersCSV);
  const oid = o.Id;
  const isHyp = Boolean(roles.hypothesis);

  for (const [idxStr, role] of Object.entries(roles)) {
    if (idxStr === 'hypothesis') continue;
    const idx = Number(idxStr) - 1;
    const val = params[idx];
    if (val == null || val === '') continue;
    const rel = OBJECTIVE_TYPE_RELS[role];
    if (!rel) continue;
    if (role === 'qty' || role === 'text_id' || role === 'challenge') continue;

    let dstType = role;
    if (role === 'interactive') dstType = 'interactive';

    push(makeEdge({
      src: nodeId('objective', oid),
      rel,
      dst: nodeId(dstType, val),
      ref: `quests_objectives Id=${oid} Type=${typeNum} ParametersCSV='${o.ParametersCSV}'`,
      method: 'csv+type-resolve',
      confidence: isHyp ? 0.5 : 0.7,
      hypothesis: isHyp,
      status: edgeStatusForDst(dstType, val, catalogs),
    }));
  }
}

// --- queststep rewards ---
for (const s of questSteps) {
  const sid = s.Id;
  for (const pair of splitCsvPairs(s.ItemsRewardCSV)) {
    if (Number.isNaN(pair.id)) continue;
    push(makeEdge({
      src: nodeId('queststep', sid),
      rel: 'REWARDS_ITEM',
      dst: nodeId('item', pair.id),
      ref: `quests_steps.Id=${sid} ItemsRewardCSV`,
      method: 'csv-pairs',
      confidence: 0.6,
      props: { qty: pair.qty },
      status: edgeStatusForDst('item', pair.id, catalogs),
    }));
  }
  for (const sp of splitCsv(s.SpellsRewardCSV)) {
    push(makeEdge({
      src: nodeId('queststep', sid),
      rel: 'REWARDS_SPELL',
      dst: nodeId('spell', sp),
      ref: `quests_steps.Id=${sid} SpellsRewardCSV`,
      method: 'csv-split',
      confidence: 0.6,
      status: edgeStatusForDst('spell', sp, catalogs),
    }));
  }
}

// --- worlds_interactives: interactive SPAWNED_IN map ---
const { rows: worldInteractives } = parseTableInserts(sql, 'worlds_interactives');
for (const wi of worldInteractives) {
  if (!wi.Map || !wi.Type) continue;
  push(makeEdge({
    src: nodeId('interactive', wi.Type),
    rel: 'SPAWNED_IN',
    dst: nodeId('map', wi.Map),
    ref: `worlds_interactives Id=${wi.Id} Type=${wi.Type} Map=${wi.Map}`,
    method: 'column-ref',
    confidence: 0.9,
    props: { element: wi.Element, skillsCSV: wi.SkillsCSV },
    status: edgeStatusForDst('map', wi.Map, catalogs),
  }));
}

// --- teleports (TELEPORT_FROM only — dest is name not map id) ---
for (const table of ['teleports_maps', 'teleports_zones_maps', 'teleports_donjons_maps']) {
  const { rows } = parseTableInserts(sql, table);
  for (const t of rows) {
    if (!t.TeleportMapId) continue;
    push(makeEdge({
      src: nodeId('map', t.TeleportMapId),
      rel: 'TELEPORT_FROM',
      dst: nodeId('map', t.TeleportMapId),
      ref: `${table} Id=${t.Id} TeleportMapId=${t.TeleportMapId}`,
      method: 'teleport-source-map',
      confidence: 0.5,
      hypothesis: true,
      props: {
        destinationName: t.DestinationName,
        kamasCost: t.KamasCost,
        requiredItemId: t.RequiredItemId,
        gap: 'destination is name string not map id',
      },
      status: edgeStatusForDst('map', t.TeleportMapId, catalogs),
    }));
    if (t.RequiredItemId && Number(t.RequiredItemId) > 0) {
      push(makeEdge({
        src: nodeId('map', t.TeleportMapId),
        rel: 'REQUIRES_ITEM',
        dst: nodeId('item', t.RequiredItemId),
        ref: `${table} Id=${t.Id} RequiredItemId=${t.RequiredItemId}`,
        method: 'column-ref',
        confidence: 0.6,
        status: edgeStatusForDst('item', t.RequiredItemId, catalogs),
      }));
    }
  }
}

// --- derived STARTS_QUEST: first INVOLVES_NPC on lowest step per quest ---
const involvesByQuest = new Map();
for (const e of edges) {
  if (e.rel !== 'INVOLVES_NPC') continue;
  const stepEdges = edges.filter((x) => x.rel === 'HAS_OBJECTIVE' && x.dst === e.src);
  if (!stepEdges.length) continue;
  const stepId = parseNodeId(stepEdges[0].src).id;
  const step = questSteps.find((s) => String(s.Id) === stepId);
  if (!step) continue;
  const qid = Number(step.Quest);
  const npcId = parseNodeId(e.dst).id;
  if (!involvesByQuest.has(qid)) involvesByQuest.set(qid, { stepNum: Number(stepId), npcId });
  else {
    const cur = involvesByQuest.get(qid);
    if (Number(stepId) < cur.stepNum) involvesByQuest.set(qid, { stepNum: Number(stepId), npcId });
  }
}

for (const [qid, { npcId }] of involvesByQuest) {
  push(makeEdge({
    src: nodeId('npc', npcId),
    rel: 'STARTS_QUEST',
    dst: nodeId('quest', qid),
    ref: `derived: first INVOLVES_NPC on lowest queststep for quest ${qid}`,
    method: 'derived-from-objectives',
    confidence: 0.5,
    hypothesis: true,
  }));
}

// --- derived quest -> map via objective DISCOVER_MAP or npc spawn chain ---
for (const e of edges) {
  if (e.rel !== 'DISCOVER_MAP') continue;
  const objId = parseNodeId(e.src).id;
  const stepEdge = edges.find((x) => x.rel === 'HAS_OBJECTIVE' && x.dst === e.src);
  if (!stepEdge) continue;
  const questEdge = edges.find((x) => x.rel === 'HAS_STEP' && x.dst === stepEdge.src);
  if (!questEdge) continue;
  push(makeEdge({
    src: questEdge.src,
    rel: 'PARTICIPATES_IN_MAP',
    dst: e.dst,
    ref: `derived: quest via objective DISCOVER_MAP objective=${objId}`,
    method: 'path-derive',
    confidence: 0.5,
    hypothesis: true,
  }));
}

for (const e of edges) {
  if (e.rel !== 'INVOLVES_NPC') continue;
  const objId = parseNodeId(e.src).id;
  const npcId = parseNodeId(e.dst).id;
  const spawns = edges.filter((x) => x.rel === 'SPAWNED_IN' && x.src === e.dst);
  const stepEdge = edges.find((x) => x.rel === 'HAS_OBJECTIVE' && x.dst === e.src);
  if (!stepEdge || !spawns.length) continue;
  const questEdge = edges.find((x) => x.rel === 'HAS_STEP' && x.dst === stepEdge.src);
  if (!questEdge) continue;
  for (const sp of spawns) {
    push(makeEdge({
      src: questEdge.src,
      rel: 'PARTICIPATES_IN_MAP',
      dst: sp.dst,
      ref: `derived: quest via INVOLVES_NPC npc=${npcId} SPAWNED_IN`,
      method: 'path-derive',
      confidence: 0.6,
      hypothesis: true,
      props: { via_npc: npcId },
    }));
  }
}

export const recoveredEdges = edges;

const outDir = dirname(fileURLToPath(import.meta.url));
const jsonl = edges.map((e) => JSON.stringify(e)).join('\n') + '\n';
writeFileSync(join(outDir, 'recovered_edges.jsonl'), jsonl, 'utf8');

if (process.argv[1]?.includes('recover-edges')) {
  const types = {};
  for (const e of edges) types[e.rel] = (types[e.rel] || 0) + 1;
  console.log(JSON.stringify({ edges_discovered: edges.length, relationship_types: Object.keys(types).length, types }, null, 2));
}
