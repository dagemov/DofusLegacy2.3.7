-- Audit Incarnam profession NPC dialog/reply state (pre/post patch).
-- Run against database: sunshine

-- 1) Confirm job IDs from jobs table (do not assume)
SELECT Id, Name, Specialization
FROM jobs
WHERE Id IN (2, 28, 36, 41)
ORDER BY Id;

-- 2) Incarnam profession NPC spawns
SELECT wn.Npc, n.Name, wn.Map, wn.Cell, m.SubAreaId,
       CASE WHEN n.DialogRepliesIdCSV IS NULL OR n.DialogRepliesIdCSV = '' THEN 0 ELSE 1 END AS has_reply_csv,
       (SELECT COUNT(*) FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map) AS reply_rows
FROM worlds_npcs wn
JOIN npcs n ON n.Id = wn.Npc
JOIN worlds_maps m ON m.Id = wn.Map
WHERE wn.Npc IN (863, 881, 882, 883)
ORDER BY wn.Npc;

-- 3) Existing replies for target NPCs
SELECT r.Npc, n.Name, r.Map, r.Type, r.MessageId, r.ReplieId, r.ParametersCSV
FROM npcs_replies r
JOIN npcs n ON n.Id = r.Npc
WHERE r.Npc IN (863, 881, 882, 883)
ORDER BY r.Npc, r.Map, r.MessageId, r.ReplieId;

-- 4) LearnJob replies (type 8) server-wide
SELECT r.Npc, n.Name, r.Map, r.MessageId, r.ReplieId, r.ParametersCSV
FROM npcs_replies r
JOIN npcs n ON n.Id = r.Npc
WHERE r.Type = 8
ORDER BY r.Npc;

-- 5) EndDialog replies (type 0) for Incarnam targets
SELECT r.Npc, n.Name, r.Map, r.MessageId, r.ReplieId
FROM npcs_replies r
JOIN npcs n ON n.Id = r.Npc
WHERE r.Npc IN (863, 881, 882, 883) AND r.Type = 0;

-- 6) NPCs with messages but zero replies (broken dialog risk)
SELECT COUNT(*) AS broken_dialog_npcs
FROM (
    SELECT n.Id, wn.Map
    FROM npcs n
    JOIN worlds_npcs wn ON wn.Npc = n.Id
    WHERE n.DialogMessagesIdCSV IS NOT NULL AND n.DialogMessagesIdCSV != ''
      AND (n.DialogRepliesIdCSV IS NULL OR n.DialogRepliesIdCSV = '')
      AND NOT EXISTS (SELECT 1 FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map)
) t;
