-- Patch: Incarnam profession NPC dialog replies (P0)
-- Target NPCs: 863 Paysan, 881 Bûcheron, 882 Chasseur, 883 Pêcheur
-- Job IDs validated against jobs table: 28, 2, 41, 36
--
-- Dialog flow (mirrors Contremaître Ikul pattern):
--   Message 3695: reply 3182 (navigate), 3183 (close)
--   Message 3696: reply 3189 (LearnJob type 8)
--
-- Reply IDs 3182/3183/3189 are already used successfully by NPC 849 on this server.

USE sunshine;

-- Paysan d Incarnam (job 28)
UPDATE npcs SET DialogRepliesIdCSV = ';3182,20471;3183,22885;3696,3189,26478' WHERE Id = 863;
DELETE FROM npcs_replies WHERE Npc = 863 AND Map = 21760002;
INSERT INTO npcs_replies (Npc, Map, Type, MessageId, ReplieId, ParametersCSV, DialogParamsCSV, Note) VALUES
(863, 21760002, 1, 3695, 3182, NULL, NULL, 'P0 Incarnam Paysan nav'),
(863, 21760002, 0, 3695, 3183, NULL, NULL, 'P0 Incarnam Paysan close'),
(863, 21760002, 8, 3696, 3189, '28', NULL, 'P0 Incarnam Paysan learn');

-- Bûcheron d Incarnam (job 2)
UPDATE npcs SET DialogRepliesIdCSV = ';3182,20471;3183,22885;3696,3189,26478' WHERE Id = 881;
DELETE FROM npcs_replies WHERE Npc = 881 AND Map = 21759493;
INSERT INTO npcs_replies (Npc, Map, Type, MessageId, ReplieId, ParametersCSV, DialogParamsCSV, Note) VALUES
(881, 21759493, 1, 3695, 3182, NULL, NULL, 'P0 Incarnam Bûcheron nav'),
(881, 21759493, 0, 3695, 3183, NULL, NULL, 'P0 Incarnam Bûcheron close'),
(881, 21759493, 8, 3696, 3189, '2', NULL, 'P0 Incarnam Bûcheron learn');

-- Chasseur d Incarnam (job 41)
UPDATE npcs SET DialogRepliesIdCSV = ';3182,20471;3183,22885;3696,3189,26478' WHERE Id = 882;
DELETE FROM npcs_replies WHERE Npc = 882 AND Map = 21758977;
INSERT INTO npcs_replies (Npc, Map, Type, MessageId, ReplieId, ParametersCSV, DialogParamsCSV, Note) VALUES
(882, 21758977, 1, 3695, 3182, NULL, NULL, 'P0 Incarnam Chasseur nav'),
(882, 21758977, 0, 3695, 3183, NULL, NULL, 'P0 Incarnam Chasseur close'),
(882, 21758977, 8, 3696, 3189, '41', NULL, 'P0 Incarnam Chasseur learn');

-- Pêcheur d Incarnam (job 36)
UPDATE npcs SET DialogRepliesIdCSV = ';3182,20471;3183,22885;3696,3189,26478' WHERE Id = 883;
DELETE FROM npcs_replies WHERE Npc = 883 AND Map = 21760005;
INSERT INTO npcs_replies (Npc, Map, Type, MessageId, ReplieId, ParametersCSV, DialogParamsCSV, Note) VALUES
(883, 21760005, 1, 3695, 3182, NULL, NULL, 'P0 Incarnam Pêcheur nav'),
(883, 21760005, 0, 3695, 3183, NULL, NULL, 'P0 Incarnam Pêcheur close'),
(883, 21760005, 8, 3696, 3189, '36', NULL, 'P0 Incarnam Pêcheur learn');
