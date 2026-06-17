-- Restore: revert Incarnam profession NPC dialog replies (P0 patch rollback)
USE sunshine;

DELETE FROM npcs_replies
WHERE Npc IN (863, 881, 882, 883)
  AND Map IN (21760002, 21759493, 21758977, 21760005);

UPDATE npcs SET DialogRepliesIdCSV = '' WHERE Id IN (863, 881, 882, 883);
