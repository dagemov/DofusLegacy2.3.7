-- Controlled restore: remove Dofus Tester item template.
-- Preferred order:
--   1. run restore_grant_dofus_tester_to_sebcos1.sql
--   2. run this file

DELETE FROM items
WHERE Id = 12617
LIMIT 1;
