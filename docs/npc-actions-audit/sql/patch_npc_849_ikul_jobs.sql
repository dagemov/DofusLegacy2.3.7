-- Patch: Contremaître Ikul (849) — per-profession LearnJob on message 3597
-- Root cause: only reply 3189 on message 3598 had type 8 jobId=2; all menu paths converged to lumberjack.
-- Fix: assign LearnJob (type 8) directly on profession selection replies 3184-3187.
USE sunshine;

DELETE FROM npcs_replies WHERE Npc = 849 AND Map = 21759491;

INSERT INTO npcs_replies (Npc, Map, Type, MessageId, ReplieId, ParametersCSV, DialogParamsCSV, Note) VALUES
(849, 21759491, 1, 3596, 3182, NULL, NULL, 'P1 Ikul nav to menu'),
(849, 21759491, 0, 3596, 3183, NULL, NULL, 'P1 Ikul close'),
(849, 21759491, 8, 3597, 3184, '28', NULL, 'P1 Ikul learn Paysan'),
(849, 21759491, 8, 3597, 3185, '2', NULL, 'P1 Ikul learn Bûcheron'),
(849, 21759491, 8, 3597, 3186, '41', NULL, 'P1 Ikul learn Chasseur'),
(849, 21759491, 8, 3597, 3187, '36', NULL, 'P1 Ikul learn Pêcheur'),
(849, 21759491, 0, 3597, 3188, NULL, NULL, 'P1 Ikul close'),
(849, 21759491, 1, 3598, 3189, NULL, NULL, 'P1 Ikul legacy nav'),
(849, 21759491, 1, 3599, 3190, NULL, NULL, 'P1 Ikul nav'),
(849, 21759491, 1, 3600, 3191, NULL, NULL, 'P1 Ikul nav'),
(849, 21759491, 1, 3601, 3192, NULL, NULL, 'P1 Ikul nav');
