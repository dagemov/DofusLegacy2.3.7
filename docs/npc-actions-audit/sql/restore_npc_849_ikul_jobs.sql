-- Restore: Contremaître Ikul (849) pre-P1 state (original dump)
USE sunshine;

DELETE FROM npcs_replies WHERE Npc = 849 AND Map = 21759491;

INSERT INTO npcs_replies (Npc, Map, Type, MessageId, ReplieId, ParametersCSV, DialogParamsCSV, Note) VALUES
(849, 21759491, 1, 3596, 3182, NULL, NULL, NULL),
(849, 21759491, 0, 3596, 3183, NULL, NULL, NULL),
(849, 21759491, 1, 3597, 3184, '', NULL, NULL),
(849, 21759491, 1, 3597, 3185, NULL, NULL, NULL),
(849, 21759491, 1, 3597, 3186, '', NULL, ''),
(849, 21759491, 1, 3597, 3187, '', NULL, ''),
(849, 21759491, 0, 3597, 3188, '', NULL, ''),
(849, 21759491, 8, 3598, 3189, '2', NULL, NULL);
