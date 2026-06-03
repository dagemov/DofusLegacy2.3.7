-- Controlled restore: remove Dofus Tester inventory rows from all
-- characters linked to sebcos1.

DELETE ci
FROM characters_items ci
INNER JOIN worlds_characters wc ON wc.Owner = ci.OwnerId
INNER JOIN accounts a ON a.Id = wc.Account
WHERE a.Username = 'sebcos1'
  AND ci.Item = 12617;
