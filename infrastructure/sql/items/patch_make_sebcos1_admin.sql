-- Controlled patch: elevate sebcos1 to Administrator.
-- RoleEnum:
--   1 = Player
--   2 = Moderator
--   3 = GameMaster_Padawan
--   4 = GameMaster
--   5 = Administrator

UPDATE accounts
SET Role = 5
WHERE Username = 'sebcos1'
  AND Role <> 5;
