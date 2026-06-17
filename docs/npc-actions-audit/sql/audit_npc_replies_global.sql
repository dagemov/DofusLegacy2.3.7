-- Global NPC replies audit (P1)
USE sunshine;

-- 1) NPCs with messages but zero replies (CSV + DB)
SELECT n.Id AS npcId, n.Name, wn.Map AS mapId, m.SubAreaId,
       LENGTH(COALESCE(n.DialogMessagesIdCSV,'')) AS msg_csv_len,
       LENGTH(COALESCE(n.DialogRepliesIdCSV,'')) AS reply_csv_len,
       (SELECT COUNT(*) FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map) AS db_reply_rows
FROM npcs n
JOIN worlds_npcs wn ON wn.Npc = n.Id
LEFT JOIN worlds_maps m ON m.Id = wn.Map
WHERE n.DialogMessagesIdCSV IS NOT NULL AND n.DialogMessagesIdCSV != ''
  AND (n.DialogRepliesIdCSV IS NULL OR n.DialogRepliesIdCSV = '')
  AND NOT EXISTS (SELECT 1 FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map)
ORDER BY m.SubAreaId, n.Id;

-- 2) Reply action types without Sunshine handler (registered: 0-11, 1=nav, negative=quest branch)
SELECT r.Type AS actionType, COUNT(*) AS cnt,
       GROUP_CONCAT(DISTINCT r.Npc ORDER BY r.Npc SEPARATOR ',') AS sample_npcs
FROM npcs_replies r
WHERE r.Type NOT IN (0,1,2,3,4,5,6,7,8,9,10,11,-1,-2,-3)
GROUP BY r.Type
ORDER BY r.Type;

-- 3) LearnJob replies — suspicious duplicate jobId across many NPCs
SELECT r.ParametersCSV AS jobId, COUNT(*) AS reply_count,
       GROUP_CONCAT(CONCAT(r.Npc, ':', r.ReplieId) ORDER BY r.Npc SEPARATOR '; ') AS npc_reply_pairs
FROM npcs_replies r
WHERE r.Type = 8
GROUP BY r.ParametersCSV
HAVING reply_count > 1;

-- 4) LearnJob grouped by NPC
SELECT r.Npc, n.Name, r.Map, r.MessageId, r.ReplieId, r.ParametersCSV AS jobId, j.Name AS jobName
FROM npcs_replies r
JOIN npcs n ON n.Id = r.Npc
LEFT JOIN jobs j ON j.Id = CAST(r.ParametersCSV AS UNSIGNED)
WHERE r.Type = 8
ORDER BY r.Npc, r.MessageId, r.ReplieId;

-- 5) All action types in npcs_replies
SELECT r.Type AS actionType, COUNT(*) AS cnt
FROM npcs_replies r
GROUP BY r.Type
ORDER BY r.Type;

-- 6) Negative action types detail
SELECT r.Npc, n.Name, r.Map, r.Type, r.MessageId, r.ReplieId, r.ParametersCSV
FROM npcs_replies r
JOIN npcs n ON n.Id = r.Npc
WHERE r.Type < 0
ORDER BY r.Type, r.Npc;

-- 7) Replies referencing messages not in NPC DialogMessagesIdCSV (orphan messageId)
SELECT r.Npc, n.Name, r.Map, r.MessageId, r.ReplieId, r.Type, r.ParametersCSV
FROM npcs_replies r
JOIN npcs n ON n.Id = r.Npc
WHERE n.DialogMessagesIdCSV IS NOT NULL
  AND n.DialogMessagesIdCSV NOT LIKE CONCAT('%', r.MessageId, ',%')
  AND n.DialogMessagesIdCSV NOT LIKE CONCAT('%;', r.MessageId, ',%')
ORDER BY r.Npc, r.MessageId;

-- 8) Incarnam / tavern / dungeon NPCs by subarea or known maps
SELECT wn.Npc, n.Name, wn.Map, m.SubAreaId,
       LENGTH(COALESCE(n.DialogRepliesIdCSV,'')) AS reply_csv_len,
       (SELECT COUNT(*) FROM npcs_replies r WHERE r.Npc = n.Id AND r.Map = wn.Map) AS db_replies
FROM worlds_npcs wn
JOIN npcs n ON n.Id = wn.Npc
JOIN worlds_maps m ON m.Id = wn.Map
WHERE m.SubAreaId BETWEEN 442 AND 450
   OR wn.Map IN (23592962, 21759491, 54534173, 54535193, 21761540)
ORDER BY wn.Map, n.Name;

-- 9) NPCs with CSV replies but zero typed npcs_replies (navigation-only risk)
SELECT n.Id, n.Name, wn.Map,
       LENGTH(n.DialogRepliesIdCSV) AS reply_csv_len,
       (SELECT COUNT(*) FROM npcs_replies r WHERE r.Npc=n.Id AND r.Map=wn.Map AND r.Type NOT IN (1)) AS typed_replies
FROM npcs n
JOIN worlds_npcs wn ON wn.Npc = n.Id
WHERE n.DialogRepliesIdCSV IS NOT NULL AND n.DialogRepliesIdCSV != ''
HAVING typed_replies = 0
ORDER BY n.Id
LIMIT 50;

-- 10) Summary counts
SELECT
  (SELECT COUNT(*) FROM npcs) AS total_npcs,
  (SELECT COUNT(*) FROM npcs_replies) AS total_replies,
  (SELECT COUNT(*) FROM (
      SELECT n.Id, wn.Map FROM npcs n JOIN worlds_npcs wn ON wn.Npc=n.Id
      WHERE n.DialogMessagesIdCSV != '' AND (n.DialogRepliesIdCSV IS NULL OR n.DialogRepliesIdCSV='')
        AND NOT EXISTS (SELECT 1 FROM npcs_replies r WHERE r.Npc=n.Id AND r.Map=wn.Map)
  ) t) AS broken_dialog_npcs,
  (SELECT COUNT(*) FROM npcs_replies WHERE Type=8) AS learn_job_replies,
  (SELECT COUNT(*) FROM npcs_replies WHERE Type < 0) AS negative_type_rows;
