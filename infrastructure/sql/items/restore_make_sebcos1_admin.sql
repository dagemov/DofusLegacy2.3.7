-- Controlled restore: return sebcos1 to the audited original role.
-- Audited baseline on 2026-06-02:
--   AccountId = 265
--   Role = 1 (Player)

UPDATE accounts
SET Role = 1
WHERE Username = 'sebcos1';
